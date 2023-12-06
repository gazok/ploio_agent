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

using System.Runtime.InteropServices;
using System.Threading.Channels;
using Frouros.Proxy.Bridges.Abstract;
using Frouros.Proxy.Models;
using Frouros.Proxy.Repositories.Abstract;
using Frouros.Shared.Imports;

namespace Frouros.Proxy.Bridges;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void ModuleResultSetter(ushort code, [MarshalAs(UnmanagedType.LPUTF8Str)] string msg);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void ModuleInitializer(ModuleResultSetter res);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void ModuleEntrypoint(uint id, Timeval tv, PacketRegistry* pkt);

public class Membrane : IMembrane
{
    private readonly Channel<ModuleMessage> _queue = Channel.CreateUnbounded<ModuleMessage>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        }
    );

    private readonly IModuleRepository _repository;

    public Membrane(IModuleRepository repository)
    {
        _repository                 =  repository;
        _repository.MessageReceived += (sender, args) =>
        {
            // do NOT await for this operation; it causes bottleneck
            _queue.Writer.WriteAsync(new ModuleMessage(sender, args.Code, args.Message)).AsTask();
        };
    }

    public IEnumerable<ModuleMessage> Transmit(uint id, PacketRegistry pkt, Timeval tv)
    {
        unsafe
        {
            foreach (var lib in _repository.Handles)
                lib.Entrypoint!.Invoke(id, tv, &pkt);
        }

        return _queue.Reader.ReadAllAsync().ToBlockingEnumerable();
    }
}