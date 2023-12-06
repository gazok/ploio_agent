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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using Frouros.Shared.Imports;

namespace Frouros.Host.Imports;

[SuppressUnmanagedCodeSecurity]
public static unsafe partial class Native
{
    // ReSharper disable InconsistentNaming
    public const ushort PF_INET  = 2;
    public const ushort PF_INET6 = 10;

    public const byte NFQNL_COPY_PACKET = 2;

    public const uint NF_DROP   = 0;
    public const uint NF_ACCEPT = 1;
    // ReSharper restore InconsistentNaming

    [NativeCppClass, StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct MessagePacketHeader
    {
        public readonly uint PacketId;
        public readonly uint HardwareProtocol;
        public readonly byte Hook;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int NFCallback(void* qh, void* msg, void* nfa, void* data);


    internal static string GetLastError()
    {
        var errno = Marshal.GetLastWin32Error();
        return Marshal.PtrToStringUTF8(strerror(errno)) ?? string.Empty;
    }

    [LibraryImport("netfilter_queue", SetLastError = true)]
    internal static partial void* nfq_open();

    [LibraryImport("netfilter_queue", SetLastError = true)]
    internal static partial int nfq_unbind_pf(
        void*  h,
        ushort pf);

    [LibraryImport("netfilter_queue", SetLastError = true)]
    internal static partial int nfq_bind_pf(
        void*  h,
        ushort pf);

    [LibraryImport("netfilter_queue", SetLastError = true)]
    internal static partial void* nfq_create_queue(
        void*                                             h,
        ushort                                            num,
        [MarshalAs(UnmanagedType.FunctionPtr)] NFCallback cb,
        void*                                             data);

    [LibraryImport("netfilter_queue", SetLastError = true)]
    internal static partial int nfq_set_mode(
        void* qh,
        byte  mode,
        uint  len);

    [LibraryImport("netfilter_queue", SetLastError = true)]
    internal static partial int nfq_fd(void* h);

    [LibraryImport("netfilter_queue", SetLastError = true)]
    internal static partial int nfq_handle_packet(
        void* h,
        byte* buf,
        int   len);

    [LibraryImport("netfilter_queue", SetLastError = true)]
    internal static partial int nfq_destroy_queue(void* qh);

    [LibraryImport("netfilter_queue", SetLastError = true)]
    internal static partial int nfq_close(void* h);

    [LibraryImport("netfilter_queue", SetLastError = true)]
    internal static partial MessagePacketHeader* nfq_get_msg_packet_hdr(void* nfad);

    [LibraryImport("netfilter_queue", SetLastError = true)]
    internal static partial uint nfq_get_indev(void* nfad);

    [LibraryImport("netfilter_queue", SetLastError = true)]
    internal static partial int nfq_get_timestamp(
        void*    nfad,
        Timeval* tv);

    [LibraryImport("netfilter_queue", SetLastError = true)]
    internal static partial int nfq_get_payload(
        void*  nfad,
        byte** data);

    [LibraryImport("netfilter_queue", SetLastError = true)]
    internal static partial int nfq_set_verdict(
        void* qh,
        uint  id,
        uint  verdict,
        uint  dataLen,
        byte* buf);

    // ReSharper disable once InconsistentNaming
    [LibraryImport("libc.so.6", SetLastError = true)]
    private static partial IntPtr strerror(int errnum);

    // ReSharper disable once InconsistentNaming
    [LibraryImport("libc.so.6", SetLastError = true)]
    internal static partial nint recv(
        int   fd,
        byte* buf,
        nuint n,
        int   flags);
}