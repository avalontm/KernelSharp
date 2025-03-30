using Internal.Runtime.CompilerHelpers;

namespace System.Text
{
    public abstract unsafe class Encoding
    {
        // Static encoding instances
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

        // Helper methods to get safe encoding instances
        public static Encoding GetASCIIEncoding()
        {
            return new ASCIIEncoding();
        }

        public static Encoding GetUTF8Encoding()
        {
            return new UTF8Encoding();
        }

        // Abstract methods that must be implemented by derived classes
        public abstract string GetString(byte[] bytes);
        public abstract string GetString(byte[] bytes, int index, int count);
        public abstract string GetString(byte* ptr, int count);

        // Abstract methods for calculating byte counts
        public abstract int GetByteCount(string s);
        public abstract int GetByteCount(char* chars, int count);

        // Abstract methods for getting bytes
        public abstract byte[] GetBytes(string s);
        public abstract int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex);
        public abstract int GetBytes(char* chars, int charCount, byte* bytes, int byteCount);

        // Non-abstract implementation that uses the abstract methods
        public int GetBytes(string s, byte[] bytes, int byteIndex)
        {
            if (s == null || bytes == null)
                ThrowHelpers.ArgumentNullException(s == null ? "s" : "bytes");

            return GetBytes(s, 0, s.Length, bytes, byteIndex);
        }

        // Default implementation for character-by-character encoding (can be overridden)
        public virtual byte[] GetBytes(char[] chars)
        {
            if (chars == null)
                ThrowHelpers.ArgumentNullException("chars");

            byte[] bytes = new byte[GetByteCount(chars, 0, chars.Length)];
            GetBytes(chars, 0, chars.Length, bytes, 0);
            return bytes;
        }

        public virtual unsafe string GetString(byte* ptr)
        {
            if (ptr == null)
                ThrowHelpers.ArgumentNullException("ptr");

            // Determine the length by finding null terminator
            int length = 0;
            byte* p = ptr;
            while (*p != 0)
            {
                length++;
                p++;
            }

            // Now that we know the length, call the overload
            return GetString(ptr, length);
        }

        // Helper method for estimating byte count from character count
        public virtual int GetByteCount(char[] chars)
        {
            if (chars == null)
                ThrowHelpers.ArgumentNullException("chars");

            return GetByteCount(chars, 0, chars.Length);
        }

        // Abstract method that must be implemented
        public abstract int GetByteCount(char[] chars, int index, int count);

        // Abstract method that must be implemented
        public abstract int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex);
    }
}