//
// This is where we group together all the internal calls.
//

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.Runtime;

#pragma warning disable SYSLIB1054 // Use DllImport here instead of LibraryImport because this file is used by Test.CoreLib

namespace System.Runtime
{
    internal enum DispatchCellType
    {
        InterfaceAndSlot = 0x0,
        MetadataToken = 0x1,
        VTableOffset = 0x2,
    }

    internal unsafe struct DispatchCellInfo
    {
        public DispatchCellType CellType;
        public MethodTable* InterfaceType;
        public ushort InterfaceSlot;
        public byte HasCache;
        public uint MetadataToken;
        public uint VTableOffset;
    }

    // Constants used with RhpGetClasslibFunction, to indicate which classlib function
    // we are interested in.
    // Note: make sure you change the def in ICodeManager.h if you change this!
    internal enum ClassLibFunctionId
    {
        GetRuntimeException = 0,
        FailFast = 1,
        // UnhandledExceptionHandler = 2, // unused
        AppendExceptionStackFrame = 3,
        // unused = 4,
        GetSystemArrayEEType = 5,
        OnFirstChance = 6,
        OnUnhandledException = 7,
        IDynamicCastableIsInterfaceImplemented = 8,
        IDynamicCastableGetInterfaceImplementation = 9,
        ObjectiveCMarshalTryGetTaggedMemory = 10,
        ObjectiveCMarshalGetIsTrackedReferenceCallback = 11,
        ObjectiveCMarshalGetOnEnteredFinalizerQueueCallback = 12,
        ObjectiveCMarshalGetUnhandledExceptionPropagationHandler = 13,
    }

}