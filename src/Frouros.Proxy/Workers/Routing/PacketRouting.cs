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
using Frouros.Proxy.Models.Serialization;
using Frouros.Proxy.Models.Web;
using Frouros.Proxy.Workers.Routing.Abstract;
using Frouros.Shared;

namespace Frouros.Proxy.Workers.Routing;

public class PacketRouting(ILogger<PacketRouting> logger) : RoutingBase
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
        while (!token.IsCancellationRequested)
        {
            var dictionary = _queue.Reader
                 .ReadAllAsync(token)
                 .ToBlockingEnumerable()
                 .AsParallel()
                 .SelectMany(dict => dict)
                 .ToDictionary();

            using var response = await Http.PostAsJsonAsync(
                new Uri(Specials.CentralServer, "packet"),
                dictionary,
                SourceGenerationContext.Default.DictionaryUInt32Packet,
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