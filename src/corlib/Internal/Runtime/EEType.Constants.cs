﻿using System;

namespace Internal.Runtime
{
    /// <summary>
    /// Represents the flags stored in the <c>_usFlags</c> field of a <c>System.Runtime.EEType</c>.
    /// </summary>
    [Flags]
    internal enum EETypeFlags : ushort
    {
        /// <summary>
        /// There are four kinds of EETypes, defined in <c>Kinds</c>.
        /// </summary>
        EETypeKindMask = 0x0003,

        /// <summary>
        /// This flag is set when m_RelatedType is in a different module.  In that case, _pRelatedType
        /// actually points to an IAT slot in this module, which then points to the desired EEType in the
        /// other module.  In other words, there is an extra indirection through m_RelatedType to get to 
        /// the related type in the other module.  When this flag is set, it is expected that you use the 
        /// "_ppXxxxViaIAT" member of the RelatedTypeUnion for the particular related type you're 
        /// accessing.
        /// </summary>
        RelatedTypeViaIATFlag = 0x0004,

        /// <summary>
        /// This type was dynamically allocated at runtime.
        /// </summary>
        IsDynamicTypeFlag = 0x0008,

        /// <summary>
        /// This EEType represents a type which requires finalization.
        /// </summary>
        HasFinalizerFlag = 0x0010,

        /// <summary>
        /// This type contain GC pointers.
        /// </summary>
        HasPointersFlag = 0x0020,

        // Unused = 0x0040,

        /// <summary>
        /// This type is generic and one or more of its type parameters is co- or contra-variant. This
        /// only applies to interface and delegate types.
        /// </summary>
        GenericVarianceFlag = 0x0080,

        /// <summary>
        /// This type has optional fields present.
        /// </summary>
        OptionalFieldsFlag = 0x0100,

        // Unused = 0x0200,

        /// <summary>
        /// This type is generic.
        /// </summary>
        IsGenericFlag = 0x0400,

        /// <summary>
        /// We are storing a EETypeElementType in the upper bits for unboxing enums.
        /// </summary>
        ElementTypeMask = 0xf800,
        ElementTypeShift = 11,

        /// <summary>
        /// Single mark to check TypeKind and two flags. When non-zero, casting is more complicated.
        /// </summary>
        ComplexCastingMask = EETypeKindMask | RelatedTypeViaIATFlag | GenericVarianceFlag
    };

    internal enum EETypeKind : ushort
    {
        /// <summary>
        /// Represents a standard ECMA type
        /// </summary>
        CanonicalEEType = 0x0000,

        /// <summary>
        /// Represents a type cloned from another EEType
        /// </summary>
        ClonedEEType = 0x0001,

        /// <summary>
        /// Represents a parameterized type. For example a single dimensional array or pointer type
        /// </summary>
        ParameterizedEEType = 0x0002,

        /// <summary>
        /// Represents an uninstantiated generic type definition
        /// </summary>
        GenericTypeDefEEType = 0x0003,
    }

    /// <summary>
    /// These are flag values that are rarely set for types. If any of them are set then an optional field will
    /// be associated with the EEType to represent them.
    /// </summary>
    [Flags]
    internal enum EETypeRareFlags : int
    {
        /// <summary>
        /// This type requires 8-byte alignment for its fields on certain platforms (only ARM currently).
        /// </summary>
        RequiresAlign8Flag = 0x00000001,

        // UNUSED1 = 0x00000002,

        // UNUSED = 0x00000004,

        // UNUSED = 0x00000008,

        // UNUSED = 0x00000010,

        /// <summary>
        /// This EEType has a Class Constructor
        /// </summary>
        HasCctorFlag = 0x0000020,

        // UNUSED2 = 0x00000040,

        /// <summary>
        /// This EEType was constructed from a universal canonical template, and has
        /// its own dynamically created DispatchMap (does not use the DispatchMap of its template type)
        /// </summary>
        HasDynamicallyAllocatedDispatchMapFlag = 0x00000080,

        /// <summary>
        /// This EEType represents a structure that is an HFA
        /// </summary>
        IsHFAFlag = 0x00000100,

        /// <summary>
        /// This EEType has sealed vtable entries
        /// </summary>
        HasSealedVTableEntriesFlag = 0x00000200,

