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
using Frouros.Host;
using Frouros.Proxy.Services.Abstract;
using Grpc.Core;
using Grpc.Net.Client;

namespace Frouros.Proxy.Services;

public class GrpcServiceProvider : IGrpcServiceProvider
{
    private readonly GrpcChannel                        _ch;
    private readonly FrozenDictionary<Type, ClientBase> _services;

    public GrpcServiceProvider(ILogger<GrpcServiceProvider> logger)
    {
        logger.LogTrace("Creating gRPC channels...");
        _ch = Shared.Net.Grpc.CreateChannel();
        logger.LogTrace("gRPC channels established");

        _services = new Dictionary<Type, ClientBase>
        {
            [typeof(CRI.CRIClient)] = new CRI.CRIClient(_ch),
            [typeof(PVI.PVIClient)] = new PVI.PVIClient(_ch),
            [typeof(ARP.ARPClient)] = new ARP.ARPClient(_ch)
        }.ToFrozenDictionary();
    }

    public object? GetService(Type serviceType)
    {
        return _services.GetValueOrDefault(serviceType);
    }

    public void Dispose()
    {
        _ch.Dispose();
        GC.SuppressFinalize(this);
    }
}