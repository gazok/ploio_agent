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
using Frouros.Host.Repositories.Abstract;
using Frouros.Shared.Models;

namespace Frouros.Host.Repositories;

public class PodAuthRepository : IPodAuthRepository
{
    private FrozenDictionary<string, PodInfo>? _auth;

    public IReadOnlyDictionary<string, PodInfo> Auth
    {
        get => _auth ?? FrozenDictionary<string, PodInfo>.Empty; 
        set => _auth = value as FrozenDictionary<string, PodInfo> ?? value.ToFrozenDictionary();
    }
}