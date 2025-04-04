using Internal.Runtime;
using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    public unsafe class Object
    {
        // The layout of object is a contract with the compiler.
        internal unsafe EEType* m_pEEType;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref byte GetRawData()
        {
            return ref Unsafe.As<RawData>(this).Data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal uint GetRawDataSize()
        {
            return m_pEEType->BaseSize - (uint)sizeof(ObjHeader) - (uint)sizeof(EEType*);
        }

        public Object() { }

        ~Object() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool ReferenceEquals(object a, object b)
        {
            // Optimizaci�n: comparaci�n directa de punteros
            if (Unsafe.As<object, IntPtr>(ref a) == Unsafe.As<object, IntPtr>(ref b))
                return true;

            if (a == null || b == null)
                return false;

            return a.Equals(b);
        }

        public unsafe virtual bool Equals(object b)
        {
            object a = this;
            if (a == null || b == null)
            {
                return false;
            }

            // Optimizaci�n: comparaci�n directa de punteros primero
            if (Unsafe.As<object, IntPtr>(ref a) == Unsafe.As<object, IntPtr>(ref b))
                return true;

            // Si son tipos diferentes, no pueden ser iguales
            if (a.m_pEEType != b.m_pEEType)
                return false;

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
                default:
                    return false;
            }
        }

        public virtual string GetType()
        {
            unsafe
            {
                if (m_pEEType == null)
                    return "Object";

                // Obtener flags y tipo de EEType
                ushort flags = m_pEEType->Flags;
                var kind = (EETypeKind)(flags & (ushort)EETypeFlags.EETypeKindMask);

                // Verificar si es un tipo primitivo seg�n ElementTypeMask
                if ((flags & EETypeFlags.ElementTypeMask) != 0)
                {
                    var elementType = (EETypeElementType)(((ushort)flags & (ushort)EETypeFlags.ElementTypeMask) >>
                        (int)EETypeFlags.ElementTypeShift);

                    switch (elementType)
                    {
                        case EETypeElementType.Byte: return "System.Byte";
                        case EETypeElementType.SByte: return "System.SByte";
                        case EETypeElementType.Int16: return "System.Int16";
                        case EETypeElementType.UInt16: return "System.UInt16";
                        case EETypeElementType.Int32: return "System.Int32";
                        case EETypeElementType.UInt32: return "System.UInt32";
                        case EETypeElementType.Int64: return "System.Int64";
                        case EETypeElementType.UInt64: return "System.UInt64";
                        case EETypeElementType.IntPtr: return "System.IntPtr";
                        case EETypeElementType.UIntPtr: return "System.UIntPtr";
                        case EETypeElementType.Char: return "System.Char";
                        case EETypeElementType.Boolean: return "System.Boolean";
                        case EETypeElementType.Single: return "System.Single";
                        case EETypeElementType.Double: return "System.Double";
                        case EETypeElementType.Array: return "System.Array";
                        case EETypeElementType.Class: return kind == EETypeKind.CanonicalEEType ? "System.Object" : "Class";
                        case EETypeElementType.ValueType: return "ValueType";
                        default: return "Object";
                    }
                }

                // Si no es un tipo primitivo, clasificar seg�n EETypeKind
                switch (kind)
                {
                    case EETypeKind.CanonicalEEType:
                        return GetCanonicalTypeName(m_pEEType, (EETypeFlags)flags);

                    case EETypeKind.ClonedEEType:
                        return $"ClonedType_{((ulong)m_pEEType->HashCode).ToStringHex()}";

                    case EETypeKind.ParameterizedEEType:
                        // Si es un tipo de array o puntero, intentar representarlo
                        if (m_pEEType->IsArray)
                            return "Array";
                        else if (m_pEEType->IsPointerType)
                            return "Pointer";
                        else
                            return $"ParameterizedType_{((ulong)m_pEEType->HashCode).ToStringHex()}";

                    case EETypeKind.GenericTypeDefEEType:
                        return (flags & EETypeFlags.IsGenericFlag) != 0
                            ? "GenericTypeDefinition"
                            : $"TypeDef_{((ulong)m_pEEType->HashCode).ToStringHex()}";

                    default:
                        return $"Type_{((ulong)m_pEEType->HashCode).ToStringHex()}";
                }
            }
        }

        private unsafe string GetCanonicalTypeName(EEType* eeType, EETypeFlags flags)
        {
            if ((flags & EETypeFlags.HasFinalizerFlag) != 0)
                return "FinalizableObject";

            if ((flags & EETypeFlags.IsGenericFlag) != 0)
            {
                return "GenericType";
            }

            return $"Type_{((ulong)eeType->HashCode).ToStringHex()}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual int GetHashCode()
        {
            IntPtr ptr = Unsafe.As<object, IntPtr>(ref Unsafe.AsRef(this));
            long longPtr = ptr.ToInt64();
            return (int)longPtr ^ (int)(longPtr >> 32);
        }

        public virtual string ToString()
        {
            object obj = this;
            if (obj == null)
                return "null";

            var typeStr = GetType();

            switch (m_pEEType->ElementType)
            {
                case EETypeElementType.Byte:
                    return ((Byte)obj).ToString();
                case EETypeElementType.SByte:
                    return ((SByte)obj).ToString();
                case EETypeElementType.Int16:
                    return ((Int16)obj).ToString();
                case EETypeElementType.UInt16:
                    return ((UInt16)obj).ToString();
                case EETypeElementType.Int32:
                    return ((Int32)obj).ToString();
                case EETypeElementType.UInt32:
                    return ((UInt32)obj).ToString();
                case EETypeElementType.Int64:
                    return ((Int64)obj).ToString();
                case EETypeElementType.UInt64:
                    return ((UInt64)obj).ToString();
                case EETypeElementType.IntPtr:
                    return ((IntPtr)obj).ToString();
                case EETypeElementType.UIntPtr:
                    return ((UIntPtr)obj).ToString();
                case EETypeElementType.Char:
                    return ((Char)obj).ToString();
                case EETypeElementType.Boolean:
                    return ((Boolean)obj).ToString();
                case EETypeElementType.Single:
                    return ((Single)obj).ToString();
                case EETypeElementType.Double:
                    return ((Double)obj).ToString();
                case EETypeElementType.Array:
                    return ArrayToString((Array)obj);
                case EETypeElementType.Class:
                    if (obj.GetType() != typeof(Object))
                    {
                        return obj.ToString();
                    }
                    return typeStr;
                default:
                    return typeStr;
            }
        }

        private string ArrayToString(Array arr)
        {
            if (arr == null)
                return "null";

            if (arr.Length == 0)
                return "[]";

            // Limit the number of elements displayed
            int displayCount = Math.Min(arr.Length, 10);
            var elements = new string[displayCount];

            for (int i = 0; i < displayCount; i++)
            {
                elements[i] = arr.GetValue(i)?.ToString() ?? "null";
            }

            string suffix = arr.Length > displayCount ? "..." : "";
            return $"[{string.Join(", ", elements)}{suffix}]";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Dispose()
        {
            var obj = this;
            MemoryHelpers.Free(Unsafe.As<object, IntPtr>(ref obj));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr GetHandle()
        {
            object _this = this;
            return Unsafe.As<object, IntPtr>(ref _this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FromHandle<T>(IntPtr handle) where T : class
        {
            return Unsafe.As<IntPtr, T>(ref handle);
        }

        public static implicit operator bool(object obj) => obj != null;

        public static implicit operator IntPtr(object obj) => Unsafe.As<object, IntPtr>(ref obj);
    }
}