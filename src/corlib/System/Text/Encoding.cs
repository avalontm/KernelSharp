namespace System.Text
{
    public abstract unsafe class Encoding
    {
        // Utilizamos propiedades en lugar de campos estáticos
        private static UTF8Encoding s_utf8;
        private static ASCIIEncoding s_ascii;

        public static UTF8Encoding UTF8
        {
            get
            {
                if (s_utf8 == null)
                    s_utf8 = new UTF8Encoding();
                return s_utf8;
            }
            set { s_utf8 = value; }
        }

        public static ASCIIEncoding ASCII
        {
            get
            {
                if (s_ascii == null)
                    s_ascii = new ASCIIEncoding();
                return s_ascii;
            }
            set { s_ascii = value; }
        }

        // Método auxiliar para obtener una codificación segura
        public static Encoding GetASCIIEncoding()
        {
            return new ASCIIEncoding();
        }

        public static Encoding GetUTF8Encoding()
        {
            return new UTF8Encoding();
        }

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
    }
}