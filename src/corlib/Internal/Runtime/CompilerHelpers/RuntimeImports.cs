using Internal.Runtime.CompilerServices;
using System;
using System.Runtime;

namespace Internal.Runtime.CompilerHelpers
{
    internal unsafe class RuntimeImports
    {
        [RuntimeExport("RhpNewArray")]
        public static unsafe object RhpNewArray(EEType* pEEType, int length)
        {
            var size = pEEType->BaseSize + (ulong)length * pEEType->ComponentSize;

            // Round to next power of 8
            if (size % 8 > 0)
                size = (size / 8 + 1) * 8;

            var data = MemoryHelpers.Malloc(size);
            var obj = Unsafe.As<IntPtr, object>(ref data);
            MemoryHelpers.MemSet((byte*)data, 0, size);
            *(IntPtr*)data = (IntPtr)pEEType;

            var b = (byte*)data;
            b += sizeof(IntPtr);
            MemoryHelpers.MemCpy(b, (byte*)&length, sizeof(int));

            return obj;
        }

        [RuntimeExport("RhpCheckedMultiply")]
        public static unsafe int RhpCheckedMultiply(int a, int b)
        {
            long result = (long)a * (long)b;
            if (result > int.MaxValue || result < int.MinValue)
                ThrowHelpers.ThrowOverflowException("Multiplication overflow");
            return (int)result;
        }

        [RuntimeExport("RhpCheckedMultiply64")]
        public static unsafe long RhpCheckedMultiply64(long a, long b)
        {
            // Simple overflow check for multiplication
            if (a > 0 && b > 0 && a > long.MaxValue / b)
                ThrowHelpers.ThrowOverflowException();

            if (a < 0 && b < 0 && a < long.MaxValue / b)
                ThrowHelpers.ThrowOverflowException();

            if (a > 0 && b < 0 && b < long.MinValue / a)
                ThrowHelpers.ThrowOverflowException();

            if (a < 0 && b > 0 && a < long.MinValue / b)
                ThrowHelpers.ThrowOverflowException();

            return a * b;
        }
    }
}