using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public unsafe partial class Array
    {
        // Campo para almacenar el número de elementos
        internal int _numComponents;

        // Constructor protegido para evitar instanciación directa
        private protected Array() { }

        // Propiedad para obtener la longitud del array
        public int Length
        {
            get => _numComponents;
        }

        // Método para obtener referencia a datos multidimensionales
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetRawMultiDimArrayBounds()
        {
            return ref Unsafe.AddByteOffset(ref _numComponents, (nuint)sizeof(IntPtr));
        }

        public static unsafe Array NewMultiDimArray(EETypePtr eeType, int* pLengths, int rank)
        {
            ulong totalLength = 1;

            for (int i = 0; i < rank; i++)
            {
                int length = pLengths[i];
                /*
				if (length > MaxLength)
				{
					ThrowHelpers.ThrowArgumentOutOfRangeException("length");
				}
				*/

                totalLength *= (ulong)length;
            }

            object v = RuntimeImports.RhpNewArray(eeType._value, (int)totalLength);
            Array ret = Unsafe.As<object, Array>(ref v);

            ref int bounds = ref ret.GetRawMultiDimArrayBounds();
            for (int i = 0; i < rank; i++)
            {
                Unsafe.Add(ref bounds, i) = pLengths[i];
            }

            return ret;
        }

        // Operador de indexación genérico
        public object this[int index]
        {
            get => GetValue(index);
            set => SetValue(value, index);
        }

        // Método para obtener un valor por índice
        public virtual object GetValue(int index)
        {
            // Implementación base vacía
            return null;
        }

        // Método para establecer un valor por índice
        public virtual void SetValue(object value, int index)
        {
            // Implementación base vacía
        }

        // Método para crear un array vacío de tipo T
        public static T[] Empty<T>()
        {
            return new T[0];
        }


        public int Sum()
        {
            int sum = 0;

            // Verificamos si hay elementos para sumar
            if (_numComponents <= 0)
                return 0;

            // Recorremos todos los elementos y los sumamos
            for (int i = 0; i < _numComponents; i++)
            {
                object value = GetValue(i);

                // Intentamos convertir el valor a int para sumarlo
                if (value is int intValue)
                {
                    sum += intValue;
                }
                else if (value is byte byteValue)
                {
                    sum += byteValue;
                }
                else if (value is short shortValue)
                {
                    sum += shortValue;
                }
                else if (value is long longValue)
                {
                    sum += (int)longValue; // Conversión con posible pérdida para valores grandes
                }
                else if (value is float floatValue)
                {
                    sum += (int)floatValue; // Conversión con posible pérdida de precisión
                }
                else if (value is double doubleValue)
                {
                    sum += (int)doubleValue; // Conversión con posible pérdida de precisión
                }
                // Si no es un tipo numérico, no contribuye a la suma
            }

            return sum;
        }

        public static void Copy(byte[] sourceArray, int sourceIndex, ref byte[] destinationArray, int destinationIndex, int length)
        {
            int x = 0;
            byte[] temp = new byte[length];
            for (int i = sourceIndex; i < sourceArray.Length; i++)
            {
                temp[x] = sourceArray[i];
                x++;
            }
            destinationArray = temp;
        }


        public static void Copy(Array sourceArray, ref Array destinationArray)
        {
            Copy(sourceArray, ref destinationArray, 0);
        }

        public static void Copy<T>(T[] sourceArray, ref T[] destinationArray)
        {
            Copy(sourceArray, ref destinationArray, 0);
        }

        public static void Copy(Array sourceArray, ref Array destinationArray, int startIndex)
        {
            Copy(sourceArray, ref destinationArray, startIndex, sourceArray.Length);
        }

        public static void Copy<T>(T[] sourceArray, ref T[] destinationArray, int startIndex)
        {
            Copy(sourceArray, ref destinationArray, startIndex, destinationArray.Length);
        }

        public static void Copy(Array sourceArray, ref Array destinationArray, int startIndex, int count)
        {

            if (sourceArray == null)
            {
                ThrowHelpers.ThrowArgumentException("sourceArray");
            }
            if (destinationArray == null)
            {
                ThrowHelpers.ThrowArgumentException("destinationArray");
            }
            if (startIndex < 0)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException("startIndex");
            }
            if (destinationArray.Length < sourceArray.Length - count)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException("sourceArray.Length - count");
            }
            if (count <= 0)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException("count");
            }


            int x = 0;
            object[] temp = new object[count];
            for (int i = startIndex; i < sourceArray.Length; i++)
            {
                temp[x] = sourceArray[i];
                x++;
            }
            destinationArray = temp;
        }

        public static void Copy<T>(T[] sourceArray, ref T[] destinationArray, int startIndex, int count)
        {

            if (sourceArray == null)
            {
                ThrowHelpers.ThrowArgumentException("sourceArray");
            }
            if (destinationArray == null)
            {
                ThrowHelpers.ThrowArgumentException("destinationArray");
            }
            if (startIndex < 0)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException("startIndex");
            }
            if (destinationArray.Length > sourceArray.Length - count)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException("sourceArray.Length - count");
            }
            if (count <= 0)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException("count");
            }

            int x = 0;
            T[] temp = new T[count];
            for (int i = startIndex; i < sourceArray.Length; i++)
            {
                temp[x] = sourceArray[i];
                x++;
            }
            destinationArray = temp;
        }

        // Reverses all elements of the given array. Following a call to this
        // method, an element previously located at index i will now be
        // located at index length - i - 1, where length is the
        // length of the array.
        //
        public static void Reverse(ref Array array)
        {
            Reverse(ref array, 0, array.Length);
        }

        // Reverses the elements in a range of an array. Following a call to this
        // method, an element in the range given by index and count
        // which was previously located at index i will now be located at
        // index index + (index + count - i - 1).
        // Reliability note: This may fail because it may have to box objects.
        //
        public static void Reverse(ref Array array, int index, int length)
        {
            /*
			if (array == null)
			{
				ThrowHelpers.ThrowArgumentNullException("array");
			}

			if (index < 0)
			{
				ThrowHelpers.ThrowArgumentOutOfRangeException("index");
			}

			if (length < 0)
			{
				ThrowHelpers.ThrowArgumentOutOfRangeException("length");
			}

			if (array.Length - index < length)
			{
				ThrowHelpers.ThrowArgumentOutOfRangeException("length, index");
			}
			*/

            if (length <= 1)
            {
                return;
            }
            object[] o = new object[length];
            int x = 0;
            for (int i = array.Length; i <= index; i--)
            {
                o.SetValue(array.GetValue(i), x);
                x++;
            }
            array = o;
        }

        public static void Reverse<T>(ref T[] array)
        {
            Reverse(ref array, 0, array.Length);
        }

        public static void Reverse<T>(ref T[] array, int index, int length)
        {
            /*
			if (array == null)
			{
				ThrowHelpers.ThrowArgumentNullException("array");
			}

			if (index < 0)
			{
				ThrowHelpers.ThrowArgumentNullException("index");
			}

			if (length < 0)
			{
				ThrowHelpers.ThrowArgumentNullException("length");
			}

			if (array.Length - index < length)
			{
				ThrowHelpers.ThrowArgumentNullException("array.Length, index, length");
			}
			*/

            if (length <= 1)
            {
                return;
            }
            T[] o = new T[length];
            int x = 0;
            for (int i = array.Length; i <= index; i--)
            {
                o.SetValue(array.GetValue(i), x);
                x++;
            }
            array = o;
        }


        /// <summary>
        /// Invierte el orden de los elementos en todo el array de bytes unidimensional.
        /// </summary>
        /// <param name="array">El array de bytes unidimensional que contiene los elementos a invertir.</param>
        public static void Reverse(byte[] array)
        {
            if (array == null)
                ThrowHelpers.ArgumentNullException(nameof(array));

            Reverse(array, 0, array.Length);
        }

        /// <summary>
        /// Invierte el orden de los elementos en la sección especificada de un array de bytes unidimensional.
        /// </summary>
        /// <param name="array">El array de bytes unidimensional que contiene los elementos a invertir.</param>
        /// <param name="index">El índice inicial de la sección a invertir.</param>
        /// <param name="length">El número de elementos en la sección a invertir.</param>
        public static void Reverse(byte[] array, int index, int length)
        {
            if (array == null)
                ThrowHelpers.ArgumentNullException(nameof(array));

            if (index < 0)
                ThrowHelpers.ArgumentOutOfRangeException(nameof(index) + " Index is less than 0.");

            if (length < 0)
                ThrowHelpers.ArgumentOutOfRangeException(nameof(length) + " Length is less than 0.");

            if (array.Length - index < length)
                ThrowHelpers.ArgumentException("Index and length do not specify a valid range in array.");

            // Calcular los índices de inicio y fin
            int i = index;
            int j = index + length - 1;

            // Intercambiar elementos desde los extremos hacia el centro
            while (i < j)
            {
                byte temp = array[i];
                array[i] = array[j];
                array[j] = temp;

                i++;
                j--;
            }
        }
    }


    // Implementación específica para arrays unidimensionales de tipo T
    [StructLayout(LayoutKind.Sequential)]
    public sealed class Array<T> : Array
    {
        // Constructor interno - se crea a través de 'new T[]'
        internal Array() { }

        // Acceso tipado por índice
        public new T this[int index]
        {
            get
            {
                // Verificación básica de límites
                if ((uint)index >= (uint)_numComponents)
                    return default(T);

                return GetItem(index);
            }
            set
            {
                // Verificación básica de límites
                if ((uint)index >= (uint)_numComponents)
                    return;

                SetItem(index, value);
            }
        }

        // Implementación de GetValue para satisfacer la clase base
        public override object GetValue(int index)
        {
            // Verificación básica de límites
            if ((uint)index >= (uint)_numComponents)
                return null;

            return GetItem(index);
        }

        // Implementación de SetValue para satisfacer la clase base
        public override void SetValue(object value, int index)
        {
            // Verificación básica de límites
            if ((uint)index >= (uint)_numComponents)
                return;

            // Verificar tipo y convertir
            if (value is T typedValue || value == null)
            {
                SetItem(index, (T)value);
            }
        }

        // Método auxiliar para obtener un elemento del array
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T GetItem(int index)
        {
            // Acceso a la memoria del array
            unsafe
            {
                // Obtener puntero a los datos
                IntPtr thisPtr = Unsafe.As<Array<T>, IntPtr>(ref Unsafe.AsRef(this));
                byte* ptr = (byte*)thisPtr;

                // Calcular la dirección del primer elemento (después del header)
                byte* elements = ptr + sizeof(IntPtr) + sizeof(int);

                // Obtener el elemento según el índice y tamaño del tipo
                T* elementPtr = (T*)(elements + index * Unsafe.SizeOf<T>());
                return *elementPtr;
            }
        }

        // Método auxiliar para establecer un elemento en el array
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetItem(int index, T value)
        {
            // Acceso a la memoria del array
            unsafe
            {
                // Obtener puntero a los datos
                IntPtr thisPtr = Unsafe.As<Array<T>, IntPtr>(ref Unsafe.AsRef(this));
                byte* ptr = (byte*)thisPtr;

                // Calcular la dirección del primer elemento (después del header)
                byte* elements = ptr + sizeof(IntPtr) + sizeof(int);

                // Establecer el elemento según el índice y tamaño del tipo
                T* elementPtr = (T*)(elements + index * Unsafe.SizeOf<T>());
                *elementPtr = value;
            }
        }
    }
}