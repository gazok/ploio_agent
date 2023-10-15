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

using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Frouros.Net.Models;

public readonly struct PacketLog(
    DateTime     timestamp,
    ProtocolType l3,
    LxProto      lx,
    IPEndPoint   source,
    IPEndPoint   destination,
    uint         size)
{
    public readonly DateTime     Timestamp   = timestamp;
    public readonly ProtocolType L3          = l3;
    public readonly LxProto      LX          = lx;
    public readonly IPEndPoint   Source      = source;
    public readonly IPEndPoint   Destination = destination;
    public readonly uint         Size        = size;

    internal NativePacketLog AsNative()
    {
        UInt128 src = 0;
        UInt128 dst = 0;

        unsafe
        {
            fixed (byte* pSrc = Source.Address.GetAddressBytes())
                Unsafe.CopyBlock(&src, pSrc, 16);
            fixed (byte* pDst = Source.Address.GetAddressBytes())
                Unsafe.CopyBlock(&dst, pDst, 16);
        }

        var ts = (ulong)((DateTimeOffset)Timestamp).ToUnixTimeSeconds();
        var l2 = NativePacketLog.GetL2Family(Source.AddressFamily);
        var l3 = (byte)L3;
        var lx = (byte)LX;
        var sp = (ushort)Source.Port;
        var dp = (ushort)Destination.Port;

        return new NativePacketLog(ts, l2, l3, lx, src, dst, sp, dp, Size);
    }
}