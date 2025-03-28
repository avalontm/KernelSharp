using System;
using System.Runtime;
using Internal.Runtime;
using Internal.Runtime.CompilerServices;

namespace Internal.Runtime.CompilerHelpers
{
    internal static unsafe class WriteBarrierHelpersExpanded
    {
        [RuntimeExport("RhpAssignRefECX")]
        public static void RhpAssignRefECX(void** dst, void* src)
        {
            // Esta función asigna una referencia a un slot de memoria
            // Es similar a RhpAssignRefEAX pero usa el registro ECX en lugar de EAX
            // En una implementación real de GC, aquí se realizarían verificaciones de barreras de escritura
            *dst = src;
        }

        // Añade más implementaciones para los demás registros que podrían ser necesarios
        [RuntimeExport("RhpAssignRefEBX")]
        public static void RhpAssignRefEBX(void** dst, void* src)
        {
            *dst = src;
        }

        [RuntimeExport("RhpAssignRefEDX")]
        public static void RhpAssignRefEDX(void** dst, void* src)
        {
            *dst = src;
        }

        [RuntimeExport("RhpAssignRefESI")]
        public static void RhpAssignRefESI(void** dst, void* src)
        {
            *dst = src;
        }

        [RuntimeExport("RhpAssignRefEDI")]
        public static void RhpAssignRefEDI(void** dst, void* src)
        {
            *dst = src;
        }
    }
}