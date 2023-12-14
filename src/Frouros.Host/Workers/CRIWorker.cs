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
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Frouros.Host.Repositories.Abstract;
using Frouros.Shared.Models;

namespace Frouros.Host.Workers;

public sealed class CRIWorker(ILogger<CRIWorker> logger, IPodAuthRepository repo) : IHostedService
{
    private Timer? _timer;

    public Task StartAsync(CancellationToken token)
    {
        _timer = new Timer(_ =>
        {
            try
            {
                repo.Auth = QueryIds()
                    .Select(QueryPod)
                    .Where(info => info is not null)
                    .Cast<PodInfo>()
                    .ToFrozenDictionary(info => info.UId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Couldn't fetch CRI information; CRI-information-table will not be updated");
            }
        }, null, 0, 500);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    private static IEnumerable<string> QueryIds()
    {
        var info = new ProcessStartInfo("crictl", "pods -q")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        using var proc = Process.Start(info);
        if (proc is null)
            return ArraySegment<string>.Empty;

        proc.WaitForExit();
        return proc.StandardOutput.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    private static PodInfo? QueryPod(string id)
    {
        var info = new ProcessStartInfo("crictl", new[] { "inspectp", id })
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        using var proc = Process.Start(info);
        if (proc is null)
            return null;
        proc.WaitForExit();

        try
        {
            using var json   = JsonDocument.Parse(proc.StandardOutput.ReadToEnd());
            var       status = json.RootElement.GetProperty("status");

            var meta = status.GetProperty("metadata");
            var name = meta.GetProperty("name").GetString();
            var ns   = meta.GetProperty("namespace").GetString();
            var uid  = meta.GetProperty("uid").GetString();

            var state   = status.GetProperty("state").GetString();
            var created = status.GetProperty("createdAt").GetDateTime();

            var net = status.GetProperty("network");

            var ips = net
                     .GetProperty("additionalIps")
                     .EnumerateArray()
                     .Select(e => e.GetString())
                     .Prepend(net.GetProperty("ip").GetString())
                     .Where(ip => ip is not null)
                     .Select(sip => IPAddress.Parse(sip!));

            return new PodInfo(uid!, name!, ns!, state!, created, ips.ToArray());
        }
        catch
        {
            return null;
        }
    }
}