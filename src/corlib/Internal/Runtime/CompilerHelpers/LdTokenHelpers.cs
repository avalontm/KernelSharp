using System;
using System.Runtime;
using System.Runtime.CompilerServices;

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
        public static unsafe Type GetRuntimeType(EETypePtr pEEType)
        {
            // Implementación básica - convierte el puntero EEType a un tipo
            return Type.GetTypeFromHandle(new RuntimeTypeHandle(pEEType));
        }

        [RuntimeExport("GetRuntimeTypeHandle")]
        public static unsafe RuntimeTypeHandle GetRuntimeTypeHandle(EETypePtr pEEType)
        {
            // Devuelve un RuntimeTypeHandle basado en el puntero EEType
            return new RuntimeTypeHandle(pEEType);
        }

        /// <summary>
        /// Obtiene el puntero EEType para un objeto dado.
        /// </summary>
        [RuntimeExport("GetEETypePtr")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EETypePtr GetEETypePtr(object obj)
        {
            if (obj == null)
                return default;

            // En una implementación real, obtendríamos el EEType del objeto 
            // del encabezado del objeto.
            // Para una implementación minimalista, usamos RuntimeHelpers.
            return (EETypePtr)obj;
        }

        /// <summary>
        /// Obtiene el puntero EEType para un RuntimeTypeHandle dado.
        /// </summary>
        [RuntimeExport("GetEETypeFromRuntimeTypeHandle")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EETypePtr GetEETypeFromRuntimeTypeHandle(RuntimeTypeHandle handle)
        {
            // Extraemos el EETypePtr directamente del RuntimeTypeHandle
            return new EETypePtr(handle);
        }
    }
}