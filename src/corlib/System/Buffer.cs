// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;
using System.Runtime;
using System.Runtime.CompilerServices;

using nuint = System.UInt64;

namespace System
{
    public unsafe partial class Buffer
    {
        /// <summary>
     /// Copies a specified number of bytes from a source buffer to a destination buffer with write barrier.
     /// </summary>
     /// <param name="source">The source buffer.</param>
     /// <param name="sourceIndex">The zero-based byte offset into the source buffer.</param>
     /// <param name="destination">The destination buffer.</param>
     /// <param name="destinationIndex">The zero-based byte offset into the destination buffer.</param>
     /// <param name="count">The number of bytes to copy.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BulkMoveWithWriteBarrier(
            ref byte source,
            int sourceIndex,
            ref byte destination,
            int destinationIndex,
            int count)
        {
            // Basic validation
            if (count <= 0)
                return;

            // Get pointers to source and destination
            byte* src = (byte*)Unsafe.AsPointer(ref Unsafe.Add(ref source, sourceIndex));
            byte* dest = (byte*)Unsafe.AsPointer(ref Unsafe.Add(ref destination, destinationIndex));

            // Use memory copy helper
            MemoryHelpers.MemCpy(dest, src, (uint)count);
        }
        // Non-inlinable wrapper around the QCall that avoids polluting the fast path
        // with P/Invoke prolog/epilog.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static unsafe void _ZeroMemory(ref byte b, ulong byteLength)
        {
            fixed (byte* bytePointer = &b)
            {
                MemoryHelpers.MemSet(bytePointer, 0, byteLength);
            }
        }


        public static unsafe void MemCpy(byte* dest, byte* src, ulong count)
        {
            MemoryHelpers.MemCpy(dest, src, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void __Memmove(byte* dest, byte* src, nuint len) =>
            MemCpy(dest, src, (ulong)len);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void __Memmove(ref byte dest, ref byte src, nuint len)
        {
            fixed (byte* pdest = &dest)
            fixed (byte* psrc = &src)
                MemCpy(pdest, psrc, (ulong)len);
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