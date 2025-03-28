namespace Kernel
{
    public static unsafe class SMP
    {
        public const ulong BaseAddress = 0x50000;

        public const ulong APMain = BaseAddress + 0x0;
        public const ulong Stacks = BaseAddress + 0x8;
        public const ulong SharedGDT = BaseAddress + 0x16;
        public const ulong SharedIDT = BaseAddress + 0x24;
        public const ulong SharedPageTable = BaseAddress + 0x1000;
        public const ulong Trampoline = BaseAddress + 0x10000;

        public static ulong NumActivedProcessors = 0;
        public const int StackSizeForEachCPU = 1048576;

    }
}