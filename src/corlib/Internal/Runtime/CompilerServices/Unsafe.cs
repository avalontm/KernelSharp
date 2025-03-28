using System;
using System.Runtime.CompilerServices;

namespace Internal.Runtime.CompilerServices
{
    /// <summary>
    /// Proporciona funcionalidad de bajo nivel para manipular punteros.
    /// </summary>
    public static unsafe partial class Unsafe
    {
        /// <summary>
        /// Retorna un puntero al par�metro por referencia dado.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AsPointer<T>(ref T value)
        {
            // Implementaci�n directa usando un puntero fijo
            fixed (T* p = &value)
            {
                return (void*)p;
            }
        }

        /// <summary>
        /// Retorna el tama�o de un objeto del tipo de par�metro dado.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>()
        {
            // Implementación básica para tipos comunes
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
                return 1;
            if (typeof(T) == typeof(char) || typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
                return 2;
            if (typeof(T) == typeof(int) || typeof(T) == typeof(uint) || typeof(T) == typeof(float))
                return 4;
            if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong) || typeof(T) == typeof(double))
                return 8;

            // Para string, devolver el tamaño de un puntero (ya que es un tipo de referencia)
            if (typeof(T) == typeof(string))
                return sizeof(IntPtr);

            // Para punteros e IntPtr, usar el tamaño del puntero de la plataforma
            if (typeof(T) == typeof(IntPtr) || typeof(T) == typeof(UIntPtr) || typeof(T).IsPointer)
                return sizeof(IntPtr);

            // Para otros tipos, usar una estimación
            return 16; // Valor por defecto
        }

        /// <summary>
        /// Convierte el objeto dado al tipo especificado, sin realizar comprobaci�n de tipo din�mica.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T As<T>(object value) where T : class
        {
            // Conversi�n directa sin verificaci�n
            return (T)value;
        }

        /// <summary>
        /// Reinterpreta la referencia dada como una referencia a un valor de tipo TTo.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TTo As<TFrom, TTo>(ref TFrom source)
        {
            // Implementaci�n utilizando punteros
            return ref *(TTo*)AsPointer(ref source);
        }

        /// <summary>
        /// A�ade un desplazamiento de elemento a la referencia dada.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Add<T>(ref T source, int elementOffset)
        {
            // Calcular el desplazamiento en bytes y usar AddByteOffset
            return ref AddByteOffset(ref source, elementOffset * SizeOf<T>());
        }

        /// <summary>
        /// A�ade un desplazamiento de elemento a la referencia dada.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Add<T>(ref T source, IntPtr elementOffset)
        {
            // Calcular el desplazamiento en bytes
            int offset = (int)elementOffset * SizeOf<T>();
            return ref AddByteOffset(ref source, offset);
        }

        /// <summary>
        /// A�ade un desplazamiento de elemento al puntero dado.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* Add<T>(void* source, int elementOffset)
        {
            // Implementaci�n directa para punteros
            return (byte*)source + (elementOffset * SizeOf<T>());
        }

        /// <summary>
        /// Determina si las referencias especificadas apuntan a la misma ubicaci�n.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreSame<T>(ref T left, ref T right)
        {
            // Comparar las direcciones de memoria
            return AsPointer(ref left) == AsPointer(ref right);
        }

        /// <summary>
        /// Determina si la direcci�n de memoria referenciada por left es mayor que
        /// la direcci�n de memoria referenciada por right.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAddressGreaterThan<T>(ref T left, ref T right)
        {
            // Comparar direcciones
            return (ulong)AsPointer(ref left) > (ulong)AsPointer(ref right);
        }

        /// <summary>
        /// Determina si la direcci�n de memoria referenciada por left es menor que
        /// la direcci�n de memoria referenciada por right.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAddressLessThan<T>(ref T left, ref T right)
        {
            // Comparar direcciones
            return (ulong)AsPointer(ref left) < (ulong)AsPointer(ref right);
        }

        /// <summary>
        /// Inicializa un bloque de memoria en la ubicaci�n dada con un valor inicial dado
        /// sin asumir la alineaci�n de la direcci�n dependiente de la arquitectura.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitBlockUnaligned(ref byte startAddress, byte value, uint byteCount)
        {
            // Implementaci�n para inicializar memoria
            byte* p = (byte*)AsPointer(ref startAddress);
            for (uint i = 0; i < byteCount; i++)
            {
                p[i] = value;
            }
        }

