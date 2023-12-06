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

using Frouros.Shared.Imports;
using static Frouros.Host.Imports.Native;

namespace Frouros.Host.Bridges;

public delegate bool NFManagedCallback(uint id, Span<byte> pkt, Timeval tv);

public class Netfilter(ILogger<Netfilter> logger, IConfiguration config)
{
    private const nuint BufferSize = 0xFFFF;

    public Task<bool> StartupAsync(NFManagedCallback cb, CancellationToken token)
    {
        return Task.Factory.StartNew(() =>
        {
            unsafe
            {
                return StartupUnsafe((qh, _, nfa, _) =>
                {
                    byte*   buf;
                    Timeval tv;
                    
                    var id  = nfq_get_msg_packet_hdr(nfa)->PacketId;
                    var len = nfq_get_payload(nfa, &buf);
                    if (len < 0 || nfq_get_timestamp(nfa, &tv) != 0)
                    {
                        return nfq_set_verdict(qh, id, NF_ACCEPT, 0, null);
                    }

                    return nfq_set_verdict(
                        qh, id, 
                        cb.Invoke(id, new Span<byte>(buf, len), tv) 
                            ? NF_ACCEPT 
                            : NF_DROP, 
                        0, null);
                }, token);
            }
        }, token);
    }
    
    private unsafe bool StartupUnsafe(NFCallback cb, CancellationToken token)
    {
        void* h;
        void* qh;

        logger.LogInformation("opening nfq");
        if ((h = nfq_open()) == null)
        {
            logger.LogCritical("nfq_open(): {}", GetLastError());
            return false;
        }

        logger.LogInformation("unbinding pf");
        if (nfq_unbind_pf(h, PF_INET) < 0 ||
            nfq_unbind_pf(h, PF_INET6) < 0)
        {
            logger.LogCritical("nfq_unbind_pf(): {}", GetLastError());
            return false;
        }

        logger.LogInformation("binding pf");
        if (nfq_bind_pf(h, PF_INET) < 0 ||
            nfq_bind_pf(h, PF_INET6) < 0)
        {
            logger.LogCritical("nfq_bind_pf(): {}", GetLastError());
            return false;
        }

        logger.LogInformation("creating queue");
        if ((qh = nfq_create_queue(h, (ushort)config.GetValue<uint>("Queue"), cb, null)) == null)
        {
            logger.LogCritical("nfq_create_queue(): {}", GetLastError());
            return false;
        }

        logger.LogInformation("set nfq mode");
        if (nfq_set_mode(qh, NFQNL_COPY_PACKET, 0xFFFF) < 0)
        {
            logger.LogCritical("nfq_set_mode(): {}", GetLastError());
            return false;
        }
        
        var buf = new byte[BufferSize];
        var fd  = nfq_fd(h);
        
        fixed (byte* pBuf = buf)
        {
            while (!token.IsCancellationRequested)
            {
                nint rcv;
                if ((rcv = recv(fd, pBuf, BufferSize, 0)) >= 0)
                {
                    nfq_handle_packet(h, pBuf, (int)rcv);
                    continue;
                }

                logger.LogCritical("recv(): {}", GetLastError());
                break;
            }
        } 
        
        logger.LogInformation("destroy queue");
        nfq_destroy_queue(qh);
        nfq_close(h);

        return true;
    }
}