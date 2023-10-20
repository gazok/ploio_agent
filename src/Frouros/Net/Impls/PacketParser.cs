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

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using Frouros.Net.Abstraction;
using Frouros.Net.Models;
using PacketDotNet;
using ProtocolType = System.Net.Sockets.ProtocolType;

namespace Frouros.Net.Impls;

public class PacketParser : IPacketParser
{
    private readonly ILogger<PacketParser> _logger;
    private readonly IConfiguration        _config;

    private readonly bool _httpOnly;

    public PacketParser(ILogger<PacketParser> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;

        _httpOnly = _config.GetValue<bool>("HttpOnly");
    }

    private static bool IsHttp(TcpPacket tcp)
    {
        if (!tcp.HasPayloadData)
            return false;

        try
        {
            var data = Encoding.UTF8.GetString(tcp.PayloadData);
            return data.Split('\n')
                       .First()
                       .Contains("HTTP");
        }
        catch (ArgumentException)
        {
            return false; // i don't know why/how this works!
        }
    }

    private static bool IsQuic(UdpPacket udp)
    {
        if (!udp.HasPayloadData)
            return false;

        // See https://gist.github.com/martinthomson/744d04cbcec9be554f2f8e7bae2715b8
        return Encoding.UTF8.GetString(udp.PayloadData[1..4]) switch
        {
            "uic" => true,
            "UIC" => true,
            _     => false
        };
    }

    private LxProto GetLxProto(IPPacket pkt)
    {
        if (pkt is not { HasPayloadPacket: true, PayloadPacket: TransportPacket tp })
        {
            return LxProto.None;
        }

        return tp switch
        {
            TcpPacket tcp when IsHttp(tcp) => LxProto.HTTP,
            UdpPacket udp when IsQuic(udp) => LxProto.QUIC,
            _                              => LxProto.None
        };
    }

    private PacketLog Parse(DateTime ts, IPPacket pkt)
    {
        var sp = 0;
        var dp = 0;

        if (pkt is { HasPayloadPacket: true, PayloadPacket: TransportPacket tp })
        {
            sp = tp.SourcePort;
            dp = tp.DestinationPort;
        }

        return new PacketLog(
            ts,
            (ProtocolType)pkt.Protocol,
            GetLxProto(pkt),
            new IPEndPoint(pkt.SourceAddress,      sp),
            new IPEndPoint(pkt.DestinationAddress, dp),
            pkt.PayloadLength);
    }

    public bool? TryParse(DateTime ts, Packet packet, [NotNullWhen(true)] out PacketLog? log)
    {
        if (packet is not EthernetPacket { HasPayloadPacket: true, PayloadPacket: IPPacket ip })
        {
            log = null;
            return false;
        }

        log = Parse(ts, ip);
        if (_httpOnly && log.Value.LX != LxProto.HTTP)
        {
            return null;
        }
        
        return true;
    }
}