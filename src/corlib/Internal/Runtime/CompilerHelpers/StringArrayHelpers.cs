using System;
using System.Runtime;
using Internal.Runtime;
using Internal.Runtime.CompilerServices;

namespace Internal.Runtime.CompilerHelpers
{
    internal unsafe class StringArrayHelpers
    {
        [RuntimeExport("StringArrayStore")]
        public static unsafe void StringArrayStore(Array array, int index, string value)
        {
            if (array == null)
                ThrowHelpers.ThrowNullReferenceException();

            if (index < 0 || index >= array.Length)
                ThrowHelpers.ThrowIndexOutOfRangeException();

            // Acceso directo a la memoria del array para guardar el puntero a string
            IntPtr arrayPtr = Unsafe.As<Array, IntPtr>(ref array);
            byte* dataStart = (byte*)arrayPtr + IntPtr.Size + sizeof(int);
            IntPtr* slot = (IntPtr*)(dataStart + (index * IntPtr.Size));

            // Obtener el puntero al string (o null)
            IntPtr valuePtr = IntPtr.Zero;
            if (value != null)
                valuePtr = Unsafe.As<string, IntPtr>(ref value);

            // Asignar la referencia
            *slot = valuePtr;
        }

        // Implementación manual para este escenario específico
        public static void StoreString(string[] array, int index, string value)
        {
            StringArrayStore(array, index, value);
        }
    }
}