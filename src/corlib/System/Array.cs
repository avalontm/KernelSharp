using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;

namespace System
{
    public abstract unsafe partial class Array
    {
        internal int _numComponents;
        public const int MaxLength = 0x7FFFFFC7;
        internal const int IntrosortSizeThreshold = 16;

        // Static field for empty array initialization
        private static readonly Array s_emptyArray = InitializeEmptyArray();

        private static Array InitializeEmptyArray()
        {
            EETypePtr et = EETypePtr.EETypePtrOf<object[]>();
            Array arrayObj = (Array)RuntimeImports.RhpNewArray(et._value, 0);
            return arrayObj;
        }

        // This ctor exists solely to prevent C# from generating a protected .ctor that violates the surface area.
        private protected Array() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetRawMultiDimArrayBounds()
        {
            return ref Unsafe.AddByteOffset(ref _numComponents, (nuint)sizeof(void*));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal new ref byte GetRawData()
        {
            // The array data starts after the _numComponents field and any bounds.
            // For single-dimensional arrays, this is immediately after _numComponents.
            return ref Unsafe.AddByteOffset(ref Unsafe.As<int, byte>(ref _numComponents), (nuint)sizeof(int));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal new uint GetRawDataSize()
        {
            // Get the element size from the array's EEType
            // This is a simplified implementation for low-level kernel
            return (uint)sizeof(int); // Default to int size if unknown
        }

        public static void ForEach<T>(T[] array, Action<T> action)
        {
            if (array == null)
            {
                ThrowHelpers.ArgumentNullException("Argument null");
            }

            if (action == null)
            {
                ThrowHelpers.ArgumentOutOfRangeException("Argument out of range");
            }

            for (int i = 0; i < array.Length; i++)
            {
                action(array[i]);
            }
        }

        public static void Map<T>(ref T[] array, Func<T, T> func)
        {
            if (array == null)
            {
                ThrowHelpers.ArgumentNullException("Argument null");
            }

            if (func == null)
            {
                ThrowHelpers.ArgumentOutOfRangeException("Argument out of range");
            }

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = func(array[i]);
            }
        }
        public static unsafe Array NewMultiDimArray(EETypePtr eeType, int* pLengths, int rank)
        {
            ulong totalLength = 1;

            for (int i = 0; i < rank; i++)
            {
                int length = pLengths[i];

                if (length > MaxLength)
                {
                    ThrowHelpers.ThrowArgumentOutOfRangeException("length");
                }


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

        public int Length
        {
            get
            {
                return _numComponents;
            }
            set
            {
                _numComponents = value;
            }
        }

        public object this[int i]
        {
            get => GetValue(i);
            set => SetValue(value, i);
        }

        public static void Resize<T>(ref T[] array, int newSize)
        {

            if (newSize < 0)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException("newSize");
            }

            T[] larray = array;

            if (larray == null)
            {
                array = new T[newSize];
                return;
            }

            if (larray.Length != newSize)
            {
                T[] newArray = new T[newSize];
                Copy(larray, newArray, 0, larray.Length > newSize ? newSize : larray.Length);
                array = newArray;
            }
        }

        public static Array CreateInstance<T>(uint length)
        {
            if (length < MaxLength)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException("length");
            }

            return new T[length];
        }

        /// <summary>
        /// Copia elementos de un array de origen a un array de destino.
        /// </summary>
        /// <param name="sourceArray">Array de origen</param>
        /// <param name="destinationArray">Array de destino</param>
        public static void Copy(Array sourceArray, Array destinationArray)
        {
            if (sourceArray == null)
                ThrowHelpers.ArgumentNullException("sourceArray");

            if (destinationArray == null)
                ThrowHelpers.ArgumentNullException("destinationArray");

            Copy(sourceArray, 0, destinationArray, 0, Math.Min(sourceArray.Length, destinationArray.Length));
        }

        /// <summary>
        /// Copia elementos de un array de origen a un array de destino con un índice de inicio.
        /// </summary>
        /// <param name="sourceArray">Array de origen</param>
        /// <param name="destinationArray">Array de destino</param>
        /// <param name="startIndex">Índice de inicio en el array de origen</param>
        public static void Copy(Array sourceArray, Array destinationArray, int startIndex)
        {
            if (sourceArray == null)
                ThrowHelpers.ArgumentNullException("sourceArray");

            if (destinationArray == null)
                ThrowHelpers.ArgumentNullException("destinationArray");

            Copy(sourceArray, startIndex, destinationArray, 0, Math.Min(sourceArray.Length - startIndex, destinationArray.Length));
        }

        /// <summary>
        /// Copia elementos de un array de origen a un array de destino con índices de origen y destino.
        /// </summary>
        /// <param name="sourceArray">Array de origen</param>
        /// <param name="sourceIndex">Índice de inicio en el array de origen</param>
        /// <param name="destinationArray">Array de destino</param>
        /// <param name="destinationIndex">Índice de inicio en el array de destino</param>
        /// <param name="length">Número de elementos a copiar</param>
        public static void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
        {
            if (sourceArray == null)
                ThrowHelpers.ArgumentNullException("sourceArray");

            if (destinationArray == null)
                ThrowHelpers.ArgumentNullException("destinationArray");

            // Validar índices y longitud
            if (sourceIndex < 0)
                ThrowHelpers.ArgumentOutOfRangeException("sourceIndex");

            if (destinationIndex < 0)
                ThrowHelpers.ArgumentOutOfRangeException("destinationIndex");

            if (length < 0)
                ThrowHelpers.ArgumentOutOfRangeException("length");

            if (sourceIndex + length > sourceArray.Length)
                ThrowHelpers.ArgumentException("Source array too short");

            if (destinationIndex + length > destinationArray.Length)
                ThrowHelpers.ArgumentException("Destination array too short");


            fixed (void* sourcePtr = &sourceArray.GetRawData(), destPtr = &destinationArray.GetRawData())
            {
                byte* src = (byte*)sourcePtr + (sourceIndex * sourceArray.GetRawDataSize());
                byte* dest = (byte*)destPtr + (destinationIndex * destinationArray.GetRawDataSize());
                MemoryHelpers.Movsb(dest, src, (ulong)(length * sourceArray.GetRawDataSize()));
            }

        }

        /// <summary>
        /// Copia elementos de un array de origen a un array de destino de tipo genérico.
        /// </summary>
        public static void Copy<T>(T[] sourceArray, T[] destinationArray)
        {
            if (sourceArray == null)
                ThrowHelpers.ArgumentNullException("sourceArray");

            if (destinationArray == null)
                ThrowHelpers.ArgumentNullException("destinationArray");

            Copy(sourceArray, 0, destinationArray, 0, Math.Min(sourceArray.Length, destinationArray.Length));
        }

        /// <summary>
        /// Copia elementos de un array de origen a un array de destino de tipo genérico con un índice de inicio.
        /// </summary>
        public static void Copy<T>(T[] sourceArray, T[] destinationArray, int startIndex)
        {
            if (sourceArray == null)
                ThrowHelpers.ArgumentNullException("sourceArray");

            if (destinationArray == null)
                ThrowHelpers.ArgumentNullException("destinationArray");

            Copy(sourceArray, startIndex, destinationArray, 0, Math.Min(sourceArray.Length - startIndex, destinationArray.Length));
        }

        /// <summary>
        /// Copia elementos de un array de origen a un array de destino de tipo genérico con índices de origen y destino.
        /// </summary>
        public static void Copy<T>(T[] sourceArray, int sourceIndex, T[] destinationArray, int destinationIndex, int length)
        {
            if (sourceArray == null)
                ThrowHelpers.ArgumentNullException("sourceArray");

            if (destinationArray == null)
                ThrowHelpers.ArgumentNullException("destinationArray");

            // Validar índices y longitud
            if (sourceIndex < 0)
                ThrowHelpers.ArgumentOutOfRangeException("sourceIndex");

            if (destinationIndex < 0)
                ThrowHelpers.ArgumentOutOfRangeException("destinationIndex");

            if (length < 0)
                ThrowHelpers.ArgumentOutOfRangeException("length");

            if (sourceIndex + length > sourceArray.Length)
                ThrowHelpers.ArgumentException("Source array too short");

            if (destinationIndex + length > destinationArray.Length)
                ThrowHelpers.ArgumentException("Destination array too short");

            for (int i = 0; i < length; i++)
            {
                destinationArray[destinationIndex + i] = sourceArray[sourceIndex + i];
            }
        }

        public static unsafe void Copy(Array sourceArray, Array destinationArray, int startIndex, int length)
        {
            // Validar arrays de entrada
            if (sourceArray == null)
                ThrowHelpers.ArgumentNullException("sourceArray");

            if (destinationArray == null)
                ThrowHelpers.ArgumentNullException("destinationArray");

            // Validar índices y longitud
            if (startIndex < 0)
                ThrowHelpers.ArgumentOutOfRangeException("startIndex");

            if (length < 0)
                ThrowHelpers.ArgumentOutOfRangeException("length");

            // Verificar que el índice de inicio más la longitud no exceda el array de origen
            if (startIndex + length > sourceArray.Length)
                ThrowHelpers.ArgumentException("Source array too short");

            // Verificar que la longitud no exceda el array de destino
            if (length > destinationArray.Length)
                ThrowHelpers.ArgumentException("Destination array too short");


            fixed (void* sourcePtr = &sourceArray.GetRawData(), destPtr = &destinationArray.GetRawData())
            {
                int elementSize = (int)sourceArray.GetRawDataSize();
                byte* src = (byte*)sourcePtr + (startIndex * elementSize);
                byte* dest = (byte*)destPtr;

                MemoryHelpers.Movsb(dest, src, (ulong)(length * elementSize));
            }
        }

        // Copia segura de memoria para arrays de tipo genérico
        public static void Copy<T>(T[] sourceArray, T[] destinationArray, int startIndex, int length)
        {
            // Validar arrays de entrada
            if (sourceArray == null)
                ThrowHelpers.ArgumentNullException("sourceArray");

            if (destinationArray == null)
                ThrowHelpers.ArgumentNullException("destinationArray");

            // Validar índices y longitud
            if (startIndex < 0)
                ThrowHelpers.ArgumentOutOfRangeException("startIndex");

            if (length < 0)
                ThrowHelpers.ArgumentOutOfRangeException("length");

            // Verificar que el índice de inicio más la longitud no exceda el array de origen
            if (startIndex + length > sourceArray.Length)
                ThrowHelpers.ArgumentException("Source array too short");

            // Verificar que la longitud no exceda el array de destino
            if (length > destinationArray.Length)
                ThrowHelpers.ArgumentException("Destination array too short");

            Copy(sourceArray, startIndex, destinationArray, 0, length);
        }

        // Clear method for non-generic Array
        public static void Clear(Array array, int index, int length)
        {
            if (array == null)
            {
                ThrowHelpers.ThrowNullReferenceException("Array is null");
            }

            if (index < 0 || length < 0)
            {
                ThrowHelpers.IndexOutOfRangeException("Invalid index or length");
            }

            if (index + length > array.Length)
            {
                ThrowHelpers.IndexOutOfRangeException("Clear would exceed array bounds");
            }

            // Set elements to null
            for (int i = index; i < index + length; i++)
            {
                array.SetValue(null, i);
            }
        }

        // Clear method for generic arrays
        public static void Clear<T>(T[] array, int index, int length)
        {
            if (array == null)
            {
                ThrowHelpers.IndexOutOfRangeException("Array is null");
            }

            if (index < 0 || length < 0)
            {
                ThrowHelpers.IndexOutOfRangeException("Invalid index or length");
            }

            if (index + length > array.Length)
            {
                ThrowHelpers.IndexOutOfRangeException("Clear would exceed array bounds");
            }

            // Set elements to default(T)
            for (int i = index; i < index + length; i++)
            {
                array[i] = default(T);
            }
        }

        public virtual object GetValue(long index)
        {
            int iindex = (int)index;
            if (index != iindex)
            {
                ThrowHelpers.ThrowArgumentException("index");
            }

            return GetValue(iindex);
        }

        public virtual object GetValue(int index)
        {
            return GetValue(index);
        }

        public virtual object GetValue(long index1, long index2)
        {
            int iindex1 = (int)index1;
            int iindex2 = (int)index2;

            if (index1 != iindex1)
            {
                ThrowHelpers.ThrowArgumentException("index1");
            }

            if (index2 != iindex2)
            {
                ThrowHelpers.ThrowArgumentException("index2");
            }


            return GetValue(iindex1, iindex2);
        }

        public virtual object GetValue(long index1, long index2, long index3)
        {
            int iindex1 = (int)index1;
            int iindex2 = (int)index2;
            int iindex3 = (int)index3;


            if (index1 != iindex1)
            {
                ThrowHelpers.ThrowArgumentException("index1");
            }

            if (index2 != iindex2)
            {
                ThrowHelpers.ThrowArgumentException("index2");
            }

            if (index3 != iindex3)
            {
                ThrowHelpers.ThrowArgumentException("index3");
            }

            return GetValue(iindex1, iindex2, iindex3);
        }

        public virtual void SetValue(object value, long index)
        {
            int iindex = (int)index;

            if (index != iindex)
            {
                ThrowHelpers.ThrowArgumentException("index");
            }

            SetValue(value, iindex);
        }

        public virtual void SetValue(object value, int index)
        {
            SetValue(value, index);
        }

        public virtual void SetValue(object value, long index1, long index2)
        {
            int iindex1 = (int)index1;
            int iindex2 = (int)index2;

            if (index1 != iindex1)
            {
                ThrowHelpers.ThrowArgumentException("index1");
            }

            if (index2 != iindex2)
            {
                ThrowHelpers.ThrowArgumentException("index2");
            }

            SetValue(value, iindex1, iindex2);
        }

        public virtual void SetValue(object value, long index1, long index2, long index3)
        {
            int iindex1 = (int)index1;
            int iindex2 = (int)index2;
            int iindex3 = (int)index3;

            if (index1 != iindex1)
            {
                ThrowHelpers.ThrowArgumentException("index1");
            }

            if (index2 != iindex2)
            {
                ThrowHelpers.ThrowArgumentException("index2");
            }

            if (index3 != iindex3)
            {
                ThrowHelpers.ThrowArgumentException("index3");
            }

            SetValue(value, iindex1, iindex2, iindex3);
        }


        // Returns an object appropriate for synchronizing access to this
        // Array.
        public object SyncRoot => this;

        // Is this Array read-only?
        public static bool IsReadOnly => false;

        public static bool IsFixedSize => true;

        public static bool IsSynchronized => false;

        // Extremely minimal Empty<T> implementation
        public static T[] Empty<T>()
        {
            // Direct, no-frills array creation
            T[] emptyArray = new T[0];

            // Minimal validation
            if (emptyArray == null)
            {
                // Kernel-level error handling
                ThrowHelpers.ThrowArgumentException("Empty array creation failed");
            }

            return emptyArray;
        }

        // Alternate empty array creation for problematic scenarios
        private static class EmptyArray<T>
        {
            // Static constructor to ensure initialization
            static EmptyArray()
            {
                Value = new T[0];
            }

            // Statically initialized empty array
            internal static T[] Value;
        }


        public static bool Exists<T>(T[] array, T match)
        {
            return IndexOf(array, match) != -1;
        }

        public static void Fill<T>(T[] array, T value)
        {
            if (array == null)
            {
                ThrowHelpers.ThrowArgumentNullException("Fill: array");
            }

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }

        public static void Fill<T>(T[] array, T value, int startIndex, int count)
        {

            if (array == null)
            {
                ThrowHelpers.ThrowArgumentNullException("Fill: array");
            }

            if (startIndex < 0 || startIndex > array.Length)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException("startIndex, array.Length");
            }

            if (count < 0 || startIndex > array.Length - count)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException("count, array.Length - count, startIndex");
            }

