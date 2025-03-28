namespace System
{
    public struct Byte
    {
        public const byte MaxValue = 255;

        public static byte Parse(string s)
        {
            const string digits = "0123456789";
            var result = 0;

            int z = 0;
            bool neg = false;

            if (s.Length >= 1)
            {
                if (s[0] == '+')
                {
                    z = 1;
                }

                if (s[0] == '-')
                {
                    z = 1;
                    neg = true;
                }
            }

            for (int i = z; i < s.Length; i++)
            {
                var ind = digits.IndexOf(s[i]);
                if (ind == -1)
                {
                    return 0;
                }
                result = (result * 10) + ind;
            }

            if (neg)
            {
                result *= -1;
            }

            return (byte)result;
        }

        public int CompareTo(byte b)
        {
            if (this != b)
            {
                return -1;
            }

            return 0;
        }

        public override readonly unsafe string ToString()
        {
            return ((ushort)this).ToString();
        }

        public readonly string ToString(string format)
        {
            return ((ushort)this).ToString(format);
        }
    }
}
