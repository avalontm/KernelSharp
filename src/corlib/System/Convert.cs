namespace System
{
    public static class Convert
    {
        public static unsafe string ToString(ulong value, ulong toBase)
        {
            if (toBase > 0 && toBase <= 16)
            {
                char* x = stackalloc char[128];
                var i = 126;

                x[127] = '\0';

                do
                {
                    var d = value % toBase;
                    value /= toBase;

                    if (d > 9)
                        d += 0x37;
                    else
                        d += 0x30;

                    x[i--] = (char)d;
                } while (value > 0);

                i++;

                return new string(x + i, 0, 127 - i);
            }
            return null;
        }

        public static int ToUInt16(bool boolean)
        {
            return boolean ? 1 : 0;
        }

        public static int ToInt16(bool boolean)
        {
            return boolean ? 1 : 0;
        }

        public static int ToInt16(byte b)
        {
            return b;
        }

        public static bool ToBoolean(int integer)
        {
            return integer != 0;
        }

        /// <summary>
        /// Convierte una representación de cadena de un número a su equivalente entero de 8 bits con signo (byte).
        /// </summary>
        /// <param name="value">La cadena que contiene el número a convertir.</param>
        /// <returns>Un valor de 8 bits con signo equivalente a la representación de cadena del número.</returns>
        public static byte ToByte(string value)
        {
            return ToByte(value, 10);
        }

        /// <summary>
        /// Convierte una representación de cadena de un número en una base especificada a su equivalente entero de 8 bits con signo (byte).
        /// </summary>
        /// <param name="value">La cadena que contiene el número a convertir.</param>
        /// <param name="fromBase">La base de la representación numérica de value, que debe ser 2, 8, 10 o 16.</param>
        /// <returns>Un valor de 8 bits con signo equivalente a la representación de cadena del número en la base especificada.</returns>
        public static byte ToByte(string value, int fromBase)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
                return 0; // Bases no válidas

            byte result = 0;
            int digit;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                // Convertir el carácter a un valor numérico
                if (c >= '0' && c <= '9')
                    digit = c - '0';
                else if (c >= 'a' && c <= 'f')
                    digit = c - 'a' + 10;
                else if (c >= 'A' && c <= 'F')
                    digit = c - 'A' + 10;
                else
                    return 0; // Carácter no válido

                // Verificar que el dígito sea válido para la base dada
                if (digit >= fromBase)
                    return 0;

                // Evitar desbordamiento antes de multiplicar
                if ((result > byte.MaxValue / fromBase) ||
                    (result == byte.MaxValue / fromBase && digit > byte.MaxValue % fromBase))
                    return 0; // Desbordamiento

                result = (byte)(result * fromBase + digit);
            }

            return result;
        }

        public static byte ToByte(uint v)
        {
            return (byte)v;
        }

        public static int ToInt32(byte b)
        {
            return b;
        }

        public static int ToInt32(int b)
        {
            return b;
        }

        public static long ToInt64(string str)
        {
            int i = 0;
            long val = 0;
            bool neg = false;
            if (str[0] == '-')
            {
                i = 1;
                neg = true;
            }
            for (; i < str.Length; i++)
            {
                val *= 10;
                val += str[i] - 0x30;
            }
            return neg ? -(val) : val;
        }

        public static int ToInt32(string str)
        {
            return (int)ToInt64(str);
        }

        public static short ToInt16(string str)
        {
            return (short)ToInt64(str);
        }

        public static sbyte ToInt8(string str)
        {
            return (sbyte)ToInt64(str);
        }

        public static ulong ToUInt64(string str)
        {
            return (ulong)ToInt64(str);
        }

        public static uint ToUInt32(string str)
        {
            return (uint)ToInt64(str);
        }

        public static ushort ToUInt16(string str)
        {
            return (ushort)ToInt64(str);
        }

        public static byte ToUInt8(string str)
        {
            return (byte)ToInt64(str);
        }

        public static int HexToDec(string x)
        {
            int result = 0;
            int count = x.Length - 1;
            for (int i = 0; i < x.Length; i++)
            {
                int temp = 0;
                switch (x[i])
                {
                    case 'A': temp = 10; break;
                    case 'B': temp = 11; break;
                    case 'C': temp = 12; break;
                    case 'D': temp = 13; break;
                    case 'E': temp = 14; break;
                    case 'F': temp = 15; break;
                    default: temp = -48 + (int)x[i]; break; // -48 because of ASCII
                }

                result += temp * (int)(Math.Pow(16, count));
                count--;
            }

            return result;
        }
        public static string DecToHex(int x)
        {
            string result = "";

            while (x != 0)
            {
                if ((x % 16) < 10)
                    result = x % 16 + result;
                else
                {
                    string temp = "";

                    switch (x % 16)
                    {
                        case 10: temp = "A"; break;
                        case 11: temp = "B"; break;
                        case 12: temp = "C"; break;
                        case 13: temp = "D"; break;
                        case 14: temp = "E"; break;
                        case 15: temp = "F"; break;
                    }

                    result = temp + result;
                }

                x /= 16;
            }

            return result;
        }
    }
}
