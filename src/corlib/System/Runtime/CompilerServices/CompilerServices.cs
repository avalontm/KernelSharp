using System;

namespace System.Runtime.CompilerServices
{
    public unsafe class RuntimeHelpers
    {
        public static unsafe int OffsetToStringData => sizeof(IntPtr) + sizeof(int);

    }
}