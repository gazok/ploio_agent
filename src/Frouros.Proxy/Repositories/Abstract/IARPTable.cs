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

using System.Net;
using Frouros.Host;

namespace Frouros.Proxy.Repositories.Abstract;

[Flags]
public enum ResolveFlag
{
    None    = 0,
    Confirm = 1
}

public interface IARPTable
{
    public Task<string?> ResolveAsync(IPAddress addr, ResolveFlag flag);

    public ARP.ARPClient GetOrigin(string uid);

    public bool TryResolve(
        IPAddress          addr,
        out ARP.ARPClient? client,
        out string?        uid);

    public void Update(ARP.ARPClient client, IPAddress addr, string uid);

    public void Clear();
}