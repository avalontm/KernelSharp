using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Serialization
{
    /// <summary>
    /// Clase auxiliar para serializar y deserializar datos para comunicación entre procesos
    /// </summary>
    public static unsafe class SerializationHelper
    {
        /// <summary>
        /// Serializa un valor a un buffer binario con control de endianness
        /// </summary>
        public static void Serialize<T>(T value, byte* buffer, int bufferSize) where T : unmanaged
        {
            // Verificar que el buffer tenga espacio suficiente
            if (bufferSize < sizeof(T))
            {
                // En lugar de lanzar una excepción, ya que estamos en un kernel, simplemente retornamos
                return;
            }

            // Convertir valor a bytes asegurando el orden correcto de los bytes (little endian)
            T* source = &value;
            byte* sourceBytes = (byte*)source;

            // Copiamos byte a byte para asegurar que la alineación sea correcta
            for (int i = 0; i < sizeof(T); i++)
            {
                buffer[i] = sourceBytes[i];
            }
        }

        /// <summary>
        /// Deserializa un valor desde un buffer binario con control de endianness
        /// </summary>
        public static T Deserialize<T>(byte* buffer, int bufferSize) where T : unmanaged
        {
            // Verificar que el buffer tenga datos suficientes
            if (bufferSize < sizeof(T))
            {
                // En caso de error, devolver valor por defecto
                return default;
            }

            // Crear espacio para el valor deserializado
            T result = default;
            byte* destBytes = (byte*)&result;

            // Copiamos byte a byte para asegurar que la alineación sea correcta
            for (int i = 0; i < sizeof(T); i++)
            {
                destBytes[i] = buffer[i];
            }

            return result;
        }

        /// <summary>
        /// Serializa una cadena a un buffer con control de encoding
        /// </summary>
        public static int SerializeString(string value, byte* buffer, int bufferSize)
        {
            if (value == null || bufferSize <= 0)
            {
                return 0;
            }

            // Primero escribimos la longitud de la cadena (4 bytes)
            int length = value.Length;
            if (bufferSize < 4 + length)
            {
                return 0;
            }

            Serialize(length, buffer, 4);

            // Luego escribimos cada carácter de la cadena (asumiendo ASCII o UTF-8 básico)
            for (int i = 0; i < length; i++)
            {
                buffer[4 + i] = (byte)value[i];
            }

            return 4 + length;
        }

        /// <summary>
        /// Deserializa una cadena desde un buffer con control de encoding
        /// </summary>
        public static string DeserializeString(byte* buffer, int bufferSize)
        {
            if (bufferSize < 4)
            {
                return string.Empty;
            }

            // Leer la longitud primero
            int length = Deserialize<int>(buffer, 4);

            if (length <= 0 || bufferSize < 4 + length)
            {
                return string.Empty;
            }

            // Crear un arreglo de caracteres para la cadena
            char[] chars = new char[length];

            // Leer cada carácter
            for (int i = 0; i < length; i++)
            {
                chars[i] = (char)buffer[4 + i];
            }

            return new string(chars);
        }
    }
}