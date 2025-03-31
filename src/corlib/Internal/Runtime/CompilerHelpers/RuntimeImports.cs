using Internal.Runtime.CompilerServices;
using System;
using System.Runtime;

namespace Internal.Runtime.CompilerHelpers
{
    internal unsafe class RuntimeImports
    {
        // Métodos esenciales para la gestión de memoria
        [RuntimeExport("RhpInitializeHeap")]
        public static void RhpInitializeHeap(void* heap, ulong size)
        {
            // No hacemos nada especial aquí, ya que malloc ya está inicializado
        }

        [RuntimeExport("RhpNewArray")]
        public static unsafe object RhpNewArray(EEType* pEEType, int length)
        {
            var size = pEEType->BaseSize + (uint)length * pEEType->ComponentSize;

            // Round to next power of 8
            if (size % 8 > 0)
                size = ((size / 8) + 1) * 8;

            var data = MemoryHelpers.Malloc(size);
            var obj = Unsafe.As<IntPtr, object>(ref data);
            MemoryHelpers.MemSet((byte*)data, 0, (int)size);
            *(IntPtr*)data = (IntPtr)pEEType;

            var b = (byte*)data;
            b += sizeof(IntPtr);
            MemoryHelpers.MemCpy(b, (byte*)(&length), sizeof(int));

            return obj;
        }

        [RuntimeExport("RhpReportExceptionForCatch")]
        public static void RhpReportExceptionForCatch()
        {
            // Manejo simple de excepciones - simplemente retorna
        }

        [RuntimeExport("RhpReportUnhandledException")]
        public static void RhpReportUnhandledException()
        {
            // Manejo de excepciones no controladas
            while (true) { } // Detener el sistema
        }

        [RuntimeExport("RhpCallCatchFunclet")]
        public static void* RhpCallCatchFunclet(void* framePtr, void* catchHandler, void* exceptionObj)
        {
            return exceptionObj; // Simplificado
        }

        [RuntimeExport("RhpSetTeb")]
        public static void RhpSetTeb(void* teb)
        {
            // No hacemos nada en esta implementación simplificada
        }

        [RuntimeExport("RhpGetThunksBase")]
        public static void* RhpGetThunksBase()
        {
            return null; // Simplificado
        }

        [RuntimeExport("RhpGetThunkSize")]
        public static int RhpGetThunkSize()
        {
            return 0; // Simplificado
        }

        [RuntimeExport("RhpGetNumThunks")]
        public static int RhpGetNumThunks()
        {
            return 0; // Simplificado
        }

        [RuntimeExport("RhpNewObject")]
        public static void* RhpNewObject(void* type)
        {
            // Asumimos un tamaño fijo para todos los objetos en esta implementación simple
            return (void*)MemoryHelpers.Malloc(24); // 24 bytes por objeto
        }

        [RuntimeExport("RhpCopyObjectContents")]
        public static unsafe void RhpCopyObjectContents(void* destination, void* source, int size)
        {
            byte* dest = (byte*)destination;
            byte* src = (byte*)source;

            for (int i = 0; i < size; i++)
            {
                dest[i] = src[i];
            }
        }


        [RuntimeExport("RhpAssignRef")]
        public static unsafe void RhpAssignRef(void** address, void* obj)
        {
            *address = obj;
        }

        [RuntimeExport("RhpByRefAssignRef")]
        public static unsafe void RhpByRefAssignRef(void** address, void* obj)
        {
            *address = obj;
        }

        // [RuntimeExport("RhpCheckedAssignRef")]
        public static void RhpCheckedAssignRef(void** address, void* obj)
        {
            *address = obj;
        }

        [RuntimeExport("RhpStelemRef")]
        public static void RhpStelemRef(void* array, int index, void* obj)
        {
            void** arr = (void**)array;
            arr[index] = obj;
        }
    }
}