            for (int i = startIndex; i < startIndex + count; i++)
            {
                array[i] = value;
            }
        }

        // Returns the index of the first occurrence of a given value in an array.
        // The array is searched forwards, and the elements of the array are
        // compared to the given value using the Object.Equals method.
        //
        public static int IndexOf(Array array, object value)
        {
            return IndexOf(array, value, 0);
        }

        // IndexOf for non-generic Array with 4 parameters
        public static int IndexOf(Array array, object value, int startIndex, int count)
        {
            if (array == null)
            {
                ThrowHelpers.ThrowNullReferenceException("Array is null");
            }

            if (startIndex < 0 || count < 0)
            {
                ThrowHelpers.ArgumentOutOfRangeException("Invalid startIndex or count");
            }

            if (startIndex + count > array.Length)
            {
                ThrowHelpers.ArgumentOutOfRangeException("IndexOf would exceed array bounds");
            }

            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                object currentValue = array.GetValue(i);

                // Handle null value comparison
                if (value == null)
                {
                    if (currentValue == null)
                        return i;
                }
                // Use Equals for value comparison
                else if (currentValue != null && currentValue.Equals(value))
                {
                    return i;
                }
            }

            return -1;
        }

        // IndexOf for generic arrays with 4 parameters
        public static int IndexOf<T>(T[] array, T value, int startIndex, int count)
        {
            if (array == null)
            {
                ThrowHelpers.ThrowArgumentNullException("Array is null");
            }

            if (startIndex < 0 || count < 0)
            {
                ThrowHelpers.IndexOutOfRangeException("Invalid startIndex or count");
            }

            if (startIndex + count > array.Length)
            {
                ThrowHelpers.IndexOutOfRangeException("IndexOf would exceed array bounds");
            }

            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                // Use EqualityComparer for type-safe comparison
                if (EqualityComparer<T>.Default.Equals(array[i], value))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int IndexOf(Array array, object value, int startIndex)
        {

            if (array == null)
            {
                ThrowHelpers.ThrowArgumentNullException("IndexOf: array");
            }

            if ((uint)startIndex > (uint)array.Length)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException("startIndex, array.Length");
            }


            for (int i = startIndex; i < array.Length; i++)
            {
                if (array.GetValue(i).Equals(value))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int IndexOf<T>(T[] array, T value)
        {
            return IndexOf(array, value, 0);
        }

        public static int IndexOf<T>(T[] array, T value, int startIndex)
        {

            if (array == null)
            {
                ThrowHelpers.ThrowArgumentNullException("IndexOf: array");
            }

            if ((uint)startIndex > (uint)array.Length)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException("startIndex, array.Length");
            }


            for (int i = startIndex; i < startIndex + array.Length; i++)
            {
                if (array[i].Equals(value))
                {
                    return i;
                }
            }
            return -1;
        }

        public static void Reverse(ref Array array)
        {
            Reverse(ref array, 0, array.Length);
        }

        public static void Reverse(ref Array array, int index, int length)
        {

            if (array == null)
            {
                ThrowHelpers.ThrowArgumentNullException("Reverse: array");
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

        public static int LastIndexOf(Array array, object value, int startIndex, int count)
        {

            if (array == null)
            {
                ThrowHelpers.ThrowArgumentNullException("LastIndexOf: array");
            }


            if (array.Length == 0)
            {
                return -1;
            }


            if (startIndex < 0 || startIndex >= array.Length)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(SR.ArgumentOutOfRange_Index);
            }

            if (count < 0)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(SR.ArgumentOutOfRange_Count);
            }

            if (count > startIndex - 1)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(SR.ArgumentOutOfRange_EndIndexStartIndex);
            }


            int endIndex = startIndex - count + 1;
            if (value == null)
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    if (array[i] == null)
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    object obj = array[i];
                    if (obj != null && obj.Equals(value))
                    {
                        return i;
                    }
                }
            }
            return -1;  // Return lb-1 for arrays with negative lower bounds.
        }

        public static void Reverse<T>(ref T[] array)
        {
            Reverse(ref array, 0, array.Length);
        }

        public static void Reverse<T>(ref T[] array, int index, int length)
        {

            if (array == null)
            {
                ThrowHelpers.ThrowArgumentNullException("Reverse: array");
            }

            if (index < 0)
            {
                ThrowHelpers.ThrowArgumentNullException("Reverse: index");
            }

            if (length < 0)
            {
                ThrowHelpers.ThrowArgumentNullException("Reverse: length");
            }

            if (array.Length - index < length)
            {
                ThrowHelpers.ThrowArgumentNullException("array.Length, index, length");
            }


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
    }

     public class Array<T> : Array { }

}