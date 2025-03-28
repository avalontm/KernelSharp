using System;
using System.Runtime;

namespace Internal.Runtime.CompilerHelpers
{
    internal static class LdTokenHelpers
    {
        [RuntimeExport("RhLdToken")]
        public static IntPtr RhLdToken(IntPtr token)
        {
            // Simplemente devuelve el token tal cual
            return token;
        }

        [RuntimeExport("RhHandleAlloc")]
        public static IntPtr RhHandleAlloc(IntPtr value, int type)
        {
            // Devolver el mismo valor para manipulación básica
            return value;
        }

        [RuntimeExport("GetRuntimeType")]
        public static unsafe Type GetRuntimeType(IntPtr pEEType)
        {
            // Implementación básica - convierte el puntero EEType a un tipo
            return Type.GetTypeFromHandle(new RuntimeTypeHandle(pEEType));
        }

        [RuntimeExport("GetRuntimeTypeHandle")]
        public static unsafe RuntimeTypeHandle GetRuntimeTypeHandle(IntPtr pEEType)
        {
            // Devuelve un RuntimeTypeHandle basado en el puntero EEType
            return new RuntimeTypeHandle(pEEType);
        }
    }
}