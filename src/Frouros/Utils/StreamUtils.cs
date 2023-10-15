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

namespace Frouros.Utils;

public static class StreamUtils
{
    public static void CopyTo(this Stream src, Stream dst, long bytes, int bufferSize)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

        while (bytes > 0)
        {
            var read = src.Read(buffer);
            dst.Write(buffer, 0, read);
            bytes -= read;
        }
        
        ArrayPool<byte>.Shared.Return(buffer);
    }
}