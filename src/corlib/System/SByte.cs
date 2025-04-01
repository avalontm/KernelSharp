namespace System
{
    public struct SByte
    {
        public override string ToString()
        {
            return ((ulong)this).ToString();
        }
    }
}
