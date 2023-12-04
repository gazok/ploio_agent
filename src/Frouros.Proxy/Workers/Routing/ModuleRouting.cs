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
using Frouros.Proxy.Workers.Routing.Abstract;
using Frouros.Shared;

namespace Frouros.Proxy.Workers.Routing;

public class ModuleRouting(ILogger<ModuleRouting> logger, IModuleRepository repo) : RoutingBase
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            using var response = await Http.PostAsJsonAsync(
                new Uri(Specials.CentralServer, "module"),
                repo.Handles.Select(handle => handle.Info),
                SourceGenerationContext.Default.IEnumerableModuleInfo,
                cancellationToken: token
            );

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                logger.LogError(e, "Couldn't route module-data; real-time data will be lost");
            }
        }
    }
}