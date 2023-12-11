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

public class LogRouting(HttpClient http, ILogger<LogRouting> logger) : BackgroundService
{
    private readonly Channel<IEnumerable<Log>> _queue = Channel.CreateUnbounded<IEnumerable<Log>>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

    public ChannelWriter<IEnumerable<Log>> Writer => _queue.Writer;

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var jobs = await _queue.Reader.ReadAllAsync(token).AsTask();
            var logs = jobs.SelectMany(job => job);

            using var response = await http.PostAsJsonAsync(
                new Uri(Specials.CentralServer, "log"),
                logs,
                SerializerOptions.Default,
                cancellationToken: token
            );

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                logger.LogError(e, "Couldn't route log-data; real-time data will be lost");
            }
        }
    }
}