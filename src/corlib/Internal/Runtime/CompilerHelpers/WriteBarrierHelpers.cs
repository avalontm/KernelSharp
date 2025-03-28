using System.Runtime;

namespace Internal.Runtime.CompilerHelpers
{
    internal static unsafe class WriteBarrierHelpers
    {
        [RuntimeExport("RhpAssignRefEAX")]
        public static void RhpAssignRefEAX(void** dst, void* src)
        {
            *dst = src;
        }

        [RuntimeExport("RhpAssignRefAny")]
        public static void RhpAssignRefAny(void** dst, void* src)
        {
            *dst = src;
        }

        //[RuntimeExport("RhpAssignRefEBX")]
        public static void RhpAssignRefEBX(void** dst, void* src)
        {
            *dst = src;
        }
    }
}