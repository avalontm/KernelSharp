namespace System
{
    /// <summary>
    /// Clase base para todas las excepciones en el kernel
    /// </summary>
    public abstract class Exception
    {
        // Mensaje de la excepción
        private string _message;

        // Excepción interna que causó esta excepción
        private Exception _innerException;

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public Exception()
        {
            _message = "Ha ocurrido una excepción no especificada.";
            _innerException = null;
        }

        /// <summary>
        /// Constructor con mensaje personalizado
        /// </summary>
        /// <param name="message">Mensaje descriptivo de la excepción</param>
        public Exception(string message)
        {
            _message = message ?? "Ha ocurrido una excepción no especificada.";
            _innerException = null;
        }

        /// <summary>
        /// Constructor con mensaje y excepción interna
        /// </summary>
        /// <param name="message">Mensaje descriptivo de la excepción</param>
        /// <param name="innerException">Excepción que causó esta excepción</param>
        public Exception(string message, Exception innerException)
        {
            _message = message ?? "Ha ocurrido una excepción no especificada.";
            _innerException = innerException;
        }

        /// <summary>
        /// Obtiene el mensaje de la excepción
        /// </summary>
        public virtual string Message
        {
            get { return _message; }
        }

        /// <summary>
        /// Obtiene la excepción interna que causó esta excepción
        /// </summary>
        public Exception InnerException
        {
            get { return _innerException; }
        }

        /// <summary>
        /// Obtiene una representación de cadena de la excepción
        /// </summary>
        /// <returns>Cadena que describe la excepción</returns>
        public override string ToString()
        {
            // Formato básico: tipo de excepción y mensaje
            string exceptionType = GetType();

            if (string.IsNullOrEmpty(_message))
            {
                return exceptionType;
            }

            return $"{exceptionType}: {_message}";
        }
    }
}