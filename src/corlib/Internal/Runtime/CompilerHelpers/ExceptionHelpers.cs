using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;

namespace Internal.Runtime.CompilerHelpers
{
    // Crear una excepción base concreta
    public class UnknownException : Exception
    {
        public UnknownException(string message) : base(message) { }
    }

    /// <summary>
    /// Clase de ayuda para la gestión de excepciones en tiempo de ejecución
    /// </summary>
    public static unsafe class ExceptionHelpers
    {
        /// <summary>
        /// Función de bajo nivel para lanzar excepciones en tiempo de ejecución
        /// </summary>
        /// <param name="pEx">Puntero a la excepción a lanzar</param>
        [RuntimeExport("RhpThrowEx")]
        public static void RhpThrowEx(IntPtr pEx)
        {
            // Convertir el puntero a un objeto de excepción
            Exception ex = Unsafe.As<object>(pEx) as Exception;

            // Llamar al método de pánico si no se puede manejar la excepción
            if (ex == null)
            {
                ThrowHelpers.Panic("Excepción nula no se puede lanzar");
                return;
            }

            // En un kernel, normalmente esto resultará en detener el sistema
            ThrowHelpers.Panic($"Excepción no controlada: {ex.GetType()} - {ex.Message}");
        }

        /// <summary>
        /// Convierte un objeto a un puntero para lanzamiento de excepciones
        /// </summary>
        /// <param name="obj">Objeto a convertir</param>
        /// <returns>Puntero al objeto</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr ObjectToIntPtr(object obj)
        {
            return Unsafe.As<object, IntPtr>(ref obj);
        }

        /// <summary>
        /// Convierte un puntero a un objeto para recuperación de excepciones
        /// </summary>
        /// <param name="ptr">Puntero al objeto</param>
        /// <returns>Objeto recuperado</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object IntPtrToObject(IntPtr ptr)
        {
            return Unsafe.As<IntPtr, object>(ref ptr);
        }
    }
}