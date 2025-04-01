namespace System.Text
{
    public unsafe class ASCIIEncoding : Encoding
    {
        public override string GetString(byte* ptr)
        {
            int length = string.strlen(ptr);
            byte* p = (byte*)ptr;
            char* newp = stackalloc char[length];
            for (int i = 0; i < length; i++)
            {
                newp[i] = (char)p[i];
            }
            return new string(newp, 0, length);
        }

        public override string GetString(byte[] bytes)
        {
            byte* ptr;

            fixed (byte* _ptr = bytes)
            {
                ptr = _ptr;
            }

            int length = string.strlen(ptr);
            byte* p = (byte*)ptr;
            char* newp = stackalloc char[length];
            for (int i = 0; i < length; i++)
            {
                newp[i] = (char)p[i];
            }
            return new string(newp, 0, length);
        }

        public string GetString(byte[] bytes, int index, int count)
        {
            char[] chars = new char[count];

            for (int i = 0; i < count; i++)
            {
                chars[i] = (char)bytes[index + i];
            }

            return new string(chars);
        }
    }
}

