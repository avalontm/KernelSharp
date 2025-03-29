namespace System
{
    // Esta clase es la que usará el compilador para manejar cadenas interpoladas
    public static class FormattableStringFactory
    {
        // Método que el compilador llamará para procesar cadenas interpoladas
        public static string Create(string format, params object[] args)
        {
            if (format == null)
                return null;

            if (args == null || args.Length == 0)
                return format;

            // La implementación simple que solo concatena
            // Estimar el tamaño necesario
            int maxLength = format.Length;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] != null)
                {
                    string argStr = args[i].ToString();
                    if (argStr != null)
                    {
                        maxLength += argStr.Length;
                    }
                }
            }

            // Crear un buffer para el resultado
            char[] result = new char[maxLength];
            int resultIndex = 0;

            // Copiar el formato inicial
            for (int i = 0; i < format.Length; i++)
            {
                result[resultIndex++] = format[i];
            }

            // Concatenar cada argumento
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] != null)
                {
                    string argStr = args[i].ToString();
                    if (argStr != null)
                    {
                        // Copiar el string del argumento
                        for (int j = 0; j < argStr.Length; j++)
                        {
                            result[resultIndex++] = argStr[j];
                        }
                    }
                }
            }

            // Crear el string final con el tamaño exacto
            char[] finalResult = new char[resultIndex];
            for (int i = 0; i < resultIndex; i++)
            {
                finalResult[i] = result[i];
            }

            return new string(finalResult);
        }
    }

    // También podemos implementar versiones con uno o varios argumentos
    public static class StringInterpolationHelper
    {
        // Para expresiones como $"{valor}"
        public static string Format(string format, object arg0)
        {
            return FormattableStringFactory.Create(format, arg0);
        }

        // Para expresiones como $"{valor1}{valor2}"
        public static string Format(string format, object arg0, object arg1)
        {
            return FormattableStringFactory.Create(format, arg0, arg1);
        }

        // Para expresiones como $"{valor1}{valor2}{valor3}"
        public static string Format(string format, object arg0, object arg1, object arg2)
        {
            return FormattableStringFactory.Create(format, arg0, arg1, arg2);
        }

        // Para más argumentos
        public static string Format(string format, params object[] args)
        {
            return FormattableStringFactory.Create(format, args);
        }
    }
}