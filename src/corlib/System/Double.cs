using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    public unsafe struct Double
    {
        public const double MinValue = -1.7976931348623157E+308;
        public const double MaxValue = 1.7976931348623157E+308;
        public const double Epsilon = 4.9406564584124654E-324;
        public const double NaN = 0.0d / 0.0d;
        public const double PositiveInfinity = 1.0d / 0.0d;
        public const double NegativeInfinity = -1.0d / 0.0d;

        public static unsafe bool IsNaN(double d)
        {
            // A NaN will never equal itself so this is an
            // easy and efficient way to check for NaN.
#pragma warning disable CS1718
            return d != d;
#pragma warning restore CS1718
        }

        public static bool IsPositiveInfinity(double d)
        {
            return d == PositiveInfinity;
        }

        public static bool IsNegativeInfinity(double d)
        {
            return d == NegativeInfinity;
        }

        public override string ToString()
        {
            double value = this;

            // Manejar casos especiales
            if (IsNaN(value))
                return "NaN";
            if (IsPositiveInfinity(value))
                return "Infinity";
            if (IsNegativeInfinity(value))
                return "-Infinity";

            // Buffer para el resultado
            char* buffer = stackalloc char[32];
            int position = 0;

            // Manejar signo negativo
            if (value < 0)
            {
                buffer[position++] = '-';
                value = -value;
            }

            // Separar parte entera y decimal
            long intPart = (long)value;
            double fractPart = value - intPart;

            // Convertir parte entera
            if (intPart == 0)
            {
                buffer[position++] = '0';
            }
            else
            {
                // Necesitamos convertir los dígitos en orden inverso
                int startPos = position;
                while (intPart > 0)
                {
                    buffer[position++] = (char)('0' + (intPart % 10));
                    intPart /= 10;
                }

                // Invertir los dígitos
                int endPos = position - 1;
                while (startPos < endPos)
                {
                    char temp = buffer[startPos];
                    buffer[startPos] = buffer[endPos];
                    buffer[endPos] = temp;
                    startPos++;
                    endPos--;
                }
            }

            // Añadir parte decimal si existe
            if (fractPart > 0)
            {
                buffer[position++] = '.';

                // Mostrar 6 decimales como máximo
                for (int i = 0; i < 6; i++)
                {
                    fractPart *= 10;
                    int digit = (int)fractPart;
                    buffer[position++] = (char)('0' + digit);
                    fractPart -= digit;

                    // Si no quedan decimales significativos, terminar
                    if (fractPart < 0.000001)
                        break;
                }
            }

            // Añadir terminador nulo
            buffer[position] = '\0';

            // Convertir a string
            return new string(buffer, 0, position);
        }
    }
}