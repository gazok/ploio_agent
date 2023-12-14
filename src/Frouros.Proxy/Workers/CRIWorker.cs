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

using System.Collections.Frozen;
using System.Net;
using Frouros.Host;
using Frouros.Proxy.Repositories.Abstract;
using Frouros.Proxy.Services.Abstract;
using Frouros.Shared.Models;
using Google.Protobuf.WellKnownTypes;

namespace Frouros.Proxy.Workers;

public class CRIWorker(IGrpcServiceProvider grpc, IPodAuthRepository repo, ILogger<CRIWorker> logger) : BackgroundService
{
    private readonly CRI.CRIClient _client = grpc.GetRequiredService<CRI.CRIClient>();

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        logger.LogTrace("{} is started", GetType().Name);
        
        while (!token.IsCancellationRequested)
        {
            var pods = await _client.QueryAllAsync(new Empty(), cancellationToken: token);
        
            var list = new Dictionary<IPAddress, PodInfo>(pods.Pods.Count);
            foreach (var uid in pods.Pods)
            {
                var pod = await _client.QueryAsync(new PodRequest { Uid = uid }, cancellationToken: token);
                var ips = pod.Network.Select(bs => new IPAddress(bs.Span)).ToArray();
                foreach (var ip in ips)
                {
                    list.Add(ip, new PodInfo(
                        pod.Uid.Replace("-", string.Empty), 
                        pod.Name, 
                        pod.Namespace, 
                        pod.State, 
                        pod.CreatedAt.ToDateTime(), 
                        ips)
                    );
                }
            }

            repo.Auth = list.ToFrozenDictionary();

            await Task.Delay(500, token);
        }
    }
}