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
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Frouros.Net.Models;

public readonly struct PacketDumpHeader
{
    public readonly string Signature;
    public readonly byte[] Version;
    public readonly uint   Count;

    public PacketDumpHeader(Version version, uint count)
    {
        Signature  = "FROS";
        Version    = new byte[]
        {
            checked((byte)version.Major),
            checked((byte)version.Minor), 
            checked((byte)version.Build), 
            checked((byte)version.Revision)
        };
        Count = count;
    }

    internal void WriteTo(Stream dst)
    {
        var buffer = new byte[12];
        
        Encoding.ASCII.GetBytes(Signature).CopyTo(buffer, 0);
        Version.CopyTo(buffer, 4);
        BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(buffer, 8, 4), Count);

        dst.Write(buffer);
    }

    public static bool TryReadFrom(Stream src, [NotNullWhen(true)] out PacketDumpHeader? hdr)
    {
        var buf = new byte[12];
        if (src.Read(buf) != 12)
        {
            hdr = null;
            return false;
        }

        if (!Encoding.ASCII.GetString(buf[..4]).Equals("FROS", StringComparison.Ordinal))
        {
            hdr = null;
            return false;
        }

        var ver = new Version(buf[4], buf[5], buf[6], buf[7]);
        var cnt = BinaryPrimitives.ReadUInt32BigEndian(buf[8..12]);

        hdr = new PacketDumpHeader(ver, cnt);
        return true;
    }
}