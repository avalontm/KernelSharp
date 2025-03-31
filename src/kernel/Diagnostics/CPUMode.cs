using System.Runtime.InteropServices;

namespace Kernel.Diagnostics
{
    /// <summary>
    /// Clase de utilidad para obtener información sobre el modo del CPU
    /// </summary>
    public static class CPUMode
    {
        /// <summary>
        /// Determina si el CPU está en modo protegido
        /// </summary>
        /// <returns>true si está en modo protegido, false si está en modo real</returns>
        public static bool IsInProtectedMode()
        {
            uint cr0 = GetCR0Register();

            // El bit 0 del registro CR0 es el bit PE (Protected Mode Enable)
            return (cr0 & 1) != 0;
        }

        /// <summary>
        /// Determina si la paginación está habilitada
        /// </summary>
        /// <returns>true si la paginación está habilitada</returns>
        public static bool IsPagingEnabled()
        {
            uint cr0 = GetCR0Register();

            // El bit 31 del registro CR0 es el bit PG (Paging)
            return (cr0 & 0x80000000) != 0;
        }

        /// <summary>
        /// Obtiene el valor del registro CR0
        /// </summary>
        [DllImport("*", EntryPoint = "_GetCR0")]
        private static extern uint GetCR0Register();

        /// <summary>
        /// Obtiene el valor del registro CR3 (directorio de páginas)
        /// </summary>
        [DllImport("*", EntryPoint = "_GetCR3")]
        public static extern uint GetCR3Register();
    }
}