        /// <summary>
        /// Lee un valor de tipo T desde la ubicaci�n dada.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadUnaligned<T>(void* source)
        {
            // Leer memoria sin alineaci�n
            return *(T*)source;
        }

        /// <summary>
        /// Lee un valor de tipo T desde la ubicaci�n dada.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadUnaligned<T>(ref byte source)
        {
            // Usar As para convertir la referencia y luego leer
            return As<byte, T>(ref source);
        }

        /// <summary>
        /// Escribe un valor de tipo T en la ubicaci�n dada.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUnaligned<T>(void* destination, T value)
        {
            // Escribir memoria sin alineaci�n
            *(T*)destination = value;
        }

        /// <summary>
        /// Escribe un valor de tipo T en la ubicaci�n dada.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUnaligned<T>(ref byte destination, T value)
        {
            // Usar As para convertir la referencia y luego escribir
            As<byte, T>(ref destination) = value;
        }

        /// <summary>
        /// A�ade un desplazamiento de bytes a la referencia dada.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddByteOffset<T>(ref T source, int byteOffset)
        {
            // Implementaci�n para a�adir un desplazamiento de bytes
            byte* ptr = (byte*)AsPointer(ref source) + byteOffset;
            return ref *(T*)ptr;
        }

        /// <summary>
        /// A�ade un desplazamiento de bytes a la referencia dada.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddByteOffset<T>(ref T source, IntPtr byteOffset)
        {
            // Implementaci�n para a�adir un desplazamiento de bytes
            byte* ptr = (byte*)AsPointer(ref source) + (int)byteOffset;
            return ref *(T*)ptr;
        }

        /// <summary>
        /// A�ade un desplazamiento de bytes a la referencia dada.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ref T AddByteOffset<T>(ref T source, ulong byteOffset)
        {
            // Implementaci�n para a�adir un desplazamiento de bytes
            return ref AddByteOffset(ref source, (int)byteOffset);
        }

        /// <summary>
        /// Lee un valor de tipo T desde la ubicaci�n dada.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(void* source)
        {
            // Implementaci�n directa
            return *(T*)source;
        }

        /// <summary>
        /// Lee un valor de tipo T desde la ubicaci�n dada.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(ref byte source)
        {
            // Usar As para convertir la referencia
            return As<byte, T>(ref source);
        }

        /// <summary>
        /// Escribe un valor de tipo T en la ubicaci�n dada.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(void* destination, T value)
        {
            // Implementaci�n directa
            *(T*)destination = value;
        }

        /// <summary>
        /// Escribe un valor de tipo T en la ubicaci�n dada.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(ref byte destination, T value)
        {
            // Usar As para convertir la referencia
            As<byte, T>(ref destination) = value;
        }

        /// <summary>
        /// Reinterpreta la ubicaci�n dada como una referencia a un valor de tipo T.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsRef<T>(void* source)
        {
            // Implementaci�n directa
            return ref *(T*)source;
        }

        /// <summary>
        /// Reinterpreta la ubicaci�n dada como una referencia a un valor de tipo T.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsRef<T>(in T source)
        {
            // Simplemente devuelve la misma referencia
            return ref Unsafe.As<T, T>(ref Unsafe.AsRef(source));
        }

        /// <summary>
        /// Determina el desplazamiento en bytes desde el origen hasta el destino de las referencias dadas.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr ByteOffset<T>(ref T origin, ref T target)
        {
            // Calcular la diferencia de punteros
            return (IntPtr)((byte*)AsPointer(ref target) - (byte*)AsPointer(ref origin));
        }

        /// <summary>
        /// Devuelve una referencia a un tipo T que es una referencia nula.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T NullRef<T>()
        {
            // Devolver una referencia a memoria nula (peligroso, solo para uso interno)
            return ref *(T*)null;
        }

        /// <summary>
        /// Devuelve si una referencia dada a un tipo T es una referencia nula.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullRef<T>(ref T source)
        {
            // Comprobar si la direcci�n es nula
            return AsPointer(ref source) == null;
        }

        /// <summary>
        /// Evita las reglas de asignaci�n definitiva aprovechando la sem�ntica de 'out'.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SkipInit<T>(out T value)
        {
            // Implementaci�n que no inicializa la variable
            value = default!;
        }
    }
}