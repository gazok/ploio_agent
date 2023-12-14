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

using System.Threading.Channels;
using Frouros.Proxy.Collections;
using Frouros.Proxy.Models.Serialization;
using Frouros.Proxy.Models.Web;
using Frouros.Shared;

namespace Frouros.Proxy.Workers.Routing;

public class PacketRouting(HttpClient http, ILogger<PacketRouting> logger) : BackgroundService
{
    private readonly Channel<Dictionary<uint, Packet>> _queue = Channel.CreateUnbounded<Dictionary<uint, Packet>>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        }
    );

    public ChannelWriter<Dictionary<uint, Packet>> Writer => _queue.Writer;

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        logger.LogTrace("{} is started", GetType().Name);
        
        while (!token.IsCancellationRequested)
        {
            var jobs = await _queue.Reader.ReadAllAsync(token).AsTask();
            var dict = jobs.SelectMany(dict => dict).ToDictionary();
            
            using var response = await http.PostAsJsonAsync(
                new Uri(Specials.CentralServer, "packet"),
                dict,
                SerializerOptions.Default,
                cancellationToken: token
            );

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                logger.LogError(e, "Couldn't route pods-data; real-time data will be lost");
            }
        }
    }
}