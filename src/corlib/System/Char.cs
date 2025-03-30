using Internal.Runtime.CompilerHelpers;
using System.Globalization;
namespace System
{
    public struct Char
    {
        public const char MaxValue = (char)0xffff;
        public const char MinValue = (char)0;

        // New methods for surrogate pair and UTF-32 conversion
        public static bool IsSurrogatePair(string s, int index)
        {
            if (s == null)
                ThrowHelpers.ArgumentNullException(nameof(s));

            if (index < 0 || index >= s.Length)
                ThrowHelpers.ArgumentOutOfRangeException(nameof(index));

            // Check if there's a next character and if the current and next form a surrogate pair
            return (index + 1 < s.Length) &&
                   IsHighSurrogate(s[index]) &&
                   IsLowSurrogate(s[index + 1]);
        }

        public static bool IsSurrogatePair(char highSurrogate, char lowSurrogate)
        {
            return IsHighSurrogate(highSurrogate) && IsLowSurrogate(lowSurrogate);
        }

        public static bool IsHighSurrogate(char c)
        {
            // High surrogate range is D800 to DBFF
            return (c >= '\uD800' && c <= '\uDBFF');
        }

        public static bool IsLowSurrogate(char c)
        {
            // Low surrogate range is DC00 to DFFF
            return (c >= '\uDC00' && c <= '\uDFFF');
        }

        public static int ConvertToUtf32(string s, int index)
        {
            // Null check using ThrowHelper
            if (s == null)
                ThrowHelpers.ThrowArgumentNullException(s);

            // Index bounds check using ThrowHelper
            if ((uint)index >= (uint)s.Length)
                ThrowHelpers.IndexOutOfRangeException(s);

            // Surrogate pair handling
            if (index + 1 < s.Length &&
                IsHighSurrogate(s[index]) &&
                IsLowSurrogate(s[index + 1]))
            {
                char highSurrogate = s[index];
                char lowSurrogate = s[index + 1];

                // Convert surrogate pair to code point
                return (((highSurrogate - 0xD800) * 0x400) +
                        (lowSurrogate - 0xDC00) +
                        0x10000);
            }

            // Normal character
            return s[index];
        }


        public static int ConvertToUtf32(char highSurrogate, char lowSurrogate)
        {
            if (!IsSurrogatePair(highSurrogate, lowSurrogate))
               ThrowHelpers.ArgumentException("Invalid surrogate pair");

            // Convert surrogate pair to code point
            // Formula: (High - 0xD800) * 0x400 + (Low - 0xDC00) + 0x10000
            return (((highSurrogate - 0xD800) * 0x400) +
                    (lowSurrogate - 0xDC00) +
                    0x10000);
        }

        // Existing methods remain the same...
        public override string ToString()
        {
            string r = " ";
            r._firstChar = this;
            return r;
        }

        public char ToUpper()
        {
            return this >= 'a' && this <= 'z' ? (char)(this - 32) : this;
        }

        public char ToLower()
        {
            return this >= 'A' && this <= 'Z' ? (char)(this + 32) : this;
        }

        public static bool IsLetter(char c)
        {
            /*plug
    * IsAscii
    * IsLatin1
    */
            c |= ' ';
            return c >= 'a' && c <= 'z';
        }

        public static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        public static bool IsWhiteSpace(char c)
        {
            return IsLatin1(c) ? IsWhiteSpaceLatin1(c) : CharUnicodeInfo.IsWhiteSpace(c);
        }

        private static bool IsLatin1(char ch)
        {
            return ch <= '\x00ff';
        }

        private static bool IsWhiteSpaceLatin1(char c)
        {
            return (c == ' ') || (c >= '\x0009' && c <= '\x000d') || c == '\x00a0' || c == '\x0085';
        }

        public static bool IsLetterOrDigit(char c)
        {
            return (IsLetter(c) || IsDigit(c));
        }

        public override bool Equals(object obj)
        {
            if (obj is Char)
            {
                return (this == ((Char)obj));
            }
            else
            {
                return false;
            }
        }

        public bool Equals(char obj)
        {
            return this == obj;
        }

        public static bool operator ==(char left, char right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(char left, char right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return this;
        }
    }
}