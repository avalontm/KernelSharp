using System;
using System.Runtime.CompilerServices;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// Clase de ayuda para información de excepciones en tiempo de ejecución
    /// </summary>
    internal static class EHInfo
    {
        // Marcadores de información de excepciones para diferentes símbolos

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void EHInfoAllocatorFree() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void EHInfoAllocatorAllocate() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void EHInfoSchedulerYield() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void EHInfoDriverManagerRegisterDriver() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void EHInfoMonitorTryEnter() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void EHInfoMonitorExit() { }

        // Método auxiliar para registrar símbolos de información de excepciones
        public static void RegisterEHInfoSymbols()
        {
            // Llamar a cada método para asegurar que los símbolos estén presentes
            EHInfoAllocatorFree();
            EHInfoAllocatorAllocate();
            EHInfoSchedulerYield();
            EHInfoDriverManagerRegisterDriver();
            EHInfoMonitorTryEnter();
            EHInfoMonitorExit();
        }
    }
}