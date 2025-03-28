using Internal.Runtime.CompilerServices;
using System;
using System.Runtime;

namespace Internal.Runtime.CompilerHelpers
{
    internal unsafe class ArrayAccessHelpers
    {
        // Para asignar valores enteros a arrays de int
        [RuntimeExport("ArrayStoreInt")]
        public static unsafe void ArrayStoreInt(Array array, int index, int value)
        {
            if (array == null)
                while (true) { } // Bloqueo simple en caso de error

            if (index < 0 || index >= array.Length)
                while (true) { } // Bloqueo simple en caso de error

            // Calcular la dirección del elemento
            IntPtr arrayPtr = Unsafe.As<Array, IntPtr>(ref array);
            byte* dataStart = (byte*)arrayPtr + IntPtr.Size + sizeof(int);
            int* elementAddr = (int*)(dataStart + (index * sizeof(int)));

            // Asignar el valor
            *elementAddr = value;
        }

        // Para leer valores enteros de arrays de int
        [RuntimeExport("ArrayLoadInt")]
        public static unsafe int ArrayLoadInt(Array array, int index)
        {
            if (array == null)
                while (true) { } // Bloqueo simple en caso de error

            if (index < 0 || index >= array.Length)
                while (true) { } // Bloqueo simple en caso de error

            // Calcular la dirección del elemento
            IntPtr arrayPtr = Unsafe.As<Array, IntPtr>(ref array);
            byte* dataStart = (byte*)arrayPtr + IntPtr.Size + sizeof(int);
            int* elementAddr = (int*)(dataStart + (index * sizeof(int)));

            // Devolver el valor
            return *elementAddr;
        }
    }
}