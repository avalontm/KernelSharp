namespace System.Text
{
    public abstract unsafe class Encoding
    {
        public static UTF8Encoding UTF8;
        public static ASCIIEncoding ASCII;

        public abstract string GetString(byte[] bytes);
        public abstract string GetString(byte* ptr);

        public byte[] GetBytes(string s)
        {
            byte[] buffer = new byte[s.Length];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)s[i];
            }
            return buffer;
        }
        /*
        public string GetString(byte[] bytes)
        {
            string response = string.Empty;

            for (int i = 0; i < bytes.Length; i++)
            {
                response += (char)bytes[i];
            }

            return response;
        }*/

        static Encoding()
        {
            UTF8 = new UTF8Encoding();
            ASCII = new ASCIIEncoding();
        }
    }
}
