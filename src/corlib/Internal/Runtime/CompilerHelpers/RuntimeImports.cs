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
            var size = pEEType->BaseSize + (uint)length * pEEType->ComponentSize;

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
    }
}