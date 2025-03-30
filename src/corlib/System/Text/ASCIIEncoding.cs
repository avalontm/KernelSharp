using Internal.Runtime.CompilerHelpers;

namespace System.Text
{
    public unsafe class ASCIIEncoding : Encoding
    {
        public override string GetString(byte* ptr)
        {
            if (ptr == null)
                ThrowHelpers.ThrowArgumentNullException("ptr");

            int length = string.strlen(ptr);
            char* newp = stackalloc char[length];
            for (int i = 0; i < length; i++)
            {
                newp[i] = (char)(ptr[i] & 0x7F); // Ensure ASCII range (0-127)
            }
            return new string(newp, 0, length);
        }

        public override string GetString(byte[] bytes)
        {
            if (bytes == null)
                ThrowHelpers.ThrowArgumentNullException("bytes");

            fixed (byte* ptr = bytes)
            {
                return GetString(ptr, bytes.Length);
            }
        }

        public override string GetString(byte[] bytes, int index, int count)
        {
            if (bytes == null)
                ThrowHelpers.ThrowArgumentNullException("bytes");
            if (index < 0 || index > bytes.Length)
                ThrowHelpers.ThrowArgumentOutOfRangeException("index");
            if (count < 0 || index + count > bytes.Length)
                ThrowHelpers.ThrowArgumentOutOfRangeException("count");

            char[] chars = new char[count];
            for (int i = 0; i < count; i++)
            {
                chars[i] = (char)(bytes[index + i] & 0x7F); // Ensure ASCII range
            }
            return new string(chars);
        }

        public override string GetString(byte* ptr, int count)
        {
            if (ptr == null)
                ThrowHelpers.ThrowArgumentNullException("ptr");
            if (count < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("count");

            char* newp = stackalloc char[count];
            for (int i = 0; i < count; i++)
            {
                newp[i] = (char)(ptr[i] & 0x7F); // Ensure ASCII range
            }
            return new string(newp, 0, count);
        }

        public override int GetByteCount(string s)
        {
            if (s == null)
                ThrowHelpers.ThrowArgumentNullException("s");

            return s.Length; // In ASCII, each character is one byte
        }

        public override int GetByteCount(char* chars, int count)
        {
            if (chars == null)
                ThrowHelpers.ThrowArgumentNullException("chars");
            if (count < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("count");

            return count; // In ASCII, each character is one byte
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            if (chars == null)
                ThrowHelpers.ThrowArgumentNullException("chars");
            if (index < 0 || index > chars.Length)
                ThrowHelpers.ThrowArgumentOutOfRangeException("index");
            if (count < 0 || index + count > chars.Length)
                ThrowHelpers.ThrowArgumentOutOfRangeException("count");

            return count; // In ASCII, each character is one byte
        }

        public override byte[] GetBytes(string s)
        {
            if (s == null)
                ThrowHelpers.ThrowArgumentNullException("s");

            byte[] bytes = new byte[s.Length];

            for (int i = 0; i < s.Length; i++)
            {
                bytes[i] = (byte)(s[i] & 0x7F); // Truncate to ASCII
            }

            return bytes;
        }

        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (s == null || bytes == null)
                ThrowHelpers.ThrowArgumentNullException(s == null ? "s" : "bytes");
            if (charIndex < 0 || charIndex > s.Length)
                ThrowHelpers.ThrowArgumentOutOfRangeException("charIndex");
            if (charCount < 0 || charIndex + charCount > s.Length)
                ThrowHelpers.ThrowArgumentOutOfRangeException("charCount");
            if (byteIndex < 0 || byteIndex > bytes.Length)
                ThrowHelpers.ThrowArgumentOutOfRangeException("byteIndex");
            if (bytes.Length - byteIndex < charCount)
                ThrowHelpers.ThrowArgumentException("Insufficient space in the byte array.");

            for (int i = 0; i < charCount; i++)
            {
                bytes[byteIndex + i] = (byte)(s[charIndex + i] & 0x7F); // Truncate to ASCII
            }

            return charCount;
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (chars == null || bytes == null)
                ThrowHelpers.ThrowArgumentNullException(chars == null ? "chars" : "bytes");
            if (charIndex < 0 || charIndex > chars.Length)
                ThrowHelpers.ThrowArgumentOutOfRangeException("charIndex");
            if (charCount < 0 || charIndex + charCount > chars.Length)
                ThrowHelpers.ThrowArgumentOutOfRangeException("charCount");
            if (byteIndex < 0 || byteIndex > bytes.Length)
                ThrowHelpers.ThrowArgumentOutOfRangeException("byteIndex");
            if (bytes.Length - byteIndex < charCount)
                ThrowHelpers.ThrowArgumentException("Insufficient space in the byte array.");

            for (int i = 0; i < charCount; i++)
            {
                bytes[byteIndex + i] = (byte)(chars[charIndex + i] & 0x7F); // Truncate to ASCII
            }

            return charCount;
        }

        public override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
        {
            if (chars == null || bytes == null)
                ThrowHelpers.ThrowArgumentNullException(chars == null ? "chars" : "bytes");
            if (charCount < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("charCount");
            if (byteCount < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("byteCount");

            int count = charCount < byteCount ? charCount : byteCount;

            for (int i = 0; i < count; i++)
            {
                bytes[i] = (byte)(chars[i] & 0x7F); // Truncate to ASCII
            }

            return count;
        }
    }
}