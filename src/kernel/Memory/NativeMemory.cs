using Kernel.Diagnostics;

namespace Kernel.Memory
{
    public unsafe static class NativeMemory
    {
        // Starting address for dynamic memory allocations
        private static byte* _heapStart;
        private static byte* _heapCurrent;
        private static ulong _heapSize;
        private static ulong _usedSize;
        private static bool _initialized;

        /// <summary>
        /// Initializes the native memory system
        /// </summary>
        /// <param name="heapStart">Starting address of the heap</param>
        /// <param name="heapSize">Size of the heap in bytes</param>
        public static void Initialize(byte* heapStart, ulong heapSize)
        {
            _heapStart = heapStart;
            _heapCurrent = heapStart;
            _heapSize = heapSize;
            _usedSize = 0;
            _initialized = true;
            //SendString.Info($"NativeMemory initialized: address 0x{((ulong)heapStart).ToString("X16")}");
        }

        /// <summary>
        /// Allocates a block of memory of the specified size.
        /// </summary>
        /// <param name="size">Size in bytes to allocate</param>
        /// <returns>Pointer to the start of the allocated block, or null if allocation failed</returns>
        public static void* Alloc(nuint size)
        {
            if (!_initialized)
            {
                //SendString.Error("Error: NativeMemory not initialized. Call Initialize first.");
                return null;
            }

            // Simple implementation: increments a pointer
            // Note: This implementation doesn't handle free memory management

            // Ensure proper alignment (multiple of 8 for 64-bit)
            ulong alignedSize = (ulong)size;
            if (alignedSize % 8 != 0)
                alignedSize = alignedSize + (8 - (alignedSize % 8));

            // Check if there is enough space
            if (_heapCurrent + alignedSize > _heapStart + _heapSize)
            {
                // Not enough memory
                //SendString.Error($"Error: Not enough memory to allocate {alignedSize.ToString()} bytes");
                return null;
            }

            // Save the current pointer to return it
            byte* result = _heapCurrent;

            // Move the pointer forward for the next allocation
            _heapCurrent += alignedSize;

            // Update the used memory counter
            _usedSize += alignedSize;

            // Initialize the memory to zero
            for (ulong i = 0; i < alignedSize; i++)
                result[i] = 0;

            return result;
        }

        /// <summary>
        /// Gets the total size of the heap
        /// </summary>
        public static ulong TotalSize => _heapSize;

        /// <summary>
        /// Gets the amount of memory currently used
        /// </summary>
        public static ulong UsedSize => _usedSize;

        /// <summary>
        /// Gets the amount of free memory available
        /// </summary>
        public static ulong FreeSize => _heapSize - _usedSize;
    }
}
