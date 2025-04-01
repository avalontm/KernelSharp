using Internal.Runtime.CompilerHelpers;
using System.Runtime.InteropServices;
namespace System.Diagnostics
{
    public static unsafe class Debug
    {
        // Core method for writing debug output
        public static void WriteLine(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            // Obtener la longitud del mensaje para reservar buffer
            int length = message.Length;
            byte* buffer = stackalloc byte[length];

            // Convertir cada carácter directamente a byte
            // Esto funciona para ASCII pero trunca caracteres Unicode
            for (int i = 0; i < length; i++)
            {
                buffer[i] = (byte)message[i];
            }

            // Enviar al método nativo
            NativeDebugWrite(buffer, length);

        }

        // Native method for output
        [DllImport("*", EntryPoint = "_DebugWrite", PreserveSig = true, CallingConvention = CallingConvention.ThisCall)]
        private static extern void NativeDebugWrite(byte* value, int length);
    }

}