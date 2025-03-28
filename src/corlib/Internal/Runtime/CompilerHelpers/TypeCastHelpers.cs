using Internal.Runtime.CompilerServices;
using System;
using System.Runtime;

namespace Internal.Runtime.CompilerHelpers
{
    internal static unsafe class TypeCastHelpers
    {
        [RuntimeExport("RhTypeCast_IsInstanceOfArray")]
        public static bool IsInstanceOfArray(EEType* pTargetArrayEEType, object obj)
        {
            // Verificar si el objeto es nulo
            if (obj == null)
                return false;

            // Obtener el EEType del objeto
            IntPtr objPtr = Unsafe.As<object, IntPtr>(ref obj);
            EEType* pObjEEType = *(EEType**)objPtr;

            // Verificar si es un array
            if (pObjEEType == null)
                return false;

            // Verificación de que el objeto es un array
            return pObjEEType->IsArray;
        }

        [RuntimeExport("RhTypeCast_CheckCastClassSpecial")]
        public static object CheckCastClassSpecial(EEType* pTargetType, object obj)
        {
            if (obj == null)
                return null;

            // Get the object's EEType
            IntPtr objPtr = Unsafe.As<object, IntPtr>(ref obj);
            EEType* pObjType = *(EEType**)objPtr;

            // Perform type compatibility check
            if (!IsAssignableFrom(pTargetType, pObjType))
                ThrowHelpers.ThrowInvalidCastException();

            return obj;
        }

        private static bool IsAssignableFrom(EEType* pTargetType, EEType* pSourceType)
        {
            // Simple type compatibility check
            // In a real implementation, this would be more complex
            if (pTargetType == pSourceType)
                return true;

            // Check base types and interfaces (simplified)
            EEType* currentType = pSourceType;
            while (currentType != null)
            {
                if (currentType == pTargetType)
                    return true;

                // Move to base type
                currentType = currentType->m_pEEType;
            }

            return false;
        }

        [RuntimeExport("RhTypeCast_CheckCastClass")]
        public static object RhTypeCast_CheckCastClass(EEType* pTargetType, object obj)
        {
            if (obj == null)
                return null;

            // Get the object's EEType
            IntPtr objPtr = Unsafe.As<object, IntPtr>(ref obj);
            EEType* pObjType = *(EEType**)objPtr;

            // Perform type compatibility check
            if (!IsAssignableFrom(pTargetType, pObjType))
                ThrowHelpers.ThrowInvalidCastException();

            return obj;
        }

        [RuntimeExport("RhpCheckedAssignRef")]
        static unsafe void RhpCheckedAssignRef(void** address, void* obj)
        {
            *address = obj;
        }

        internal static unsafe class WriteBarrierHelpers
        {
            [RuntimeExport("RhpCheckedAssignRefEAX")]
            public static void RhpCheckedAssignRefEAX(void** dst, void* src)
            {
                // Esta función verifica asignaciones de referencias
                // En un GC real, aquí se implementarían verificaciones y barreras de escritura
                *dst = src;
            }
        }
    }
}

