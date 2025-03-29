using Internal.Runtime.CompilerHelpers;

namespace System.Text
{
    public class StringBuilder
    {
        private const int DefaultCapacity = 16;
        private const int MaximumCapacity = 2147483647;

        private char[] _buffer;
        private int _length;
        private int _capacity;

        // Constructores
        public StringBuilder() : this(DefaultCapacity) { }

        public StringBuilder(int capacity)
        {
            if (capacity < 0)
                ThrowHelpers.ArgumentOutOfRangeException(nameof(capacity));

            _capacity = capacity > 0 ? capacity : DefaultCapacity;
            _buffer = new char[_capacity];
            _length = 0;
        }

        public StringBuilder(string value) : this(value, DefaultCapacity) { }

        public StringBuilder(string value, int capacity)
        {
            if (value == null)
                value = "";

            if (capacity < 0)
                ThrowHelpers.ArgumentOutOfRangeException(nameof(capacity));

            int valueLength = value.Length;
            _capacity = Math.Max(capacity, valueLength);
            _capacity = _capacity > 0 ? _capacity : DefaultCapacity;
            _buffer = new char[_capacity];
            _length = valueLength;

            // Copiar la cadena inicial
            if (valueLength > 0)
            {
                for (int i = 0; i < valueLength; i++)
                {
                    _buffer[i] = value[i];
                }
            }
        }

        // Propiedades
        public int Length
        {
            get { return _length; }
            set
            {
                if (value < 0)
                    ThrowHelpers.ArgumentOutOfRangeException(nameof(value));

                if (value > _capacity)
                {
                    EnsureCapacity(value);
                }

                if (value < _length)
                {
                    // Truncar el contenido
                    for (int i = value; i < _length; i++)
                    {
                        _buffer[i] = '\0';
                    }
                }

                _length = value;
            }
        }

        public int Capacity
        {
            get { return _capacity; }
            set
            {
                if (value < _length)
                    ThrowHelpers.ArgumentOutOfRangeException(nameof(value));

                if (value != _capacity)
                {
                    if (value > 0)
                    {
                        char[] newBuffer = new char[value];
                        if (_length > 0)
                        {
                            for (int i = 0; i < _length; i++)
                            {
                                newBuffer[i] = _buffer[i];
                            }
                        }
                        _buffer = newBuffer;
                    }
                    else
                    {
                        _buffer = new char[DefaultCapacity];
                    }
                    _capacity = value;
                }
            }
        }

        // Métodos principales
        public StringBuilder Append(char value)
        {
            EnsureCapacity(_length + 1);
            _buffer[_length++] = value;
            return this;
        }

        public StringBuilder Append(string value)
        {
            if (value == null || value.Length == 0)
                return this;

            EnsureCapacity(_length + value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                _buffer[_length + i] = value[i];
            }

            _length += value.Length;
            return this;
        }

        public StringBuilder Append(object value)
        {
            return value == null ? this : Append(value.ToString());
        }

        public StringBuilder AppendLine()
        {
            return Append(Environment.NewLine);
        }

        public StringBuilder AppendLine(string value)
        {
            Append(value);
            return AppendLine();
        }

        public StringBuilder AppendFormat(string format, object arg0)
        {
            if (format == null)
                return this;
            return Append(String.Format(format, arg0));
        }

        public StringBuilder AppendFormat(string format, object arg0, object arg1)
        {
            if (format == null)
                return this;
            return Append(String.Format(format, arg0, arg1));
        }

        public StringBuilder AppendFormat(string format, object arg0, object arg1, object arg2)
        {
            if (format == null)
                return this;
            return Append(String.Format(format, arg0, arg1, arg2));
        }

        public StringBuilder AppendFormat(string format, params object[] args)
        {
            if (format == null)
                return this;
            if (args == null || args.Length == 0)
                return Append(format);
            return Append(String.Format(format, args));
        }

        public StringBuilder Clear()
        {
            _length = 0;
            return this;
        }

        public void EnsureCapacity(int capacity)
        {
            if (capacity > _capacity)
            {
                int newCapacity = Math.Max(_capacity * 2, capacity);
                // Evitar desbordamiento
                if (newCapacity < 0 || newCapacity > MaximumCapacity)
                    newCapacity = MaximumCapacity;

                Capacity = newCapacity;
            }
        }

        public StringBuilder Insert(int index, char value)
        {
            if (index < 0 || index > _length)
                ThrowHelpers.ArgumentOutOfRangeException(nameof(index));

            EnsureCapacity(_length + 1);

            // Desplazar los caracteres existentes
            for (int i = _length; i > index; i--)
            {
                _buffer[i] = _buffer[i - 1];
            }

            _buffer[index] = value;
            _length++;

            return this;
        }

