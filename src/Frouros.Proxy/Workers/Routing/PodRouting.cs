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

using Frouros.Proxy.Models.Serialization;
using Frouros.Proxy.Repositories.Abstract;
using Frouros.Shared;

namespace Frouros.Proxy.Workers.Routing;

public class PodRouting(HttpClient http, ILogger<PodRouting> logger, IPodAuthRepository cri) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        logger.LogTrace("{} is started", GetType().Name);
        
        while (!token.IsCancellationRequested)
        {
            using var response = await http.PostAsJsonAsync(
                new Uri(Specials.CentralServer, "pod"),
                cri.Auth.Values.ToDictionary(value => value.UId),
                SerializerOptions.Default,
                cancellationToken: token);

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