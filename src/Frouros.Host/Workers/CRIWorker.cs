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
using Frouros.Shared.Models;

namespace Frouros.Host.Workers;

public sealed class CRIWorker : IHostedService
{
    private class PodPairNetworkEqualityComparer : IEqualityComparer<KeyValuePair<string, PodInfo>>
    {
        public static readonly PodPairNetworkEqualityComparer Default = new();
        
        public bool Equals(KeyValuePair<string, PodInfo> x, KeyValuePair<string, PodInfo> y)
        {
            return x.Value.Network.SequenceEqual(y.Value.Network);
        }

        public int GetHashCode(KeyValuePair<string, PodInfo> obj)
        {
            return obj.Value.Network.GetHashCode();
        }
    }

    public delegate void CRIUpdateEvent(string uid, IPAddress[] diff);

    private bool _init;

    private Timer?                             _timer;
    private FrozenDictionary<string, PodInfo>? _infos;

    private readonly ILogger<CRIWorker> _logger;

    public CRIWorker(ILogger<CRIWorker> logger)
    {
        _logger = logger;
    }

    public event CRIUpdateEvent? Updated;

    public Task StartAsync(CancellationToken token)
    {
        _timer = new Timer(_ =>
        {
            try
            {
                _init = true;

                var tmp = QueryIds()
                         .Select(QueryPod)
                         .Where(info => info is not null)
                         .Cast<PodInfo>()
                         .ToFrozenDictionary(info => info.UId);

                if (_infos is not null)
                {
                    foreach (var (uid, diff) in tmp.Except(_infos, PodPairNetworkEqualityComparer.Default))
                    {
                        Updated?.Invoke(uid, diff.Network);
                    }
                }
                
                _infos = tmp;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Couldn't fetch CRI information; CRI-information-table will not be updated");
            }
        }, null, 0, 500);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    public FrozenDictionary<string, PodInfo> Query()
    {
        while (!_init)
        {
        }

        return _infos!;
    }

    private static IEnumerable<string> QueryIds()
    {
        var info = new ProcessStartInfo("crictl", "pods -q")
        {
            CreateNoWindow         = true,
            RedirectStandardOutput = true,
            UseShellExecute        = false
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
            CreateNoWindow         = true,
            RedirectStandardOutput = true,
            UseShellExecute        = false
        };
        using var proc = Process.Start(info);
        if (proc is null)
            return null;
        proc.WaitForExit();

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
}