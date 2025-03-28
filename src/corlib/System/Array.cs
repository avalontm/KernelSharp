using Internal.Runtime;
using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public unsafe partial class Array
    {
        // Campo para almacenar el n�mero de elementos
        internal int _numComponents;

        // Constructor protegido para evitar instanciaci�n directa
        private protected Array() { }

        // Propiedad para obtener la longitud del array
        public int Length
        {
            get => _numComponents;
        }

        // M�todo para obtener referencia a datos multidimensionales
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetRawMultiDimArrayBounds()
        {
            return ref Unsafe.AddByteOffset(ref _numComponents, (nuint)sizeof(IntPtr));
        }

        // Operador de indexaci�n gen�rico
        public object this[int index]
        {
            get => GetValue(index);
            set => SetValue(value, index);
        }

        // M�todo para obtener un valor por �ndice
        public virtual object GetValue(int index)
        {
            // Implementaci�n base vac�a
            return null;
        }

        // M�todo para establecer un valor por �ndice
        public virtual void SetValue(object value, int index)
        {
            // Implementaci�n base vac�a
        }

        // M�todo para crear un array vac�o de tipo T
        public static T[] Empty<T>()
        {
            return new T[0];
        }
    }

    // Implementaci�n espec�fica para arrays unidimensionales de tipo T
    [StructLayout(LayoutKind.Sequential)]
    public sealed class Array<T> : Array
    {
        // Constructor interno - se crea a trav�s de 'new T[]'
        internal Array() { }

        // Acceso tipado por �ndice
        public new T this[int index]
        {
            get
            {
                // Verificaci�n b�sica de l�mites
                if ((uint)index >= (uint)_numComponents)
                    return default(T);

                return GetItem(index);
            }
            set
            {
                // Verificaci�n b�sica de l�mites
                if ((uint)index >= (uint)_numComponents)
                    return;

                SetItem(index, value);
            }
        }

        // Implementaci�n de GetValue para satisfacer la clase base
        public override object GetValue(int index)
        {
            // Verificaci�n b�sica de l�mites
            if ((uint)index >= (uint)_numComponents)
                return null;

            return GetItem(index);
        }

        // Implementaci�n de SetValue para satisfacer la clase base
        public override void SetValue(object value, int index)
        {
            // Verificaci�n b�sica de l�mites
            if ((uint)index >= (uint)_numComponents)
                return;

            // Verificar tipo y convertir
            if (value is T typedValue || value == null)
            {
                SetItem(index, (T)value);
            }
        }

        // M�todo auxiliar para obtener un elemento del array
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T GetItem(int index)
        {
            // Acceso a la memoria del array
            unsafe
            {
                // Obtener puntero a los datos
                IntPtr thisPtr = Unsafe.As<Array<T>, IntPtr>(ref Unsafe.AsRef(this));
                byte* ptr = (byte*)thisPtr;

                // Calcular la direcci�n del primer elemento (despu�s del header)
                byte* elements = ptr + sizeof(IntPtr) + sizeof(int);

                // Obtener el elemento seg�n el �ndice y tama�o del tipo
                T* elementPtr = (T*)(elements + index * Unsafe.SizeOf<T>());
                return *elementPtr;
            }
        }

        // M�todo auxiliar para establecer un elemento en el array
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetItem(int index, T value)
        {
            // Acceso a la memoria del array
            unsafe
            {
                // Obtener puntero a los datos
                IntPtr thisPtr = Unsafe.As<Array<T>, IntPtr>(ref Unsafe.AsRef(this));
                byte* ptr = (byte*)thisPtr;

                // Calcular la direcci�n del primer elemento (despu�s del header)
                byte* elements = ptr + sizeof(IntPtr) + sizeof(int);

                // Establecer el elemento seg�n el �ndice y tama�o del tipo
                T* elementPtr = (T*)(elements + index * Unsafe.SizeOf<T>());
                *elementPtr = value;
            }
        }
    }
}