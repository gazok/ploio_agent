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
using System.Net;
using Frouros.Proxy.Repositories.Abstract;
using Frouros.Shared.Models;

namespace Frouros.Proxy.Repositories;

public class PodAuthRepository : IPodAuthRepository
{
    private FrozenDictionary<IPAddress, PodInfo>? _auth;

    public IReadOnlyDictionary<IPAddress, PodInfo> Auth
    {
        get => _auth ?? FrozenDictionary<IPAddress, PodInfo>.Empty; 
        set => _auth = value as FrozenDictionary<IPAddress, PodInfo> ?? value.ToFrozenDictionary();
    }
}
