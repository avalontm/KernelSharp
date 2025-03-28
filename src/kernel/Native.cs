using System.Runtime.InteropServices;

namespace Kernel
{
    static unsafe class Native
    {
        [DllImport("*", EntryPoint = "_Movsb")]
        public static extern unsafe void Movsb(void* dest, void* source, ulong count);

        [DllImport("*", EntryPoint = "_Stosb")]
        public static extern unsafe void Stosb(void* p, byte value, ulong count);
        
        [DllImport("*", EntryPoint = "_Invlpg")]
        public static extern unsafe void Invlpg(ulong physicalAddress);

        [DllImport("*", EntryPoint = "_CPU_WriteCR3")]
        internal static extern unsafe void WriteCR3(ulong value);
    }
}
