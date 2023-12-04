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

using System.Buffers;
using System.Net;
using System.Runtime.InteropServices;

namespace Frouros.Proxy.Models;

public readonly struct PacketRegistryContainer : IDisposable
{
    public MemoryHandle   Handle      { get; }
    public PacketRegistry Registry    { get; }
    public IPAddress      Source      { get; }
    public IPAddress      Destination { get; }

    private PacketRegistryContainer(
        MemoryHandle   handle,
        PacketRegistry reg,
        IPAddress      src,
        IPAddress      dst)
    {
        Handle      = handle;
        Registry    = reg;
        Source      = src;
        Destination = dst;
    }

    public static PacketRegistryContainer FromMemory(ReadOnlyMemory<byte> mem)
    {
        unsafe
        {
            var handle = mem.Pin();
            var pkt    = mem.Span;

            if (pkt.Length < 1)
                goto L2_DEFAULT;

            ulong     magic;
            int       ihl;
            byte      proto;
            IPAddress src;
            IPAddress dst;

            var ver = (pkt[0] & 0xF0) >> 4;
            switch (ver)
            {
                case 6:
                {
                    if (pkt.Length < 40)
                        goto L2_DEFAULT;

                    magic = 2;
                    ihl   = 40;
                    proto = pkt[6];
                    src   = new IPAddress(pkt[8..24]);
                    dst   = new IPAddress(pkt[24..40]);

                    break;
                }
                case 4:
                {
                    if (pkt.Length < 20)
                        goto L2_DEFAULT;

                    magic = 1;
                    ihl   = (pkt[0] & 0x0F) * 4;
                    proto = pkt[9];
                    src   = new IPAddress(pkt[12..16]);
                    dst   = new IPAddress(pkt[16..20]);

                    break;
                }
                default:
                    goto L2_DEFAULT;
            }

            var child = new PacketRegistry
            {
                Magic = proto,
                Size  = (nuint)(pkt.Length - ihl),
                Data  = (byte*)((nuint)handle.Pointer + (nuint)ihl),
                Next  = null
            };

            var nbReg = Marshal.SizeOf<PacketRegistry>();
            var ptr   = (PacketRegistry*)NativeMemory.Alloc((nuint)nbReg);

            var parent = new PacketRegistry
            {
                Magic = magic,
                Size  = (nuint)ihl,
                Data  = (byte*)handle.Pointer,
                Next  = ptr
            };

            MemoryMarshal.Write(new Span<byte>(ptr, nbReg), in child);

            return new PacketRegistryContainer(handle, parent, src, dst);

            L2_DEFAULT:
            return new PacketRegistryContainer(
                handle,
                new PacketRegistry
                {
                    Magic = 0,
                    Size  = 0,
                    Data  = (byte*)handle.Pointer,
                    Next  = null
                },
                IPAddress.None,
                IPAddress.None);
        }
    }

    public void Dispose()
    {
        Handle.Dispose();

        unsafe
        {
            var q   = new Stack<PacketRegistry>();
            var reg = Registry;

            while (true)
            {
                q.Push(reg);
                if (reg.Next is null)
                    break;
                reg = *reg.Next;
            }

            while (q.TryPop(out reg))
            {
                NativeMemory.Free(reg.Next);
            }
        }
    }
}
