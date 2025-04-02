using System.Runtime.CompilerServices;

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
            // IEEE 754 floating-point representation of NaN
            // Use bit manipulation to check NaN status
            ulong bits = *(ulong*)&d;

            // NaN has all exponent bits set (0x7FF) and non-zero fraction
            const ulong exponentMask = 0x7FF0000000000000UL;
            const ulong fractionMask = 0x000FFFFFFFFFFFFFUL;

            return (bits & exponentMask) == exponentMask && (bits & fractionMask) != 0;
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

            // Handle special cases first
            if (IsNaN(value))
                return "NaN";
            if (IsPositiveInfinity(value))
                return "Infinity";
            if (IsNegativeInfinity(value))
                return "-Infinity";

            // Buffer for result
            char* buffer = stackalloc char[32];
            int position = 0;

            // Handle sign
            bool isNegative = false;
            if (value < 0)
            {
                isNegative = true;
                value = -value;
            }

            // Special case for zero
            if (value < 0.000001)
            {
                return "0.0";
            }

            // 32-bit safe integer part conversion
            int intPartLow = (int)value;
            int intPartHigh = (int)((value - intPartLow) * 4294967296.0);
            double fractPart = value - intPartLow;

            // Convert integer part
            bool digitStarted = false;

            // Handle high part first
            if (intPartHigh > 0)
            {
                int tempHigh = intPartHigh;
                while (tempHigh > 0)
                {
                    buffer[position++] = (char)('0' + (tempHigh % 10));
                    tempHigh /= 10;
                    digitStarted = true;
                }
            }

            // Handle low part
            int tempLow = intPartLow;
            if (intPartHigh > 0 || tempLow > 0)
            {
                while (tempLow > 0 || !digitStarted)
                {
                    buffer[position++] = (char)('0' + (tempLow % 10));
                    tempLow /= 10;
                    digitStarted = true;

                    // Prevent infinite loop if both parts are zero
                    if (tempLow == 0 && intPartHigh == 0)
                        break;
                }
            }
            else
            {
                buffer[position++] = '0';
            }

            // Reverse the digits
            int startPos = 0;
            int endPos = position - 1;
            while (startPos < endPos)
            {
                char temp = buffer[startPos];
                buffer[startPos] = buffer[endPos];
                buffer[endPos] = temp;
                startPos++;
                endPos--;
            }

            // Add negative sign if necessary
            if (isNegative)
            {
                // Move existing digits to make room for minus sign
                for (int i = position - 1; i >= 0; i--)
                {
                    buffer[i + 1] = buffer[i];
                }
                buffer[0] = '-';
                position++;
            }

            // Add decimal part if exists
            if (fractPart > 0)
            {
                buffer[position++] = '.';

                // Show up to 6 decimal places
                for (int i = 0; i < 6; i++)
                {
                    fractPart *= 10;
                    int digit = (int)fractPart;
                    buffer[position++] = (char)('0' + digit);
                    fractPart -= digit;

                    // Stop if no significant decimal parts remain
                    if (fractPart < 0.000001)
                        break;
                }
            }

            // Null terminator
            buffer[position] = '\0';

            // Convert to string
            return new string(buffer, 0, position);
        }

        /// <summary>
        /// Determines whether the specified value is infinite.
        /// </summary>
        /// <param name="value">The double-precision floating-point number to test.</param>
        /// <returns>true if value is positive or negative infinity; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInfinity(double value)
        {
            // Obtener representación en bits para verificar el patrón de bits de infinito
            long bits = BitConverter.DoubleToInt64Bits(value);

            // Máscara para los bits de exponente
            long exponentMask = 0x7FF0000000000000L;

            // Máscara para la fracción
            long fractionMask = 0x000FFFFFFFFFFFFFL;

            // Verificar si los bits de exponente están completamente establecidos 
            // y la fracción es cero
            return (bits & exponentMask) == exponentMask && (bits & fractionMask) == 0;
        }
    }
}
