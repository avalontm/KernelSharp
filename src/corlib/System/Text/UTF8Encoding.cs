using Internal.Runtime.CompilerHelpers;

namespace System.Text
{
    public unsafe class UTF8Encoding : Encoding
    {
        public override string GetString(byte[] bytes)
        {
            if (bytes == null)
                ThrowHelpers.ArgumentNullException("bytes");

            fixed (byte* ptr = bytes)
            {
                return GetString(ptr, bytes.Length);
            }
        }

        public override string GetString(byte[] bytes, int index, int count)
        {
            if (bytes == null)
                ThrowHelpers.ArgumentNullException("bytes");
            if (index < 0 || index > bytes.Length)
                ThrowHelpers.ArgumentOutOfRangeException("index");
            if (count < 0 || index + count > bytes.Length)
                ThrowHelpers.ArgumentOutOfRangeException("count");

            fixed (byte* ptr = &bytes[index])
            {
                return GetString(ptr, count);
            }
        }

        public override unsafe string GetString(byte* ptr)
        {
            if (ptr == null)
                ThrowHelpers.ArgumentNullException("ptr");

            int length = string.strlen(ptr);
            return GetString(ptr, length);
        }

        public override unsafe string GetString(byte* ptr, int count)
        {
            if (ptr == null)
                ThrowHelpers.ArgumentNullException("ptr");
            if (count < 0)
                ThrowHelpers.ArgumentOutOfRangeException("count");

            char* output = stackalloc char[count]; // Maximum possible character count
            int index = 0;

            for (int i = 0; i < count;)
            {
                if (i >= count) break;

                if ((ptr[i] >> 7) == 0)
                {
                    // ASCII character (0xxxxxxx)
                    output[index++] = (char)ptr[i++];
                }
                else if ((ptr[i] >> 5) == 0b110 && i + 1 < count)
                {
                    // 2-byte sequence (110xxxxx 10xxxxxx)
                    int c = ((ptr[i++] & 0b11111) << 6) | ((ptr[i++] & 0b111111));
                    output[index++] = (char)c;
                }
                else if ((ptr[i] >> 4) == 0b1110 && i + 2 < count)
                {
                    // 3-byte sequence (1110xxxx 10xxxxxx 10xxxxxx)
                    int c = ((ptr[i++] & 0b1111) << 12) | ((ptr[i++] & 0b111111) << 6) | ((ptr[i++] & 0b111111));
                    output[index++] = (char)c;
                }
                else if ((ptr[i] >> 3) == 0b11110 && i + 3 < count)
                {
                    // 4-byte sequence (11110xxx 10xxxxxx 10xxxxxx 10xxxxxx)
                    // This requires surrogate pairs in UTF-16
                    int codePoint = ((ptr[i++] & 0b111) << 18) | ((ptr[i++] & 0b111111) << 12) |
                                    ((ptr[i++] & 0b111111) << 6) | ((ptr[i++] & 0b111111));

                    // Convert to surrogate pair
                    codePoint -= 0x10000;
                    output[index++] = (char)((codePoint >> 10) + 0xD800); // High surrogate
                    output[index++] = (char)((codePoint & 0x3FF) + 0xDC00); // Low surrogate
                }
                else
                {
                    // Invalid sequence, skip
                    i++;
                }
            }

            return new string(output, 0, index);
        }

        public override int GetByteCount(string s)
        {
            if (s == null)
                ThrowHelpers.ArgumentNullException("s");

            int count = 0;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if (c < 0x80)
                {
                    // ASCII character
                    count += 1;
                }
                else if (c < 0x800)
                {
                    // 2-byte sequence
                    count += 2;
                }
                else if (c >= 0xD800 && c <= 0xDBFF && i + 1 < s.Length && s[i + 1] >= 0xDC00 && s[i + 1] <= 0xDFFF)
                {
                    // Surrogate pair (4-byte sequence)
                    count += 4;
                    i++; // Skip the low surrogate
                }
                else
                {
                    // 3-byte sequence
                    count += 3;
                }
            }

            return count;
        }

        public override int GetByteCount(char* chars, int count)
        {
            if (chars == null)
                ThrowHelpers.ArgumentNullException("chars");
            if (count < 0)
                ThrowHelpers.ArgumentOutOfRangeException("count");

            int byteCount = 0;

            for (int i = 0; i < count; i++)
            {
                char c = chars[i];

                if (c < 0x80)
                {
                    // ASCII character
                    byteCount += 1;
                }
                else if (c < 0x800)
                {
                    // 2-byte sequence
                    byteCount += 2;
                }
                else if (c >= 0xD800 && c <= 0xDBFF && i + 1 < count && chars[i + 1] >= 0xDC00 && chars[i + 1] <= 0xDFFF)
                {
                    // Surrogate pair (4-byte sequence)
                    byteCount += 4;
                    i++; // Skip the low surrogate
                }
                else
                {
                    // 3-byte sequence
                    byteCount += 3;
                }
            }

            return byteCount;
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            if (chars == null)
                ThrowHelpers.ArgumentNullException("chars");
            if (index < 0 || index > chars.Length)
                ThrowHelpers.ArgumentOutOfRangeException("index");
            if (count < 0 || index + count > chars.Length)
                ThrowHelpers.ArgumentOutOfRangeException("count");

            fixed (char* ptr = &chars[index])
            {
                return GetByteCount(ptr, count);
            }
        }

