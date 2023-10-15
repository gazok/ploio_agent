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

using System.Runtime.InteropServices;
using Frouros.Utils;

namespace Frouros.Net.Models;

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