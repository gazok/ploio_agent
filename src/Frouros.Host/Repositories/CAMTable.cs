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
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Frouros.Host.Repositories.Abstract;
using Grpc.Core;
using Grpc.Net.Client;
using BindingFlags = System.Reflection.BindingFlags;

namespace Frouros.Host.Repositories;

public class CAMTable : IServiceProvider, ICAMTable
{
    private readonly ConcurrentDictionary<Type, ChannelBase[]> _dict = new();
    private readonly GrpcChannel[]                             _channels;

    public CAMTable(IApplicationInformation app, IConfiguration config)
    {
        var port = config.GetValue<int>("Port");

        _channels = app
                   .Hosts
                   .Select(ip => Shared.Net.Grpc.CreateChannel(new IPEndPoint(ip, port)))
                   .ToArray();
    }

    object IServiceProvider.GetService(Type type)
    {
        return _dict.GetOrAdd(type, static ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] type, channels) =>
        {
            var clients = channels
                         .Select(channel => type
                                           .GetConstructor(
                                                BindingFlags.Public | BindingFlags.Instance,
                                                new[] { typeof(GrpcChannel) })
                                          ?.Invoke(new object?[] { channel }) as ChannelBase)
                         .Where(client => client is not null)
                         .Cast<ChannelBase>()
                         .ToArray();

            return clients;
        }, _channels);
    }

    public T[] GetService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : ClientBase<T>
    {
        return (T[])((IServiceProvider)this).GetService(typeof(T))!;
    }

    public void Dispose()
    {
        Task.WaitAll(_channels.Select(ch => ch.ShutdownAsync()).ToArray());
        foreach (var channel in _channels) 
            channel.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var channel in _channels)
        {
            await channel.ShutdownAsync().ConfigureAwait(false);
            channel.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}