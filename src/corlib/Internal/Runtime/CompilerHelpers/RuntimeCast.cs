using System;
using System.Runtime;
using static Internal.Runtime.EEType;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// Proporciona funciones de ayuda para operaciones de casting entre tipos.
    /// </summary>
    public static unsafe class RuntimeCast
    {
        /// <summary>
        /// Verifica si un objeto puede ser convertido a un tipo específico.
        /// </summary>
        /// <param name="obj">Objeto que se quiere convertir</param>
        /// <param name="targetType">Tipo destino de la conversión</param>
        /// <returns>El objeto original si la conversión es válida, null en caso contrario</returns>
        [RuntimeExport("RhTypeCast_CheckCastAny")]
        public static object RhTypeCast_CheckCastAny(object obj, EETypePtr targetType)
        {
            // Si el objeto es null, siempre es válido para cualquier tipo de referencia
            if (obj == null)
                return null;

            // Obtener el EEType del objeto
            EEType* objEEType = obj.m_pEEType;

            // Comprobar si los tipos son compatibles
            if (AreTypesAssignable(objEEType, targetType._value))
                return obj;

            // Si el tipo destino es una interfaz, requerimos verificación adicional
            if (targetType._value->IsInterface)
            {
                if (ImplementsInterface(objEEType, targetType._value))
                    return obj;
            }

            // Si el destino es un tipo genérico con varianza, hay que comprobar la compatibilidad
            if (targetType._value->HasGenericVariance)
            {
                if (CheckVarianceCompatibility(objEEType, targetType._value))
                    return obj;
            }

            // La conversión no es válida
            ThrowInvalidCastException(obj, targetType);
            return null; // Nunca llega aquí (ThrowInvalidCastException lanza una excepción)
        }

        /// <summary>
        /// Comprueba si un tipo es asignable a otro.
        /// </summary>
        private static bool AreTypesAssignable(EEType* sourceType, EEType* targetType)
        {
            // Caso 1: Los tipos son idénticos
            if (sourceType == targetType)
                return true;

            // Caso 2: Verificar jerarquía de herencia
            EEType* currentType = sourceType;
            while (currentType != null)
            {
                if (currentType == targetType)
                    return true;

                // Avanzar al tipo base
                currentType = currentType->NonArrayBaseType;

                // Si llegamos al final de la jerarquía, terminar
                if (currentType == null || WellKnownEETypes.IsSystemObject(currentType))
                    break;
            }

            // No hay relación de herencia directa
            return false;
        }

        /// <summary>
        /// Comprueba si un tipo implementa una interfaz específica.
        /// </summary>
        private static bool ImplementsInterface(EEType* objType, EEType* interfaceType)
        {
            // Implementación simplificada - en un caso real se consultaría
            // la tabla de interfaces del objeto

            // Por ahora, retornamos false para indicar que no implementa la interfaz
            // Esta función necesitaría acceder a la tabla de interfaces del objeto
            return false;
        }

        /// <summary>
        /// Comprueba la compatibilidad de varianza entre tipos genéricos.
        /// </summary>
        private static bool CheckVarianceCompatibility(EEType* sourceType, EEType* targetType)
        {
            // Implementación simplificada de comprobación de varianza
            // En un caso real se comprobarían los parámetros de tipo genéricos
            // y sus restricciones de varianza (covarianza/contravarianza)

            // Por ahora, retornamos false
            return false;
        }

        /// <summary>
        /// Lanza una excepción de invalid cast.
        /// </summary>
        private static void ThrowInvalidCastException(object obj, EETypePtr targetType)
        {
            // En un sistema sin excepciones completo, esto podría imprimir un mensaje
            // de error y detener el sistema o realizar otra acción

            // Simplemente detenemos el sistema con un mensaje de error
            ThrowHelpers.ThrowInvalidCastException($"No se puede convertir objeto de tipo {obj.GetType()} a {targetType}");
        }
    }
}