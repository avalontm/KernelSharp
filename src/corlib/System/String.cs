using Corlib.Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// String.cs - Implementation by AvalonTM
// Created: March 28, 2025
// KernelSharp CoreLib implementation for strings

namespace System
{
    public sealed unsafe class String
    {
        internal ref char GetRawStringData() => ref _firstChar;

        public static string Empty
        {
            get
            {
                return new string(new char[0]);
            }
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
                    ThrowHelpers.ThrowIndexOutOfRangeException();

                fixed (char* p = &_firstChar)
                {
                    return p[index];
                }
            }

            set
            {
                if (index < 0 || index >= _length)
                    ThrowHelpers.ThrowIndexOutOfRangeException();

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
                ThrowHelpers.ThrowArgumentNullException("ptr");

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

        // Constructor from char array with index and length
        public String(char[] buf, int index, int length)
        {
            // Validate arguments
            if (buf == null)
                ThrowHelpers.ThrowArgumentNullException("buf");

            if (index < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("index");

            if (length < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("length");

            if (index + length > buf.Length)
                ThrowHelpers.ThrowArgumentOutOfRangeException("length");

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
                ThrowHelpers.ThrowArgumentNullException("ptr");

            if (index < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("index");

            if (length < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("length");

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
        /// Constructor que crea una nueva cadena a partir de otra cadena existente.
        /// </summary>
        public String(string value)
        {
            string newString = Ctor(value);
            // Copiar los datos de newString a esta instancia
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
        /// Método estático para crear una cadena a partir de otra cadena
        /// Este método es llamado por el compilador cuando se usa new string(string)
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
                    // Crear nueva instancia usando el constructor que acepta puntero, índice y longitud
                    return new string(srcPtr, 0, value.Length);
                }
            }
        }

        /// <summary>
        /// Crea una nueva instancia de String a partir de un puntero a caracteres.
        /// </summary>
        /// <param name="ptr">Puntero a la secuencia de caracteres de origen</param>
        /// <param name="index">Índice de inicio en la secuencia</param>
        /// <param name="length">Longitud de la cadena a crear</param>
        /// <returns>Una nueva instancia de String</returns>
        internal static unsafe string Ctor(char* ptr, int index, int length)
        {
            // Validar los argumentos
            if (ptr == null)
                ThrowHelpers.ThrowArgumentNullException("ptr");

            if (index < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("index");

            if (length < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("length");

            // Caso especial para cadenas vacías
            if (length == 0)
                return Empty;

            // Obtener el EEType para String
            EETypePtr et = EETypePtr.EETypePtrOf<string>();

            // Calcular el puntero de inicio
            char* start = ptr + index;

            // Crear una nueva instancia de String utilizando la infraestructura nativa
            object stringObj = RuntimeImports.RhpNewArray(et._value, length);

            // Convertir el objeto a string
            string s = Unsafe.As<object, string>(ref stringObj);

            // Establecer la longitud correcta
            s.Length = length;

            // Copiar los caracteres desde el origen
            fixed (char* c = &s._firstChar)
            {
                // Copiar manualmente carácter por carácter para mayor control
                for (int i = 0; i < length; i++)
                {
                    c[i] = start[i];
                }

                // Añadir el terminador nulo después de la longitud especificada
                // Esto es útil para interoperabilidad con código nativo
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
            return a.Equals(b);
        }

        public static bool operator !=(string a, string b)
        {
            return !a.Equals(b);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public static string Concat(string a, string b)
        {
            int Length = a.Length + b.Length;
            char* ptr = stackalloc char[Length];
            int currentIndex = 0;
            for (int i = 0; i < a.Length; i++)
            {
                ptr[currentIndex] = a[i];
                currentIndex++;
            }
            for (int i = 0; i < b.Length; i++)
            {
                ptr[currentIndex] = b[i];
                currentIndex++;
            }
            return new string(ptr, 0, Length);
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

        public int IndexOf(string subcadena)
        {
            int _indice = -1;

            for (int i = 0; i <= this.Length - subcadena.Length; i++)
            {
                bool encontrado = true;

                for (int j = 0; j < subcadena.Length; j++)
                {
                    if (this[i + j] != subcadena[j])
                    {
                        encontrado = false;
                        break;
                    }
                }

                if (encontrado)
                {
                    _indice = i;
                    break;
                }
            }

            return _indice;
        }

        public static string charToString(char* charArray)
        {
            string str = "";
            for (int i = 0; charArray[i] != '\0'; i++)
            {
                str += charArray[i];
            }
            return str;
        }


        public static string Copy(string a)
        {
            int Length = a.Length;
            char* ptr = stackalloc char[Length];
            int currentIndex = 0;

            for (int i = 0; i < a.Length; i++)
            {
                ptr[currentIndex] = a[i];
                currentIndex++;
            }
            return new string(ptr, 0, Length);
        }

        public int IndexOf(string subcadena, int indiceInicial)
        {
            int _indice = -1;

            while (indiceInicial < this.Length)
            {
                int i = indiceInicial;
                int j = 0;

                while (i < this.Length && j < subcadena.Length && this[i] == subcadena[j])
                {
                    i++;
                    j++;
                }

                if (j == subcadena.Length)
                {
                    _indice = indiceInicial;
                    break;
                }

                indiceInicial++;
            }

            return _indice;
        }


        public static string Concat(string a, string b, string c)
        {
            string p1 = a + b;
            string p2 = p1 + c;
            p1.Dispose();
            return p2;
        }

        public static string Concat(string a, string b, string c, string d)
        {
            string p1 = a + b;
            string p2 = p1 + c;
            string p3 = p2 + d;
            p1.Dispose();
            p2.Dispose();
            return p3;
        }

        public static string Concat(params string[] vs)
        {
            string s = "";
            for (int i = 0; i < vs.Length; i++)
            {
                string tmp = s + vs[i];
                s.Dispose();
                s = tmp;
            }
            vs.Dispose();
            return s;
        }

        public static string Format(string format, params object[] args)
        {
            if (format == null)
                ThrowHelpers.ThrowArgumentNullException("format");

            // Si no hay argumentos, devolver el formato tal cual
            if (args == null || args.Length == 0)
                return format;

            lock (format)
            {
                string res = Empty;
                for (int i = 0; i < format.Length; i++)
                {
                    string chr;
                    if ((i + 2) < format.Length && format[i] == '{' && format[i + 2] == '}')
                    {
                        int argIndex = format[i + 1] - 0x30; // Convertir '0'-'9' a 0-9

                        // Verificar que el índice sea válido
                        if (argIndex >= 0 && argIndex < args.Length)
                        {
                            // Manejar argumento nulo con seguridad
                            object arg = args[argIndex];
                            chr = arg != null ? arg.ToString() : "null";
                        }
                        else
                        {
                            // Mantener el marcador de posición si el índice está fuera de rango
                            chr = $"{{{format[i + 1]}}}";
                        }

                        i += 2;
                    }
                    else
                    {
                        chr = format[i].ToString();
                    }

                    // Concatenar con seguridad
                    string str = res + chr;

                    // No llamar a Dispose en cadenas
                    res = str;
                }

                return res;
            }
        }

        public string Remove(int startIndex)
        {
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
            if ((Length == 0) && (startIndex == 0))
            {
                return new string(new char[0]);
            }
            fixed (char* ptr = this)
            {
                return new string(ptr, startIndex, Length - startIndex);
            }
        }

        public unsafe string Substring(int startIndex, int endIndex)
        {
            if ((Length == 0) && (startIndex == 0))
            {
                return new string(new char[0]);
            }
            fixed (char* ptr = this)
            {
                return new string(ptr, startIndex, endIndex - startIndex);
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
            fixed (char* pthis = this)
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
            fixed (char* pthis = this)
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

        public string PadLeft(int num, char chr)
        {
            string result = "";

            for (int i = 0; i < (num - this.Length); i++)
            {
                result += chr;
            }

            return result + this;
        }

        public string PadLeft(string str, int num)
        {
            string result = "";

            for (int i = 0; i < num; i++)
            {
                result += this[i];
            }

            result += str;

            return result;
        }

        public string Trim()
        {
            string result = "";

            for (int i = 0; i < this.Length; i++)
            {
                if (!char.IsWhiteSpace(this[i]))
                {
                    result += this[i];
                }
            }

            return result;
        }

        public string Trim(char c)
        {
            string result = "";

            for (int i = 0; i < this.Length; i++)
            {
                if (!char.IsWhiteSpace(this[i]) && this[i] != c)
                {
                    result += this[i];
                }
            }

            return result;

        }

        public string Replace(string a, string b)
        {
            string result = "";

            for (int i = 0; i < this.Length; i++)
            {
                if (this[i] == a[0])
                {
                    this[i] = b[0];
                }
                if (!char.IsWhiteSpace(this[i]))
                {
                    result += this[i];
                }
            }

            return result;
        }

        /// <summary>
        /// Reemplaza todas las apariciones de un carácter especificado por otro carácter.
        /// </summary>
        /// <param name="oldChar">El carácter que se va a reemplazar.</param>
        /// <param name="newChar">El carácter que reemplazará todas las apariciones de oldChar.</param>
        /// <returns>Una nueva cadena con todos los caracteres oldChar reemplazados por newChar.</returns>
        public string Replace(char oldChar, char newChar)
        {
            string result = "";
            for (int i = 0; i < this.Length; i++)
            {
                if (this[i] == oldChar)
                {
                    result += newChar;
                }
                else
                {
                    result += this[i];
                }
            }
            return result;
        }

        /// <summary>
        /// Determina si el comienzo de esta cadena coincide con la cadena especificada.
        /// </summary>
        /// <param name="value">La cadena a buscar al principio de esta instancia.</param>
        /// <returns>true si value coincide con el comienzo de esta cadena; de lo contrario, false.</returns>
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
        /// Determina si la cadena especificada aparece dentro de esta cadena.
        /// </summary>
        /// <param name="value">La cadena a buscar.</param>
        /// <returns>true si value ocurre dentro de esta cadena; de lo contrario, false.</returns>
        public bool Contains(string value)
        {
            if (value == null || value.Length == 0)
                return false;

            if (value.Length > this.Length)
                return false;

            for (int i = 0; i <= this.Length - value.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < value.Length; j++)
                {
                    if (this[i + j] != value[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determina si el carácter especificado aparece dentro de esta cadena.
        /// </summary>
        /// <param name="value">El carácter a buscar.</param>
        /// <returns>true si value ocurre dentro de esta cadena; de lo contrario, false.</returns>
        public bool Contains(char value)
        {
            for (int i = 0; i < this.Length; i++)
            {
                if (this[i] == value)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Devuelve una nueva cadena que alinea a la derecha los caracteres de esta cadena, rellenando con espacios a la derecha hasta alcanzar la longitud total especificada.
        /// </summary>
        /// <param name="totalWidth">El número de caracteres en la cadena resultante.</param>
        /// <returns>Una nueva cadena de longitud totalWidth que consiste en esta cadena alineada a la derecha y rellenada con espacios en blanco.</returns>
        public string PadRight(int totalWidth)
        {
            return PadRight(totalWidth, ' ');
        }

        /// <summary>
        /// Devuelve una nueva cadena que alinea a la derecha los caracteres de esta cadena, rellenando con el carácter especificado a la derecha hasta alcanzar la longitud total especificada.
        /// </summary>
        /// <param name="totalWidth">El número de caracteres en la cadena resultante.</param>
        /// <param name="paddingChar">El carácter a usar para el relleno a la derecha.</param>
        /// <returns>Una nueva cadena de longitud totalWidth que consiste en esta cadena alineada a la derecha y rellenada con el carácter especificado.</returns>
        public string PadRight(int totalWidth, char paddingChar)
        {
            if (totalWidth <= this.Length)
                return this;

            string result = this;
            for (int i = this.Length; i < totalWidth; i++)
            {
                result += paddingChar;
            }
            return result;
        }
    }
}
