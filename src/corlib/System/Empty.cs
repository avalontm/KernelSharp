namespace System
{
    internal sealed class Empty
    {
        private Empty()
        {
        }

        public static readonly Empty Value = new Empty();

        public override string ToString()
        {
            return string.Empty;
        }
    }
}