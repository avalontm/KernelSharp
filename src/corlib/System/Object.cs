using Internal.Runtime;
using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public unsafe class Object
    {
        // The layout of object is a contract with the compiler.
        internal unsafe EEType* m_pEEType;

        [StructLayout(LayoutKind.Sequential)]
        private class RawData
        {
            public byte Data;
        }

        internal ref byte GetRawData()
        {
            return ref Unsafe.As<RawData>(this).Data;
        }

        internal uint GetRawDataSize()
        {
            return m_pEEType->BaseSize - (uint)sizeof(ObjHeader) - (uint)sizeof(EEType*);
        }

        public Object() { }
        ~Object() { }

        public unsafe static bool ReferenceEquals(object a, object b)
        {
            return a.Equals(b);
        }

        public unsafe virtual bool Equals(object b)
        {
            object a = this;

            if (a == null || b == null)
            {
                return false;
            }

            switch (a.m_pEEType->ElementType)
            {
                case EETypeElementType.Array:
                    return ((Array)a == (Array)b);
                case EETypeElementType.Byte:
                    return ((Byte)a == (Byte)b);
                case EETypeElementType.SByte:
                    return ((SByte)a == (SByte)b);
                case EETypeElementType.Int16:
                    return ((Int16)a == (Int16)b);
                case EETypeElementType.UInt16:
                    return ((UInt16)a == (UInt16)b);
                case EETypeElementType.Int32:
                    return ((Int32)a == (Int32)b);
                case EETypeElementType.UInt32:
                    return ((UInt32)a == (UInt32)b);
                case EETypeElementType.Int64:
                    return ((Int64)a == (Int64)b);
                case EETypeElementType.UInt64:
                    return ((UInt64)a == (UInt64)b);
                case EETypeElementType.IntPtr:
                    return ((IntPtr)a == (IntPtr)b);
                case EETypeElementType.UIntPtr:
                    return ((UIntPtr)a == (UIntPtr)b);
                case EETypeElementType.Char:
                    return ((Char)a == (Char)b);
                case EETypeElementType.Class:
                    return (a == b);

            }

            return false;
        }

        public virtual string GetType()
        {
            return "Object";
        }

        public virtual int GetHashCode()
        {
            return (int)this.m_pEEType->HashCode;
        }

        public virtual string ToString()
        {
            // Obtener el tipo real del objeto
            string typeName = GetType();
            /*
            // Comprobar si es uno de los tipos primitivos y proporcionar una representación especial
            if (this.m_pEEType != null)
            {
                switch (this.m_pEEType->ElementType)
                {
                    case EETypeElementType.Int32:
                        // Para enteros, convertir el valor a cadena
                        return ((Int32)this).ToString();
                    case EETypeElementType.Double:
                        // Para enteros, convertir el valor a cadena
                        return ((Double)this).ToString();
                    case EETypeElementType.Boolean:
                        // Para booleanos, devolver "True" o "False"
                        return ((Boolean)this).ToString();
                    case EETypeElementType.Char:
                        // Alternativa sin usar el constructor string(char, int)
                        char c = (Char)this;
                        // Crear un array de un solo carácter
                        char[] charArray = new char[1];
                        charArray[0] = c;
                        return new string(charArray);

                    case EETypeElementType.String:
                        // Para strings, devolver el string mismo
                        return (string)this;
                }
            }
            */
            // Para otros tipos, devolver el nombre del tipo
            return typeName;
        }
        public virtual void Dispose()
        {
            var obj = this;
            MemoryHelpers.Free(Unsafe.As<object, IntPtr>(ref obj));
        }

        public IntPtr GetHandle()
        {
            object _this = this;
            return Unsafe.As<object, IntPtr>(ref _this);
        }


        public static T FromHandle<T>(IntPtr handle) where T : class
        {
            return Unsafe.As<IntPtr, T>(ref handle);
        }

        public static implicit operator bool(object obj) => obj != null;

        public static implicit operator IntPtr(object obj) => Unsafe.As<object, IntPtr>(ref obj);

    }
}