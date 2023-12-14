using System.Runtime.CompilerServices;
using Frouros.Proxy.Models.Serialization;
using Frouros.Proxy.Models.Web;
using Frouros.Proxy.Repositories.Abstract;
using Frouros.Shared;

namespace Frouros.Proxy.Workers;

public class VMAWorker(HttpClient http, IModuleRepository repo, ILogger<VMAWorker> logger) : BackgroundService
{
    private async IAsyncEnumerable<HttpResponseMessage> RequestAsync([EnumeratorCancellation] CancellationToken token)
    {
        var tasks = repo.Handles.Select(handle =>
            http.PostAsJsonAsync(
                new Uri(Specials.CentralServer, "activation"),
                handle.Info,
                SerializerOptions.Default,
                cancellationToken: token
            )
        );
        
        logger.LogTrace("VMA Sent!");

        foreach (var task in tasks)
        {
            if (task.IsCompleted)
                yield return task.Result;
            else
                yield return await task;
        }
        
        logger.LogTrace("VMA Received!");
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        logger.LogTrace("{} is started", GetType().Name);

        while (!token.IsCancellationRequested)
        {
            await foreach (var response in RequestAsync(token))
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