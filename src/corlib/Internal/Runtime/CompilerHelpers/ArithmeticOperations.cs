using System;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// Proporciona implementaciones de operaciones aritméticas de bajo nivel.
    /// </summary>
    public static unsafe class ArithmeticOperations
    {
        /// <summary>
        /// Realiza la multiplicación con comprobación de desbordamiento entre dos enteros largos (64 bits).
        /// Esta implementación es necesaria para sistemas de 32 bits donde la multiplicación de 64 bits
        /// no es soportada directamente por el hardware.
        /// </summary>
        /// <param name="a">Primer operando de 64 bits</param>
        /// <param name="b">Segundo operando de 64 bits</param>
        /// <returns>El resultado de la multiplicación</returns>
        [RuntimeExport("RhpLMul")]
        public static long RhpLMul(long a, long b)
        {
            // Descomponer los operandos de 64 bits en componentes de 32 bits
            uint a_lo = (uint)a;
            int a_hi = (int)(a >> 32);
            uint b_lo = (uint)b;
            int b_hi = (int)(b >> 32);

            // Algoritmo de multiplicación para enteros de 64 bits
            // Multiplicamos cada parte y combinamos los resultados

            // Multiplicar las partes bajas (produce un valor de 64 bits)
            ulong lo_lo = (ulong)a_lo * b_lo;

            // Multiplicar parte alta de a por parte baja de b
            long hi_lo = (long)a_hi * b_lo;

            // Multiplicar parte baja de a por parte alta de b
            long lo_hi = (long)a_lo * b_hi;

            // Multiplicar las partes altas (puede producir desbordamiento)
            long hi_hi = (long)a_hi * b_hi;

            // Verificar desbordamiento en la parte alta del resultado
            if (hi_hi != 0 && hi_hi != -1)
            {
                // Hay desbordamiento si la parte alta no es toda 0 o toda 1
                // En lugar de lanzar una excepción, devolvemos un resultado definido
                // similar a lo que haría un sistema de bajo nivel en caso de overflow
                return a >= 0 ? long.MaxValue : long.MinValue;
            }

            // Combinar los resultados parciales
            long result = (long)lo_lo;  // Parte baja del resultado

            // Agregar los productos cruzados (ajustados a su posición correcta)
            // Verificar manualmente si hay desbordamiento al sumar
            long temp = result;
            result += (hi_lo << 32);
            // Si hay desbordamiento al sumar, el signo cambiará de manera inesperada
            if ((temp ^ result) < 0 && (temp ^ (hi_lo << 32)) >= 0)
            {
                return a >= 0 ? long.MaxValue : long.MinValue;
            }

            temp = result;
            result += (lo_hi << 32);
            // Otra verificación de desbordamiento
            if ((temp ^ result) < 0 && (temp ^ (lo_hi << 32)) >= 0)
            {
                return a >= 0 ? long.MaxValue : long.MinValue;
            }

            // Verificar si el signo del resultado es correcto
            bool resultShouldBeNegative = (a < 0) != (b < 0); // XOR de signos
            bool resultIsNegative = result < 0;

            if (resultShouldBeNegative != resultIsNegative)
            {
                return resultShouldBeNegative ? long.MinValue : long.MaxValue;
            }

            return result;
        }

        /// <summary>
        /// Método auxiliar para multiplicación sin comprobación de desbordamiento.
        /// </summary>
        [RuntimeExport("RhpLMulUn")]
        public static long RhpLMulUn(long a, long b)
        {
            // Versión simple sin comprobación de desbordamiento
            // Implementación directa para 32 bits
            uint a_lo = (uint)a;
            int a_hi = (int)(a >> 32);
            uint b_lo = (uint)b;
            int b_hi = (int)(b >> 32);

            ulong lo_lo = (ulong)a_lo * b_lo;
            long hi_lo = (long)a_hi * b_lo;
            long lo_hi = (long)a_lo * b_hi;

            // Combinar resultados sin comprobar desbordamiento
            long result = (long)lo_lo;
            result += (hi_lo << 32);
            result += (lo_hi << 32);

            return result;
        }
    }
}