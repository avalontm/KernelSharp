using Internal.Runtime.CompilerServices;
using System;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;

namespace Internal.Runtime.CompilerHelpers
{
    internal unsafe class StartupCodeHelpers
    {
        [RuntimeExport("RhpReversePInvoke")]
        static void RhpReversePInvoke(IntPtr frame) { }
        [RuntimeExport("RhpReversePInvokeReturn")]
        static void RhpReversePInvokeReturn(IntPtr frame) { }
        [RuntimeExport("RhpPInvoke")]
        static void RhpPInvoke(IntPtr frame) { }
        [RuntimeExport("RhpPInvokeReturn")]
        static void RhpPInvokeReturn(IntPtr frame) { }
        [RuntimeExport("RhpFallbackFailFast")]
        static void RhpFallbackFailFast() { while (true) ; }


       // [RuntimeExport("RhpStelemRef")]
        static unsafe void RhpStelemRef(Array array, int index, object obj)
        {
            fixed (int* n = &array._numComponents)
            {
                var ptr = (byte*)n;
                ptr += sizeof(void*);   // Array length is padded to 8 bytes on 64-bit
                ptr += index * array.m_pEEType->ComponentSize;  // Component size should always be 8, seeing as it's a pointer...
                var pp = (IntPtr*)ptr;
                *pp = Unsafe.As<object, IntPtr>(ref obj);
            }
        }

        [RuntimeExport("RhTypeCast_IsInstanceOfClass")]
        public static unsafe object RhTypeCast_IsInstanceOfClass(EEType* pTargetType, object obj)
        {
            if (obj == null)
                return null;

            if (pTargetType == obj.m_pEEType)
                return obj;

            var bt = obj.m_pEEType->RawBaseType;

            while (true)
            {
                if (bt == null)
                    return null;

                if (pTargetType == bt)
                    return obj;

                bt = bt->RawBaseType;
            }
        }


       // [RuntimeExport("RhpNewFast")]
        internal static unsafe object RhpNewFast(EEType* pEEType)
        {
            uint size = pEEType->BaseSize;

            // Round to next power of 8
            if (size % 8 > 0)
                size = (size / 8 + 1) * 8;

            var data = MemoryHelpers.Malloc(size);
            var obj = Unsafe.As<IntPtr, object>(ref data);
            MemoryHelpers.MemSet((byte*)data, 0, (int)size);
            *(IntPtr*)data = (IntPtr)pEEType;

            return obj;
        }

        // También necesitarás probablemente estos otros helpers relacionados
        [RuntimeExport("RhpCheckedAssignRefArithmetic")]
        private static unsafe void RhpCheckedAssignRefArithmetic(ref object dest, object src)
        {
            dest = src;
        }

        //[RuntimeExport("RhpCheckedAssignRefECX")]
        private static unsafe void RhpCheckedAssignRefECX(ref object dest, object src)
        {
            dest = src;
        }

        //[RuntimeExport("RhpAssignRef")]
        private static unsafe void RhpAssignRef(ref object dest, object src)
        {
            dest = src;
        }

       // [RuntimeExport("RhpByRefAssignRef")]
        private static unsafe void RhpByRefAssignRef(ref object dest, ref object src)
        {
            dest = src;
        }


        [RuntimeExport("RhUnbox2")]
        public static unsafe ref byte RhUnbox2(EEType* pUnboxToEEType, object obj)
        {
            if ((obj == null) || !UnboxAnyTypeCompare(obj.m_pEEType, pUnboxToEEType))
            {
                ExceptionIDs exID = obj == null ? ExceptionIDs.NullReference : ExceptionIDs.InvalidCast;
                //throw pUnboxToEEType->GetClasslibException(exID);
            }
            return ref obj.GetRawData();
        }

