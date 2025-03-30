using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// Implementación específica para arrays unidimensionales de tipo SZArray (single-dimension, zero-based)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe class SZArray<T> : Array
    {
        // El layout de un SZArray en memoria:
        // [EEType*][Length][Elements...]

        // El campo _numComponents se hereda de Array
        // internal int _numComponents;

        // Los elementos del array comienzan inmediatamente después
        // private first T element; // no se declara, pero existe en memoria

        // Acceso de indexación optimizado
        public new T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)_numComponents)
                    ThrowHelpers.IndexOutOfRangeException();

                // Calcular puntero al array de datos
                ref T firstElement = ref Unsafe.As<int, T>(ref Unsafe.AddByteOffset(ref _numComponents, sizeof(int)));

                // Obtener referencia al elemento por índice
                return Unsafe.Add(ref firstElement, index);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if ((uint)index >= (uint)_numComponents)
                    ThrowHelpers.IndexOutOfRangeException();

                // Calcular puntero al array de datos
                ref T firstElement = ref Unsafe.As<int, T>(ref Unsafe.AddByteOffset(ref _numComponents, sizeof(int)));

                // Establecer valor en el índice
                Unsafe.Add(ref firstElement, index) = value;
            }
        }

        // Sobrescribir GetValue para mantener la coherencia
        public override object GetValue(int index)
        {
            if ((uint)index >= (uint)_numComponents)
                ThrowHelpers.IndexOutOfRangeException();

            return this[index];
        }

        // Sobrescribir SetValue para mantener la coherencia
        public override void SetValue(object value, int index)
        {
            if ((uint)index >= (uint)_numComponents)
                ThrowHelpers.IndexOutOfRangeException();

            if (!(value is T) && (value != null || typeof(T).IsValueType))
                ThrowHelpers.ThrowArgumentException("Tipo incompatible");

            this[index] = (T)value;
        }
    }

    /// <summary>
    /// Ayudante para crear arrays fuertemente tipados
    /// </summary>
    public static class SZArrayHelper
    {
        // Método para crear un array con elementos inicializados
        public static T[] CreateInstance<T>(params T[] values)
        {
            int length = values.Length;
            T[] newArray = new T[length];

            for (int i = 0; i < length; i++)
            {
                newArray[i] = values[i];
            }

            return newArray;
        }
    }
}