using Internal.Runtime;
using Internal.Runtime.CompilerServices;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// Estructura que encapsula un puntero a EEType para proporcionar una API segura.
    /// </summary>
    public unsafe struct EETypePtr
    {
        // Puntero al EEType
        internal EEType* _value;

        // Propiedades de acceso

        /// <summary>
        /// Indica si este EEType representa un array SZ (single-dimensional, zero-based).
        /// </summary>
        public bool IsSzArray
        {
            get
            {
                if (_value == null)
                    return false;
                return _value->IsArray;
            }
        }

        /// <summary>
        /// Obtiene el tipo de elemento para arrays.
        /// </summary>
        public EETypePtr ArrayElementType
        {
            get
            {
                if (!IsSzArray)
                    return default;

                // En una implementación real, esto necesitaría acceder
                // a la información del tipo de elemento
                // Simplificación: asumimos que está disponible a través de RelatedParameterType
                return new EETypePtr(_value->RelatedParameterType);
            }
        }

        /// <summary>
        /// Obtiene el rango (número de dimensiones) de un array.
        /// </summary>
        internal int ArrayRank
        {
            get
            {
                if (!IsSzArray)
                    return 0;

                // Simplificación: asumimos que está disponible a través de ArrayRank
                return _value->ArrayRank;
            }
        }

        /// <summary>
        /// Obtiene el valor del puntero como IntPtr.
        /// </summary>
        public IntPtr RawValue
        {
            get
            {
                return (IntPtr)_value;
            }
        }

        /// <summary>
        /// Determina si este tipo es un tipo de valor.
        /// </summary>
        public bool IsValueType
        {
            get
            {
                if (_value == null)
                    return false;
                return _value->IsValueType;
            }
        }

        /// <summary>
        /// Determina si este tipo es un array.
        /// </summary>
        public bool IsArray
        {
            get
            {
                if (_value == null)
                    return false;
                return _value->IsArray;
            }
        }

        /// <summary>
        /// Determina si este tipo es una interfaz.
        /// </summary>
        public bool IsInterface
        {
            get
            {
                if (_value == null)
                    return false;
                return _value->IsInterface;
            }
        }

        /// <summary>
        /// Obtiene el tamaño base de este tipo.
        /// </summary>
        public uint BaseSize
        {
            get
            {
                if (_value == null)
                    return 0;
                return _value->BaseSize;
            }
        }

        /// <summary>
        /// Obtiene el tamaño de componente de este tipo.
        /// </summary>
        public ushort ComponentSize
        {
            get
            {
                if (_value == null)
                    return 0;
                return _value->ComponentSize;
            }
        }

        internal bool HasPointers
        {
            get
            {
                return _value->HasGCPointers;
            }
        }

        /// <summary>
        /// Crea una nueva instancia de EETypePtr a partir de un IntPtr.
        /// </summary>
        public EETypePtr(IntPtr value)
        {
            _value = (EEType*)value;
        }

        /// <summary>
        /// Crea una nueva instancia de EETypePtr a partir de un puntero a EEType.
        /// </summary>
        public EETypePtr(EEType* value)
        {
            _value = value;
        }

        /// <summary>
        /// Obtiene el EETypePtr para el tipo genérico especificado.
        /// </summary>
        [Intrinsic]
        internal static EETypePtr EETypePtrOf<T>()
        {
            // Compilers are required to provide a low level implementation of this method.
            // This can be achieved by optimizing away the reflection part of this implementation
            // by optimizing typeof(!!0).TypeHandle into "ldtoken !!0", or by
            // completely replacing the body of this method.
            return default;
        }

        /// <summary>
        /// Conversión implícita a IntPtr.
        /// </summary>
        public static implicit operator IntPtr(EETypePtr ptr)
        {
            return (IntPtr)ptr._value;
        }

        /// <summary>
        /// Representación de cadena para depuración.
        /// </summary>
        public override string ToString()
        {
            if (_value == null)
                return "null EETypePtr";
            return $"EETypePtr: 0x{((ulong)_value).ToStringHex()}";
        }

    }
}