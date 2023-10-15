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

using System.Buffers;

namespace Frouros.Primitives;

public sealed class ScopedBuffer : IDisposable
{
    private byte[] _buffer;

    public ScopedBuffer(int size)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(size);
    }

    ~ScopedBuffer()
    {
        Dispose();
    }

    public byte[] GetBuffer()
    {
        return _buffer;
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_buffer);
        GC.SuppressFinalize(this);
    }
}