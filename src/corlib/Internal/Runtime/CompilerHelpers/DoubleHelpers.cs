using System;
using System.Runtime;

namespace Internal.Runtime.CompilerHelpers
{
    public static class DoubleHelpers
    {
        [RuntimeExport("double_tostring")]
        public static IntPtr DoubleToString(double value)
        {
            // Implementación básica para convertir un double a string
            // Esto es más complejo y depende de cómo gestiones las cadenas

            // Ejemplo simplificado:
            char[] buffer = new char[32];
            int pos = 0;

            // Manejo del signo
            if (value < 0)
            {
                buffer[pos++] = '-';
                value = -value;
            }

            // Parte entera
            long intPart = (long)value;
            double fracPart = value - intPart;

            // Convertir parte entera
            if (intPart == 0)
            {
                buffer[pos++] = '0';
            }
            else
            {
                // Convertir dígitos en orden inverso
                int start = pos;
                while (intPart > 0)
                {
                    buffer[pos++] = (char)('0' + (intPart % 10));
                    intPart /= 10;
                }

                // Invertir los dígitos
                for (int i = start, j = pos - 1; i < j; i++, j--)
                {
                    char temp = buffer[i];
                    buffer[i] = buffer[j];
                    buffer[j] = temp;
                }
            }

            // Parte decimal
            if (fracPart > 0)
            {
                buffer[pos++] = '.';

                // Mostrar 6 decimales
                for (int i = 0; i < 6; i++)
                {
                    fracPart *= 10;
                    int digit = (int)fracPart;
                    buffer[pos++] = (char)('0' + digit);
                    fracPart -= digit;

                    // Evitar ceros finales
                    if (fracPart == 0)
                        break;
                }
            }

            // Terminar con nulo
            buffer[pos] = '\0';

            // Crear string desde buffer de caracteres
            return CreateStringFromCharArray(buffer, 0, pos).GetHandle();
        }

        // Método auxiliar para crear una string a partir de caracteres
        private static string CreateStringFromCharArray(char[] chars, int startIndex, int length)
        {
            // Tu implementación para crear una string
            return new string(chars, startIndex, length);
        }


        /// <summary>
        /// Convierte un valor double a long (int64)
        /// </summary>
        [RuntimeExport("RhpDbl2Lng")]
        public static long RhpDbl2Lng(double value)
        {
            // Manejar casos especiales
            if (double.IsNaN(value))
                return 0;

            if (value >= long.MaxValue)
                return long.MaxValue;

            if (value <= long.MinValue)
                return long.MinValue;

            // Conversión normal
            return (long)value;
        }

        /// <summary>
        /// Convierte un valor long (int64) a double
        /// </summary>
        [RuntimeExport("RhpLng2Dbl")]
        public static double RhpLng2Dbl(long value)
        {
            return (double)value;
        }
    }
}