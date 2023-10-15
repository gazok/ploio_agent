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

using Frouros.Primitives;
using Frouros.Utils;
using Microsoft.Win32.SafeHandles;

namespace Frouros.Net;

public class PacketLogBuffer : IDisposable
{
    // Buffer size must be multiple of BufferSize
    private const int FileSize = BufferSize * 4096;

    // Buffer size must be multiple of NativePacketLog.StructSize
    // Buffer size must be greater than 4096; See benchmarks
    private const int BufferSize = NativePacketLog.StructSize * 2048;

    private readonly SafeFileHandle _fd;
    private readonly ScopedBuffer   _buffer;
    private readonly object         _bufferSync = new();

    private long _fileSize;
    private long _fileOffset;
    private int  _offset;
    
    public PacketLogBuffer()
    {
        var filepath = Path.GetTempFileName();

        _fd = File.OpenHandle(
            filepath,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            FileOptions.RandomAccess | FileOptions.Asynchronous | FileOptions.DeleteOnClose);

        _buffer = new ScopedBuffer(BufferSize);

        RandomAccess.SetLength(_fd, FileSize);

        _fileSize   = FileSize;
        _fileOffset = 0;
        _offset     = 0;
    }

    private void Flush()
    {
        // double-checking-lock,early-return
        if (_offset <= BufferSize)
            return;

        lock (_bufferSync)
        {
            // double-checking-lock,early-return
            if (_offset <= BufferSize)
                return;

            // pre-reallocate file
            if (_fileOffset >= FileSize)
            {
                while (_fileOffset >= FileSize)
                    _fileSize += FileSize;
                RandomAccess.SetLength(_fd, _fileSize);
            }

            RandomAccess.Write(
                _fd,
                new ReadOnlySpan<byte>(_buffer.GetBuffer(), 0, _offset),
                _fileOffset);
            _fileOffset += _offset;
            _offset     =  0;
        }
    }

    private long ReserveOffset()
    {
        return Interlocked.Add(ref _offset, NativePacketLog.StructSize) - NativePacketLog.StructSize;
    }

    public void Write(PacketLog log)
    {
        var ofs = ReserveOffset();
        if (ofs > BufferSize)
        {
            Flush();
            // update offset after update
            ofs = ReserveOffset();
        }

        log.AsNative().TryWriteTo(_buffer.GetBuffer(), ofs);
        return;
    }

    public void Read(Stream stream)
    {
        Flush();
        
        lock (_bufferSync)
        {
            using var fs = new FileStream(_fd, FileAccess.Read, BufferSize, true);
            fs.CopyTo(stream, _fileOffset, BufferSize);
            _fileOffset = 0;
        }
    }

    public void Dispose()
    {
        _fd.Dispose();
        _buffer.Dispose();
        GC.SuppressFinalize(this);
    }
}