        static unsafe bool UnboxAnyTypeCompare(EEType* pEEType, EEType* ptrUnboxToEEType)
        {
            if (TypeCast.AreTypesEquivalent(pEEType, ptrUnboxToEEType))
                return true;

            if (pEEType->ElementType == ptrUnboxToEEType->ElementType)
            {
                // Enum's and primitive types should pass the UnboxAny exception cases
                // if they have an exactly matching cor element type.
                switch (ptrUnboxToEEType->ElementType)
                {
                    case EETypeElementType.Byte:
                    case EETypeElementType.SByte:
                    case EETypeElementType.Int16:
                    case EETypeElementType.UInt16:
                    case EETypeElementType.Int32:
                    case EETypeElementType.UInt32:
                    case EETypeElementType.Int64:
                    case EETypeElementType.UInt64:
                    case EETypeElementType.IntPtr:
                    case EETypeElementType.UIntPtr:
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Crea un nuevo array del tipo especificado por el EEType.
        /// </summary>
        /// <param name="pEEType">Puntero al EEType del array a crear</param>
        /// <param name="length">Longitud del array</param>
        /// <returns>El nuevo array creado</returns>
        // [RuntimeExport("RhpNewArray")]
        internal static unsafe object RhpNewArray(EEType* pEEType, int length)
        {
            // Validación de argumentos
            if (pEEType == null)
                ThrowHelpers.ThrowArgumentNullException("pEEType");

            if (length < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("length");

            // Determinar si el tipo es de valor o de referencia
            bool isValueType = pEEType->IsValueType;

            // Crear el array apropiado basado en el tipo
            if (isValueType)
            {
                // Para tipos de valor, crear un KernelValueArray apropiado
                return CreateValueTypeArray(pEEType, length);
            }
            else
            {
                // Para tipos de referencia, crear un KernelRefArray
                return CreateReferenceTypeArray(pEEType, length);
            }
        }

        /// <summary>
        /// Crea un array para un tipo de referencia.
        /// </summary>
        private static unsafe object CreateReferenceTypeArray(EEType* pEEType, int length)
        {
            // Determinar el tipo específico de referencia
            byte elementType = (byte)pEEType->ElementType;

            // Para strings, crear un KernelRefArray<string>
            if (elementType == (byte)EETypeElementType.Class && pEEType->IsString)
            {
                return new KernelStringArray<string>(length);
            }

            // Para otros tipos de referencia, podríamos usar tipos específicos si podemos determinarlos
            // o usar un KernelRefArray<object> genérico
            return new KernelStringArray<object>(length);
        }

        /// <summary>
        /// Crea un array para un tipo de valor.
        /// </summary>
        private static unsafe object CreateValueTypeArray(EEType* pEEType, int length)
        {
            // Determinar el tipo específico de valor y crear el array correspondiente
            // Nota: Esto es una simplificación. En un escenario real, necesitarías 
            // determinar el tipo exacto del EEType.

            // Ejemplo para algunos tipos comunes - extiende esto según tus necesidades
            EETypeElementType elementType = pEEType->ElementType;

            switch (elementType)
            {
                case EETypeElementType.Int32:
                    return new KernelValueArray<int>(length);

                case EETypeElementType.Byte:
                    return new KernelValueArray<byte>(length);

                case EETypeElementType.Char:
                    return new KernelValueArray<char>(length);

                case EETypeElementType.Boolean:
                    return new KernelValueArray<bool>(length);

                case EETypeElementType.Int16:
                    return new KernelValueArray<short>(length);

                case EETypeElementType.UInt16:
                    return new KernelValueArray<ushort>(length);

                case EETypeElementType.Int64:
                    return new KernelValueArray<long>(length);

                case EETypeElementType.UInt64:
                    return new KernelValueArray<ulong>(length);

                case EETypeElementType.Single:
                    return new KernelValueArray<float>(length);

                case EETypeElementType.Double:
                    return new KernelValueArray<double>(length);

                default:
                    // Para otros tipos de valor, o si no podemos determinar el tipo específico,
                    // caer en la implementación original basada en memoria
                    return CreateArrayUsingRawMemory(pEEType, length);
            }
        }

        /// <summary>
        /// Crea un array usando la asignación de memoria directa (método original).
        /// </summary>
        private static unsafe object CreateArrayUsingRawMemory(EEType* pEEType, int length)
        {
            // Esta es la implementación original para cuando no podemos usar KernelValueArray o KernelRefArray

            // Calcular el tamaño total necesario para el array
            ulong headerSize = (ulong)pEEType->BaseSize;
            ulong elementsTotalSize = (ulong)length * (ulong)pEEType->ComponentSize;
            ulong totalSize = headerSize + elementsTotalSize;

            // Alinear a 8 bytes
            if (totalSize % 8 != 0)
                totalSize = ((totalSize / 8) + 1) * 8;

            // Asignar memoria para el array
            IntPtr memoryBlock = (IntPtr)MemoryHelpers.Malloc(totalSize);

            if (memoryBlock == IntPtr.Zero)
                ThrowHelpers.ThrowOutOfMemoryException();

            // Inicializar toda la memoria a cero
            MemoryHelpers.MemSet((byte*)memoryBlock, 0, (int)totalSize);

            // Configurar el EEType del objeto
            *(IntPtr*)memoryBlock = (IntPtr)pEEType;

            // Configurar la longitud del array
            *((int*)memoryBlock + 1) = length;

            // Convertir el bloque de memoria a un objeto y devolverlo
            return Unsafe.As<IntPtr, object>(ref memoryBlock);
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
                var blockAddr = (long)(*pBlock);

                if ((blockAddr & GCStaticRegionConstants.Uninitialized) == GCStaticRegionConstants.Uninitialized)
                {
                    var obj = RhpNewFast((EEType*)(blockAddr & ~GCStaticRegionConstants.Mask));

                    if ((blockAddr & GCStaticRegionConstants.HasPreInitializedData) == GCStaticRegionConstants.HasPreInitializedData)
                    {
                        IntPtr pPreInitDataAddr = *(pBlock + 1);
                        fixed (byte* p = &obj.GetRawData())
                        {
                            Buffer.Memcpy(p, (byte*)pPreInitDataAddr, obj.GetRawDataSize());
                        }
                    }

                    var handle = MemoryHelpers.Malloc((ulong)sizeof(IntPtr));
                    *(IntPtr*)handle = Unsafe.As<object, IntPtr>(ref obj);
                    *pBlock = handle;
                }
            }
        }

    }
}