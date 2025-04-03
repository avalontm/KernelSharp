using System;
using System.Runtime.InteropServices;

namespace Internal.Runtime.CompilerHelpers
{
    internal static unsafe class MemoryHelpers
    {
        /// <summary>
        /// Asigna memoria del sistema operativo
        /// </summary>
        /// <param name="size">Tamaño en bytes a asignar</param>
        /// <returns>Puntero a la memoria asignada o IntPtr.Zero si hay un error</returns>
        [DllImport("*", EntryPoint = "_malloc")]
        public static extern unsafe IntPtr Malloc(ulong size);

        /// <summary>
        /// Libera memoria previamente asignada
        /// </summary>
        /// <param name="ptr">Puntero a la memoria a liberar</param>
        [DllImport("*", EntryPoint = "_free")]
        public static extern unsafe void Free(IntPtr ptr);

        /// <summary>
        /// Establece un bloque de memoria a un valor específico
        /// </summary>
        /// <param name="ptr">Puntero donde comenzar</param>
        /// <param name="value">Valor byte para establecer</param>
        /// <param name="size">Número de bytes a establecer</param>
        public static unsafe void MemSet(byte* ptr, int c, ulong count)
        {
            for (byte* p = ptr; p < ptr + count; p++)
                *p = (byte)c;
        }


        /// <summary>
        /// Copia un bloque de memoria de origen a destino
        /// </summary>
        /// <param name="dest">Puntero de destino</param>
        /// <param name="src">Puntero de origen</param>
        /// <param name="size">Número de bytes a copiar</param>
        public static unsafe void MemCpy(byte* dest, byte* src, ulong count)
        {
            for (ulong i = 0; i < count; i++) dest[i] = src[i];
        }

        /// <summary>
        /// Compara dos bloques de memoria
        /// </summary>
        /// <param name="ptr1">Primer puntero</param>
        /// <param name="ptr2">Segundo puntero</param>
        /// <param name="size">Número de bytes a comparar</param>
        /// <returns>0 si son iguales, <0 si ptr1 < ptr2, >0 si ptr1 > ptr2</returns>
        public static unsafe int MemCmp(void* ptr1, void* ptr2, ulong size)
        {
            byte* p1 = (byte*)ptr1;
            byte* p2 = (byte*)ptr2;

            // Comparar byte por byte
            for (uint i = 0; i < size; i++)
            {
                if (*p1 != *p2)
                    return *p1 - *p2;

                p1++;
                p2++;
            }

            return 0; // Iguales
        }

        /// <summary>
        /// Copia un bloque de memoria de una ubicación a otra (similar a memcpy)
        /// </summary>
        /// <param name="dest">Puntero al destino</param>
        /// <param name="src">Puntero al origen</param>
        /// <param name="count">Número de bytes a copiar</param>
        public static unsafe void Movsb(void* dest, void* src, ulong count)
        {
            // Conversión a punteros de bytes para copia byte a byte
            byte* destination = (byte*)dest;
            byte* source = (byte*)src;

            // Optimización para copias largas usando bloque de 8 bytes (64 bits)
            ulong longCount = count / 8;
            ulong remainingBytes = count % 8;

            // Copiar en bloques de 8 bytes cuando sea posible
            if (longCount > 0)
            {
                ulong* destLong = (ulong*)destination;
                ulong* srcLong = (ulong*)source;

                for (ulong i = 0; i < longCount; i++)
                {
                    destLong[i] = srcLong[i];
                }

                // Ajustar punteros
                destination += longCount * 8;
                source += longCount * 8;
            }

            // Copiar bytes restantes uno por uno
            for (ulong i = 0; i < remainingBytes; i++)
            {
                destination[i] = source[i];
            }
        }

        /// <summary>
        /// Versión genérica de Movsb para arrays de tipos conocidos
        /// </summary>
        public static unsafe void Movsb<T>(T* dest, T* src, ulong count) where T : unmanaged
        {
            // Copia directa de elementos del mismo tipo
            for (ulong i = 0; i < count; i++)
            {
                dest[i] = src[i];
            }
        }
    }
}