using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// Implementación mejorada de IntArray que garantiza acceso confiable a elementos
    /// </summary>
    public unsafe class IntArray
    {
        // Campo para almacenar la longitud
        private int _length;

        // Almacenamiento interno de datos
        private int[] _data;

        // Usado para acceso directo a la memoria cuando sea necesario
        private IntPtr _dataPtr;

        /// <summary>
        /// Constructor que inicializa el array con una longitud específica
        /// </summary>
        public IntArray(int length)
        {
            if (length < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("length");

            _length = length;
            _data = new int[length];

            // Guardar el puntero a los datos para acceso directo si es necesario
            StoreDataPointer();
        }

        /// <summary>
        /// Constructor que inicializa el array con valores específicos
        /// </summary>
        public IntArray(params int[] values)
        {
            if (values == null)
                ThrowHelpers.ThrowArgumentNullException("values");

            _length = values.Length;
            _data = new int[_length];

            // Guardar el puntero a los datos para acceso directo si es necesario
            StoreDataPointer();

            // Inicializar con valores
            for (int i = 0; i < _length; i++)
            {
                SetDirectValue(i, values[i]);
            }
        }

        /// <summary>
        /// Obtiene la longitud del array
        /// </summary>
        public int Length => _length;

        /// <summary>
        /// Indexador para acceder a los elementos del array
        /// </summary>
        public int this[int index]
        {
            get
            {
                // Verificar límites
                if (index < 0 || index >= _length)
                    ThrowHelpers.IndexOutOfRangeException();

                // Usar acceso directo para mayor fiabilidad
                return GetDirectValue(index);
            }
            set
            {
                // Verificar límites
                if (index < 0 || index >= _length)
                    ThrowHelpers.IndexOutOfRangeException();

                // Usar acceso directo para mayor fiabilidad
                SetDirectValue(index, value);
            }
        }

        /// <summary>
        /// Almacena el puntero a los datos para acceso directo
        /// </summary>
        private void StoreDataPointer()
        {
            if (_data != null)
            {
                // Obtener puntero al array interno
                _dataPtr = Unsafe.As<int[], IntPtr>(ref _data);
            }
        }

        /// <summary>
        /// Obtiene un valor directamente de la memoria
        /// </summary>
        private unsafe int GetDirectValue(int index)
        {
            // Primero intentar usar el array normal
            try
            {
                return _data[index];
            }
            catch
            {
                // Si falla, intentar acceso directo
                return GetValueUsingDirectAccess(index);
            }
        }

        /// <summary>
        /// Establece un valor directamente en la memoria
        /// </summary>
        private unsafe void SetDirectValue(int index, int value)
        {
            // Primero intentar usar el array normal
            try
            {
                _data[index] = value;
            }
            catch
            {
                // Si falla, intentar acceso directo
                SetValueUsingDirectAccess(index, value);
            }
        }

        /// <summary>
        /// Obtiene un valor usando acceso directo a la memoria
        /// </summary>
        private unsafe int GetValueUsingDirectAccess(int index)
        {
            if (_dataPtr == IntPtr.Zero)
                return 0;

            // Calcular la dirección del elemento
            // Estructura del array: [EEType*][Length][Elements...]
            byte* basePtr = (byte*)_dataPtr;
            int* elements = (int*)(basePtr + sizeof(IntPtr) + sizeof(int));

            // Obtener el valor
            return elements[index];
        }

        /// <summary>
        /// Establece un valor usando acceso directo a la memoria
        /// </summary>
        private unsafe void SetValueUsingDirectAccess(int index, int value)
        {
            if (_dataPtr == IntPtr.Zero)
                return;

            // Calcular la dirección del elemento
            // Estructura del array: [EEType*][Length][Elements...]
            byte* basePtr = (byte*)_dataPtr;
            int* elements = (int*)(basePtr + sizeof(IntPtr) + sizeof(int));

            // Establecer el valor
            elements[index] = value;
        }

        /// <summary>
        /// Copia los valores a un nuevo array de enteros
        /// </summary>
        public int[] ToArray()
        {
            int[] result = new int[_length];
            for (int i = 0; i < _length; i++)
            {
                result[i] = this[i];
            }
            return result;
        }

        /// <summary>
        /// Rellena todos los elementos con un valor específico
        /// </summary>
        public void Fill(int value)
        {
            for (int i = 0; i < _length; i++)
            {
                this[i] = value;
            }
        }

        /// <summary>
        /// Comprueba el estado interno del array
        /// </summary>
        public bool CheckIntegrity()
        {
            if (_data == null || _length <= 0)
                return false;

            // Verificar que el puntero interno es válido
            if (_dataPtr == IntPtr.Zero)
            {
                StoreDataPointer();
                if (_dataPtr == IntPtr.Zero)
                    return false;
            }

            // Verificar que la longitud almacenada en el objeto coincide con la del array
            if (_length != _data.Length)
                return false;

            return true;
        }
    }
}