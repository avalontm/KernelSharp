using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Internal.Runtime.CompilerHelpers
{
    internal static unsafe class MemoryHelpers
    {
        [DllImport("*", EntryPoint = "_malloc")]
        public static extern IntPtr Malloc(ulong size);
        [DllImport("*", EntryPoint = "_free")]
        public static extern void Free(IntPtr ptr);
        [DllImport("*", EntryPoint = "_realloc")]
        public static extern nint Realloc(nint ptr, ulong new_size);

        [RuntimeExport("memset")]
        public static unsafe void MemSet(byte* ptr, int c, int count)
        {
            for (byte* p = ptr; p < ptr + count; p++)
                *p = (byte)c;
        }

        [RuntimeExport("memcpy")]
        public static unsafe void MemCpy(byte* dest, byte* src, ulong count)
        {
            for (ulong i = 0; i < count; i++) dest[i] = src[i];
        }

        // Tamaño fijo para memoria del kernel - 16MB
        private const int POOL_SIZE = 16 * 1024 * 1024;

        // Puntero a la memoria preasignada
        private static IntPtr s_memoryPoolPtr;
        private static ulong s_currentPosition = 0;
        private static bool s_initialized = false;

        // Inicialización del pool de memoria
        private static void EnsureInitialized()
        {
            if (!s_initialized)
            {
                // En un sistema real, esto podría ser una dirección física específica
                // o memoria reservada por el bootloader
                byte[] initialPool = new byte[POOL_SIZE];
                fixed (byte* pPool = initialPool)
                {
                    s_memoryPoolPtr = (IntPtr)pPool;
                }
                s_initialized = true;
            }
        }

        // Helper method for safe array creation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] CreateArray<T>(int length)
        {
            if (length < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(length));

            return new T[length];
        }

        // Zero memory utility
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ZeroMemory(IntPtr address, ulong size)
        {
            MemoryHelpers.MemSet((byte*)address, 0, (int)size);
        }
    }
}