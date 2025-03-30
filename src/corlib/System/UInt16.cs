namespace System
{
    public struct UInt16
    {
        public unsafe override string ToString()
        {
            return ((uint)this).ToString();
        }

    }
}
