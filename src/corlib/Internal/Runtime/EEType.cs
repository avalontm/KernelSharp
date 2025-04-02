using Internal.Runtime.CompilerServices;
using Internal.Runtime.NativeFormat;
using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Internal.Runtime
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ObjHeader
    {
        // Contents of the object header
        private IntPtr _objHeaderContents;

        private uint _entryCount;

        public bool IsEmpty
        {
            get
            {
                return _entryCount == 0;
            }
        }

        public uint NumEntries
        {
            get
            {
                return _entryCount;
            }
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct EEType
    {
        private const int POINTER_SIZE = 8;
        private const int PADDING = 1; // _numComponents is padded by one Int32 to make the first element pointer-aligned
        internal const int SZARRAY_BASE_SIZE = POINTER_SIZE + POINTER_SIZE + (1 + PADDING) * 4;

        [StructLayout(LayoutKind.Explicit)]
        private unsafe struct RelatedTypeUnion
        {
            // Kinds.CanonicalEEType
            [FieldOffset(0)]
            public EEType* _pBaseType;
            [FieldOffset(0)]
            public EEType** _ppBaseTypeViaIAT;

            // Kinds.ClonedEEType
            [FieldOffset(0)]
            public EEType* _pCanonicalType;
            [FieldOffset(0)]
            public EEType** _ppCanonicalTypeViaIAT;

            // Kinds.ArrayEEType
            [FieldOffset(0)]
            public EEType* _pRelatedParameterType;
            [FieldOffset(0)]
            public EEType** _ppRelatedParameterTypeViaIAT;
        }

        private static unsafe class OptionalFieldsReader
        {
            internal static uint GetInlineField(byte* pFields, EETypeOptionalFieldTag eTag, uint uiDefaultValue)
            {
                if (pFields == null)
                    return uiDefaultValue;

                bool isLastField = false;
                while (!isLastField)
                {
                    byte fieldHeader = NativePrimitiveDecoder.ReadUInt8(ref pFields);
                    isLastField = (fieldHeader & 0x80) != 0;
                    EETypeOptionalFieldTag eCurrentTag = (EETypeOptionalFieldTag)(fieldHeader & 0x7f);
                    uint uiCurrentValue = NativePrimitiveDecoder.DecodeUnsigned(ref pFields);

                    // If we found a tag match return the current value.
                    if (eCurrentTag == eTag)
                        return uiCurrentValue;
                }
                return uiDefaultValue;
            }
        }

        private ushort _usComponentSize;
        private ushort _usFlags;
        private uint _uBaseSize;
        private RelatedTypeUnion _relatedType;
        private ushort _usNumVtableSlots;
        private ushort _usNumInterfaces;
        private uint _uHashCode;


        private const uint ValueTypePaddingLowMask = 0x7;
        private const uint ValueTypePaddingHighMask = 0xFFFFFF00;
        private const uint ValueTypePaddingMax = 0x07FFFFFF;
        private const int ValueTypePaddingHighShift = 8;
        private const uint ValueTypePaddingAlignmentMask = 0xF8;
        private const int ValueTypePaddingAlignmentShift = 3;

        internal ushort ComponentSize
        {
            get
            {
                return _usComponentSize;
            }
        }

        internal ushort GenericArgumentCount
        {
            get
            {
                return _usComponentSize;
            }
        }

        internal ushort Flags
        {
            get
            {
                return _usFlags;
            }
        }

        internal uint BaseSize
        {
            get
            {
                return _uBaseSize;
            }
        }

        internal ushort NumVtableSlots
        {
            get
            {
                return _usNumVtableSlots;
            }
        }

        internal ushort NumInterfaces
        {
            get
            {
                return _usNumInterfaces;
            }
        }

        internal uint HashCode
        {
            get
            {
                return _uHashCode;
            }
        }

        internal EETypeKind Kind
        {
            get
            {
                return (EETypeKind)(_usFlags & (ushort)EETypeFlags.EETypeKindMask);
            }
        }

        internal bool HasOptionalFields
        {
            get
            {
                return ((_usFlags & (ushort)EETypeFlags.OptionalFieldsFlag) != 0);
            }
        }

        internal bool HasGenericVariance
        {
            get
            {
                return ((_usFlags & (ushort)EETypeFlags.GenericVarianceFlag) != 0);
            }
        }

        internal bool IsFinalizable
        {
            get
            {
                return ((_usFlags & (ushort)EETypeFlags.HasFinalizerFlag) != 0);
            }
        }

        internal bool IsNullable
        {
            get
            {
                return ElementType == EETypeElementType.Nullable;
            }
        }

        internal bool IsCloned
        {
            get
            {
                return Kind == EETypeKind.ClonedEEType;
            }
        }

        internal bool IsCanonical
        {
            get
            {
                return Kind == EETypeKind.CanonicalEEType;
            }
        }

        internal bool IsString
        {
            get
            {
                // String is currently the only non-array type with a non-zero component size.
                return ComponentSize == sizeof(char) && !IsArray && !IsGenericTypeDefinition;
            }
        }

        internal bool IsArray
        {
            get
            {
                EETypeElementType elementType = ElementType;
                return elementType == EETypeElementType.Array || elementType == EETypeElementType.SzArray;
            }
        }

        internal static class WellKnownEETypes
        {
            internal static unsafe bool IsSystemObject(EEType* pEEType)
            {
                if (pEEType->IsArray)
                    return false;
                return (pEEType->NonArrayBaseType == null) && !pEEType->IsInterface;
            }

            internal static unsafe bool IsSystemArray(EEType* pEEType)
            {
                return (pEEType->ElementType == EETypeElementType.SystemArray);
            }
        }

        internal int ArrayRank
        {
            get
            {
                int boundsSize = (int)this.ParameterizedTypeShape - SZARRAY_BASE_SIZE;
                if (boundsSize > 0)
                {
                    // Multidim array case: Base size includes space for two Int32s
                    // (upper and lower bound) per each dimension of the array.
                    return boundsSize / (2 * sizeof(int));
                }
                return 1;
            }
        }

        internal bool IsSzArray
        {
            get
            {
                return ElementType == EETypeElementType.SzArray;
            }
        }

        internal bool IsGeneric
        {
            get
            {
                return ((_usFlags & (ushort)EETypeFlags.IsGenericFlag) != 0);
            }
        }

        internal bool IsGenericTypeDefinition
        {
            get
            {
                return Kind == EETypeKind.GenericTypeDefEEType;
            }
        }

        internal bool IsPointerType
        {
            get
            {
                return ElementType == EETypeElementType.Pointer;
            }
        }

        internal bool IsByRefType
        {
            get
            {
                return ElementType == EETypeElementType.ByRef;
            }
        }

        internal bool IsInterface
        {
            get
            {
                return ElementType == EETypeElementType.Interface;
            }
        }

        internal bool IsDynamicType
        {
            get
            {
                return (_usFlags & (ushort)EETypeFlags.IsDynamicTypeFlag) != 0;
            }
        }

        internal bool IsParameterizedType
        {
            get
            {
                return Kind == EETypeKind.ParameterizedEEType;
            }
        }

        internal uint ParameterizedTypeShape
        {
            get
            {
                return _uBaseSize;
            }
        }

        internal bool IsRelatedTypeViaIAT
        {
            get
            {
                return ((_usFlags & (ushort)EETypeFlags.RelatedTypeViaIATFlag) != 0);
            }
        }


        internal bool IsValueType
        {
            get
            {
                return ElementType < EETypeElementType.Class;
            }
        }
        internal bool IsEnum
        {
            get
            {
                return ElementType < EETypeElementType.Enum;
            }
        }
        internal bool HasGCPointers
        {
            get
            {
                return ((_usFlags & (ushort)EETypeFlags.HasPointersFlag) != 0);
            }
        }
        internal EEType* NonArrayBaseType
        {
            get
            {
                if (IsCloned)
                {
                    // Assuming that since this is not an Array, the CanonicalEEType is also not an array
                    return CanonicalEEType->NonArrayBaseType;
                }

                if (IsRelatedTypeViaIAT)
                {
                    return *_relatedType._ppBaseTypeViaIAT;
                }

                return _relatedType._pBaseType;
            }
        }

        internal EEType* NonClonedNonArrayBaseType
        {
            get
            {
                if (IsRelatedTypeViaIAT)
                {
                    return *_relatedType._ppBaseTypeViaIAT;
                }

                return _relatedType._pBaseType;
            }
        }

        internal EEType* RawBaseType
        {
            get
            {
                return _relatedType._pBaseType;
            }
        }

        internal EEType* CanonicalEEType
        {
            get
            {
                // cloned EETypes must always refer to types in other modules
                if (IsRelatedTypeViaIAT)
                    return *_relatedType._ppCanonicalTypeViaIAT;
                else
                    return _relatedType._pCanonicalType;
            }
        }


        internal EEType* RelatedParameterType
        {
            get
            {
                if (IsRelatedTypeViaIAT)
                    return *_relatedType._ppRelatedParameterTypeViaIAT;
                else
                    return _relatedType._pRelatedParameterType;
            }
        }

        internal unsafe IntPtr* GetVTableStartAddress()
        {
            byte* pResult;

            // EETypes are always in unmanaged memory, so 'leaking' the 'fixed pointer' is safe.
            fixed (EEType* pThis = &this)
                pResult = (byte*)pThis;

            pResult += sizeof(EEType);
            return (IntPtr*)pResult;
        }

        private static IntPtr FollowRelativePointer(int* pDist)
        {
            int dist = *pDist;
            IntPtr result = (IntPtr)((byte*)pDist + dist);
            return result;
        }

        internal EETypeElementType ElementType
        {
            get
            {
                return (EETypeElementType)((_usFlags & (ushort)EETypeFlags.ElementTypeMask) >> (ushort)EETypeFlags.ElementTypeShift);
            }
        }

        public Exception GetClasslibException(ExceptionIDs exID)
        {
            return new InternalException($"{exID}");
        }

        //// Wrapper around pointers
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct Pointer
        {
            private readonly IntPtr _value;

            public IntPtr Value
            {
                get
                {
                    return _value;
                }
            }
        }

        //// Wrapper around pointers
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe readonly struct Pointer<T> where T : unmanaged
        {
            private readonly T* _value;

            public T* Value
            {
                get
                {
                    return _value;
                }
            }
        }

        //// Wrapper around pointers that might be indirected through IAT
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe readonly struct IatAwarePointer<T> where T : unmanaged
        {
            private readonly T* _value;

            public T* Value
            {
                get
                {
                    if (((int)_value & IndirectionConstants.IndirectionCellPointer) == 0)
                        return _value;
                    return *(T**)((byte*)_value - IndirectionConstants.IndirectionCellPointer);
                }
            }
        }

        //// Wrapper around relative pointers
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct RelativePointer
        {
            private readonly int _value;

            public unsafe IntPtr Value
            {
                get
                {
                    return (IntPtr)((byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in _value)) + _value);
                }
            }
        }

        //// Wrapper around relative pointers
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe readonly struct RelativePointer<T> where T : unmanaged
        {
            private readonly int _value;

            public T* Value
            {
                get
                {
                    return (T*)((byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in _value)) + _value);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe readonly struct IatAwareRelativePointer<T> where T : unmanaged
        {
            private readonly int _value;

            public T* Value
            {
                get
                {
                    if ((_value & IndirectionConstants.IndirectionCellPointer) == 0)
                    {
                        return (T*)((byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in _value)) + _value);
                    }
                    else
                    {
                        return *(T**)((byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in _value)) + (_value & ~IndirectionConstants.IndirectionCellPointer));
                    }
                }
            }
        }
    }
}