using System;

namespace System.Runtime
{
    /// <summary>
    /// Información de arquitectura para detección en tiempo de ejecución
    /// </summary>
    public static class RuntimeArchitecture
    {
        // Propiedades para detección de arquitectura
        public static bool Is32Bit => IntPtr.Size == 4;
        public static bool Is64Bit => IntPtr.Size == 8;

        // Obtener tamaño de puntero en bytes
        public static int PointerSize => IntPtr.Size;

        // Obtener máximo número de bits 
        public static int MaxBits => IntPtr.Size * 8;
    }
}
