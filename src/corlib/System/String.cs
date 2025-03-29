using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

            // Obtener longitud del array
            int length = value.Length;

            // Establecer longitud en la instancia
            this._length = length;

            // Si length es 0, usar string vacío
            if (length == 0)
            {
                this._firstChar = '\0';
                return;
            }

            // Copiar caracteres del array a la instancia
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
                ThrowHelpers.ThrowArgumentNullException("[Constructor String] ptr");

            if (index < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("[Constructor String] index");

            if (length < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("[Constructor String] length: " + length.ToString());

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
                return null;

            if (args == null || args.Length == 0)
                return format;

            // Estimar el tamaño máximo necesario
            int maxLength = format.Length * 2; // Estimación conservadora
            char[] result = new char[maxLength];
            int resultIndex = 0;

            for (int i = 0; i < format.Length; i++)
            {
                // Buscar el inicio de un marcador de formato
                if (i + 1 < format.Length && format[i] == '{')
                {
                    // Comprobar si es un escape {{
                    if (i + 1 < format.Length && format[i + 1] == '{')
                    {
                        result[resultIndex++] = '{';
                        i++; // Saltar el segundo {
                        continue;
                    }

                    // Buscar el cierre del marcador
                    int indexStart = i + 1;
                    int indexEnd = indexStart;

                    while (indexEnd < format.Length && format[indexEnd] != '}')
                    {
                        indexEnd++;
                    }

                    // Si encontramos el cierre y hay espacio para al menos un dígito
                    if (indexEnd < format.Length && indexEnd > indexStart)
                    {
                        bool isValidIndex = true;
                        int argIndex = 0;

                        // Parsear el índice
                        for (int j = indexStart; j < indexEnd; j++)
                        {
                            char digit = format[j];
                            if (digit >= '0' && digit <= '9')
                            {
                                argIndex = argIndex * 10 + (digit - '0');
                            }
                            else
                            {
                                isValidIndex = false;
                                break;
                            }
                        }

                        // Si el índice es válido y está dentro del rango
                        if (isValidIndex && argIndex >= 0 && argIndex < args.Length)
                        {
                            object arg = args[argIndex];
                            
                            string argStr;

                            // Manejar diferentes tipos de argumentos
                            if (arg == null)
                            {
                                argStr = "";
                            }
                            else
                            {
                                // Manejar tipos específicos para asegurar representación correcta
                                if (arg is int)
                                {
                                    // Manejar enteros explícitamente
                                    int intValue = (int)arg;
                                    argStr = IntToString(intValue);
                                }
                                else if (arg is bool)
                                {
                                    // Manejar booleanos explícitamente
                                    bool boolValue = (bool)arg;
                                    argStr = boolValue ? "True" : "False";
                                }
                                else if (arg is char)
                                {
                                    // Manejar caracteres explícitamente
                                    char charValue = (char)arg;
                                    // Crear un array con un solo carácter
                                    char[] charArray = new char[1];
                                    charArray[0] = charValue;
                                    argStr = new string(charArray);
                                }
                                else if (arg is string)
                                {
                                    // Si ya es un string, usarlo directamente
                                    argStr = (string)arg;
                                }
                                else
                                {
                                    // Para otros tipos, llamar a ToString()
                                    argStr = arg.ToString();

                                    // Si ToString devuelve null, usar una cadena vacía
                                    if (argStr == null)
                                    {
                                        argStr = "";
                                    }
                                }
                            }

                            // Asegurarse de que hay suficiente espacio
                            if (resultIndex + argStr.Length > result.Length)
                            {
                                // Ampliar el buffer
                                int newSize = result.Length * 2;
                                while (resultIndex + argStr.Length > newSize)
                                {
                                    newSize *= 2;
                                }

                                char[] newResult = new char[newSize];
                                for (int k = 0; k < resultIndex; k++)
                                {
                                    newResult[k] = result[k];
                                }
                                result = newResult;
                            }

                            // Copiar el string del argumento
                            for (int k = 0; k < argStr.Length; k++)
                            {
                                result[resultIndex++] = argStr[k];
                            }

                            // Avanzar después del marcador de cierre
                            i = indexEnd;
                            continue;
                        }
                    }
                }
                else if (i + 1 < format.Length && format[i] == '}' && format[i + 1] == '}')
                {
                    // Escape para }
                    result[resultIndex++] = '}';
                    i++; // Saltar el segundo }
                    continue;
                }

                // Si llegamos aquí, es un carácter normal o un marcador inválido
                // Asegurarse de que hay suficiente espacio
                if (resultIndex >= result.Length)
                {
                    // Ampliar el buffer
                    char[] newResult = new char[result.Length * 2];
                    for (int k = 0; k < resultIndex; k++)
                    {
                        newResult[k] = result[k];
                    }
                    result = newResult;
                }

                result[resultIndex++] = format[i];
            }

            // Crear el string final con el tamaño exacto
            char[] finalResult = new char[resultIndex];
            for (int i = 0; i < resultIndex; i++)
            {
                finalResult[i] = result[i];
            }

            return new string(finalResult);
        }

        // Método auxiliar para convertir int a string manualmente
        private static string IntToString(int value)
        {
            // Manejar el caso especial de 0
            if (value == 0)
                return "0";

            // Manejar el caso especial de Int32.MinValue
            if (value == int.MinValue)
                return "-2147483648";

            bool isNegative = value < 0;
            if (isNegative)
                value = -value;

            // Determinar la longitud del resultado
            int length = 0;
            int temp = value;
            while (temp > 0)
            {
                temp /= 10;
                length++;
            }

            if (isNegative)
                length++; // Para el signo -

            // Crear el array de caracteres para el resultado
            char[] chars = new char[length];

            // Llenar el array desde el final
            int index = length - 1;
            while (value > 0)
            {
                chars[index--] = (char)('0' + (value % 10));
                value /= 10;
            }

            // Añadir el signo negativo si es necesario
            if (isNegative)
                chars[0] = '-';

            return new string(chars);
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
