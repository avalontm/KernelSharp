using System.Runtime.CompilerServices;

namespace System
{
    public unsafe struct IntPtr
    {
        void* _value;
        public static readonly int Size = sizeof(void*);

        public IntPtr(void* value) { _value = value; }
        public IntPtr(int value) { _value = (void*)value; }
        public IntPtr(uint value) { _value = (void*)value; }
        public IntPtr(long value) { _value = (void*)value; }
        public IntPtr(ulong value) { _value = (void*)value; }

        // Manual definition of a zero IntPtr
        public static readonly IntPtr Zero = new IntPtr(0);

        public bool Equals(IntPtr ptr)
            => _value == ptr._value;

        public static explicit operator IntPtr(int value) => new IntPtr(value);

        public static explicit operator IntPtr(uint value) => new IntPtr(value);

        public static explicit operator IntPtr(long value) => new IntPtr(value);

        public static explicit operator IntPtr(ulong value) => new IntPtr(value);

        public static explicit operator IntPtr(void* value) => new IntPtr(value);

        public static explicit operator void*(IntPtr value) => value._value;

        public static explicit operator int(IntPtr value)
        {
            var l = (long)value._value;

            return checked((int)l);
        }

        public static explicit operator long(IntPtr value) => (long)value._value;

        public static explicit operator ulong(IntPtr value) => (ulong)value._value;

        public static explicit operator IntPtr(UIntPtr ptr) => new IntPtr() { _value = (void*)ptr };

        public static IntPtr operator +(IntPtr a, uint b)
            => new IntPtr((byte*)a._value + b);

        public static IntPtr operator +(IntPtr a, ulong b)
            => new IntPtr((byte*)a._value + b);

        public static bool operator ==(IntPtr a, IntPtr b)
        {
            return a._value == b._value;
        }

        public static bool operator !=(IntPtr a, IntPtr b)
        {
            return !(a._value == b._value);
        }

        public static IntPtr Add(IntPtr ptr, int offset)
        {
            // Convierte el offset de �ndice a bytes seg�n el tipo de dato
            return new IntPtr((byte*)ptr._value + offset);
        }

        public static IntPtr Add(IntPtr ptr, int offset, int elementSize)
        {
            // Convierte el offset de �ndice a bytes seg�n el tama�o del elemento
            return new IntPtr((byte*)ptr._value + (offset * elementSize));
        }

        public override string ToString()
        {
            return ((UIntPtr)this).ToString();
        }

        internal void* ToPointer()
        {
            return (void*)this;
        }

        internal long ToInt64()
        {
            return (long)this;
        }
    }
}