        public override byte[] GetBytes(string s)
        {
            if (s == null)
                ThrowHelpers.ArgumentNullException("s");

            int byteCount = GetByteCount(s);
            byte[] bytes = new byte[byteCount];

            fixed (char* charPtr = s)
            fixed (byte* bytePtr = bytes)
            {
                GetBytes(charPtr, s.Length, bytePtr, byteCount);
            }

            return bytes;
        }

        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (s == null || bytes == null)
                ThrowHelpers.ArgumentNullException(s == null ? "s" : "bytes");
            if (charIndex < 0 || charIndex > s.Length)
                ThrowHelpers.ArgumentOutOfRangeException("charIndex");
            if (charCount < 0 || charIndex + charCount > s.Length)
                ThrowHelpers.ArgumentOutOfRangeException("charCount");
            if (byteIndex < 0 || byteIndex > bytes.Length)
                ThrowHelpers.ArgumentOutOfRangeException("byteIndex");

            fixed (char* charPtr = s)
            fixed (byte* bytePtr = &bytes[byteIndex])
            {
                return GetBytes(charPtr + charIndex, charCount, bytePtr, bytes.Length - byteIndex);
            }
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (chars == null || bytes == null)
                ThrowHelpers.ArgumentNullException(chars == null ? "chars" : "bytes");
            if (charIndex < 0 || charIndex > chars.Length)
                ThrowHelpers.ArgumentOutOfRangeException("charIndex");
            if (charCount < 0 || charIndex + charCount > chars.Length)
                ThrowHelpers.ArgumentOutOfRangeException("charCount");
            if (byteIndex < 0 || byteIndex > bytes.Length)
                ThrowHelpers.ArgumentOutOfRangeException("byteIndex");

            fixed (char* charPtr = &chars[charIndex])
            fixed (byte* bytePtr = &bytes[byteIndex])
            {
                return GetBytes(charPtr, charCount, bytePtr, bytes.Length - byteIndex);
            }
        }

        public override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
        {
            if (chars == null || bytes == null)
                ThrowHelpers.ArgumentNullException(chars == null ? "chars" : "bytes");
            if (charCount < 0)
                ThrowHelpers.ArgumentOutOfRangeException("charCount");
            if (byteCount < 0)
                ThrowHelpers.ArgumentOutOfRangeException("byteCount");

            int byteIndex = 0;

            for (int i = 0; i < charCount && byteIndex < byteCount;)
            {
                char c = chars[i++];

                if (c < 0x80)
                {
                    // ASCII character
                    if (byteIndex < byteCount)
                        bytes[byteIndex++] = (byte)c;
                    else
                        break;
                }
                else if (c < 0x800)
                {
                    // 2-byte sequence
                    if (byteIndex + 1 < byteCount)
                    {
                        bytes[byteIndex++] = (byte)(0xC0 | (c >> 6));
                        bytes[byteIndex++] = (byte)(0x80 | (c & 0x3F));
                    }
                    else
                        break;
                }
                else if (c >= 0xD800 && c <= 0xDBFF && i < charCount && chars[i] >= 0xDC00 && chars[i] <= 0xDFFF)
                {
                    // Surrogate pair (4-byte sequence)
                    if (byteIndex + 3 < byteCount)
                    {
                        // Calculate the Unicode code point
                        int codePoint = 0x10000 + ((c - 0xD800) << 10) + (chars[i++] - 0xDC00);

                        bytes[byteIndex++] = (byte)(0xF0 | (codePoint >> 18));
                        bytes[byteIndex++] = (byte)(0x80 | ((codePoint >> 12) & 0x3F));
                        bytes[byteIndex++] = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
                        bytes[byteIndex++] = (byte)(0x80 | (codePoint & 0x3F));
                    }
                    else
                        break;
                }
                else
                {
                    // 3-byte sequence
                    if (byteIndex + 2 < byteCount)
                    {
                        bytes[byteIndex++] = (byte)(0xE0 | (c >> 12));
                        bytes[byteIndex++] = (byte)(0x80 | ((c >> 6) & 0x3F));
                        bytes[byteIndex++] = (byte)(0x80 | (c & 0x3F));
                    }
                    else
                        break;
                }
            }

            return byteIndex;
        }
    }
}