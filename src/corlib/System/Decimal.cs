using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    /// <summary>
    /// Implementación básica de la estructura Decimal.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Decimal
    {
        // Campos internos para almacenar los datos
        private int _lo;
        private int _mid;
        private int _hi;
        private int _flags;

        // Constantes
        private const int SignMask = unchecked((int)0x80000000);
        private const int ScaleMask = 0x00FF0000;
        private const int ScaleShift = 16;

        // Valores constantes básicos
        public static readonly Decimal Zero = new Decimal();
        public static readonly Decimal One = new Decimal(1);
        public static readonly Decimal MinusOne = new Decimal(-1);

        // Constructor simple desde int
        public Decimal(int value)
        {
            _lo = value < 0 ? -value : value;
            _mid = 0;
            _hi = 0;
            _flags = value < 0 ? SignMask : 0;
        }

        // Constructor básico para todos los componentes
        private Decimal(int lo, int mid, int hi, bool isNegative, byte scale)
        {
            _lo = lo;
            _mid = mid;
            _hi = hi;
            _flags = (isNegative ? SignMask : 0) | ((scale & 0xFF) << ScaleShift);
        }

        // Conversiones implícitas básicas
        public static implicit operator Decimal(int value) => new Decimal(value);

        // Conversiones explícitas básicas
        public static explicit operator int(Decimal value)
        {
            // Implementación simple solo para enteros
            bool isNegative = (value._flags & SignMask) != 0;
            byte scale = (byte)((value._flags & ScaleMask) >> ScaleShift);

            // Solo considera la parte baja para la conversión simple
            int result = value._lo;

            // Simplificación extrema: solo dividimos por 10 según la escala
            while (scale > 0)
            {
                result /= 10;
                scale--;
            }

            return isNegative ? -result : result;
        }

        // Operadores aritméticos básicos
        public static Decimal operator +(Decimal d1, Decimal d2)
        {
            // Implementación muy básica que solo funciona para casos simples
            int result = (int)d1 + (int)d2;
            return new Decimal(result);
        }

        public static Decimal operator -(Decimal d1, Decimal d2)
        {
            // Implementación muy básica
            int result = (int)d1 - (int)d2;
            return new Decimal(result);
        }

        // Operadores de comparación básicos
        public static bool operator ==(Decimal d1, Decimal d2)
        {
            return d1._lo == d2._lo &&
                   d1._mid == d2._mid &&
                   d1._hi == d2._hi &&
                   d1._flags == d2._flags;
        }

        public static bool operator !=(Decimal d1, Decimal d2)
        {
            return !(d1 == d2);
        }

        // Implementaciones requeridas de Object
        public override bool Equals(object obj)
        {
            if (!(obj is Decimal)) return false;
            return this == (Decimal)obj;
        }

        public override int GetHashCode()
        {
            return _lo ^ _mid ^ _hi ^ _flags;
        }

        public override string ToString()
        {
            // Implementación muy básica
            bool isNegative = (_flags & SignMask) != 0;
            byte scale = (byte)((_flags & ScaleMask) >> ScaleShift);

            string result = _lo.ToString();

            if (isNegative)
                return "-" + result;

            return result;
        }
    }
}