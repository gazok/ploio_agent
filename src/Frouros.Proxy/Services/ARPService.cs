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

using System.Collections.Concurrent;
using System.Net;
using Frouros.Host;
using Frouros.Proxy.Repositories.Abstract;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;

namespace Frouros.Proxy.Services;

public class ARPService(IPodAuthRepository repo, IARPTable arp, ILogger<ARPService> logger) : ARP.ARPBase
{
    private readonly ConcurrentDictionary<IPEndPoint, (GrpcChannel Channel, ARP.ARPClient Service)> _dict   = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<IPEndPoint, ARP.ARPClient>>  _events = new();

    public override Task<ResolvedTarget> Resolve(EndPointTarget request, ServerCallContext context)
    {
        var uid = repo.Auth.Values
                      .First(pod => pod.Network.Any(
                               net => net.Equals(
                                   new IPAddress(request.Ip.Span)
                               )
                           )
                       )
                      .UId;

        return Task.FromResult(new ResolvedTarget { Uid = uid });
    }

    public override Task<Empty> ResolveCallback(ARPEventArgs request, ServerCallContext context)
    {
        foreach (var ip in request.Ip.Select(ip => new IPAddress(ip.Span)))
        {
            arp.Update(arp.GetOrigin(request.Uid), ip, request.Uid);
        }

        return Task.FromResult(new Empty());
    }

    public override Task<Empty> Register(ARPEvent request, ServerCallContext context)
    {
        var ep = new IPEndPoint(new IPAddress(request.Ip.Span), (int)request.Port);

        var (_, client) = _dict.GetOrAdd(ep, static ep =>
        {
            var ch     = Net.Grpc.CreateChannel(ep);
            var client = new ARP.ARPClient(ch);
            return (ch, client);
        });

        _events.AddOrUpdate(request.Uid,
            _ =>
            {
                var dict = new ConcurrentDictionary<IPEndPoint, ARP.ARPClient>(
                    TaskScheduler.Default.MaximumConcurrencyLevel,
                    16);
                dict.TryAdd(ep, client);
                return dict;
            },
            (uid, bag) =>
            {
                bag.AddOrUpdate(ep, client, (oldEp, _) =>
                {
                    logger.LogWarning(
                        "duplicated registry for one endpoint is detected\n" +
                        "handler for '{uid}' is changed from '{oldEp}' to '{ep}'",
                        uid, oldEp, ep);
                    return client;
                });
                return bag;
            });

        return Task.FromResult(new Empty());
    }

    public override Task<Empty> Unregister(ARPEvent request, ServerCallContext context)
    {
        var ep = new IPEndPoint(new IPAddress(request.Ip.Span), (int)request.Port);

        _events.AddOrUpdate(
            request.Uid,
            _ => new ConcurrentDictionary<IPEndPoint, ARP.ARPClient>(
                TaskScheduler.Default.MaximumConcurrencyLevel,
                16),
            (uid, bag) =>
            {
                bag.Remove(ep, out _);
                return bag;
            });

        return Task.FromResult(new Empty());
    }
}