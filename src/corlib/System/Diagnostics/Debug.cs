using System.Runtime.InteropServices;
using System.Text;

namespace System.Diagnostics
{

    public static unsafe class Debug
    {
        // Core method for writing debug output
        public static void WriteLine(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            // Convert to UTF-8 bytes with newline
            byte[] bytes = Encoding.UTF8.GetBytes(message + "\n");

            // Use fixed to get pointer
            fixed (byte* ptr = bytes)
            {
                NativeDebugWrite(ptr, bytes.Length);
            }
        }

        // Overload for byte array
        public static void WriteLine(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return;

            fixed (byte* ptr = bytes)
            {
                NativeDebugWrite(ptr, bytes.Length);
            }
        }

        // Native method for output
        [DllImport("*", EntryPoint = "_DebugWrite",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void NativeDebugWrite(byte* text, int length);
    }

}