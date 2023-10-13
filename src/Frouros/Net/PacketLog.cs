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

using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Frouros;

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

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = StructSize)]
internal readonly partial struct NativePacketLog(
    ulong   epoch,
    ushort  l2,
    byte    l3,
    byte    lx,
    UInt128 sIp,
    UInt128 dIp,
    ushort  sPort,
    ushort  dPort,
    uint    size)
{
    public readonly ulong Epoch = epoch;

    public readonly ushort L2 = l2;
    public readonly byte   L3 = l3;
    public readonly byte   LX = lx;

    public readonly UInt128 SIp = sIp;
    public readonly UInt128 DIp = dIp;

    public readonly ushort SPort = sPort;
    public readonly ushort DPort = dPort;

    public readonly uint Size = size;

    public bool TryWriteTo(Span<byte> span, long offset)
    {
        if (span.Length - offset < StructSize)
            return false;

        var ofs = offset;
        Endianness.WriteBigEndian(span, ref ofs, Epoch);
        Endianness.WriteBigEndian(span, ref ofs, L2);

        Endianness.WriteBigEndian(span, ref ofs, L3);
        Endianness.WriteBigEndian(span, ref ofs, LX);
        Endianness.WriteBigEndian(span, ref ofs, SIp);
        Endianness.WriteBigEndian(span, ref ofs, DIp);
        Endianness.WriteBigEndian(span, ref ofs, SPort);
        Endianness.WriteBigEndian(span, ref ofs, DPort);
        Endianness.WriteBigEndian(span, ref ofs, Size);

        return true;
    }
}

public enum LxProto : byte
{
    QUIC = 0x01,
    HTTP = 0x02,
    None = 0xFF
}

public readonly struct PacketLog(
    DateTime     timestamp,
    ProtocolType l3,
    LxProto      lx,
    IPEndPoint   source,
    IPEndPoint   destination,
    uint size)
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