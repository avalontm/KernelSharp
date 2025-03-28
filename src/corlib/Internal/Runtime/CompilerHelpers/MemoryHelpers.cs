using System;
using System.Runtime;
using System.Runtime.CompilerServices;
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
        public static unsafe void MemSet(IntPtr ptr, byte value, uint size)
        {
            // Implementación manual de memset
            byte* p = (byte*)ptr;
            uint remaining = size;

            // Primero, configurar bytes individuales hasta alinear a 4 bytes
            while (remaining > 0 && ((ulong)p & 3) != 0)
            {
                *p++ = value;
                remaining--;
            }

            // Si el valor no es cero, crear un valor de relleno de 32 bits
            uint fillValue = 0;
            if (value != 0)
            {
                fillValue = (uint)value;
                fillValue |= (fillValue << 8);
                fillValue |= (fillValue << 16);
            }

            // Luego configurar bloques de 4 bytes (más eficiente)
            while (remaining >= 4)
            {
                *(uint*)p = fillValue;
                p += 4;
                remaining -= 4;
            }

            // Configurar los bytes restantes
            while (remaining > 0)
            {
                *p++ = value;
                remaining--;
            }
        }

        /// <summary>
        /// Copia un bloque de memoria de origen a destino
        /// </summary>
        /// <param name="dest">Puntero de destino</param>
        /// <param name="src">Puntero de origen</param>
        /// <param name="size">Número de bytes a copiar</param>
        public static unsafe void MemCpy(void* dest, void* src, uint size)
        {
            // Comprobar superposición
            if ((ulong)dest == (ulong)src)
                return; // No hay nada que hacer

            byte* pDest = (byte*)dest;
            byte* pSrc = (byte*)src;
            uint remaining = size;

            // Verificar si hay superposición y hacer copia segura si es necesario
            if ((ulong)pDest < (ulong)pSrc || (ulong)pDest >= ((ulong)pSrc + size))
            {
                // Sin superposición o pDest está antes de pSrc, copia normal

                // Primero, copiar bytes individuales hasta alinear pDest a 4 bytes
                while (remaining > 0 && ((ulong)pDest & 3) != 0)
                {
                    *pDest++ = *pSrc++;
                    remaining--;
                }

                // Luego copiar por bloques de 4 bytes si está alineado
                if (((ulong)pSrc & 3) == 0)
                {
                    while (remaining >= 4)
                    {
                        *(uint*)pDest = *(uint*)pSrc;
                        pDest += 4;
                        pSrc += 4;
                        remaining -= 4;
                    }
                }

                // Copiar los bytes restantes
                while (remaining > 0)
                {
                    *pDest++ = *pSrc++;
                    remaining--;
                }
            }
            else
            {
                // Hay superposición y pDest está después de pSrc, copia inversa
                pDest += remaining;
                pSrc += remaining;

                // Copiar byte por byte en orden inverso
                while (remaining > 0)
                {
                    *--pDest = *--pSrc;
                    remaining--;
                }
            }
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