        /// <summary>
        /// This dynamically created types has gc statics
        /// </summary>
        IsDynamicTypeWithGcStatics = 0x00000400,

        /// <summary>
        /// This dynamically created types has non gc statics
        /// </summary>
        IsDynamicTypeWithNonGcStatics = 0x00000800,

        /// <summary>
        /// This dynamically created types has thread statics
        /// </summary>
        IsDynamicTypeWithThreadStatics = 0x00001000,

        /// <summary>
        /// This EEType contains a pointer to dynamic module information
        /// </summary>
        HasDynamicModuleFlag = 0x00002000,

        /// <summary>
        /// This EEType is an abstract class (but not an interface).
        /// </summary>
        IsAbstractClassFlag = 0x00004000,

        /// <summary>
        /// This EEType is for a Byref-like class (TypedReference, Span&lt;T&gt;,...)
        /// </summary>
        IsByRefLikeFlag = 0x00008000,
    }

    internal enum EETypeField
    {
        ETF_InterfaceMap,
        ETF_TypeManagerIndirection,
        ETF_WritableData,
        ETF_Finalizer,
        ETF_OptionalFieldsPtr,
        ETF_SealedVirtualSlots,
        ETF_DynamicTemplateType,
        ETF_DynamicDispatchMap,
        ETF_DynamicModule,
        ETF_GenericDefinition,
        ETF_GenericComposition,
        ETF_DynamicGcStatics,
        ETF_DynamicNonGcStatics,
        ETF_DynamicThreadStaticOffset,
    }

    // Subset of the managed TypeFlags enum understood by Redhawk.
    // This should match the values in the TypeFlags enum except for the special
    // entry that marks System.Array specifically.
    internal enum EETypeElementType
    {
        // Primitive
        Unknown = 0x00,
        Void = 0x01,
        Boolean = 0x02,
        Char = 0x03,
        SByte = 0x04,
        Byte = 0x05,
        Int16 = 0x06,
        UInt16 = 0x07,
        Int32 = 0x08,
        UInt32 = 0x09,
        Int64 = 0x0A,
        UInt64 = 0x0B,
        IntPtr = 0x0C,
        UIntPtr = 0x0D,
        Single = 0x0E,
        Double = 0x0F,

        ValueType = 0x10,
        Enum = 0x11, // EETypes store enums as their underlying type
        Nullable = 0x12,
        Unused = 0x13,

        Class = 0x14,
        Interface = 0x15,

        SystemArray = 0x16, // System.Array type

        Array = 0x17,
        SzArray = 0x18,
        ByRef = 0x19,
        Pointer = 0x1A,
    }

    internal enum EETypeOptionalFieldTag : byte
    {
        /// <summary>
        /// Extra <c>EEType</c> flags not commonly used such as HasClassConstructor
        /// </summary>
        RareFlags,

        /// <summary>
        /// Index of the dispatch map pointer in the DispathMap table
        /// </summary>
        DispatchMap,

        /// <summary>
        /// Padding added to a value type when allocated on the GC heap
        /// </summary>
        ValueTypeFieldPadding,

        /// <summary>
        /// Offset in Nullable&lt;T&gt; of the value field
        /// </summary>
        NullableValueOffset,

        // Number of field types we support
        Count
    }

    // Keep this synchronized with GenericVarianceType in rhbinder.h.
    internal enum GenericVariance : byte
    {
        NonVariant = 0,
        Covariant = 1,
        Contravariant = 2,
        ArrayCovariant = 0x20,
    }

    internal static class ParameterizedTypeShapeConstants
    {
        // NOTE: Parameterized type kind is stored in the BaseSize field of the EEType.
        // Array types use their actual base size. Pointer and ByRef types are never boxed,
        // so we can reuse the EEType BaseSize field to indicate the kind.
        // It's important that these values always stay lower than any valid value of a base
        // size for an actual array.
        public const int Pointer = 0;
        public const int ByRef = 1;
    }

    internal static class StringComponentSize
    {
        public const int Value = sizeof(char);
    }

    internal static class WritableData
    {
        public static int GetSize(int pointerSize) => pointerSize;
        public static int GetAlignment(int pointerSize) => pointerSize;
    }
}
