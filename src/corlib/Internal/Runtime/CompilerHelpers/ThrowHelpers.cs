using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Internal.Runtime.CompilerHelpers
{
    public unsafe static class ThrowHelpers
    {
        // Declarar la función externa implementada en ensamblador
        [DllImport("*", EntryPoint="_panic")]
        private static extern void panic();
        [DllImport("*", EntryPoint = "_kernel_print")]
        private static extern void kernel_print(byte* text, int length);

        public static unsafe void DisplayPanicMessage(string message)
        {
            // Encabezado del mensaje de pánico
            string fullMessage = "\n\n====== KERNEL PANIC ======\n\n";
            fullMessage += message;
            fullMessage += "\n\n====== SYSTEM HALTED ======\n";

            // Puntero al buffer de video (modo texto VGA)
            byte* videoBuffer = (byte*)0xB8000;

            int posX = 0;
            int posY = 0;
            byte attribute = 0x4F; // Blanco brillante sobre rojo
            
            // Limpiar la pantalla
            for (int i = 0; i < 80 * 25; i++)
            {
                videoBuffer[i * 2] = (byte)' ';      // Espacio
                videoBuffer[i * 2 + 1] = 0x07;       // Atributo normal
            }
            
            // Imprimir el mensaje
            for (int i = 0; i < fullMessage.Length; i++)
            {
                char c = fullMessage[i];

                if (c == '\n')
                {
                    // Nueva línea
                    posX = 0;
                    posY++;
                    if (posY >= 25) break; // Salir si llegamos al final de la pantalla
                }
                else if (c == '\r')
                {
                    // Retorno de carro
                    posX = 0;
                }
                else if (c >= 32 && c <= 126)
                {
                    // Carácter imprimible
                    int offset = (posY * 80 + posX) * 2;
                    videoBuffer[offset] = (byte)c;
                    videoBuffer[offset + 1] = attribute;

                    posX++;
                    if (posX >= 80)
                    {
                        posX = 0;
                        posY++;
                        if (posY >= 25) break;
                    }
                }
            }
        }

        /// <summary>
        /// Provoca un pánico del kernel y muestra un mensaje
        /// </summary>
        /// <param name="message">Mensaje a mostrar antes del pánico</param>
        public static void Panic(string message)
        {
            // Mostrar mensaje de pánico
            DisplayPanicMessage(message);

            // Llamar a la función de pánico en ensamblador
            panic();

            // Este código nunca se ejecutará, pero el compilador no lo sabe
            while (true) { }
        }


        public static void ThrowInvalidOperationException(string message)
        {
            Panic("InvalidOperationException: " + message);
        }

        public static void ThrowArgumentException(string message)
        {
            Panic("ArgumentException: " + message);
        }

        public static void ThrowArgumentNullException(string paramName)
        {
            Panic("ArgumentNullException: " + paramName);
        }

        public static void ThrowArgumentOutOfRangeException(string paramName)
        {
            Panic("ArgumentOutOfRangeException: " + paramName);
        }

        public static void ThrowInvalidProgramException(string message = null)
        {
            Panic(message ?? "Common language runtime detected an invalid program.");
        }

        public static T ThrowInvalidProgramException<T>(string message = null)
        {
            Panic(message ?? "Common language runtime detected an invalid program.");
            return default;
        }

        public static void ThrowInvalidProgramExceptionWithArgument(string argumentName)
        {
            Panic("InvalidProgramException: " + argumentName);
        }

        public static T ThrowInvalidProgramExceptionWithArgument<T>(string argumentName)
        {
            Panic("InvalidProgramException: " + argumentName);
            return default;
        }

        public static void ThrowOverflowException()
        {
            Panic("ThrowOverflowException");
        }

        public static void ThrowOverflowException(string message)
        {
            Panic("ThrowOverflowException: " + message);
        }

        public static T ThrowOverflowException<T>()
        {
            Panic("OverflowException");
            return default;
        }

        public static void ThrowDivideByZeroException()
        {
            Panic("DivideByZeroException");
        }

        public static T ThrowDivideByZeroException<T>()
        {
            Panic("DivideByZeroException");
            return default;
        }

        public static void ThrowOutOfMemoryException()
        {
            Panic("OutOfMemoryException");
        }

        public static T ThrowOutOfMemoryException<T>()
        {
            Panic("OutOfMemoryException");
            return default;
        }

        public static void ThrowNullReferenceException()
        {
            Panic("ThrowNullReferenceException");
        }

        internal static void ThrowInvalidCastException()
        {
            Panic("ThrowInvalidCastException");
        }

        internal static void ArgumentOutOfRangeException(string message)
        {
            Panic("ArgumentOutOfRangeException: " + message);
        }

        internal static void ArgumentNullException()
        {
            Panic("ArgumentOutOfRangeException:");
        }
        internal static void ArgumentNullException(string message)
        {
            Panic("ArgumentOutOfRangeException: " + message);
        }

        internal static void ArgumentNullException(string message, string parm)
        {
            Panic("ArgumentOutOfRangeException: " + message + " " + parm);
        }

        internal static void OverflowException(string message)
        {
            Panic("OverflowException: " + message);
        }

        internal static void FormatException(string message)
        {
            Panic("FormatException: " + message);
        }

        internal static void ArgumentOutOfRangeException()
        {
            Panic("ArgumentOutOfRangeException: ");
        }

        internal static void NotImplementedException(string message)
        {
            Panic("NotImplementedException: " + message);
        }

        internal static void IndexOutOfRangeException()
        {
            Panic("IndexOutOfRangeException: ");
        }

        internal static void ThrowIndexOutOfRangeException()
        {
            Panic("IndexOutOfRangeException: ");
        }

        internal static void IndexOutOfRangeException(string message)
        {
            Panic("IndexOutOfRangeException: " + message);
        }

        internal static void ThrowNotSupportedException(string message)
        {
            Panic("FormatException: " + message);
        }

        internal static void ThrowNotImplementedException(string message)
        {
            Panic("ThrowNotImplementedException: " + message);
        }

        internal static void ThrowInvalidCastException(string message)
        {
            Panic("ThrowInvalidCastException: " + message);
        }

        internal static void OutOfMemoryException(string message)
        {
            Panic("OutOfMemoryException: " + message);
        }

        internal static void NotSupportedException(string message)
        {
            Panic("NotSupportedException: " + message);

        }

        internal static void ArgumentException(string message)
        {
            Panic("ArgumentException: " + message);
        }

        internal static void NotImplementedException()
        {
            Panic("NotImplementedException");
        }

        internal static void InternalException(string message)
        {
            Panic("InternalException: " + message);
        }

        /// <summary>
        /// Throws a BadImageFormatException with the specified message.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowBadImageFormatException(string message)
        {
            Panic("InternalException: " + message);
        }

        internal static void ThrowArrayTypeMismatchException()
        {
            Panic("ThrowArrayTypeMismatchException");
        }

        internal static void InvalidCastException(string message)
        {
            Panic($"ThrowArrayTypeMismatchException: {message}");
        }

        /// <summary>
        /// Throws a TypeLoadException with the specified message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowTypeLoadException(string message)
        {
            Panic("TypeLoadException: " + message);
        }

        /// <summary>
        /// Throws a TypeLoadException with the specified message and returns a default value.
        /// </summary>
        /// <typeparam name="T">The type of value to return (never actually returns).</typeparam>
        /// <param name="message">The exception message.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T ThrowTypeLoadException<T>(string message)
        {
            Panic("TypeLoadException: " + message);
            return default;
        }

        /// <summary>
        /// Throws a TypeLoadException with an argument name.
        /// </summary>
        /// <param name="argumentName">The name of the argument that caused the exception.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowTypeLoadExceptionWithArgument(string argumentName)
        {
            Panic("TypeLoadException: " + argumentName);
        }

        /// <summary>
        /// Throws a TypeLoadException with an argument name and returns a default value.
        /// </summary>
        /// <typeparam name="T">The type of value to return (never actually returns).</typeparam>
        /// <param name="argumentName">The name of the argument that caused the exception.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T ThrowTypeLoadExceptionWithArgument<T>(string argumentName)
        {
            Panic("TypeLoadException: " + argumentName);
            return default;
        }

        internal static void ThrowNullReferenceException(string message)
        {
            Panic("ThrowNullReferenceException: " + message);
        }

        internal static void ThrowMissingMethodException(string message)
        {
            Panic("ThrowMissingMethodException: " + message);
        }

        internal static void ThrowKeyNotFoundException()
        {
            Panic("ThrowKeyNotFoundException:");
        }

        internal static void KeyNotFoundException(string message)
        {
            Panic("ThrowKeyNotFoundException: " + message);
        }

        internal static void ArgumentException(string v1, string v2)
        {
            Panic("ArgumentException: " + v1 +  ", " + v2);
        }

        /// <summary>
        /// Lanza una excepción por funcionalidad no soportada por la plataforma
        /// </summary>
        /// <param name="message">Mensaje descriptivo opcional</param>
        public static void ThrowPlatformNotSupportedException(string message = null)
        {
            string errorMessage = message ?? "Esta funcionalidad no está soportada en la plataforma actual.";
            Panic($"PlatformNotSupportedException: {errorMessage}");
        }

        /// <summary>
        /// Lanza una excepción por funcionalidad no soportada por la plataforma y devuelve un valor por defecto
        /// </summary>
        /// <typeparam name="T">Tipo de retorno</typeparam>
        /// <param name="message">Mensaje descriptivo opcional</param>
        /// <returns>Valor por defecto del tipo T (nunca se ejecuta realmente)</returns>
        public static T ThrowPlatformNotSupportedException<T>(string message = null)
        {
            ThrowPlatformNotSupportedException(message);
            return default; // Nunca se ejecuta, pero necesario para compilar
        }

        internal static void InvalidOperationException(string message)
        {
            Panic("InvalidOperationException: " + message);
        }

        internal static void ArgumentException()
        {
            throw new NotImplementedException();
        }
    }
}