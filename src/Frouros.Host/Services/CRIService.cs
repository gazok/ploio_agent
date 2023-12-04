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

using Frouros.Host.Workers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Frouros.Host.Services;

public class CRIService(CRIWorker worker) : CRI.CRIBase
{
    public override Task<PodResponse> Query(PodRequest request, ServerCallContext context)
    {
        var uid = request.Uid;
        if (!worker.Query().TryGetValue(uid, out var info))
        {
            return Task.FromResult(new PodResponse { IsNull = true });
        }

        var ret = new PodResponse
        {
            IsNull    = false,
            Uid       = info.UId,
            Name      = info.Name,
            Namespace = info.Namespace,
            State     = info.State,
            CreatedAt = info.CreatedAt.ToTimestamp(),
            Network =
            {
                info.Network.Select(ip => ByteString.CopyFrom(ip.GetAddressBytes()))
            }
        };

        return Task.FromResult(ret);
    }

    public override Task<PodCollection> QueryAll(Empty request, ServerCallContext context)
    {
        return Task.FromResult(new PodCollection{ Pods = { worker.Query().Keys }});
    }
}