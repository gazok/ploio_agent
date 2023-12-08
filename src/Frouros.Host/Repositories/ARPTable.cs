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

using System.Collections.Concurrent;
using System.Net;
using Frouros.Host.Repositories.Abstract;
using Frouros.Shared.Extensions;

namespace Frouros.Host.Repositories;

public class ARPTable(IApplicationInformation app, ICAMTable cam) : IARPTable
{
    private readonly ConcurrentDictionary<IPAddress, (ARP.ARPClient Client, DateTime Tv, string UId)> _table = new();

    private readonly ARP.ARPClient[] _clients = cam.GetService<ARP.ARPClient>();

    public async Task<string?> ResolveAsync(IPAddress addr, ResolveFlag flag)
    {
        if (TryResolve(addr, out var old, out var uid))
            return uid;
        if ((flag & ResolveFlag.Confirm) == 0)
            return null;

        old?.UnregisterAsync(new ARPEvent
        {
            Ip   = IPAddress.Any.ToByteString(),
            Port = app.Port,
            Uid  = uid
        });

        foreach (var client in _clients)
        {
            var resolved = await client.ResolveLocalAsync(new EndPointTarget
            {
                Ip = addr.ToByteString()
            });
            if (!resolved.HasUid) 
                continue;
            
            uid = resolved.Uid;

            await client.RegisterAsync(new ARPEvent
            {
                Ip   = IPAddress.Any.ToByteString(),
                Port = app.Port,
                Uid  = uid
            });
            
            break;
        }

        return uid;
    }
    
    public ARP.ARPClient GetOrigin(string uid)
    {
        return _table.First(pair => pair.Value.UId.Equals(uid, StringComparison.Ordinal)).Value.Client;
    }

    public bool TryResolve(
        IPAddress          addr,
        out ARP.ARPClient? client,
        out string?        uid)
    {
        client = null;
        uid    = null;

        // doesn't have to assert concurrency-safe
        var ret = _table.TryGetValue(addr, out var pair);
        if (!ret || pair.Tv - DateTime.UtcNow <= TimeSpan.FromSeconds(30)) 
            return ret;
        
        _table.TryRemove(addr, out _);
        client = pair.Client;
        uid    = pair.UId;
        ret    = false;

        return ret;
    }

    public void Update(ARP.ARPClient client, IPAddress addr, string uid)
    {
        _table[addr] = (client, DateTime.UtcNow, uid);
    }

    public void Clear()
    {
        _table.Clear();
    }
}