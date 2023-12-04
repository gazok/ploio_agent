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
using System.Text.Json.Serialization;

namespace Frouros.Shared.Models;

[Serializable]
public class PodInfo(string uid, string name, string ns, string state, DateTime createdAt, IPAddress[] network)
{
    [JsonIgnore]
    public string      UId       = uid;
    public string      Name      = name;
    public string      Namespace = ns;
    public string      State     = state;
    public DateTime    CreatedAt = createdAt;
    public IPAddress[] Network   = network;
}
