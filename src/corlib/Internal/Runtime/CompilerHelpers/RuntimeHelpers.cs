using System.Runtime;
using System.Runtime.InteropServices;

namespace Internal.Runtime.CompilerHelpers
{
    internal static class RuntimeHelpers
    {
        [RuntimeExport("RhpLMul")]
        public static long RhpLMul(long a, long b)
        {
            return a * b;
        }

        [RuntimeExport("RhpLMulFlt")]
        public static float RhpLMulFlt(float a, float b)
        {
            return a * b;
        }

        [RuntimeExport("RhpLMulDbl")]
        public static double RhpLMulDbl(double a, double b)
        {
            return a * b;
        }


        /// <summary>
        /// Colección de cadenas comunes precargadas para evitar
        /// problemas de inicialización dinámica.
        /// </summary>
        private static class CommonStrings
        {
            // Cadenas comunes precargadas
            public static readonly char[] EmptyChars = new char[0];

            // Método para obtener una cadena vacía sin depender de String.Empty
            public static string GetEmptyString()
            {
                return new string(EmptyChars);
            }

            // Método para obtener una cadena de espacio en blanco
            public static string GetWhiteSpace()
            {
                return new string(new char[] { ' ' });
            }
        }

        /// <summary>
        /// Obtiene una cadena vacía de forma segura, incluso durante la inicialización del sistema.
        /// </summary>
        public static string GetEmptyString()
        {
            return CommonStrings.GetEmptyString();
        }

        /// <summary>
        /// Comprueba si una cadena es nula o vacía de forma segura.
        /// </summary>
        public static bool IsNullOrEmpty(string value)
        {
            return value == null || value.Length == 0;
        }

        /// <summary>
        /// Inicializa manualmente aspectos críticos del sistema de tipos.
        /// </summary>
        public static void InitializeTypeSystem()
        {
            // Forzar la creación de cadenas comunes
            var empty = GetEmptyString();

            // Podríamos inicializar otros aspectos críticos del sistema aquí
        }

        /// <summary>
        /// Concatena dos cadenas de forma segura incluso durante la inicialización del sistema.
        /// </summary>
        public static string ConcatStrings(string a, string b)
        {
            // Manejar casos de valores nulos
            if (a == null) a = GetEmptyString();
            if (b == null) b = GetEmptyString();

            int totalLength = a.Length + b.Length;
            if (totalLength == 0) return GetEmptyString();

            char[] chars = new char[totalLength];

            for (int i = 0; i < a.Length; i++)
                chars[i] = a[i];

            for (int i = 0; i < b.Length; i++)
                chars[a.Length + i] = b[i];

            return new string(chars);
        }

        /// <summary>
        /// Crea una cadena de un solo carácter de forma segura.
        /// </summary>
        public static string CharToString(char c)
        {
            return new string(new char[] { c });
        }

    }
}