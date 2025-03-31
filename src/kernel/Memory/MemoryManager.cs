using Kernel.Boot;
using Kernel.Diagnostics;

namespace Kernel.Memory
{
    /// <summary>
    /// Gestor de memoria del sistema
    /// </summary>
    public static unsafe class MemoryManager
    {
        // Constantes para gestión de memoria
        private const uint KERNEL_BASE_ADDRESS = 0x100000;      // 1MB, dirección base del kernel
        private const uint KERNEL_HEAP_ADDRESS = 0x400000;      // 4MB, dirección base del heap del kernel
        private const uint INITIAL_HEAP_SIZE = 0x400000;        // 4MB de tamaño inicial para el heap
        private const uint DEFAULT_MEMORY_SIZE = 16 * 1024 * 1024;  // 16MB de memoria por defecto

        // Información de memoria física
        private static uint _totalMemory;                       // Memoria total en bytes
        private static uint _usedMemory;                        // Memoria usada en bytes

        /// <summary>
        /// Inicializa el sistema de gestión de memoria con valores predeterminados
        /// </summary>
        public static void Initialize(MultibootInfo* multibootInfo = null)
        {
            
            SerialDebug.Info("Inicializando sistema de gestion de memoria...");

            if (multibootInfo != null && (multibootInfo->Flags & MultibootFlags.MEMORY) != 0)
            {
                // Obtener memoria desde la información Multiboot
                //uint memLowKB = multibootInfo->MemLow;
                //uint memHighKB = multibootInfo->MemHigh;

                // Memoria total = memoria baja + memoria alta
                //_totalMemory = (memLowKB + memHighKB) * 1024;

                //SerialDebug.Info($"Memoria detectada por Multiboot: {memLowKB.ToString()}KB baja + {memHighKB.ToString()}KB alta");
            }
            else
            {
                // Usar valor predeterminado si no hay información Multiboot
                _totalMemory = DEFAULT_MEMORY_SIZE;
                SerialDebug.Warning("Usando valor predeterminado de memoria: 16MB");
            }

            // Calcular la memoria usada inicialmente (hasta el inicio del heap)
            _usedMemory = KERNEL_HEAP_ADDRESS;

            SerialDebug.Info($"Memoria total detectada: {(_totalMemory / 1024 / 1024).ToString()}MB");
            SerialDebug.Info($"Memoria disponible: {((_totalMemory - _usedMemory) / 1024 / 1024).ToString()}MB");

            // Inicializar el heap
            InitializeHeap();

            // Imprimir información de memoria
            PrintMemoryInfo();

            SerialDebug.Info("Sistema de gestion de memoria inicializado correctamente.");
        }

        /// <summary>
        /// Inicializa el heap del kernel para la asignación dinámica de memoria
        /// </summary>
        private static void InitializeHeap()
        {
            uint heapSize = INITIAL_HEAP_SIZE;

            // Asegurarnos de que no excedamos la memoria disponible
            if (KERNEL_HEAP_ADDRESS + heapSize > _totalMemory)
            {
                // Ajustar el tamaño si es necesario
                heapSize = _totalMemory - KERNEL_HEAP_ADDRESS;
                SerialDebug.Warning("Tamaño de heap ajustado a " + (heapSize / 1024 / 1024).ToString() + " MB debido a la memoria limitada.");

            }

            // Actualizar la memoria usada
            _usedMemory += heapSize;

            // Inicializar el administrador de memoria nativa
            NativeMemory.Initialize((byte*)KERNEL_HEAP_ADDRESS, heapSize);

            //SerialDebug.Info($"Heap inicializado en 0x{KERNEL_HEAP_ADDRESS.ToHexString()}, tamaño: {(heapSize / 1024 / 1024).ToString()}MB");
            SerialDebug.Info("InitializeHeap");
        }

        /// <summary>
        /// Imprime información sobre la memoria del sistema
        /// </summary>
        private static void PrintMemoryInfo()
        {
            SerialDebug.Info("===== INFORMACION DE MEMORIA =====");
            //SerialDebug.Info($"Memoria total: {(_totalMemory / 1024 / 1024).ToString()}MB ({(_totalMemory / 1024).ToString()}KB)");
            //SerialDebug.Info($"Memoria usada: {(_usedMemory / 1024 / 1024).ToString()}MB ({(_usedMemory / 1024).ToString()}KB)");
            //SerialDebug.Info($"Memoria libre: {((_totalMemory - _usedMemory) / 1024 / 1024).ToString()}MB ({((_totalMemory - _usedMemory) / 1024).ToString()}KB)");
            //SerialDebug.Info($"Base del kernel: 0x{KERNEL_BASE_ADDRESS.ToString()}");
            // Console.WriteLine($"Base del heap: 0x{KERNEL_HEAP_ADDRESS.ToHexString()}");
            //SerialDebug.Info($"Tamaño del heap: {(INITIAL_HEAP_SIZE / 1024 / 1024).ToString()}MB");
            SerialDebug.Info("===============================");
        }

        /// <summary>
        /// Reserva un bloque de memoria del tamaño especificado
        /// </summary>
        public static void* Allocate(nuint size)
        {
            return NativeMemory.Alloc(size);
        }

        /// <summary>
        /// Obtiene la cantidad total de memoria en el sistema
        /// </summary>
        public static uint TotalMemory => _totalMemory;

        /// <summary>
        /// Obtiene la cantidad de memoria usada actualmente
        /// </summary>
        public static uint UsedMemory => _usedMemory + NativeMemory.UsedSize;

        /// <summary>
        /// Obtiene la cantidad de memoria libre disponible
        /// </summary>
        public static uint FreeMemory => _totalMemory - UsedMemory;
    }
}