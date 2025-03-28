using System.Globalization;

namespace System
{
    public struct Char
    {
        public const char MaxValue = (char)0xffff;
        public const char MinValue = (char)0;

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