﻿using System;
using System.Runtime.InteropServices;

namespace Internal.Runtime
{

    [StructLayout(LayoutKind.Sequential)]
    struct ModuleInfoRow
    {
        public ReadyToRunSectionType SectionId;
        public int Flags;
        public IntPtr Start;
        public IntPtr End;

        public bool HasEndPointer => !End.Equals(IntPtr.Zero);
        public int Length => (int)((ulong)End - (ulong)Start);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ReadyToRunHeaderConstants
    {
        public const uint Signature = 0x00525452; // 'RTR'

        public const ushort CurrentMajorVersion = 4;
        public const ushort CurrentMinorVersion = 1;
    }

#pragma warning disable 0169
    [StructLayout(LayoutKind.Sequential)]
    internal struct ReadyToRunHeader
    {
        public uint Signature;      // ReadyToRunHeaderConstants.Signature
        public ushort MajorVersion;
        public ushort MinorVersion;

        public uint Flags;

        public ushort NumberOfSections;
        public byte EntrySize;
        public byte EntryType;

        // Array of sections follows.
    };
#pragma warning restore 0169

    //
    // ReadyToRunSectionType IDs are used by the runtime to look up specific global data sections
    // from each module linked into the final binary. New sections should be added at the bottom
    // of the enum and deprecated sections should not be removed to preserve ID stability.
    //
    // This list should be kept in sync with the runtime version at
    // https://github.com/dotnet/coreclr/blob/master/src/inc/readytorun.h
    //
    public enum ReadyToRunSectionType : int
    {
        //
        // CoreCLR ReadyToRun sections
        //
        CompilerIdentifier = 100,
        ImportSections = 101,
        RuntimeFunctions = 102,
        MethodDefEntryPoints = 103,
        ExceptionInfo = 104,
        DebugInfo = 105,
        DelayLoadMethodCallThunks = 106,
        // 107 is deprecated - it was used by an older format of AvailableTypes
        AvailableTypes = 108,
        InstanceMethodEntryPoints = 109,
        InliningInfo = 110, // Added in v2.1, deprecated in 4.1
        ProfileDataInfo = 111, // Added in v2.2
        ManifestMetadata = 112, // Added in v2.3
        AttributePresence = 113, // Added in V3.1
        InliningInfo2 = 114, // Added in 4.1
        ComponentAssemblies = 115, // Added in 4.1
        OwnerCompositeExecutable = 116, // Added in 4.1

        //
        // CoreRT ReadyToRun sections
        //
        StringTable = 200, // Unused
        GCStaticRegion = 201,
        ThreadStaticRegion = 202,
        InterfaceDispatchTable = 203,
        TypeManagerIndirection = 204,
        EagerCctor = 205,
        FrozenObjectRegion = 206,
        GCStaticDesc = 207,
        ThreadStaticOffsetRegion = 208,
        ThreadStaticGCDescRegion = 209,
        ThreadStaticIndex = 210,
        LoopHijackFlag = 211,
        ImportAddressTables = 212,

        // Sections 300 - 399 are reserved for RhFindBlob backwards compatibility
        ReadonlyBlobRegionStart = 300,
        ReadonlyBlobRegionEnd = 399,
    }

    [Flags]
    internal enum ModuleInfoFlags : int
    {
        HasEndPointer = 0x1,
    }
}