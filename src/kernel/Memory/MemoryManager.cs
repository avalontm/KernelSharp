using Kernel.Boot;
using Kernel.Diagnostics;
using System;

namespace Kernel.Memory
{
    /// <summary>
    /// System memory manager for 64 bits
    /// </summary>
    public static unsafe class MemoryManager
    {
        // Constants for memory management (now with 64-bit addresses)
        private const ulong KERNEL_BASE_ADDRESS = 0x100000;      // 1MB, base address of the kernel
        private const ulong KERNEL_HEAP_ADDRESS = 0x400000;      // 4MB, base address of the kernel heap
        private const ulong INITIAL_HEAP_SIZE = 0x400000;        // 4MB initial size for the heap
        private const ulong DEFAULT_MEMORY_SIZE = 16UL * 1024UL * 1024UL;  // 16MB default memory size

        // Physical memory information
        private static ulong _totalMemory;                       // Total memory in bytes
        private static ulong _usedMemory;                        // Used memory in bytes

        /// <summary>
        /// Initializes the memory management system with default values
        /// </summary>
        public static void Initialize(MultibootInfo* multibootInfo = null)
        {
            Console.WriteLine("Initializing memory management system...");

            if (multibootInfo != null && (multibootInfo->Flags & MultibootFlags.MEMORY) != 0)
            {
                // Get memory from the Multiboot information
                ulong memLowKB = multibootInfo->MemLow;
                ulong memHighKB = multibootInfo->MemHigh;
                // total = low memory + high memory
                _totalMemory = (memLowKB + memHighKB) * 1024UL;
            }
            else
            {
                // Use default value if no Multiboot information is available
                _totalMemory = DEFAULT_MEMORY_SIZE;
                Console.WriteLine("Using default memory size: 16MB");
            }

            // Calculate the initially used memory (up to the start of the heap)
            _usedMemory = KERNEL_HEAP_ADDRESS;

            Console.WriteLine($"Total detected memory: {(_totalMemory / 1024UL / 1024UL).ToString()}MB");
            Console.WriteLine($"Available memory: {((_totalMemory - _usedMemory) / 1024UL / 1024UL).ToString()}MB");

            // Initialize the heap
            InitializeHeap();

            Console.WriteLine("Memory management system initialized successfully.");
        }

        /// <summary>
        /// Initializes the kernel heap for dynamic memory allocation
        /// </summary>
        private static void InitializeHeap()
        {
            ulong heapSize = INITIAL_HEAP_SIZE;

            // Ensure we don't exceed available memory
            if (KERNEL_HEAP_ADDRESS + heapSize > _totalMemory)
            {
                // Adjust the size if necessary
                heapSize = _totalMemory - KERNEL_HEAP_ADDRESS;
                //SendString.Warning("Heap size adjusted to " + (heapSize / 1024UL / 1024UL).ToString() + " MB due to limited memory.");
            }

            // Update the used memory
            _usedMemory += heapSize;

            // Initialize the native memory manager
            NativeMemory.Initialize((byte*)KERNEL_HEAP_ADDRESS, heapSize);

            //SendString.Info($"Heap initialized at 0x{KERNEL_HEAP_ADDRESS.ToString("X")}, size: {(heapSize / 1024UL / 1024UL).ToString()}MB");
            //SendString.Info("InitializeHeap");
        }

        /// <summary>
        /// Reserves a block of memory of the specified size
        /// </summary>
        public static void* Allocate(nuint size)
        {
            return NativeMemory.Alloc(size);
        }

        /// <summary>
        /// Gets the total amount of memory in the system
        /// </summary>
        public static ulong TotalMemory => _totalMemory;

        /// <summary>
        /// Gets the amount of memory currently used
        /// </summary>
        public static ulong UsedMemory => _usedMemory + NativeMemory.UsedSize;

        /// <summary>
        /// Gets the amount of free memory available
        /// </summary>
        public static ulong FreeMemory => _totalMemory - UsedMemory;
    }
}
