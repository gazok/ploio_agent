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

using System.Buffers;
using Frouros.Primitives;
using Microsoft.Win32.SafeHandles;

namespace Frouros;

public class PacketLogWriter : IDisposable
{
    // Buffer size must be multiple of NativePacketLog.StructSize
    // Buffer size must be greater than 4096; See benchmarks
    private const int BufferSize = NativePacketLog.StructSize * 128;

    private readonly SafeFileHandle _fd;
    private readonly ScopedBuffer         _buffer;
    private readonly object         _bufferSync = new();

    private long _fileOffset;
    private long _offset;

    public PacketLogWriter()
    {
        var filepath = Path.GetTempFileName();

        _fd = File.OpenHandle(
            filepath,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            FileOptions.RandomAccess | FileOptions.Asynchronous | FileOptions.DeleteOnClose);

        _buffer = new ScopedBuffer(BufferSize);

        RandomAccess.SetLength(_fd, BufferSize * NativePacketLog.StructSize);

        _fileOffset  = 0;
        _offset = 0;
    }

    private void Write(PacketLog log)
    {
        var ofs = NewOffset();
        if (ofs > BufferSize)
        {
            lock (_bufferSync)
            {
                // ensure flushing only once at a time
                if (_offset > BufferSize)
                {
                    RandomAccess.Write(_fd, _buffer.GetBuffer(), _fileOffset);
                    _fileOffset += BufferSize;
                    _offset     =  0;
                }
            }
            // update offset after update
            ofs = NewOffset();
        }

        log.AsNative().TryWriteTo(_buffer.GetBuffer(), ofs);
        return;

        long NewOffset()
        {
            return Interlocked.Add(ref _offset, NativePacketLog.StructSize) - NativePacketLog.StructSize;
        }
    }

    public void Dispose()
    {
        _fd.Dispose();
        _buffer.Dispose();
        GC.SuppressFinalize(this);
    }
}