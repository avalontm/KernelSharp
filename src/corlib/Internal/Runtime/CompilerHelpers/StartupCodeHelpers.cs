using Internal.Runtime.CompilerServices;
using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Internal.Runtime.CompilerHelpers
{
    internal unsafe class StartupCodeHelpers
    {

        [RuntimeExport("RhpCheckedAssignRef")]
        internal static unsafe void RhpCheckedAssignRef(ref object target, object value)
        {
            // Primero, verificamos si el valor es nulo, si lo es, asignamos directamente
            if (value == null)
            {
                target = null;
                return;
            }

            // Obtenemos los tipos de los objetos (EEType* es el tipo de la instancia)
            EEType* targetType = target.m_pEEType;
            EEType* valueType = value.m_pEEType;

            // Comprobamos que los tipos sean compatibles
            if (!AreTypesEquivalent(targetType, valueType))
            {
                // Si no son equivalentes, lanzamos una excepción (o cualquier mecanismo de control de errores)
               ThrowHelpers.InvalidCastException("Cannot assign value of incompatible type.");
            }

            // Si son compatibles, realizamos la asignación
            target = value;
        }

        [RuntimeExport("RhTypeCast_AreTypesEquivalent")]
        public static unsafe bool AreTypesEquivalent(EEType* pType1, EEType* pType2)
        {
            if (pType1 == pType2)
                return true;

            if (pType1->IsCloned)
                pType1 = pType1->CanonicalEEType;

            if (pType2->IsCloned)
                pType2 = pType2->CanonicalEEType;

            if (pType1 == pType2)
                return true;

            if (pType1->IsParameterizedType && pType2->IsParameterizedType)
                return AreTypesEquivalent(pType1->RelatedParameterType, pType2->RelatedParameterType) && pType1->ParameterizedTypeShape == pType2->ParameterizedTypeShape;

            return false;
        }


        [RuntimeExport("RhpLdelemaRef")]
        public static unsafe ref object LdelemaRef(Array array, int index, IntPtr elementType)
        {
            //Debug.Assert(array.EEType->IsArray, "first argument must be an array");

            EEType* elemType = (EEType*)elementType;
            EEType* arrayElemType = array.m_pEEType->RelatedParameterType;

            if (!AreTypesEquivalent(elemType, arrayElemType))
            {
                // Throw the array type mismatch exception defined by the classlib, using the input array's EEType* 
                // to find the correct classlib.

                // throw array.EEType->GetClasslibException(ExceptionIDs.ArrayTypeMismatch);
            }

            ref object rawData = ref Unsafe.As<byte, object>(ref Unsafe.As<RawArrayData>(array).Data);
            return ref Unsafe.Add(ref rawData, index);
        }
        
 
        [RuntimeExport("RhpNewFast")]
        internal static unsafe object RhpNewFast(EEType* pEEType)
        {
            var size = pEEType->BaseSize;

            // Round to next power of 8
            if (size % 8 > 0)
                size = ((size / 8) + 1) * 8;

            var data = MemoryHelpers.Malloc(size);
            var obj = Unsafe.As<IntPtr, object>(ref data);
            MemoryHelpers.MemSet((byte*)data, 0, (int)size);
            *(IntPtr*)data = (IntPtr)pEEType;

            return obj;
        }

        [RuntimeExport("__imp_GetCurrentThreadId")]
        public static int __imp_GetCurrentThreadId() => 0;

        [RuntimeExport("__CheckForDebuggerJustMyCode")]
        public static int __CheckForDebuggerJustMyCode() => 0;

        [RuntimeExport("__fail_fast")]
        static void FailFast() { while (true) ; }

        [RuntimeExport("RhpFallbackFailFast")]
        static void RhpFallbackFailFast() { while (true) ; }

        [RuntimeExport("RhpReversePInvoke2")]
        static void RhpReversePInvoke2(IntPtr frame) { }

        [RuntimeExport("RhpReversePInvokeReturn2")]
        static void RhpReversePInvokeReturn2(IntPtr frame) { }

        [RuntimeExport("RhpReversePInvoke")]
        static void RhpReversePInvoke(IntPtr frame) { }

        [RuntimeExport("RhpReversePInvokeReturn")]
        static void RhpReversePInvokeReturn(IntPtr frame) { }

        [RuntimeExport("RhpPInvoke")]
        static void RhpPinvoke(IntPtr frame) { }

        [RuntimeExport("RhpPInvokeReturn")]
        static void RhpPinvokeReturn(IntPtr frame) { }

        public static void Test()
        {
            Debug.WriteLine("InitializeModules");
        }

        [RuntimeExport("RhpDbl2Int")]
        internal static int RhpDbl2Int(double value)
        {
            // Manejo de casos especiales
            if (double.IsNaN(value))
                return 0;

            if (value >= int.MaxValue)
                return int.MaxValue;

            if (value <= int.MinValue)
                return int.MinValue;

            // Redondeo hacia cero (truncamiento)
            return (int)Math.Truncate(value);
        }

        [RuntimeExport("RhpDbl2Lng")]
        internal static long RhpDbl2Lng(double value)
        {
            // Manejo de casos especiales
            if (double.IsNaN(value))
                return 0;

            if (value >= long.MaxValue)
                return long.MaxValue;

            if (value <= long.MinValue)
                return long.MinValue;

            // Redondeo hacia cero (truncamiento)
            return (long)Math.Truncate(value);
        }

        public static void InitializeModules(IntPtr Modules)
        {
            for (int i = 0; ; i++)
            {
                if (((IntPtr*)Modules)[i].Equals(IntPtr.Zero))
                    break;

                var header = (ReadyToRunHeader*)((IntPtr*)Modules)[i];
                var sections = (ModuleInfoRow*)(header + 1);

                if (header->Signature != ReadyToRunHeaderConstants.Signature)
                {
                    break;
                }

                for (int k = 0; k < header->NumberOfSections; k++)
                {
                    if (sections[k].SectionId == ReadyToRunSectionType.GCStaticRegion)
                        InitializeStatics(sections[k].Start, sections[k].End);

                    if (sections[k].SectionId == ReadyToRunSectionType.EagerCctor)
                        RunEagerClassConstructors(sections[k].Start, sections[k].End);
                }
            }
        }

        static unsafe void RunEagerClassConstructors(IntPtr cctorTableStart, IntPtr cctorTableEnd)
        {
            for (IntPtr* tab = (IntPtr*)cctorTableStart; tab < (IntPtr*)cctorTableEnd; tab++)
            {
                ((delegate*<void>)(*tab))();
            }
        }

        static unsafe void InitializeStatics(IntPtr rgnStart, IntPtr rgnEnd)
        {
            for (IntPtr* block = (IntPtr*)rgnStart; block < (IntPtr*)rgnEnd; block++)
            {
                var pBlock = (IntPtr*)*block;
                var blockAddr = (int)(*pBlock);

                if ((blockAddr & GCStaticRegionConstants.Uninitialized) == GCStaticRegionConstants.Uninitialized)
                {
                    var obj = RhpNewFast((EEType*)(blockAddr & ~GCStaticRegionConstants.Mask));

                    if ((blockAddr & GCStaticRegionConstants.HasPreInitializedData) == GCStaticRegionConstants.HasPreInitializedData)
                    {
                        IntPtr pPreInitDataAddr = *(pBlock + 1);
                        fixed (byte* p = &obj.GetRawData())
                        {
                            MemoryHelpers.MemCpy(p, (byte*)pPreInitDataAddr, obj.GetRawDataSize());
                        }
                    }

                    var handle = MemoryHelpers.Malloc((uint)sizeof(IntPtr));
                    *(IntPtr*)handle = Unsafe.As<object, IntPtr>(ref obj);
                    *pBlock = handle;
                }
            }
        }

    }
}