using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    /// <summary>
    /// Implementación de array para tipos de valor en KernelSharp
    /// </summary>
    /// <typeparam name="T">El tipo de los elementos del array (debe ser un tipo de valor)</typeparam>
    public unsafe struct KernelValueArray<T> : IDisposable where T : unmanaged
    {
        // Puntero a los datos
        private IntPtr _dataPtr;

        // Longitud del array
        private int _length;

        // Constructor 
        public KernelValueArray(int length)
        {
            if (length < 0)
                ThrowHelpers.ArgumentOutOfRangeException("length");

            _length = length;

            // Calcular tamaño y asignar memoria
            int elementSize = sizeof(T);
            _dataPtr = MemoryHelpers.Malloc((uint)(length * elementSize));

            // Verificar que la asignación fue exitosa
            if (_dataPtr == IntPtr.Zero)
                ThrowHelpers.ThrowOutOfMemoryException();

            // Inicializar a cero (opcional)
            Initialize();
        }

        // Propiedad Length
        public int Length => _length;

        // Indexador
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _length)
                    ThrowHelpers.ThrowIndexOutOfRangeException();

                T* ptr = (T*)_dataPtr;
                return ptr[index];
            }
            set
            {
                if (index < 0 || index >= _length)
                    ThrowHelpers.ThrowIndexOutOfRangeException();

                T* ptr = (T*)_dataPtr;
                ptr[index] = value;
            }
        }

        // Inicializar el array a cero
        private void Initialize()
        {
            byte* ptr = (byte*)_dataPtr;
            int totalSize = _length * sizeof(T);

            for (int i = 0; i < totalSize; i++)
            {
                ptr[i] = 0;
            }
        }

        // Liberar recursos
        public void Dispose()
        {
            if (_dataPtr != IntPtr.Zero)
            {
                MemoryHelpers.Free(_dataPtr);
                _dataPtr = IntPtr.Zero;
                _length = 0;
            }
        }

        // Método estático para crear un nuevo array
        public static KernelValueArray<T> Create(int length)
        {
            return new KernelValueArray<T>(length);
        }

        // Conversión implícita a T[] para compatibilidad
        public static implicit operator T[](KernelValueArray<T> array)
        {
            T[] result = new T[array._length];
            for (int i = 0; i < array._length; i++)
            {
                result[i] = array[i];
            }
            return result;
        }

        // Conversión explícita desde T[] para compatibilidad
        public static explicit operator KernelValueArray<T>(T[] array)
        {
            KernelValueArray<T> result = new KernelValueArray<T>(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                result[i] = array[i];
            }
            return result;
        }
    }

    /// <summary>
    /// Implementación simplificada de Array para KernelSharp
    /// </summary>
    public unsafe struct KernelStringArray<T> where T : class
    {
        private IntPtr _dataPtr;
        private int _length;

        public KernelStringArray(int length)
        {
            if (length < 0)
                ThrowHelpers.ArgumentOutOfRangeException("length");

            _length = length;
            // Cada elemento es un puntero a un objeto
            _dataPtr = MemoryHelpers.Malloc((uint)(length * sizeof(IntPtr)));

            if (_dataPtr == IntPtr.Zero)
                ThrowHelpers.ThrowOutOfMemoryException();

            Initialize();
        }

        public int Length => _length;

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _length)
                    ThrowHelpers.IndexOutOfRangeException();

                IntPtr* ptr = (IntPtr*)_dataPtr;
                IntPtr objPtr = ptr[index];

                if (objPtr == IntPtr.Zero)
                    return null;

                return Object.FromHandle<T>(objPtr);
            }
            set
            {
                if (index < 0 || index >= _length)
                    ThrowHelpers.IndexOutOfRangeException();

                IntPtr* ptr = (IntPtr*)_dataPtr;

                // Liberar el objeto anterior si existe
                if (ptr[index] != IntPtr.Zero)
                {
                    // Aquí podrías implementar una forma de liberar el objeto anterior
                }

                // Asignar el nuevo objeto
                ptr[index] = value != null ? Unsafe.As<T, IntPtr>(ref value) : IntPtr.Zero;
            }
        }

        private void Initialize()
        {
            IntPtr* ptr = (IntPtr*)_dataPtr;
            for (int i = 0; i < _length; i++)
            {
                ptr[i] = IntPtr.Zero;
            }
        }

        public override void Dispose()
        {
            if (_dataPtr != IntPtr.Zero)
            {
                MemoryHelpers.Free(_dataPtr);
                _dataPtr = IntPtr.Zero;
                _length = 0;
            }
            base.Dispose();
        }
    }
}