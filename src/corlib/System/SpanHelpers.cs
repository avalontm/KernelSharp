﻿using Internal.Runtime.CompilerServices;

#pragma warning disable SA1121 // explicitly using type aliases instead of built-in types
using nuint = System.UInt64;

namespace System
{
    internal static partial class SpanHelpers
    {
        public static unsafe void ClearWithoutReferences(ref byte b, nuint byteLength)
        {
            if (byteLength == 0)
                return;

            // TODO: Optimize other platforms to be on par with AMD64 CoreCLR
            // Note: It's important that this switch handles lengths at least up to 22.
            // See notes below near the main loop for why.

            // The switch will be very fast since it can be implemented using a jump
            // table in assembly. See http://stackoverflow.com/a/449297/4077294 for more info.

            switch (byteLength)
            {
                case 1:
                    b = 0;
                    return;
                case 2:
                    Unsafe.As<byte, short>(ref b) = 0;
                    return;
                case 3:
                    Unsafe.As<byte, short>(ref b) = 0;
                    Unsafe.Add<byte>(ref b, 2) = 0;
                    return;
                case 4:
                    Unsafe.As<byte, int>(ref b) = 0;
                    return;
                case 5:
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.Add<byte>(ref b, 4) = 0;
                    return;
                case 6:
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    return;
                case 7:
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    Unsafe.Add<byte>(ref b, 6) = 0;
                    return;
                case 8:
                    Unsafe.As<byte, long>(ref b) = 0;

                    return;
                case 9:
                    Unsafe.As<byte, long>(ref b) = 0;

                    Unsafe.Add<byte>(ref b, 8) = 0;
                    return;
                case 10:
                    Unsafe.As<byte, long>(ref b) = 0;

                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    return;
                case 11:
                    Unsafe.As<byte, long>(ref b) = 0;

                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.Add<byte>(ref b, 10) = 0;
                    return;
                case 12:
                    Unsafe.As<byte, long>(ref b) = 0;

                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    return;
                case 13:
                    Unsafe.As<byte, long>(ref b) = 0;

                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.Add<byte>(ref b, 12) = 0;
                    return;
                case 14:
                    Unsafe.As<byte, long>(ref b) = 0;

                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
                    return;
                case 15:
                    Unsafe.As<byte, long>(ref b) = 0;

                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
                    Unsafe.Add<byte>(ref b, 14) = 0;
                    return;
                case 16:
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    return;
                case 17:
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;

                    Unsafe.Add<byte>(ref b, 16) = 0;
                    return;
                case 18:
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;

                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 16)) = 0;
                    return;
                case 19:
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;

                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 16)) = 0;
                    Unsafe.Add<byte>(ref b, 18) = 0;
                    return;
                case 20:
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;

                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 16)) = 0;
                    return;
                case 21:
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;

                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 16)) = 0;
                    Unsafe.Add<byte>(ref b, 20) = 0;
                    return;
                case 22:
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;

                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 16)) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 20)) = 0;
                    return;
            }

            // P/Invoke into the native version for large lengths
            if (byteLength >= 512) goto PInvoke;

            nuint i = 0; // byte offset at which we're copying

            if (((nuint)Unsafe.AsPointer(ref b) & 3) != 0)
            {
                if (((nuint)Unsafe.AsPointer(ref b) & 1) != 0)
                {
                    b = 0;
                    i += 1;
                    if (((nuint)Unsafe.AsPointer(ref b) & 2) != 0)
                        goto IntAligned;
                }
                Unsafe.As<byte, short>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                i += 2;
            }

        IntAligned:

            // On 64-bit IntPtr.Size == 8, so we want to advance to the next 8-aligned address. If
            // (int)b % 8 is 0, 5, 6, or 7, we will already have advanced by 0, 3, 2, or 1
            // bytes to the next aligned address (respectively), so do nothing. On the other hand,
            // if it is 1, 2, 3, or 4 we will want to copy-and-advance another 4 bytes until
            // we're aligned.
            // The thing 1, 2, 3, and 4 have in common that the others don't is that if you
            // subtract one from them, their 3rd lsb will not be set. Hence, the below check.

            if ((((nuint)Unsafe.AsPointer(ref b) - 1) & 4) == 0)
            {
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                i += 4;
            }

            nuint end = byteLength - 16;
            byteLength -= i; // lower 4 bits of byteLength represent how many bytes are left *after* the unrolled loop

            // We know due to the above switch-case that this loop will always run 1 iteration; max
            // bytes we clear before checking is 23 (7 to align the pointers, 16 for 1 iteration) so
            // the switch handles lengths 0-22.
            //Debug.Assert(end >= 7 && i <= end);

            // This is separated out into a different variable, so the i + 16 addition can be
            // performed at the start of the pipeline and the loop condition does not have
            // a dependency on the writes.
            nuint counter;

            do
            {
                counter = i + 16;

                // This loop looks very costly since there appear to be a bunch of temporary values
                // being created with the adds, but the jit (for x86 anyways) will convert each of
                // these to use memory addressing operands.

                // So the only cost is a bit of code size, which is made up for by the fact that
                // we save on writes to b.

                Unsafe.As<byte, long>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                Unsafe.As<byte, long>(ref Unsafe.AddByteOffset<byte>(ref b, i + 8)) = 0;

                i = counter;

                // See notes above for why this wasn't used instead
                // i += 16;
            }
            while (counter <= end);

            if ((byteLength & 8) != 0)
            {
                Unsafe.As<byte, long>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;

                i += 8;
            }
            if ((byteLength & 4) != 0)
            {
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                i += 4;
            }
            if ((byteLength & 2) != 0)
            {
                Unsafe.As<byte, short>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                i += 2;
            }
            if ((byteLength & 1) != 0)
            {
                Unsafe.AddByteOffset<byte>(ref b, i) = 0;
                // We're not using i after this, so not needed
                // i += 1;
            }

            return;

        PInvoke:
            Buffer._ZeroMemory(ref b, byteLength);
        }

        public static unsafe void ClearWithReferences(ref IntPtr ip, nuint pointerSizeLength)
        {
            //Debug.Assert((int)Unsafe.AsPointer(ref ip) % sizeof(IntPtr) == 0, "Should've been aligned on natural word boundary.");

            // First write backward 8 natural words at a time.
            // Writing backward allows us to get away with only simple modifications to the
            // mov instruction's base and index registers between loop iterations.

            for (; pointerSizeLength >= 8; pointerSizeLength -= 8)
            {
                Unsafe.Add(ref Unsafe.Add(ref ip, (IntPtr)pointerSizeLength), -1) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (IntPtr)pointerSizeLength), -2) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (IntPtr)pointerSizeLength), -3) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (IntPtr)pointerSizeLength), -4) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (IntPtr)pointerSizeLength), -5) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (IntPtr)pointerSizeLength), -6) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (IntPtr)pointerSizeLength), -7) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (IntPtr)pointerSizeLength), -8) = default;
            }

            //Debug.Assert(pointerSizeLength <= 7);

            // The logic below works by trying to minimize the number of branches taken for any
            // given range of lengths. For example, the lengths [ 4 .. 7 ] are handled by a single
            // branch, [ 2 .. 3 ] are handled by a single branch, and [ 1 ] is handled by a single
            // branch.
            //
            // We can write both forward and backward as a perf improvement. For example,
            // the lengths [ 4 .. 7 ] can be handled by zeroing out the first four natural
            // words and the last 3 natural words. In the best case (length = 7), there are
            // no overlapping writes. In the worst case (length = 4), there are three
            // overlapping writes near the middle of the buffer. In perf testing, the
            // penalty for performing duplicate writes is less expensive than the penalty
            // for complex branching.

            if (pointerSizeLength >= 4)
            {
                goto Write4To7;
            }
            else if (pointerSizeLength >= 2)
            {
                goto Write2To3;
            }
            else if (pointerSizeLength > 0)
            {
                goto Write1;
            }
            else
            {
                return; // nothing to write
            }

        Write4To7:
            //Debug.Assert(pointerSizeLength >= 4);

            // Write first four and last three.
            Unsafe.Add(ref ip, 2) = default;
            Unsafe.Add(ref ip, 3) = default;
            Unsafe.Add(ref Unsafe.Add(ref ip, (IntPtr)pointerSizeLength), -3) = default;
            Unsafe.Add(ref Unsafe.Add(ref ip, (IntPtr)pointerSizeLength), -2) = default;

        Write2To3:
            //Debug.Assert(pointerSizeLength >= 2);

            // Write first two and last one.
            Unsafe.Add(ref ip, 1) = default;
            Unsafe.Add(ref Unsafe.Add(ref ip, (IntPtr)pointerSizeLength), -1) = default;

        Write1:
            //Debug.Assert(pointerSizeLength >= 1);

            // Write only element.
            ip = default;
        }

        /// <summary>
        /// Moves a specified number of bytes from a source block of memory to a destination block of memory.
        /// </summary>
        /// <param name="destination">A pointer to the destination block of memory to copy to.</param>
        /// <param name="source">A pointer to the source block of memory to copy from.</param>
        /// <param name="byteCount">The number of bytes to copy.</param>
        public static unsafe void Memmove(ref byte destination, ref byte source, nuint byteCount)
        {
            if (byteCount == 0)
                return;

            // Get pointers to source and destination
            byte* dest = (byte*)Unsafe.AsPointer(ref destination);
            byte* src = (byte*)Unsafe.AsPointer(ref source);

            // Handle small copies with direct assignment
            if (byteCount <= 22)
            {
                switch (byteCount)
                {
                    case 1:
                        dest[0] = src[0];
                        return;
                    case 2:
                        *((short*)dest) = *((short*)src);
                        return;
                    case 3:
                        *((short*)dest) = *((short*)src);
                        dest[2] = src[2];
                        return;
                    case 4:
                        *((int*)dest) = *((int*)src);
                        return;
                    case 5:
                        *((int*)dest) = *((int*)src);
                        dest[4] = src[4];
                        return;
                    case 6:
                        *((int*)dest) = *((int*)src);
                        *((short*)(dest + 4)) = *((short*)(src + 4));
                        return;
                    case 7:
                        *((int*)dest) = *((int*)src);
                        *((short*)(dest + 4)) = *((short*)(src + 4));
                        dest[6] = src[6];
                        return;
                    case 8:
                        *((long*)dest) = *((long*)src);
                        return;
                    // ... (add cases 9-22 following similar pattern as in ClearWithoutReferences)
                    default:
                        // For larger small copies, do manual byte-by-byte copy
                        for (nuint i = 0; i < byteCount; i++)
                        {
                            dest[i] = src[i];
                        }
                        return;
                }
            }

            // For larger copies, handle potential overlap
            if (dest < src)
            {
                // Copy from low to high addresses
                for (nuint i = 0; i < byteCount; i++)
                {
                    dest[i] = src[i];
                }
            }
            else if (dest > src)
            {
                // Copy from high to low addresses to prevent overwriting source
                for (nuint i = byteCount; i > 0; i--)
                {
                    dest[i - 1] = src[i - 1];
                }
            }
        }

        /// <summary>
        /// Moves a specified number of pointers from a source block of memory to a destination block of memory.
        /// </summary>
        /// <param name="destination">A pointer to the destination block of memory to copy to.</param>
        /// <param name="source">A pointer to the source block of memory to copy from.</param>
        /// <param name="pointerSizeLength">The number of pointers to copy.</param>
        public static unsafe void Memmove(ref IntPtr destination, ref IntPtr source, nuint pointerSizeLength)
        {
            if (pointerSizeLength == 0)
                return;

            // Get pointers to source and destination
            IntPtr* dest = (IntPtr*)Unsafe.AsPointer(ref destination);
            IntPtr* src = (IntPtr*)Unsafe.AsPointer(ref source);

            // Simple forward copy for small lengths
            if (pointerSizeLength <= 8)
            {
                for (nuint i = 0; i < pointerSizeLength; i++)
                {
                    dest[i] = src[i];
                }
                return;
            }

            // For larger copies, handle potential overlap
            if (dest < src)
            {
                // Copy from low to high addresses
                for (nuint i = 0; i < pointerSizeLength; i++)
                {
                    dest[i] = src[i];
                }
            }
            else if (dest > src)
            {
                // Copy from high to low addresses to prevent overwriting source
                for (nuint i = pointerSizeLength; i > 0; i--)
                {
                    dest[i - 1] = src[i - 1];
                }
            }
        }

        /// <summary>
        /// Fills a block of memory with a specific byte value.
        /// </summary>
        /// <param name="dest">Pointer to the destination memory block.</param>
        /// <param name="value">Byte value to fill the memory with.</param>
        /// <param name="byteCount">Number of bytes to fill.</param>
        public static unsafe void Fill(ref byte dest, byte value, nuint byteCount)
        {
            if (byteCount == 0)
                return;

            byte* destPtr = (byte*)Unsafe.AsPointer(ref dest);

            // Handle small fills with direct assignment
            if (byteCount <= 16)
            {
                switch (byteCount)
                {
                    case 1:
                        destPtr[0] = value;
                        return;
                    case 2:
                        *((short*)destPtr) = (short)((value << 8) | value);
                        return;
                    case 3:
                        *((short*)destPtr) = (short)((value << 8) | value);
                        destPtr[2] = value;
                        return;
                    case 4:
                        *((int*)destPtr) = (int)((value << 24) | (value << 16) | (value << 8) | value);
                        return;
                    case 5:
                        *((int*)destPtr) = (int)((value << 24) | (value << 16) | (value << 8) | value);
                        destPtr[4] = value;
                        return;
                    case 6:
                        *((int*)destPtr) = (int)((value << 24) | (value << 16) | (value << 8) | value);
                        *((short*)(destPtr + 4)) = (short)((value << 8) | value);
                        return;
                    case 7:
                        *((int*)destPtr) = (int)((value << 24) | (value << 16) | (value << 8) | value);
                        *((short*)(destPtr + 4)) = (short)((value << 8) | value);
                        destPtr[6] = value;
                        return;
                    case 8:
                        *((long*)destPtr) = (long)((ulong)value * 0x0101010101010101UL);
                        return;
                    default:
                        // For 9-16 bytes, do manual fill
                        for (nuint a = 0; a < byteCount; a++)
                        {
                            destPtr[a] = value;
                        }
                        return;
                }
            }

            // For larger fills, use repeated long/int writes
            long longValue = (long)((ulong)value * 0x0101010101010101UL);
            nuint i = 0;
            nuint end = byteCount - 8;

            // Fill 8 bytes at a time
            while (i <= end)
            {
                *((long*)(destPtr + i)) = longValue;
                i += 8;
            }

            // Fill remaining bytes
            for (; i < byteCount; i++)
            {
                destPtr[i] = value;
            }
        }

        /// <summary>
        /// Fills a block of memory with a specific pointer value.
        /// </summary>
        /// <param name="dest">Pointer to the destination memory block.</param>
        /// <param name="value">Pointer value to fill the memory with.</param>
        /// <param name="pointerSizeLength">Number of pointers to fill.</param>
        public static unsafe void Fill(ref IntPtr dest, IntPtr value, nuint pointerSizeLength)
        {
            if (pointerSizeLength == 0)
                return;

            IntPtr* destPtr = (IntPtr*)Unsafe.AsPointer(ref dest);

            // For small fills, do direct assignment
            if (pointerSizeLength <= 8)
            {
                for (nuint b = 0; b < pointerSizeLength; b++)
                {
                    destPtr[b] = value;
                }
                return;
            }

            // For larger fills, use repeated writes
            nuint i = 0;
            nuint end = pointerSizeLength - 8;

            // Fill 8 pointers at a time
            while (i <= end)
            {
                destPtr[i] = value;
                destPtr[i + 1] = value;
                destPtr[i + 2] = value;
                destPtr[i + 3] = value;
                destPtr[i + 4] = value;
                destPtr[i + 5] = value;
                destPtr[i + 6] = value;
                destPtr[i + 7] = value;
                i += 8;
            }

            // Fill remaining pointers
            for (; i < pointerSizeLength; i++)
            {
                destPtr[i] = value;
            }
        }
    }
}