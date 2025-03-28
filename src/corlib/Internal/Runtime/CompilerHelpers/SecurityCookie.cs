using System;
using System.Runtime;
using System.Runtime.InteropServices;
namespace Internal.Runtime.CompilerHelpers
{
    internal static class SecurityCookie
    {
        // Declaración para acceder a las funciones de seguridad
        [DllImport("*", EntryPoint = "__security_cookie_init")]
        public static extern void InitSecurityCookie();

        [DllImport("*", EntryPoint = "__security_cookie")]
        public static extern IntPtr GetSecurityCookie();

        // Llamar esto al inicio de tu kernel
        public static void InitializeSecurity()
        {
            InitSecurityCookie();
        }
    }
}