namespace System
{
    public unsafe struct UInt32
    {
        public const uint MaxValue = 0xFFFFFFFF;
        public const uint MinValue = 0;

        public override string ToString()
        {
           return ((int)this).ToString();
        }

        // Optional: Hexadecimal representation
        public string ToHexString()
        {
            uint val = this;
            char* x = stackalloc char[11]; // "0x" + 8 hex digits + null
            x[0] = '0';
            x[1] = 'x';
            int i = 9;
            x[10] = '\0';

            // Handle special case for zero
            if (val == 0)
            {
                x[i--] = '0';
            }
            else
            {
                do
                {
                    uint d = val & 0xF;
                    val >>= 4;

                    if (d < 10)
                        x[i--] = (char)(d + '0');
                    else
                        x[i--] = (char)(d - 10 + 'A');
                } while (val > 0);
            }

            // Fill in leading zeros to make it 8 digits
            while (i > 1)
            {
                x[i--] = '0';
            }

            return new string(x, 0, 10);
        }
    }
}