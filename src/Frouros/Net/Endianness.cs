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

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Frouros.Net;

public static class Endianness
{
    private static void Write<T>(Span<byte> dst, ref long offset, T val) where T : unmanaged
    {
        unsafe
        {
            Unsafe.Write((byte*)Unsafe.AsPointer(ref dst.GetPinnableReference()) + offset, val);
            offset += sizeof(T);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBigEndian(Span<byte> dst, ref long offset, sbyte val)
    {
        Write(dst, ref offset, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBigEndian(Span<byte> dst, ref long offset, byte val)
    {
        Write(dst, ref offset, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBigEndian(Span<byte> dst, ref long offset, short val)
    {
        if (BitConverter.IsLittleEndian)
            val = BinaryPrimitives.ReverseEndianness(val);
        Write(dst, ref offset, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBigEndian(Span<byte> dst, ref long offset, ushort val)
    {
        if (BitConverter.IsLittleEndian)
            val = BinaryPrimitives.ReverseEndianness(val);
        Write(dst, ref offset, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBigEndian(Span<byte> dst, ref long offset, int val)
    {
        if (BitConverter.IsLittleEndian)
            val = BinaryPrimitives.ReverseEndianness(val);
        Write(dst, ref offset, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBigEndian(Span<byte> dst, ref long offset, uint val)
    {
        if (BitConverter.IsLittleEndian)
            val = BinaryPrimitives.ReverseEndianness(val);
        Write(dst, ref offset, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBigEndian(Span<byte> dst, ref long offset, long val)
    {
        if (BitConverter.IsLittleEndian)
            val = BinaryPrimitives.ReverseEndianness(val);
        Write(dst, ref offset, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBigEndian(Span<byte> dst, ref long offset, ulong val)
    {
        if (BitConverter.IsLittleEndian)
            val = BinaryPrimitives.ReverseEndianness(val);
        Write(dst, ref offset, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBigEndian(Span<byte> dst, ref long offset, Int128 val)
    {
        if (BitConverter.IsLittleEndian)
            val = BinaryPrimitives.ReverseEndianness(val);
        Write(dst, ref offset, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBigEndian(Span<byte> dst, ref long offset, UInt128 val)
    {
        if (BitConverter.IsLittleEndian)
            val = BinaryPrimitives.ReverseEndianness(val);
        Write(dst, ref offset, val);
    }
}