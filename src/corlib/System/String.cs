using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// String.cs - Low-level kernel implementation
// KernelSharp CoreLib implementation for strings

namespace System
{
    public sealed unsafe class String
    {
        // Campo estático para almacenar la cadena vacía y método de inicialización
        internal static readonly string s_emptyString = InitializeEmptyString();

        private static string InitializeEmptyString()
        {
            // Crea la instancia de cadena vacía
            EETypePtr et = EETypePtr.EETypePtrOf<string>();
            object stringObj = RuntimeImports.RhpNewArray(et._value, 0);
            string s = Unsafe.As<object, string>(ref stringObj);
            s._length = 0;
            s._firstChar = '\0';
            return s;
        }

        // Propiedad Empty que utiliza la cadena precompilada
        public static string Empty
        {
            get
            {
                return s_emptyString;
            }
        }

        // Método auxiliar necesario para la preinicialización de cadenas
        internal static unsafe ref char GetFirstChar(string str)
        {
            return ref str._firstChar;
        }

        // The layout of the string type is a contract with the compiler.
        private int _length;
        internal char _firstChar;

        public int Length
        {
            [Intrinsic]
            get => _length;
            set => _length = value;
        }

        public unsafe char this[int index]
        {
            [Intrinsic]
            get
            {
                if (index < 0 || index >= _length)
                    ThrowHelpers.IndexOutOfRangeException();

                fixed (char* p = &_firstChar)
                {
                    return p[index];
                }
            }

            set
            {
                if (index < 0 || index >= _length)
                    ThrowHelpers.IndexOutOfRangeException();

                fixed (char* p = &_firstChar)
                {
                    p[index] = value;
                }
            }
        }

        // Constructor from char pointer
        public String(char* ptr)
        {
            if (ptr == null)
                ThrowHelpers.ThrowArgumentNullException("[Constructor String] char ptr");

            // Calculate length by finding null terminator
            int length = 0;
            while (ptr[length] != '\0')
                length++;

            // Create string from pointer with calculated length
            this._length = length;

            // If length is 0, use empty string
            if (length == 0)
            {
                this._firstChar = '\0';
                return;
            }

            // Allocate and copy characters
            fixed (char* dest = &this._firstChar)
            {
                for (int i = 0; i < length; i++)
                {
                    dest[i] = ptr[i];
                }
                dest[length] = '\0';
            }
        }

        // Constructor from IntPtr
        public String(IntPtr ptr) : this((char*)ptr)
        {
        }

        public String(char[] value)
        {
            if (value == null)
                ThrowHelpers.ThrowArgumentNullException("[Constructor String] char array");

            // Get array length
            int length = value.Length;

            // Set length in the instance
            this._length = length;

            // If length is 0, use empty string
            if (length == 0)
            {
                this._firstChar = '\0';
                return;
            }

            // Copy characters from array to instance
            fixed (char* dest = &this._firstChar)
            {
                for (int i = 0; i < length; i++)
                {
                    dest[i] = value[i];
                }
                dest[length] = '\0';
            }
        }

