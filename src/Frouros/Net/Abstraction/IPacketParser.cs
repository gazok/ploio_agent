//     Copyright 2023 Yeong-won Seo
// 
//     Licensed under the Apache License, Version 2.0 (the "License");
//     you may not use this file except in compliance with the License.
//     You may obtain a copy of the License at
// 
//         http://www.apache.org/licenses/LICENSE-2.0
// 
//     Unless required by applicable law or agreed to in writing, software
//     distributed under the License is distributed on an "AS IS" BASIS,
//     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     See the License for the specific language governing permissions and
//     limitations under the License.

using Frouros.Net.Models;
using PacketDotNet;

namespace Frouros.Net.Abstraction;

public interface IPacketParser
{
    /// <summary>
    /// Parses <see cref="Packet"/> into <see cref="PacketLog"/>.
    /// </summary>
    /// <param name="ts">Unix seconds when <paramref name="packet"/> detected</param>
    /// <param name="packet"><see cref="Packet"/> to be parsed.</param>
    /// <param name="log">
    /// <see cref="PacketLog"/> parsed from <paramref name="packet"/>.
    /// If failed, will be set to invalid packet log.
    /// </param>
    /// <returns>true if parsing was successful. otherwise, false.</returns>
    public bool TryParse(DateTime ts, Packet packet, out PacketLog log);
}