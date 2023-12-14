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

using Frouros.Host;
using Frouros.Proxy.Bridges.Abstract;
using Frouros.Proxy.Models;
using Frouros.Proxy.Models.Web;
using Frouros.Proxy.Services.Abstract;
using Frouros.Proxy.Workers.Routing;
using Frouros.Shared.Extensions;
using Frouros.Shared.Imports;
using Frouros.Shared.Models;
using Google.Protobuf.WellKnownTypes;
using Packet = Frouros.Proxy.Models.Web.Packet;

namespace Frouros.Proxy.Workers;

public class PVIWorker(
    IGrpcServiceProvider grpc,
    IMembrane            membrane,
    PacketRouting        packetRouting,
    LogRouting           logRouting,
    ILogger<PVIWorker>   logger) : BackgroundService
{
    private readonly PVI.PVIClient _client = grpc.GetRequiredService<PVI.PVIClient>();
    private readonly ARP.ARPClient _arp    = grpc.GetRequiredService<ARP.ARPClient>();

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        logger.LogTrace("{} is started", GetType().Name);

        while (!token.IsCancellationRequested)
        {
            var remains = await _client.GetRemainsAsync(new Empty(), cancellationToken: token);

            var dto      = new (uint UId, Packet Packet)[remains.Packets.Count];
            var verdicts = new PacketVerdict[remains.Packets.Count];
            var logs     = new IEnumerable<Log>[remains.Packets.Count];

            await Parallel.ForAsync(0, dto.Length - 1, token, async (i, localToken) =>
            {
                string? src = null;
                string? dst = null;

                var packet = remains.Packets[i];
                var prc    = PacketRegistryContainer.FromMemory(packet.Packet_.Memory);

                await Task.WhenAll(
                    _arp.ResolveAsync(
                             new EndPointTarget { Ip = prc.Source.ToByteString() },
                             cancellationToken: localToken)
                        .ResponseAsync
                        .ContinueWith(task => src = task.Result.Uid, localToken),
                    _arp.ResolveAsync(
                             new EndPointTarget { Ip = prc.Destination.ToByteString() },
                             cancellationToken: localToken)
                        .ResponseAsync
                        .ContinueWith(task => dst = task.Result.Uid, localToken));

                src ??= prc.Source.ToString();
                dst ??= prc.Source.ToString();

                dto[i] = (
                    packet.Uid,
                    new Packet(
                        packet.Timestamp.ToDateTime(),
                        src, dst,
                        (ulong)packet.Packet_.Length,
                        Convert.ToBase64String(packet.Packet_.Span)
                    )
                );

                var tv       = new Timeval((nint)packet.Timestamp.Seconds, packet.Timestamp.Nanos);
                var messages = membrane.Transmit(packet.Uid, prc, tv).ToArray();

                logs[i] = messages.Where(msg => msg.Code > (ushort)VerdictCode.Warning).Select(msg => new Log(
                        msg.Code,
                        msg.Message,
                        msg.Module.Info.GUID.ToString("D"),
                        new[]
                        {
                            new Reference(ReferenceSource.Packet, packet.Uid.ToString(),
                                Array.Empty<string>()),
                            new Reference(ReferenceSource.Pod, src, new[] { "Source" }),
                            new Reference(ReferenceSource.Pod, dst, new[] { "Destination" })
                        }
                    )
                );

                verdicts[i] = new PacketVerdict
                {
                    Accept = !messages.Any(msg => msg.Code > (ushort)VerdictCode.Error),
                    Uid    = packet.Uid
                };
            });

            await Task.WhenAll(
                packetRouting.Writer.WriteAsync(
                    dto.AsParallel().ToDictionary(t => t.UId, t => t.Packet),
                    token).AsTask(),
                logRouting.Writer.WriteAsync(logs.SelectMany(log => log), token).AsTask(),
                _client.SetVerdictAsync(new PacketVerdictCollection
                {
                    Verdicts = { verdicts }
                }, cancellationToken: token).ResponseAsync
            );
        }
    }
}