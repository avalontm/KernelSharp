using System;
using System.Runtime.InteropServices;

namespace Kernel.Drivers.IO
{
    /// <summary>
    /// Proporciona métodos auxiliares para marshalling y asignación de memoria temporal
    /// </summary>
    public static unsafe class MarshalHelper
    {
        // Tamaño del buffer temporal usado para operaciones de marshalling
        private const int TEMP_BUFFER_SIZE = 4096;

        // Buffer de memoria temporal
        private static byte* _tempBuffer;

        // Indica si el buffer temporal está en uso
        private static bool _bufferInUse;

        /// <summary>
        /// Inicializa el helper de marshalling
        /// </summary>
        public static void Initialize()
        {
            if (_tempBuffer == null)
            {
                // Asignar un buffer temporal de 4KB
                _tempBuffer = (byte*)Allocator.Allocate(TEMP_BUFFER_SIZE);
                _bufferInUse = false;
            }
        }

        /// <summary>
        /// Asigna memoria temporal para operaciones de marshalling
        /// </summary>
        /// <param name="size">Tamaño en bytes</param>
        /// <returns>Puntero a la memoria asignada</returns>
        public static IntPtr AllocateTemporaryMemory(int size)
        {
            if (_tempBuffer == null)
            {
                Initialize();
            }

            // Verificar si el buffer ya está en uso o si el tamaño solicitado es demasiado grande
            if (_bufferInUse || size > TEMP_BUFFER_SIZE)
            {
                // Asignar memoria nueva
                return (IntPtr)Allocator.Allocate((uint)size);
            }

            // Usar el buffer temporal
            _bufferInUse = true;
            return (IntPtr)_tempBuffer;
        }

        /// <summary>
        /// Libera memoria temporal asignada previamente
        /// </summary>
        /// <param name="ptr">Puntero a la memoria a liberar</param>
        public static void FreeTemporaryMemory(IntPtr ptr)
        {
            // Si es nuestro buffer temporal, simplemente marcarlo como disponible
            if (ptr == (IntPtr)_tempBuffer)
            {
                _bufferInUse = false;
                return;
            }

            // Si no es nuestro buffer, liberar normalmente
            if (ptr != IntPtr.Zero)
            {
                Allocator.Free(ptr);
            }
        }

        /// <summary>
        /// Convierte una estructura gestionada a un puntero no gestionado
        /// </summary>
        /// <typeparam name="T">Tipo de la estructura</typeparam>
        /// <param name="structure">Estructura a convertir</param>
        /// <returns>Puntero a la estructura copiada</returns>
        public static IntPtr StructureToPtr<T>(T structure) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            IntPtr ptr = AllocateTemporaryMemory(size);

            // Copiar la estructura a la memoria no gestionada
            IntPtr structurePtr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(structure, structurePtr, false);
            byte* src = (byte*)structurePtr;
            byte* dst = (byte*)ptr;

            for (int i = 0; i < size; i++)
            {
                dst[i] = src[i];
            }

            Marshal.FreeHGlobal(structurePtr);
            return ptr;
        }

        /// <summary>
        /// Convierte un puntero no gestionado a una estructura gestionada
        /// </summary>
        /// <typeparam name="T">Tipo de la estructura</typeparam>
        /// <param name="ptr">Puntero a la estructura</param>
        /// <returns>Estructura copiada</returns>
        public static T PtrToStructure<T>(IntPtr ptr) where T : struct
        {
            return Marshal.PtrToStructure<T>(ptr);
        }

        /// <summary>
        /// Convierte una cadena gestionada a un puntero de caracteres terminado en nulo
        /// </summary>
        /// <param name="str">Cadena a convertir</param>
        /// <returns>Puntero a la cadena terminada en nulo</returns>
        public static IntPtr StringToPtr(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                // Devolver una cadena vacía terminada en nulo
                IntPtr _ptr = AllocateTemporaryMemory(1);
                *(byte*)_ptr = 0;
                return _ptr;
            }

            int size = str.Length + 1; // +1 para el terminador nulo
            IntPtr ptr = AllocateTemporaryMemory(size);

            // Copiar la cadena a la memoria no gestionada
            for (int i = 0; i < str.Length; i++)
            {
                ((byte*)ptr)[i] = (byte)str[i]; // Asumir ASCII
            }

            // Añadir terminador nulo
            ((byte*)ptr)[str.Length] = 0;

            return ptr;
        }

        /// <summary>
        /// Convierte un puntero de caracteres terminado en nulo a una cadena gestionada
        /// </summary>
        /// <param name="ptr">Puntero a la cadena terminada en nulo</param>
        /// <returns>Cadena convertida</returns>
        public static string PtrToString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return string.Empty;
            }

            // Calcular la longitud de la cadena
            int length = 0;
            while (((byte*)ptr)[length] != 0)
            {
                length++;
            }

            if (length == 0)
            {
                return string.Empty;
            }

            // Crear una nueva cadena
            char[] chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = (char)((byte*)ptr)[i]; // Asumir ASCII
            }

            return new string(chars);
        }

        /// <summary>
        /// Copia datos desde un puntero a un array gestionado
        /// </summary>
        /// <typeparam name="T">Tipo de los elementos</typeparam>
        /// <param name="source">Puntero fuente</param>
        /// <param name="destination">Array destino</param>
        /// <param name="startIndex">Índice inicial en el destino</param>
        /// <param name="length">Número de elementos a copiar</param>
        public static void CopyFromPtr<T>(IntPtr source, T[] destination, int startIndex, int length) where T : struct
        {
            if (source == IntPtr.Zero || destination == null || startIndex < 0 || length <= 0 || startIndex + length > destination.Length)
            {
                // Parámetros no válidos
                return;
            }

            int elementSize = Marshal.SizeOf<T>();

            // Copiar elementos uno por uno
            for (int i = 0; i < length; i++)
            {
                IntPtr elementPtr = IntPtr.Add(source, i * elementSize);
                destination[startIndex + i] = PtrToStructure<T>(elementPtr);
            }
        }

        /// <summary>
        /// Copia datos desde un array gestionado a un puntero
        /// </summary>
        /// <typeparam name="T">Tipo de los elementos</typeparam>
        /// <param name="source">Array fuente</param>
        /// <param name="startIndex">Índice inicial en la fuente</param>
        /// <param name="destination">Puntero destino</param>
        /// <param name="length">Número de elementos a copiar</param>
        public static void CopyToPtr<T>(T[] source, int startIndex, IntPtr destination, int length) where T : struct
        {
            if (destination == IntPtr.Zero || source == null || startIndex < 0 || length <= 0 || startIndex + length > source.Length)
            {
                // Parámetros no válidos
                return;
            }

            int elementSize = Marshal.SizeOf<T>();

            // Copiar elementos uno por uno
            for (int i = 0; i < length; i++)
            {
                IntPtr elementPtr = IntPtr.Add(destination, i * elementSize);
                IntPtr tempPtr = StructureToPtr(source[startIndex + i]);

                // Copiar bytes
                for (int j = 0; j < elementSize; j++)
                {
                    ((byte*)elementPtr)[j] = ((byte*)tempPtr)[j];
                }

                // Liberar memoria temporal
                FreeTemporaryMemory(tempPtr);
            }
        }
    }
}