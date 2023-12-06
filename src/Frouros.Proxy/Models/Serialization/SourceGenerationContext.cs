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
using System.Text.Json.Serialization;
using Frouros.Proxy.Models.Web;
using Frouros.Shared.Models;

namespace Frouros.Proxy.Models.Serialization;

[JsonSerializable(typeof(Dictionary<uint, Packet>)),
 JsonSerializable(typeof(IPAddress)),
 JsonSerializable(typeof(Log[])),
 JsonSerializable(typeof(ModuleInfo)),
 JsonSerializable(typeof(ModuleActivationInfo)),
 JsonSerializable(typeof(IEnumerable<ModuleInfo>)),
 JsonSerializable(typeof(IReadOnlyDictionary<string, PodInfo>))]
#if DEBUG
[JsonSourceGenerationOptions(WriteIndented = true)]
#else
[JsonSourceGenerationOptions(WriteIndented = false)]
#endif
public partial class SourceGenerationContext : JsonSerializerContext;