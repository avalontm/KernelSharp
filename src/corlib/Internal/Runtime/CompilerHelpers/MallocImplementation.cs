using System;
using System.Runtime.InteropServices;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// Implementación nativa de malloc en C# para sistemas que aún no tienen
    /// soporte para la versión en ensamblador
    /// </summary>
    public unsafe static class MallocImplementation
    {
        // Tamaño del heap (1MB)
        private const uint HEAP_SIZE = 1024 * 1024;

        // Estructura para un nodo de memoria
        private struct MemoryBlock
        {
            public uint Size;          // Tamaño del bloque (incluyendo este encabezado)
            public uint IsFree;        // 1 si está libre, 0 si está asignado
            public MemoryBlock* Next;  // Puntero al siguiente bloque
        }

        // Puntero al inicio del heap
        private static byte* s_heapStart;

        // Puntero al primer bloque de memoria libre
        private static MemoryBlock* s_freeList;

        // Indica si el sistema de memoria ha sido inicializado
        private static bool s_initialized = false;

        /// <summary>
        /// Inicializa el sistema de memoria
        /// </summary>
        public static void Initialize()
        {
            if (s_initialized)
                return;

            // Asignar un buffer estático para el heap
            byte[] heapBuffer = new byte[HEAP_SIZE];
            fixed (byte* heapPtr = heapBuffer)
            {
                s_heapStart = heapPtr;

                // Crear el primer bloque libre (todo el heap)
                s_freeList = (MemoryBlock*)s_heapStart;
                s_freeList->Size = HEAP_SIZE;
                s_freeList->IsFree = 1;
                s_freeList->Next = null;

                s_initialized = true;
            }
        }

        /// <summary>
        /// Asigna un bloque de memoria del tamaño especificado
        /// </summary>
        public static nint Malloc(ulong size)
        {
            if (!s_initialized)
                Initialize();

            if (size == 0)
                return IntPtr.Zero;

            // Calculamos el tamaño total necesario, incluyendo el encabezado
            uint totalSize = (uint)size + (uint)sizeof(MemoryBlock);

            // Alineamos a 8 bytes
            totalSize = (totalSize + 7) & ~7u;

            // Buscamos un bloque libre lo suficientemente grande
            MemoryBlock* current = s_freeList;
            MemoryBlock* previous = null;

            while (current != null)
            {
                if (current->IsFree == 1 && current->Size >= totalSize)
                {
                    // Bloque encontrado

                    // Comprobamos si el bloque es lo suficientemente grande como para dividirlo
                    if (current->Size >= totalSize + sizeof(MemoryBlock) + 8)
                    {
                        // Dividir el bloque
                        MemoryBlock* newBlock = (MemoryBlock*)((byte*)current + totalSize);
                        newBlock->Size = current->Size - totalSize;
                        newBlock->IsFree = 1;
                        newBlock->Next = current->Next;

                        current->Size = totalSize;
                        current->Next = newBlock;
                    }

                    // Marcar el bloque como asignado
                    current->IsFree = 0;

                    // Devolver un puntero a los datos (después del encabezado)
                    return (nint)((byte*)current + sizeof(MemoryBlock));
                }

                previous = current;
                current = current->Next;
            }

            // No se encontró un bloque libre adecuado
            return IntPtr.Zero;
        }

        /// <summary>
        /// Libera un bloque de memoria previamente asignado
        /// </summary>
        public static void Free(nint ptr)
        {
            if (ptr == IntPtr.Zero || !s_initialized)
                return;

            // Obtener el encabezado del bloque (restando el tamaño del encabezado)
            MemoryBlock* block = (MemoryBlock*)((byte*)ptr - sizeof(MemoryBlock));

            // Validamos que el puntero está dentro del heap
            if ((byte*)block < s_heapStart || (byte*)block >= s_heapStart + HEAP_SIZE)
                return;

            // Marcamos el bloque como libre
            block->IsFree = 1;

            // Intentamos fusionar bloques adyacentes
            CoalesceMemory();
        }

        /// <summary>
        /// Combina bloques libres adyacentes para reducir la fragmentación
        /// </summary>
        private static void CoalesceMemory()
        {
            MemoryBlock* current = s_freeList;

            while (current != null && current->Next != null)
            {
                if (current->IsFree == 1 && current->Next->IsFree == 1)
                {
                    // Combinamos bloques
                    current->Size += current->Next->Size;
                    current->Next = current->Next->Next;
                }
                else
                {
                    current = current->Next;
                }
            }
        }

        /// <summary>
        /// Cambia el tamaño de un bloque previamente asignado
        /// </summary>
        public static nint Realloc(nint ptr, ulong newSize)
        {
            if (ptr == IntPtr.Zero)
                return Malloc(newSize);

            if (newSize == 0)
            {
                Free(ptr);
                return IntPtr.Zero;
            }

            // Obtener el bloque actual
            MemoryBlock* currentBlock = (MemoryBlock*)((byte*)ptr - sizeof(MemoryBlock));

            // Validamos que el puntero está dentro del heap
            if ((byte*)currentBlock < s_heapStart || (byte*)currentBlock >= s_heapStart + HEAP_SIZE)
                return IntPtr.Zero;

            uint currentSize = currentBlock->Size - (uint)sizeof(MemoryBlock);

            // Si el nuevo tamaño es menor o igual, podemos usar el mismo bloque
            if (newSize <= currentSize)
                return ptr;

            // Necesitamos un bloque más grande
            nint newPtr = Malloc(newSize);
            if (newPtr == IntPtr.Zero)
                return IntPtr.Zero;

            // Copiar los datos
            for (uint i = 0; i < currentSize; i++)
                *((byte*)newPtr + i) = *((byte*)ptr + i);

            // Liberar el bloque antiguo
            Free(ptr);

            return newPtr;
        }

        /// <summary>
        /// Inicializa memoria y la llena con un valor específico
        /// </summary>
        public static nint Calloc(ulong num, ulong size)
        {
            ulong totalSize = num * size;
            nint ptr = Malloc(totalSize);

            if (ptr != IntPtr.Zero)
            {
                // Inicializar a cero
                for (ulong i = 0; i < totalSize; i++)
                    *((byte*)ptr + i) = 0;
            }

            return ptr;
        }
    }
}