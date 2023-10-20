//     Copyright 2023 Yeong-won Seo
// 
//     Licensed under the Apache License, Version 2.0 (the "License");
//     you may not use this file except in compliance with the License.
//     You may obtain a copy of the License at
// 
//         http://www.apache.org/licenses/LICENSE-2.0
// 
//     Unless required by applicable law or agreed to in writing, software
//     distributed under the License is distributed on an "AS IS" BASIS,
//     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     See the License for the specific language governing permissions and
//     limitations under the License.

using Frouros.Net.Abstraction;
using Frouros.Net.Models;
using Frouros.Primitives;
using Frouros.System.Abstraction;
using Frouros.Utils;
using Microsoft.Win32.SafeHandles;

namespace Frouros.Net.Impls;

public class PacketLogChannel : IPacketChannel, IDisposable
{
    private const int BufferSize = NativePacketLog.StructSize * 2048;

    private readonly ILogger<PacketLogChannel> _logger;
    private readonly IAssemblyInfo             _info;

    private readonly FileStream     _fd;
    private readonly BufferedStream _buffer;
    private readonly object         _bufferSync = new();


    public PacketLogChannel(ILogger<PacketLogChannel> logger, IAssemblyInfo info)
    {
        _logger = logger;
        _info   = info;

        var filepath = Path.GetTempFileName();

        _fd = new FileStream(
            filepath,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            BufferSize,
            FileOptions.RandomAccess | FileOptions.Asynchronous | FileOptions.DeleteOnClose);
        _fd.Seek(0, SeekOrigin.Begin);
        
        _buffer = new BufferedStream(_fd, BufferSize);
    }

    private void Flush()
    {
        lock (_bufferSync)
        {
            _buffer.Flush();
            _fd.Flush();
        }
    }

    public void Write(PacketLog log)
    {
        var buffer = new byte[NativePacketLog.StructSize];
        if (!log.AsNative().TryWriteTo(buffer, 0))
            _logger.LogCritical("Couldn't write packet log to stream");
        lock (_bufferSync)
        {
            _buffer.Write(buffer);
        }
    }

    public void Read(Stream stream)
    {
        Flush();

        lock (_bufferSync)
        {
            _logger.LogInformation("{}", _fd.Position);
            
            new PacketDumpHeader(
                    _info.Version,
                    checked((uint)(_buffer.Length / NativePacketLog.StructSize))) // TODO: Handle integer overflow
               .WriteTo(stream);

            var ofs = _fd.Position;
            _fd.Seek(0, SeekOrigin.Begin);
            _fd.CopyTo(stream, BufferSize);
            _fd.Seek(ofs, SeekOrigin.Begin);
        }
    }

    public void Dispose()
    {
        _fd.Dispose();
        _buffer.Dispose();
        GC.SuppressFinalize(this);
    }
}