using Internal.Runtime.CompilerHelpers;

namespace System
{
    public unsafe struct Int32
    {
        private int m_value; // El valor interno

        // Constantes
        public const int MaxValue = 0x7fffffff;
        public const int MinValue = -2147483648;

        // Conversiones explícitas
        public static explicit operator uint(Int32 value) => (uint)value.m_value;
        public static implicit operator long(Int32 value) => value.m_value;

        // Operadores aritméticos básicos
        public static Int32 operator +(Int32 a, Int32 b) => a.m_value + b.m_value;
        public static Int32 operator -(Int32 a, Int32 b) => a.m_value - b.m_value;
        public static Int32 operator *(Int32 a, Int32 b) => a.m_value * b.m_value;
        public static Int32 operator /(Int32 a, Int32 b) => a.m_value / b.m_value;
        public static Int32 operator %(Int32 a, Int32 b) => a.m_value % b.m_value;

        // Operadores de comparación
        public static bool operator ==(Int32 a, Int32 b) => a.m_value == b.m_value;
        public static bool operator !=(Int32 a, Int32 b) => a.m_value != b.m_value;
        public static bool operator <(Int32 a, Int32 b) => a.m_value < b.m_value;
        public static bool operator >(Int32 a, Int32 b) => a.m_value > b.m_value;
        public static bool operator <=(Int32 a, Int32 b) => a.m_value <= b.m_value;
        public static bool operator >=(Int32 a, Int32 b) => a.m_value >= b.m_value;

        // Métodos de objeto
        public override bool Equals(object obj)
        {
            if (obj is Int32 other)
                return m_value == other.m_value;
            return false;
        }

        public override int GetHashCode()
        {
            return m_value;
        }

        // Conversión básica a string
        public override string ToString()
        {
            // Implementación simple para convertir a string
            if (m_value == 0)
                return "0";
            bool isNegative = m_value < 0;
            // Manejo especial de MinValue
            if (m_value == MinValue)
                return "-2147483648";
            // Buffer para almacenar los caracteres
            char* buffer = stackalloc char[12]; // Suficiente para un int de 32 bits con signo
            int position = 11;
            int remainingValue = isNegative ? -m_value : m_value;
            // Convertir dígito a dígito
            do
            {
                buffer[position--] = (char)('0' + (remainingValue % 10));
                remainingValue /= 10;
            } while (remainingValue > 0);
            // Añadir signo negativo si es necesario
            if (isNegative)
                buffer[position--] = '-';
            // Crear string a partir del buffer, omitiendo caracteres no usados
            return new string(buffer + position + 1, 0, 11 - position);
        }

        // Métodos Parse
        public static Int32 Parse(string s)
        {
            int result = 0;
            bool isNegative = false;
            int i = 0;

            // Manejar signo
            if (s.Length > 0 && s[0] == '-')
            {
                isNegative = true;
                i = 1;
            }
            else if (s.Length > 0 && s[0] == '+')
            {
                i = 1;
            }

            // Convertir dígitos
            for (; i < s.Length; i++)
            {
                char c = s[i];
                if (c < '0' || c > '9')
                    ThrowHelpers.FormatException("Input string was not in a correct format.");

                int digit = c - '0';

                // Verificar desbordamiento
                if (result > (MaxValue - digit) / 10)
                {
                    if (isNegative && result == MaxValue / 10 && digit == 8)
                        return MinValue; // Caso especial para MinValue
                    ThrowHelpers.OverflowException("Value was either too large or too small for an Int32.");
                }

                result = result * 10 + digit;
            }

            return isNegative ? -result : result;
        }

        /// <summary>
        /// Convierte este entero de 32 bits a su representación hexadecimal.
        /// </summary>
        /// <returns>Representación hexadecimal del valor</returns>
        public unsafe string ToHexString()
        {
            // Un UInt32 necesita 8 caracteres hexadecimales para representarse
            char* result = stackalloc char[8];

            // Convertir cada grupo de 4 bits a un carácter hexadecimal
            for (int i = 0; i < 8; i++)
            {
                // Extraer 4 bits y obtener el valor hexadecimal
                int hexDigit = (m_value >> (28 - i * 4)) & 0xF;

                // Convertir el dígito a carácter
                if (hexDigit < 10)
                    result[i] = (char)('0' + hexDigit);
                else
                    result[i] = (char)('A' + (hexDigit - 10));
            }

            return new string(result, 0, 8);
        }

        // TryParse
        public static bool TryParse(string s, out Int32 result)
        {
            result = 0;
            try
            {
                result = Parse(s);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}