        public StringBuilder Insert(int index, string value)
        {
            if (index < 0 || index > _length)
                ThrowHelpers.ArgumentOutOfRangeException(nameof(index));

            if (value == null || value.Length == 0)
                return this;

            EnsureCapacity(_length + value.Length);

            // Desplazar los caracteres existentes
            for (int i = _length + value.Length - 1; i >= index + value.Length; i--)
            {
                _buffer[i] = _buffer[i - value.Length];
            }

            // Insertar la nueva cadena
            for (int i = 0; i < value.Length; i++)
            {
                _buffer[index + i] = value[i];
            }

            _length += value.Length;

            return this;
        }

        public StringBuilder Insert(int index, object value)
        {
            return value == null ? this : Insert(index, value.ToString());
        }

        public StringBuilder Remove(int startIndex, int length)
        {
            // Verificaciones de rango corregidas
            if (startIndex < 0)
                ThrowHelpers.ArgumentOutOfRangeException("startIndex");

            if (length < 0)
                ThrowHelpers.ArgumentOutOfRangeException("length");

            // Verificar que startIndex esté dentro de los límites
            if (startIndex >= _length)
                ThrowHelpers.ArgumentOutOfRangeException("startIndex");

            // Si length es 0, no hacer nada
            if (length == 0)
                return this;

            // Ajustar length si se extiende más allá del final
            if (startIndex + length > _length)
                length = _length - startIndex;

            // Calcular cuántos caracteres necesitan moverse
            int charsToMove = _length - (startIndex + length);

            // Asegurarse de que charsToMove sea no negativo
            if (charsToMove > 0)
            {
                // Mover caracteres hacia atrás
                for (int i = 0; i < charsToMove; i++)
                {
                    _buffer[startIndex + i] = _buffer[startIndex + length + i];
                }
            }

            // Actualizar la longitud
            _length -= length;

            // Asegurarse de que la longitud nunca sea negativa
            if (_length < 0)
                _length = 0;

            // Limpiar los caracteres restantes
            for (int i = _length; i < _length + length && i < _capacity; i++)
            {
                _buffer[i] = '\0';
            }

            return this;
        }

        public StringBuilder Replace(char oldChar, char newChar)
        {
            for (int i = 0; i < _length; i++)
            {
                if (_buffer[i] == oldChar)
                {
                    _buffer[i] = newChar;
                }
            }

            return this;
        }

        public StringBuilder Replace(string oldValue, string newValue)
        {
            if (oldValue == null || oldValue.Length == 0)
                ThrowHelpers.ArgumentException("El valor a reemplazar no puede ser nulo o vacío.");

            if (newValue == null)
                newValue = "";

            if (_length == 0 || oldValue.Length > _length)
                return this;

            int currentLength = _length; // Guardar longitud actual para evitar problemas con modificaciones
            int index = 0;

            while (index <= currentLength - oldValue.Length)
            {
                bool found = true;
                for (int i = 0; i < oldValue.Length; i++)
                {
                    if (index + i >= _length || _buffer[index + i] != oldValue[i])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    // Verificar longitud antes y después de cada operación
                    int beforeRemoveLength = _length;
                    Remove(index, oldValue.Length);

                    // Asegurarse de que la longitud cambió correctamente
                    if (_length != beforeRemoveLength - oldValue.Length)
                    {
                        // Algo salió mal, ajustar _length manualmente
                        _length = beforeRemoveLength - oldValue.Length;
                        if (_length < 0) _length = 0;
                    }

                    if (newValue.Length > 0)
                    {
                        int beforeInsertLength = _length;
                        Insert(index, newValue);

                        // Verificar que la longitud cambió correctamente
                        if (_length != beforeInsertLength + newValue.Length)
                        {
                            // Algo salió mal, ajustar _length manualmente
                            _length = beforeInsertLength + newValue.Length;
                        }
                    }

                    index += newValue.Length;
                    // Actualizar la longitud actual para el bucle
                    currentLength = _length;
                }
                else
                {
                    index++;
                }

                // Verificación de seguridad para evitar bucle infinito
                if (index < 0) index = 0;
            }

            // Verificación final para asegurar que _length no es negativo
            if (_length < 0) _length = 0;

            return this;
        }

        public override string ToString()
        {
            if (_length <= 0)
                return "";

            char[] result = new char[_length];
            for (int i = 0; i < _length; i++)
            {
                result[i] = _buffer[i];
            }

            return new string(result);
        }

        public string ToString(int startIndex, int length)
        {
            if (startIndex < 0 || length < 0 || startIndex + length > _length)
                ThrowHelpers.ArgumentOutOfRangeException();

            char[] result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = _buffer[startIndex + i];
            }

            return new string(result);
        }
    }
}