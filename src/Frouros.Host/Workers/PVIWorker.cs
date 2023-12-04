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

using Frouros.Host.Bridges;
using Frouros.Host.Models;
using Frouros.Host.Services;

namespace Frouros.Host.Workers;

public class PVIWorker(Netfilter nf) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        await nf.StartupAsync((id, pkt, tv) =>
        {
            var       accept = false;
            using var wh     = new EventWaitHandle(false, EventResetMode.ManualReset);
            
            PVIService.Register(
                new PVIEvent(
                    new PacketHandle(id, pkt.ToArray(), tv),
                    (_, acc) =>
                    {
                        accept = acc;
                        // wait-handle will be disposed when wait-handle is set
                        // ReSharper disable once AccessToDisposedClosure
                        wh.Set();
                    }
                )
            );

            wh.WaitOne();
            return accept;
        }, token);
    }
}