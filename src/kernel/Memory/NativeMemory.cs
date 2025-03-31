using Kernel.Diagnostics;

namespace Kernel.Memory
{
    public unsafe static class NativeMemory
    {
        // Dirección de inicio para asignaciones de memoria dinámica
        private static byte* _heapStart;
        private static byte* _heapCurrent;
        private static uint _heapSize;
        private static uint _usedSize;
        private static bool _initialized;

        /// <summary>
        /// Inicializa el sistema de memoria nativa
        /// </summary>
        /// <param name="heapStart">Dirección de inicio del heap</param>
        /// <param name="heapSize">Tamaño del heap en bytes</param>
        public static void Initialize(byte* heapStart, uint heapSize)
        {
            _heapStart = heapStart;
            _heapCurrent = heapStart;
            _heapSize = heapSize;
            _usedSize = 0;
            _initialized = true;

            SerialDebug.Info($"NativeMemory inicializado: direccion 0x{((uint)heapStart).ToHexString()}");
        }

        /// <summary>
        /// Asigna un bloque de memoria del tamaño especificado.
        /// </summary>
        /// <param name="size">Tamaño en bytes a asignar</param>
        /// <returns>Puntero al inicio del bloque asignado, o null si no se pudo asignar</returns>
        public static void* Alloc(nuint size)
        {
            if (!_initialized)
            {
                SerialDebug.Error("Error: NativeMemory no inicializado. Llame a Initialize primero.");
                return null;
            }

            // Implementación simple: incrementa un puntero
            // Nota: Esta implementación no tiene gestión de memoria libre

            // Asegurarse de que la alineación sea adecuada (múltiplo de 4)
            uint alignedSize = (uint)size;
            if (alignedSize % 4 != 0)
                alignedSize = alignedSize + (4 - (alignedSize % 4));

            // Verificar si hay suficiente espacio
            if (_heapCurrent + alignedSize > _heapStart + _heapSize)
            {
                // No hay suficiente memoria
                SerialDebug.Error($"Error: No hay suficiente memoria para asignar {alignedSize.ToString()} bytes");
                return null;
            }

            // Guardar el puntero actual para devolverlo
            byte* result = _heapCurrent;

            // Avanzar el puntero para la próxima asignación
            _heapCurrent += alignedSize;

            // Actualizar el contador de memoria usada
            _usedSize += alignedSize;

            // Inicializar la memoria a cero
            for (uint i = 0; i < alignedSize; i++)
                result[i] = 0;

            return result;
        }

        /// <summary>
        /// Obtiene el tamaño total del heap
        /// </summary>
        public static uint TotalSize => _heapSize;

        /// <summary>
        /// Obtiene el tamaño de memoria usada actualmente
        /// </summary>
        public static uint UsedSize => _usedSize;

        /// <summary>
        /// Obtiene el tamaño de memoria libre disponible
        /// </summary>
        public static uint FreeSize => _heapSize - _usedSize;
    }
}