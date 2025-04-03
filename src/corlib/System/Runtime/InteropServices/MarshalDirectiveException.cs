using System;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Excepción que se produce cuando un método de serialización o serialización no puede 
    /// manejar un tipo específico o una situación de serialización.
    /// </summary>
    public class MarshalDirectiveException : Exception
    {
        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public MarshalDirectiveException() : base("Error en la directiva de serialización o serialización")
        {
        }

        /// <summary>
        /// Constructor con mensaje personalizado
        /// </summary>
        /// <param name="message">Mensaje que describe el error</param>
        public MarshalDirectiveException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructor con mensaje y excepción interna
        /// </summary>
        /// <param name="message">Mensaje que describe el error</param>
        /// <param name="innerException">Excepción que causó la actual</param>
        public MarshalDirectiveException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}