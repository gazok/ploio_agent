//    Copyright 2023 Yeong-won Seo
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.Json;
using Frouros.Proxy.Bridges;
using Frouros.Proxy.Models.Serialization;
using Frouros.Proxy.Models.Web;
using Frouros.Proxy.Repositories.Abstract;
using Frouros.Shared;
using ModuleHandle = Frouros.Proxy.Models.ModuleHandle;

namespace Frouros.Proxy.Repositories;

public class ModuleEventArgs(ushort code, string message) : EventArgs
{
    public ushort Code    { get; } = code;
    public string Message { get; } = message;
}

public delegate void ModuleEventHandler(ModuleHandle sender, ModuleEventArgs args);

public class ModuleRepository : IModuleRepository, IDisposable
{
    private readonly ConcurrentDictionary<string, ModuleHandle> _libs;
    private readonly FileSystemWatcher                          _watcher;

    public IEnumerable<ModuleHandle> Handles => _libs.Values;

    public event ModuleEventHandler? MessageReceived;
    
    public ModuleRepository(ILogger<ModuleRepository> logger)
    {
        _libs = new ConcurrentDictionary<string, ModuleHandle>(new DirectoryInfo(Specials.ModulePath)
                                                              .EnumerateFiles("*.so")
                                                              .ToDictionary(file => file.Name, Open));

        _watcher = new FileSystemWatcher(Specials.ModulePath, "*.so");

        FileSystemEventHandler cb = (_, args) =>
        {
            switch (args.ChangeType)
            {
                case WatcherChangeTypes.Renamed:
                {
                    if (args is not RenamedEventArgs rArgs)
                        break;
                    if (rArgs is not { OldName: not null, Name: not null })
                    {
                        logger.LogWarning("RenamedEventArgs returns null for a name of file");
                        break;
                    }

                    if (_libs.Remove(rArgs.OldName, out var module) && !_libs.TryAdd(rArgs.Name, module))
                    {
                        logger.LogWarning(
                            "Cannot move module entry\n" +
                            "some module will not be working");
                        Close(module);
                    }
                    
                    break;
                }
                case WatcherChangeTypes.Deleted:
                {
                    if (args.Name is null)
                        goto default;

                    if (_libs.Remove(args.Name, out var module))
                        NativeLibrary.Free(module.Handle);

                    break;
                }
                case WatcherChangeTypes.Created:
                {
                    if (args.Name is null)
                        goto default;

                    var module = Open(new FileInfo(args.FullPath));
                    if (!_libs.TryAdd(args.Name, module))
                    {
                        logger.LogWarning(
                            "FileSystemWatcher captured duplicated event\n" +
                            "some module will not be working");
                        Close(module);
                    }

                    break;
                }
                default:
                    if (args.Name is null)
                        logger.LogWarning("FileSystemEventArgs returns null for a name of file");
                    break;
            }
        };

        _watcher.Changed += cb;
        _watcher.Created += cb;
        _watcher.Deleted += cb;
        _watcher.Renamed += (sender, args) => cb(sender, args);
    }

    private ModuleHandle Open(FileInfo file)
    {
        var         path = Path.ChangeExtension(file.FullName, ".json");
        ModuleInfo? info;
        using (var fs = File.OpenRead(path))
            info = JsonSerializer.Deserialize(fs, SourceGenerationContext.Default.ModuleInfo);
        if (info is null)
            throw new InvalidOperationException("Couldn't deserialize module-info");
        var handle = new ModuleHandle(info, file, NativeLibrary.Load(file.FullName));
        if (!Init(handle))
            throw new TypeInitializationException(
                handle.Info.Name, 
                new MissingMethodException("Couldn't import function from module"));
        return handle;
    }

    private bool Init(ModuleHandle lib)
    {
        if (!NativeLibrary.TryGetExport(lib.Handle, "entrypoint", out var ep))
            return false;
        if (!NativeLibrary.TryGetExport(lib.Handle, "initialize", out var init))
            return false;

        lib.Entrypoint = Marshal.GetDelegateForFunctionPointer<ModuleEntrypoint>(ep);
        lib.Init       = Marshal.GetDelegateForFunctionPointer<ModuleInitializer>(init);
        
        lib.Init.Invoke((code, msg) => MessageReceived?.Invoke(lib, new ModuleEventArgs(code, msg)));

        return true;
    }

    private static void Close(ModuleHandle lib)
    {
        NativeLibrary.Free(lib.Handle);
    }

    public void Dispose()
    {
        _watcher.Dispose();
        foreach (var lib in _libs.Values)
            Close(lib);
        GC.SuppressFinalize(this);
    }
}