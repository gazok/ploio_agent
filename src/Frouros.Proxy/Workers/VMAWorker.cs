﻿using Frouros.Proxy.Models.Serialization;
using Frouros.Proxy.Models.Web;
using Frouros.Proxy.Repositories.Abstract;
using Frouros.Shared;

namespace Frouros.Proxy.Workers;

public class VMAWorker(HttpClient http, IModuleRepository repo) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            foreach (var response in await Task.WhenAll(
                         repo.Handles.Select(handle => http.PostAsJsonAsync(
                                 new Uri(Specials.CentralServer, "activation"),
                                 handle.Info,
                                 SerializerOptions.Default, 
                                 cancellationToken: token))
                             .ToArray()
                     ))
            {
                using (response)
                {
                    var info = await response.Content.ReadFromJsonAsync<ModuleActivationInfo>(
                        SerializerOptions.Default, 
                        cancellationToken: token
                    );
                    if (info is null)
                        continue;

                    repo[info.GUID].Enabled = info.Activation == ModuleActivation.Enabled;
                }
            }
        }
    }
}