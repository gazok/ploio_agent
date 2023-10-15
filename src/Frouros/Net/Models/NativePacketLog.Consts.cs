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

using System.Net.Sockets;

namespace Frouros.Net.Models;

internal partial struct NativePacketLog
{
    internal const int StructSize = 52;

    private static readonly Dictionary<AddressFamily, byte> L2Family = new()
    {
        [AddressFamily.InterNetwork]   = 1,
        [AddressFamily.InterNetworkV6] = 2,
        [AddressFamily.Ipx]            = 11,
        [AddressFamily.AppleTalk]      = 12,
        [AddressFamily.DecNet]         = 13,
        [AddressFamily.Banyan]         = 14
    };

    public static byte GetL2Family(AddressFamily family)
    {
        return L2Family.TryGetValue(family, out var value) ? value : (byte)0xFF;
    }
}