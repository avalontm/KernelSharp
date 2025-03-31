﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;
using System.Runtime;
using System.Runtime.CompilerServices;

using nuint = System.UInt64;

namespace System
{
    public partial class Buffer
    {
        // Non-inlinable wrapper around the QCall that avoids polluting the fast path
        // with P/Invoke prolog/epilog.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static unsafe void _ZeroMemory(ref byte b, uint byteLength)
        {
            fixed (byte* bytePointer = &b)
            {
                MemoryHelpers.MemSet(bytePointer, 0, (int)byteLength);
            }
        }


        public static unsafe void MemCpy(byte* dest, byte* src, uint count)
        {
            for (ulong i = 0; i < count; i++) dest[i] = src[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void __Memmove(byte* dest, byte* src, nuint len) =>
            MemCpy(dest, src, (uint)len);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void __Memmove(ref byte dest, ref byte src, nuint len)
        {
            fixed (byte* pdest = &dest)
            fixed (byte* psrc = &src)
                MemCpy(pdest, psrc, (uint)len);
        }

        // This method has different signature for x64 and other platforms and is done for performance reasons.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Memmove<T>(ref T destination, ref T source, nuint elementCount)
        {
            __Memmove(
                ref Unsafe.As<T, byte>(ref destination),
                ref Unsafe.As<T, byte>(ref source),
                elementCount * (nuint)Unsafe.SizeOf<T>());
        }

    }
}