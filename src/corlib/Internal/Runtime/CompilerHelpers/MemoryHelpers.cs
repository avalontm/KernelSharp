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
        public static extern unsafe IntPtr Malloc(uint size);

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
        public static unsafe void MemSet(byte* ptr, int c, uint count)
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
        public static unsafe void MemCpy(byte* dest, byte* src, uint count)
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
        public static unsafe int MemCmp(void* ptr1, void* ptr2, uint size)
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
    }
}