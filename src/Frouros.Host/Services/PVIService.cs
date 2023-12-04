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

using System.Data;
using Frouros.Host.Models;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Frouros.Host.Services;

public class PVIService : PVI.PVIBase
{
    private static SpinLock _lock;
    private static readonly Dictionary<uint, PVIEvent> Registry = new();

    public static void Register(PVIEvent @event)
    {
        // do NOT set state after TryAdd: this makes 'Reserved' event after even handled
        var old = @event.State;
        @event.State = PVIEventState.Reserved;

        var taken = false;
        try
        {
            _lock.TryEnter(ref taken);
            if (taken && Registry.TryAdd(@event.Packet.Id, @event))
                return;
        }
        finally
        {
            if (taken)
                _lock.Exit();
        }

        @event.State = old;
        throw new DBConcurrencyException();
    }

    public override Task<PacketCollection> GetRemains(Empty request, ServerCallContext context)
    {
        var taken = false;
        try
        {
            _lock.TryEnter(ref taken);
            if (!taken)
                return Task.FromResult(new PacketCollection());

            var list = new List<Packet>(Registry.Count);
            foreach (var @event in Registry.Values.Where(v => v.State == PVIEventState.Reserved))
            {
                @event.State = PVIEventState.Running;
                list.Add(new Packet
                {
                    Uid       = @event.Packet.Id,
                    Timestamp = new Timestamp
                    {
                        Nanos   = (int)@event.Packet.Timeval.Nanoseconds, 
                        Seconds = @event.Packet.Timeval.Seconds
                    },
                    Packet_   = ByteString.CopyFrom(@event.Packet.Bytes.Span)
                });
            }

            return Task.FromResult(new PacketCollection
            {
                Packets = { list }
            });
        }
        finally
        {
            if (taken)
                _lock.Exit();
        }
    }

    public override Task<Empty> SetVerdict(PacketVerdictCollection request, ServerCallContext context)
    {
        var taken = false;
        try
        {
            _lock.TryEnter(ref taken);
            if (!taken)
                return Task.FromResult(new Empty());

            foreach (var verdict in request.Verdicts)
            {
                var @event = Registry[verdict.Uid];
                @event.State = PVIEventState.Aborted;
                @event.Handler.Invoke(@event, verdict.Accept);

                Registry.Remove(verdict.Uid);
            }

            return Task.FromResult(new Empty());
        }
        finally
        {
            if (taken)
                _lock.Exit();
        }
    }
}