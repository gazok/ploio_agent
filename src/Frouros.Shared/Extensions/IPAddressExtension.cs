using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using Frouros.Shared.Reflection;
using Google.Protobuf;

namespace Frouros.Shared.Extensions;

public static class IPAddressExtension
{
    private const string IPv6Field = "_numbers";
    private const string IPv4Field = "_addressOrScopeId";

    private static readonly Func<IPAddress, ushort[]> IPv6Getter;
    private static readonly Func<IPAddress, uint>     IPv4Getter;

    static IPAddressExtension()
    {
        IPv6Getter =
            (typeof(IPAddress).GetField(IPv6Field, BindingFlags.Instance | BindingFlags.NonPublic)
             ?? throw new MissingFieldException(nameof(IPAddress), IPv6Field))
           .CreateSetter<IPAddress, ushort[]>();

        IPv4Getter =
            (typeof(IPAddress).GetField(IPv4Field, BindingFlags.Instance | BindingFlags.NonPublic)
             ?? throw new MissingFieldException(nameof(IPAddress), IPv4Field))
           .CreateSetter<IPAddress, uint>();
    }

    public static ByteString ToByteString(this IPAddress addr)
    {
        Span<byte> dat;

        if (addr.AddressFamily == AddressFamily.InterNetwork)
        {
            var raw = IPv4Getter(addr);
            unsafe
            {
                dat = new Span<byte>(Unsafe.AsPointer(ref raw), 4);
            }
        }
        else
        {
            var raw = IPv6Getter(addr);
            unsafe
            {
                dat = new Span<byte>(Unsafe.AsPointer(ref raw), 16);
            }
        }

        return ByteString.CopyFrom(dat);
    }
}