        // Constructor from char array with index and length
        public String(char[] buf, int index, int length)
        {
            // Validate arguments
            if (buf == null)
                ThrowHelpers.ThrowArgumentNullException("[Constructor String] buf");

            if (index < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("[Constructor String] index");

            if (length < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("[Constructor String] length");

            if (index + length > buf.Length)
                ThrowHelpers.ThrowArgumentOutOfRangeException("[Constructor String] length");

            // Handle empty string case
            if (length == 0)
            {
                this._length = 0;
                this._firstChar = '\0';
                return;
            }

            // Set length
            this._length = length;

            // Copy characters
            fixed (char* src = buf)
            fixed (char* dest = &this._firstChar)
            {
                for (int i = 0; i < length; i++)
                {
                    dest[i] = src[index + i];
                }
                dest[length] = '\0';
            }
        }

        // Constructor from char pointer with index and length
        public String(char* ptr, int index, int length)
        {
            // Validate arguments
            if (ptr == null)
                ThrowHelpers.ThrowArgumentNullException("[Constructor String] char* null");

            if (index < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("[Constructor String] index");

            if (length < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("[Constructor String] length");

            // Handle empty string case
            if (length == 0)
            {
                this._length = 0;
                this._firstChar = '\0';
                return;
            }

            // Set length
            this._length = length;

            // Copy characters
            fixed (char* dest = &this._firstChar)
            {
                for (int i = 0; i < length; i++)
                {
                    dest[i] = ptr[index + i];
                }
                dest[length] = '\0';
            }
        }

        /// <summary>
        /// Constructor that creates a new string from an existing string.
        /// </summary>
        public String(string value)
        {
            string newString = Ctor(value);
            // Copy data from newString to this instance
            this._length = newString._length;

            unsafe
            {
                fixed (char* destPtr = &this._firstChar)
                fixed (char* srcPtr = &newString._firstChar)
                {
                    for (int i = 0; i < _length; i++)
                    {
                        destPtr[i] = srcPtr[i];
                    }
                    destPtr[_length] = '\0';
                }
            }
        }

        public static unsafe string FromASCII(nint ptr, int length)
        {
            byte* p = (byte*)ptr;
            char* newp = stackalloc char[length];
            for (int i = 0; i < length; i++)
            {
                newp[i] = (char)p[i];
            }
            return new string(newp, 0, length);
        }

        internal static unsafe string Ctor(char* ptr)
        {
            int len = (int)ptr->m_pEEType->BaseSize;
            int i = 0;

            while (ptr[i++] != '\0')
            {

            }

            return Ctor(ptr, 0, i - 1);
        }

        internal static unsafe string Ctor(IntPtr ptr)
        {
            return Ctor((char*)ptr);
        }

        internal static unsafe string Ctor(char[] buf)
        {
            fixed (char* _buf = buf)
            {
                return Ctor(_buf, 0, buf.Length);
            }
        }

        /// <summary>
        /// Static method to create a string from another string
        /// This method is called by the compiler when using new string(string)
        /// </summary>
        internal static string Ctor(string value)
        {
            if (value == null)
                return null;

            if (value.Length == 0)
                return Empty;

            unsafe
            {
                fixed (char* srcPtr = &value._firstChar)
                {
                    // Create new instance using constructor that accepts pointer, index and length
                    return new string(srcPtr, 0, value.Length);
                }
            }
        }

        /// <summary>
        /// Creates a new String instance from a character pointer.
        /// </summary>
        /// <param name="ptr">Pointer to the source character sequence</param>
        /// <param name="index">Starting index in the sequence</param>
        /// <param name="length">Length of the string to create</param>
        /// <returns>A new String instance</returns>
        internal static unsafe string Ctor(char* ptr, int index, int length)
        {
            // Validate arguments
            if (ptr == null)
                ThrowHelpers.ThrowArgumentNullException("[Constructor String] ptr");

            if (index < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("[Constructor String] index");

            if (length < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("[Constructor String] length: " + length.ToString());

            // Special case for empty strings
            if (length == 0)
                return Empty;

            // Get EEType for String
            EETypePtr et = EETypePtr.EETypePtrOf<string>();

            // Calculate start pointer
            char* start = ptr + index;

            // Create a new String instance using native infrastructure
            object stringObj = RuntimeImports.RhpNewArray(et._value, length);

            // Convert object to string
            string s = Unsafe.As<object, string>(ref stringObj);

            // Set correct length
            s.Length = length;

            // Copy characters from source
            fixed (char* c = &s._firstChar)
            {
                // Manually copy character by character for better control
                for (int i = 0; i < length; i++)
                {
                    c[i] = start[i];
                }

                // Add null terminator after specified length
                // This is useful for interoperability with native code
                c[length] = '\0';
            }

            return s;
        }

        public int LastIndexOf(char j)
        {
            for (int i = Length - 1; i >= 0; i--)
            {
                if (this[i] == j)
                {
                    return i;
                }
            }

            return -1;
        }

        private static unsafe string Ctor(char[] ptr, int index, int length)
        {
            fixed (char* _ptr = ptr)
            {
                return Ctor(_ptr, index, length);
            }
        }

        public override string ToString()
        {
            return this;
        }

        public override bool Equals(object obj)
        {
            return obj is string && Equals((string)obj);
        }

        public bool Equals(string val)
        {
            if (val == null)
                return false;

            if (Length != val.Length)
            {
                return false;
            }

            for (int i = 0; i < Length; i++)
            {
                if (this[i] != val[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool operator ==(string a, string b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is null || b is null)
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(string a, string b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            int hash = 0;

            for (int i = 0; i < Length; i++)
            {
                hash = (hash * 31) + this[i];
            }

            return hash;
        }

        public int IndexOf(char j)
        {
            for (int i = 0; i < Length; i++)
            {
                if (this[i] == j)
                {
                    return i;
                }
            }

            return -1;
        }

        public int IndexOf(char j, int start)
        {
            for (int i = start; i < Length; i++)
            {
                if (this[i] == j)
                {
                    return i;
                }
            }

            return -1;
        }

        public int IndexOf(string substring)
        {
            if (substring == null || substring.Length == 0)
                return 0;

            if (substring.Length > this.Length)
                return -1;

            for (int i = 0; i <= this.Length - substring.Length; i++)
            {
                bool found = true;

                for (int j = 0; j < substring.Length; j++)
                {
                    if (this[i + j] != substring[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    return i;
                }
            }

            return -1;
        }

        public int IndexOf(string substring, int startIndex)
        {
            if (substring == null || substring.Length == 0)
                return startIndex < Length ? startIndex : -1;

            if (startIndex < 0 || startIndex >= Length)
                return -1;

            if (substring.Length > (this.Length - startIndex))
                return -1;

            for (int i = startIndex; i <= this.Length - substring.Length; i++)
            {
                bool found = true;

                for (int j = 0; j < substring.Length; j++)
                {
                    if (this[i + j] != substring[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    return i;
                }
            }

            return -1;
        }

        public static string charToString(char* charArray)
        {
            if (charArray == null)
                return Empty;

            int length = 0;
            while (charArray[length] != '\0')
                length++;

            return new string(charArray, 0, length);
        }

        public static string Copy(string a)
        {
            if (a == null)
                return null;

            if (a.Length == 0)
                return Empty;

            int length = a.Length;
            char* ptr = stackalloc char[length];

            for (int i = 0; i < a.Length; i++)
            {
                ptr[i] = a[i];
            }

            return new string(ptr, 0, length);
        }

        /// <summary>
        /// Format method for string interpolation - Improved implementation
        /// </summary>
        public static string Format(string format, object arg0)
        {
            if (format == null)
                return null;

            if (arg0 == null)
                arg0 = "";

            string argStr = arg0.ToString();

            // Parse the format string looking for {0} placeholders
            return ReplaceFormatItem(format, 0, argStr);
        }

        /// <summary>
        /// Format method for two arguments
        /// </summary>
        public static string Format(string format, object arg0, object arg1)
        {
            if (format == null)
                return null;

            if (arg0 == null)
                arg0 = "";
            if (arg1 == null)
                arg1 = "";

            string arg0Str = arg0.ToString();
            string arg1Str = arg1.ToString();

            // First replace all {0} with arg0
            string temp = ReplaceFormatItem(format, 0, arg0Str);

            // Then replace all {1} with arg1
            return ReplaceFormatItem(temp, 1, arg1Str);
        }

        /// <summary>
        /// Format method for three arguments
        /// </summary>
        public static string Format(string format, object arg0, object arg1, object arg2)
        {
            if (format == null)
                return null;

            if (arg0 == null)
                arg0 = "";
            if (arg1 == null)
                arg1 = "";
            if (arg2 == null)
                arg2 = "";

            string arg0Str = arg0.ToString();
            string arg1Str = arg1.ToString();
            string arg2Str = arg2.ToString();

            // Replace sequentially
            string temp = ReplaceFormatItem(format, 0, arg0Str);
            temp = ReplaceFormatItem(temp, 1, arg1Str);
            return ReplaceFormatItem(temp, 2, arg2Str);
        }

        /// <summary>
        /// Helper method to replace a format item {n} with its string value
        /// </summary>
        private static string ReplaceFormatItem(string format, int index, string value)
        {
            string placeholder = "{" + index + "}";

            // Calculate the resulting string length to avoid multiple allocations
            int placeholderCount = 0;
            int formatLength = format.Length;
            int valueLength = value.Length;
            int placeholderLength = placeholder.Length;

            int i = 0;
            while (i <= formatLength - placeholderLength)
            {
                bool isPlaceholder = true;
                for (int j = 0; j < placeholderLength; j++)
                {
                    if (format[i + j] != placeholder[j])
                    {
                        isPlaceholder = false;
                        break;
                    }
                }

                if (isPlaceholder)
                {
                    placeholderCount++;
                    i += placeholderLength;
                }
                else
                {
                    i++;
                }
            }

            // Calculate result length
            int resultLength = formatLength - (placeholderCount * placeholderLength) + (placeholderCount * valueLength);

            // Allocate result buffer
            char* resultBuffer = stackalloc char[resultLength];
            int resultPos = 0;

            // Perform replacement
            i = 0;
            while (i < formatLength)
            {
                // Check if this position starts a placeholder
                if (i <= formatLength - placeholderLength)
                {
                    bool isPlaceholder = true;
                    for (int j = 0; j < placeholderLength; j++)
                    {
                        if (format[i + j] != placeholder[j])
                        {
                            isPlaceholder = false;
                            break;
                        }
                    }

                    if (isPlaceholder)
                    {
                        // Copy the replacement value
                        for (int j = 0; j < valueLength; j++)
                        {
                            resultBuffer[resultPos++] = value[j];
                        }

                        i += placeholderLength;
                        continue;
                    }
                }

                // Copy character as is
                resultBuffer[resultPos++] = format[i++];
            }

            return new string(resultBuffer, 0, resultLength);
        }

        /// <summary>
        /// Formats the string using parameters array
        /// - Supports: {0}, {1}, {2}, etc.
        /// - Supports escaped braces: {{ and }}
        /// - This is the main Format method that handles string interpolation
        /// </summary>
        public static string Format(string format, params object[] args)
        {
            if (format == null)
                ThrowHelpers.ArgumentNullException("format");

            if (args == null || args.Length == 0)
                return format;

            // Parse for placeholders and produce result
            return FormatHelper(format, args);
        }

        /// <summary>
        /// Helper function to handle string formatting
        /// </summary>
        private static string FormatHelper(string format, object[] args)
        {
            if (format.Length == 0)
                return Empty;

            // First pass: calculate the required buffer size
            int estimatedLength = CalculateBufferSize(format, args);

            char* buffer = stackalloc char[estimatedLength];
            int pos = 0;

            int i = 0;
            while (i < format.Length)
            {
                char ch = format[i++];

                if (ch == '{')
                {
                    // Handle escaped '{{' -> '{'
                    if (i < format.Length && format[i] == '{')
                    {
                        buffer[pos++] = '{';
                        i++;
                        continue;
                    }

                    // Extract the argument index
                    int argIndex = 0;
                    while (i < format.Length && char.IsDigit(format[i]))
                    {
                        argIndex = argIndex * 10 + (format[i] - '0');
                        i++;
                    }

                    // Skip format specifier (anything until we hit '}')
                    while (i < format.Length && format[i] != '}')
                    {
                        i++;
                    }

                    // Skip the closing '}'
                    if (i < format.Length && format[i] == '}')
                    {
                        i++;
                    }

                    // Validate argument index
                    if (argIndex >= 0 && argIndex < args.Length)
                    {
                        // Convert argument to string
                        string argValue = args[argIndex]?.ToString() ?? "null";

                        // Copy to buffer
                        for (int j = 0; j < argValue.Length; j++)
                        {
                            if (pos < estimatedLength)
                                buffer[pos++] = argValue[j];
                        }
                    }
                }
                else if (ch == '}')
                {
                    // Handle escaped '}}' -> '}'
                    if (i < format.Length && format[i] == '}')
                    {
                        buffer[pos++] = '}';
                        i++;
                        continue;
                    }

                    // Unescaped single '}' is copied as is
                    buffer[pos++] = ch;
                }
                else
                {
                    // Regular character
                    buffer[pos++] = ch;
                }
            }

            return new string(buffer, 0, pos);
        }

        /// <summary>
        /// Calculates the required buffer size for the formatted string
        /// </summary>
        private static int CalculateBufferSize(string format, object[] args)
        {
            // Start with 2x the format length as an estimation
            int size = format.Length * 2;

            // For each argument, add its string length (if available)
            foreach (object arg in args)
            {
                if (arg != null)
                {
                    string argStr = arg.ToString();
                    if (argStr != null)
                    {
                        size += argStr.Length;
                    }
                }
            }

            return size;
        }

        /// <summary>
        /// Method to handle string interpolation
        /// </summary>
        public static string Format(FormattableString formattable)
        {
            if (formattable == null)
                ThrowHelpers.ArgumentNullException("formattable");

            string format = formattable.Format;
            object[] args = formattable.GetArguments();

            return Format(format, args);
        }

        // Concatenate two strings
        public static string Concat(string str1, string str2)
        {
            if (str1 == null) str1 = "";
            if (str2 == null) str2 = "";

            int len1 = str1.Length;
            int len2 = str2.Length;
            int totalLength = len1 + len2;

            // If both strings are empty, return empty string
            if (totalLength == 0)
                return "";

            // If one of the strings is empty, return the other
            if (len1 == 0) return str2;
            if (len2 == 0) return str1;

            // Create a new string with the combined length
            unsafe
            {
                char* buffer = stackalloc char[totalLength];

                // Copy the first string
                fixed (char* src1 = &str1._firstChar)
                {
                    for (int i = 0; i < len1; i++)
                    {
                        buffer[i] = src1[i];
                    }
                }

                // Copy the second string
                fixed (char* src2 = &str2._firstChar)
                {
                    for (int i = 0; i < len2; i++)
                    {
                        buffer[len1 + i] = src2[i];
                    }
                }

                return new string(buffer, 0, totalLength);
            }
        }

        public static string Concat(string a, string b, string c)
        {
            return Concat(Concat(a, b), c);
        }

        public static string Concat(string a, string b, string c, string d)
        {
            return Concat(Concat(a, b), Concat(c, d));
        }

        // Handle concatenation of two objects
        public static string Concat(object a, object b)
        {
            string strA = a?.ToString() ?? "";
            string strB = b?.ToString() ?? "";
            return Concat(strA, strB);
        }

        /// <summary>
        /// Concatenates three objects
        /// </summary>
        public static string Concat(object obj0, object obj1, object obj2)
        {
            return Concat(
                obj0?.ToString() ?? "",
                obj1?.ToString() ?? "",
                obj2?.ToString() ?? ""
            );
        }

        /// <summary>
        /// Concatenates four objects
        /// </summary>
        public static string Concat(object obj0, object obj1, object obj2, object obj3)
        {
            return Concat(
                obj0?.ToString() ?? "",
                obj1?.ToString() ?? "",
                obj2?.ToString() ?? "",
                obj3?.ToString() ?? ""
            );
        }

        // Implementation for string array
        public static string Concat(params string[] vs)
        {
            if (vs == null || vs.Length == 0)
                return "";

            if (vs.Length == 1)
                return vs[0] ?? "";

            // Calculate total length first
            int totalLength = 0;
            for (int i = 0; i < vs.Length; i++)
            {
                if (vs[i] != null)
                    totalLength += vs[i].Length;
            }

            // Create buffer for result
            char* buffer = stackalloc char[totalLength];
            int position = 0;

            // Copy each string to buffer
            for (int i = 0; i < vs.Length; i++)
            {
                string str = vs[i];
                if (str == null)
                    continue;

                for (int j = 0; j < str.Length; j++)
                {
                    buffer[position++] = str[j];
                }
            }

            return new string(buffer, 0, totalLength);
        }

        // Implementation for object array
        public static string Concat(params object[] vs)
        {
            if (vs == null || vs.Length == 0)
                return "";

            if (vs.Length == 1)
                return vs[0]?.ToString() ?? "";

            // Calculate total length first
            int totalLength = 0;
            for (int i = 0; i < vs.Length; i++)
            {
                string str = vs[i]?.ToString();
                if (str != null)
                    totalLength += str.Length;
            }

            // Create buffer for result
            char* buffer = stackalloc char[totalLength];
            int position = 0;

            // Copy each string to buffer
            for (int i = 0; i < vs.Length; i++)
            {
                string str = vs[i]?.ToString();
                if (str == null)
                    continue;

                for (int j = 0; j < str.Length; j++)
                {
                    buffer[position++] = str[j];
                }
            }

            return new string(buffer, 0, totalLength);
        }

        public string Remove(int startIndex)
        {
            if (startIndex < 0 || startIndex >= Length)
                ThrowHelpers.IndexOutOfRangeException();

            return Substring(0, startIndex);
        }

        public string[] Split(char chr)
        {
            List<string> strings = new();
            string tmp = string.Empty;
            for (int i = 0; i < Length; i++)
            {
                if (this[i] == chr)
                {
                    strings.Add(tmp);
                    tmp = string.Empty;
                }
                else
                {
                    tmp += this[i];
                }

                if (i == (Length - 1))
                {
                    strings.Add(tmp);
                    tmp = string.Empty;
                }
            }
            return strings.ToArray();
        }

        public unsafe string Substring(int startIndex)
        {
            if (startIndex < 0 || startIndex > Length)
                ThrowHelpers.IndexOutOfRangeException();

            if ((Length == 0) && (startIndex == 0))
            {
                return Empty;
            }

            fixed (char* ptr = &_firstChar)
            {
                return new string(ptr, startIndex, Length - startIndex);
            }
        }

        public unsafe string Substring(int startIndex, int length)
        {
            if (startIndex < 0 || startIndex > Length)
                ThrowHelpers.IndexOutOfRangeException();

            if (length < 0 || startIndex + length > Length)
                ThrowHelpers.IndexOutOfRangeException();

            if (length == 0)
                return Empty;

            fixed (char* ptr = &_firstChar)
            {
                return new string(ptr, startIndex, length);
            }
        }

        public static bool IsNullOrEmpty(string value)
        {
            return value == null || value.Length == 0;
        }

        public static bool IsNullOrWhiteSpace(string value)
        {
            if (value == null)
            {
                return true;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public bool EndsWith(char value)
        {
            int thisLen = Length;
            if (thisLen != 0)
            {
                if (this[thisLen - 1] == value)
                {
                    return true;
                }
            }
            return false;
        }

        public bool EndsWith(string value)
        {
            if (value == null)
                return false;

            if (value.Length > Length)
            {
                return false;
            }

            if (value == this)
            {
                return true;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != this[Length - value.Length + i])
                {
                    return false;
                }
            }
            return true;
        }

        public string ToUpper()
        {
            fixed (char* pthis = &_firstChar)
            {
                string output = new string(pthis, 0, this.Length);
                for (int i = 0; i < this.Length; i++)
                {
                    output[i] = pthis[i].ToUpper();
                }
                return output;
            }
        }

        public string ToLower()
        {
            fixed (char* pthis = &_firstChar)
            {
                string output = new string(pthis, 0, this.Length);
                for (int i = 0; i < this.Length; i++)
                {
                    output[i] = pthis[i].ToLower();
                }
                return output;
            }
        }

        public static int strlen(byte* c)
        {
            int i = 0;
            while (c[i] != 0) i++;
            return i;
        }

        public static int strlen(char* c)
        {
            int i = 0;
            while (c[i] != 0) i++;
            return i;
        }

        public string PadLeft(int totalWidth, char paddingChar)
        {
            if (totalWidth <= Length)
                return this;

            int paddingLength = totalWidth - Length;
            char* buffer = stackalloc char[totalWidth];

            // Add padding characters
            for (int i = 0; i < paddingLength; i++)
            {
                buffer[i] = paddingChar;
            }

            // Copy original string
            fixed (char* src = &_firstChar)
            {
                for (int i = 0; i < Length; i++)
                {
                    buffer[paddingLength + i] = src[i];
                }
            }

            return new string(buffer, 0, totalWidth);
        }

        public string PadLeft(int totalWidth)
        {
            return PadLeft(totalWidth, ' ');
        }

        public string Trim()
        {
            int startIndex = 0;
            int endIndex = Length - 1;

            // Find first non-whitespace character
            while (startIndex <= endIndex && char.IsWhiteSpace(this[startIndex]))
                startIndex++;

            // Find last non-whitespace character
            while (endIndex >= startIndex && char.IsWhiteSpace(this[endIndex]))
                endIndex--;

            int length = endIndex - startIndex + 1;

            if (length <= 0)
                return Empty;

            return Substring(startIndex, length);
        }

        public string Trim(char trimChar)
        {
            int startIndex = 0;
            int endIndex = Length - 1;

            // Find first non-trimChar character
            while (startIndex <= endIndex && this[startIndex] == trimChar)
                startIndex++;

            // Find last non-trimChar character
            while (endIndex >= startIndex && this[endIndex] == trimChar)
                endIndex--;

            int length = endIndex - startIndex + 1;

            if (length <= 0)
                return Empty;

            return Substring(startIndex, length);
        }

        public string TrimStart(char trimChar)
        {
            int startIndex = 0;

            // Find first non-trimChar character
            while (startIndex < Length && this[startIndex] == trimChar)
                startIndex++;

            if (startIndex == 0)
                return this;

            if (startIndex == Length)
                return Empty;

            return Substring(startIndex);
        }

        public string TrimEnd(char trimChar)
        {
            int endIndex = Length - 1;

            // Find last non-trimChar character
            while (endIndex >= 0 && this[endIndex] == trimChar)
                endIndex--;

            if (endIndex == Length - 1)
                return this;

            if (endIndex < 0)
                return Empty;

            return Substring(0, endIndex + 1);
        }

        /// <summary>
        /// Replaces all occurrences of a specified string with another specified string.
        /// </summary>
        public string Replace(string oldValue, string newValue)
        {
            if (oldValue == null)
                ThrowHelpers.ArgumentNullException("oldValue");

            if (oldValue.Length == 0)
                ThrowHelpers.ArgumentException("oldValue cannot be empty");

            if (newValue == null)
                newValue = "";

            // Find all occurrences
            int index = IndexOf(oldValue);
            if (index == -1)
                return this; // No occurrences found

            // Calculate the result length
            int resultLength = Length;
            int diff = newValue.Length - oldValue.Length;

            if (diff != 0)
            {
                // Count occurrences to calculate final length
                int count = 0;
                int pos = 0;
                while ((pos = IndexOf(oldValue, pos)) != -1)
                {
                    count++;
                    pos += oldValue.Length;
                }

                resultLength += diff * count;
            }

            // Create result buffer
            char* resultBuffer = stackalloc char[resultLength];
            int resultPos = 0;

            // Replace all occurrences
            int currentPos = 0;

            while (currentPos < Length)
            {
                index = IndexOf(oldValue, currentPos);

                if (index == -1)
                {
                    // Copy remaining characters
                    for (int i = currentPos; i < Length; i++)
                    {
                        resultBuffer[resultPos++] = this[i];
                    }
                    break;
                }

                // Copy characters before match
                for (int i = currentPos; i < index; i++)
                {
                    resultBuffer[resultPos++] = this[i];
                }

                // Copy replacement
                for (int i = 0; i < newValue.Length; i++)
                {
                    resultBuffer[resultPos++] = newValue[i];
                }

                // Move position after match
                currentPos = index + oldValue.Length;
            }

            return new string(resultBuffer, 0, resultLength);
        }

        /// <summary>
        /// Replaces all occurrences of a specified character with another specified character.
        /// </summary>
        public string Replace(char oldChar, char newChar)
        {
            if (IndexOf(oldChar) == -1)
                return this; // No replacement needed

            char* buffer = stackalloc char[Length];

            for (int i = 0; i < Length; i++)
            {
                buffer[i] = this[i] == oldChar ? newChar : this[i];
            }

            return new string(buffer, 0, Length);
        }

        /// <summary>
        /// Determines if the beginning of this string matches the specified string.
        /// </summary>
        public bool StartsWith(string value)
        {
            if (value == null)
                return false;

            if (value.Length > this.Length)
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                if (this[i] != value[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if the beginning of this string matches the specified character.
        /// </summary>
        public bool StartsWith(char value)
        {
            return Length > 0 && this[0] == value;
        }

        /// <summary>
        /// Determines if the string contains the specified string.
        /// </summary>
        public bool Contains(string value)
        {
            return IndexOf(value) >= 0;
        }

        /// <summary>
        /// Determines if the string contains the specified character.
        /// </summary>
        public bool Contains(char value)
        {
            return IndexOf(value) >= 0;
        }

        /// <summary>
        /// Returns a new string that right-aligns the characters by padding with spaces on the left.
        /// </summary>
        public string PadRight(int totalWidth)
        {
            return PadRight(totalWidth, ' ');
        }

        /// <summary>
        /// Returns a new string that right-aligns the characters by padding with the specified character on the left.
        /// </summary>
        public string PadRight(int totalWidth, char paddingChar)
        {
            if (totalWidth <= Length)
                return this;

            int paddingLength = totalWidth - Length;
            char* buffer = stackalloc char[totalWidth];

            // Copy original string
            fixed (char* src = &_firstChar)
            {
                for (int i = 0; i < Length; i++)
                {
                    buffer[i] = src[i];
                }
            }

            // Add padding characters
            for (int i = 0; i < paddingLength; i++)
            {
                buffer[Length + i] = paddingChar;
            }

            return new string(buffer, 0, totalWidth);
        }

        // Operator to concatenate a string and an object
        public static string operator +(string left, object right)
        {
            if (left == null)
                left = "";

            string rightStr = right == null ? "" : right.ToString();

            return Concat(left, rightStr);
        }

        // Operator to concatenate a string and a string
        public static string operator +(string left, string right)
        {
            return Concat(left, right);
        }

        // Operator to concatenate a string and a char
        public static string operator +(string left, char right)
        {
            if (left == null)
                left = "";

            int len = left.Length;
            char* buffer = stackalloc char[len + 1];

            // Copy left string
            fixed (char* src = &left._firstChar)
            {
                for (int i = 0; i < len; i++)
                {
                    buffer[i] = src[i];
                }
            }

            // Add right char
            buffer[len] = right;

            return new string(buffer, 0, len + 1);
        }

        /// <summary>
        /// Compares two strings for equality, ignoring case differences.
        /// </summary>
        public static bool EqualsIgnoreCase(string a, string b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a == null || b == null)
                return false;

            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].ToLower() != b[i].ToLower())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Joins an array of strings into a single string with the specified separator.
        /// </summary>
        public static string Join(string separator, string[] values)
        {
            if (values == null || values.Length == 0)
                return Empty;

            if (values.Length == 1)
                return values[0] ?? Empty;

            if (separator == null)
                separator = Empty;

            // Calculate total length
            int totalLength = 0;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != null)
                    totalLength += values[i].Length;
            }

            // Add length for separators
            totalLength += separator.Length * (values.Length - 1);

            // Create buffer
            char* buffer = stackalloc char[totalLength];
            int pos = 0;

            // Add first item
            if (values[0] != null)
            {
                fixed (char* src = &values[0]._firstChar)
                {
                    for (int i = 0; i < values[0].Length; i++)
                    {
                        buffer[pos++] = src[i];
                    }
                }
            }

            // Add remaining items with separators
            for (int i = 1; i < values.Length; i++)
            {
                // Add separator
                fixed (char* sep = &separator._firstChar)
                {
                    for (int j = 0; j < separator.Length; j++)
                    {
                        buffer[pos++] = sep[j];
                    }
                }

                // Add item
                if (values[i] != null)
                {
                    fixed (char* src = &values[i]._firstChar)
                    {
                        for (int j = 0; j < values[i].Length; j++)
                        {
                            buffer[pos++] = src[j];
                        }
                    }
                }
            }

            return new string(buffer, 0, totalLength);
        }

        /// <summary>
        /// Joins an array of objects into a single string with the specified separator.
        /// </summary>
        public static string Join(string separator, object[] values)
        {
            if (values == null || values.Length == 0)
                return Empty;

            // Convert objects to strings
            string[] strings = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                strings[i] = values[i]?.ToString() ?? "";
            }

            return Join(separator, strings);
        }
    }
}