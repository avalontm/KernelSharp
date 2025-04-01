using System;
using System.Runtime;
using System.Security.Cryptography;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// Clase que proporciona un cookie de seguridad para protección contra ataques de corrupción de memoria.
    /// Versión adaptada para kernel de 32 bits.
    /// </summary>
    internal static class SecurityCookie
    {
        // El valor del cookie de seguridad (uint para sistemas de 32 bits)
        private static uint _securityCookie;

        /// <summary>
        /// Inicializa el cookie de seguridad con un valor aleatorio.
        /// </summary>
        static SecurityCookie()
        {
            InitializeSecurityCookie();
        }

        /// <summary>
        /// Genera un valor aleatorio seguro para el cookie.
        /// </summary>
        /// <returns>Un valor de 32 bits aleatorio.</returns>
        private static uint GenerateSecurityCookie()
        {
            // Crear una instancia del generador
            RandomNumberGenerator rng = RandomNumberGenerator.Create();

            // Generar bytes aleatorios
            byte[] buffer = new byte[4]; // 32 bits para sistemas de 32 bits
            rng.GetBytes(buffer);

            // Liberar el generador manualmente (ya que no podemos usar 'using')
            rng.Dispose();

            // Convertir a UInt32
            return BitConverter.ToUInt32(buffer, 0);
        }

        /// <summary>
        /// Inicializa el cookie de seguridad.
        /// Exportado para que pueda ser llamado desde código nativo.
        /// </summary>
        [RuntimeExport("__security_cookie_init")]
        public static void InitializeSecurityCookie()
        {
            // Inicializar solo si aún no se ha hecho
            if (_securityCookie == 0)
            {
                _securityCookie = GenerateSecurityCookie();

                // Asegurarse de que el cookie nunca sea cero, ya que es un valor reservado
                if (_securityCookie == 0)
                {
                    _securityCookie = 0xBB40E64E; // Valor arbitrario distinto de cero
                }
            }
        }

        /// <summary>
        /// Obtiene el valor del cookie de seguridad.
        /// Exportado para que pueda ser accedido desde código nativo.
        /// </summary>
        /// <returns>El valor del cookie de seguridad.</returns>
        [RuntimeExport("__security_cookie")]
        public static uint GetSecurityCookie()
        {
            // Asegurarse de que está inicializado
            if (_securityCookie == 0)
            {
                InitializeSecurityCookie();
            }
            return _securityCookie;
        }

        /// <summary>
        /// Verifica si el cookie de seguridad sigue intacto.
        /// </summary>
        /// <param name="expectedValue">El valor esperado para comparar.</param>
        /// <returns>true si el cookie coincide con el valor esperado; de lo contrario, false.</returns>
        public static bool ValidateSecurityCookie(uint expectedValue)
        {
            return GetSecurityCookie() == expectedValue;
        }
    }
}