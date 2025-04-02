namespace System
{
    public struct Int32
    {
        public const int MaxValue = 0x7fffffff;
        public const int MinValue = -2147483648;


        public override string ToString()
        {
            return ((long)this).ToString();
        }

        public static implicit operator uint(int value) => (uint)value;

    }
}