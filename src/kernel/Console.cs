using Internal.Runtime.CompilerHelpers;
using Kernel;
using Kernel.Drivers.IO;
using System;
using System.Runtime.InteropServices;

namespace System
{
    /// <summary>
    /// Representa la consola estándar de entrada, salida y error.
    /// </summary>
    public static unsafe class Console
    {
        // Constantes para el buffer de video VGA
        private const int VIDEO_MEMORY = 0xB8000;
        private const int SCREEN_WIDTH = 80;
        private const int SCREEN_HEIGHT = 25;

        // Estructura para un carácter en el buffer de texto VGA
        private struct VGACharacter
        {
            public byte Character;
            public byte Attribute;
        }

        // Referencia al buffer de video
        private static VGACharacter* _buffer;

        // Posición actual del cursor
        private static int _cursorX;
        private static int _cursorY;

        // Colores actuales
        private static ConsoleColor _foregroundColor;
        private static ConsoleColor _backgroundColor;

        // Flag para indicar si la consola está inicializada
        private static bool _initialized;

        /// <summary>
        /// Inicializa la consola.
        /// </summary>
        static Console()
        {
            Initialize();
        }

        /// <summary>
        /// Inicializa el buffer de video y los colores predeterminados.
        /// </summary>
        private static void Initialize()
        {
            if (_initialized)
                return;

            // Inicializar buffer de video
            _buffer = (VGACharacter*)VIDEO_MEMORY;

            // Inicializar colores predeterminados
            _foregroundColor = ConsoleColor.White;
            _backgroundColor = ConsoleColor.Black;

            // Inicializar cursor
            _cursorX = 0;
            _cursorY = 0;

            // Marcar como inicializado
            _initialized = true;

            // Limpiar la pantalla al iniciar
            Clear();
        }

        /// <summary>
        /// Limpia la pantalla y restablece el cursor a la posición (0, 0).
        /// </summary>
        public static void Clear()
        {
            if (!_initialized)
                Initialize();

            VGACharacter empty;
            empty.Character = (byte)' ';
            empty.Attribute = (byte)(((byte)_backgroundColor << 4) | ((byte)_foregroundColor & 0x0F));

            for (int i = 0; i < SCREEN_WIDTH * SCREEN_HEIGHT; i++)
            {
                _buffer[i] = empty;
            }

            _cursorX = 0;
            _cursorY = 0;
            UpdateCursor();
        }

        /// <summary>
        /// Establece la posición del cursor.
        /// </summary>
        /// <param name="left">Posición horizontal del cursor.</param>
        /// <param name="top">Posición vertical del cursor.</param>
        public static void SetCursorPosition(int left, int top)
        {
            if (!_initialized)
                Initialize();

            _cursorX = Math.Clamp(left, 0, SCREEN_WIDTH - 1);
            _cursorY = Math.Clamp(top, 0, SCREEN_HEIGHT - 1);
            UpdateCursor();
        }

        /// <summary>
        /// Obtiene o establece la posición horizontal del cursor.
        /// </summary>
        public static int CursorLeft
        {
            get
            {
                if (!_initialized)
                    Initialize();
                return _cursorX;
            }
            set
            {
                SetCursorPosition(value, _cursorY);
            }
        }

        /// <summary>
        /// Obtiene o establece la posición vertical del cursor.
        /// </summary>
        public static int CursorTop
        {
            get
            {
                if (!_initialized)
                    Initialize();
                return _cursorY;
            }
            set
            {
                SetCursorPosition(_cursorX, value);
            }
        }

        /// <summary>
        /// Obtiene o establece el color de primer plano utilizado por la consola.
        /// </summary>
        public static ConsoleColor ForegroundColor
        {
            get
            {
                if (!_initialized)
                    Initialize();
                return _foregroundColor;
            }
            set
            {
                if (!_initialized)
                    Initialize();
                _foregroundColor = value;
            }
        }

        /// <summary>
        /// Obtiene o establece el color de fondo utilizado por la consola.
        /// </summary>
        public static ConsoleColor BackgroundColor
        {
            get
            {
                if (!_initialized)
                    Initialize();
                return _backgroundColor;
            }
            set
            {
                if (!_initialized)
                    Initialize();
                _backgroundColor = value;
            }
        }

        /// <summary>
        /// Escribe un carácter individual a la consola.
        /// </summary>
        /// <param name="c">Carácter a escribir.</param>
        public static void Write(char c)
        {
            if (!_initialized)
                Initialize();

            // Manejar caracteres especiales
            switch (c)
            {
                case '\n': // Nueva línea
                    _cursorX = 0;
                    _cursorY++;
                    break;

                case '\r': // Retorno de carro
                    _cursorX = 0;
                    break;

                case '\t': // Tabulador
                    const int TAB_SIZE = 4;
                    _cursorX = (_cursorX + TAB_SIZE) & ~(TAB_SIZE - 1);
                    break;

                case '\b': // Retroceso
                    if (_cursorX > 0)
                    {
                        _cursorX--;
                    }
                    else if (_cursorY > 0)
                    {
                        _cursorY--;
                        _cursorX = SCREEN_WIDTH - 1;
                    }

                    // Borrar el carácter en la posición actual
                    int index = _cursorY * SCREEN_WIDTH + _cursorX;
                    _buffer[index].Character = (byte)' ';
                    break;

                default: // Carácter normal
                    // Calcular índice en el buffer
                    int idx = _cursorY * SCREEN_WIDTH + _cursorX;

                    // Escribir carácter y atributo
                    _buffer[idx].Character = (byte)c;
                    _buffer[idx].Attribute = (byte)(((byte)_backgroundColor << 4) | ((byte)_foregroundColor & 0x0F));

                    // Avanzar cursor
                    _cursorX++;
                    break;
            }

            // Manejar desbordamiento horizontal
            if (_cursorX >= SCREEN_WIDTH)
            {
                _cursorX = 0;
                _cursorY++;
            }

            // Manejar desbordamiento vertical (scrolling)
            if (_cursorY >= SCREEN_HEIGHT)
            {
                ScrollUp();
                _cursorY = SCREEN_HEIGHT - 1;
            }

            // Actualizar posición física del cursor
            UpdateCursor();
        }

        /// <summary>
        /// Escribe una cadena a la consola.
        /// </summary>
        /// <param name="s">Cadena a escribir.</param>
        public static void Write(string s)
        {
            if (s == null)
                return;

            if (!_initialized)
                Initialize();

            for (int i = 0; i < s.Length; i++)
            {
                Write(s[i]);
            }
        }

        /// <summary>
        /// Escribe una línea a la consola.
        /// </summary>
        /// <param name="s">Cadena a escribir.</param>
        public static void WriteLine(string s)
        {
            Write(s);
            Write('\n');
        }

        /// <summary>
        /// Escribe una nueva línea a la consola.
        /// </summary>
        public static void WriteLine()
        {
            Write('\n');
        }

        /// <summary>
        /// Escribe la representación en cadena de un objeto a la consola.
        /// </summary>
        /// <param name="value">Objeto a escribir.</param>
        public static void Write(object value)
        {
            if (value == null)
            {
                Write("null");
                return;
            }

            Write(value.ToString());
        }

        /// <summary>
        /// Escribe la representación en cadena de un objeto seguido de una nueva línea.
        /// </summary>
        /// <param name="value">Objeto a escribir.</param>
        public static void WriteLine(object value)
        {
            Write(value);
            WriteLine();
        }

        /// <summary>
        /// Escribe una cadena con formato a la consola.
        /// </summary>
        /// <param name="format">Cadena de formato.</param>
        /// <param name="args">Argumentos para formatear.</param>
        public static void Write(string format, params object[] args)
        {
            if (format == null)
                ThrowHelpers.ArgumentNullException("format");

            if (args == null || args.Length == 0)
            {
                Write(format);
                return;
            }

            Write(String.Format(format, args));
        }

        /// <summary>
        /// Escribe una cadena con formato seguida de una nueva línea a la consola.
        /// </summary>
        /// <param name="format">Cadena de formato.</param>
        /// <param name="args">Argumentos para formatear.</param>
        public static void WriteLine(string format, params object[] args)
        {
            Write(format, args);
            WriteLine();
        }

        /// <summary>
        /// Actualiza la posición del cursor de hardware.
        /// </summary>
        private static void UpdateCursor()
        {
            // Calcular posición lineal
            ushort position = (ushort)(_cursorY * SCREEN_WIDTH + _cursorX);

            // Actualizar cursor de hardware a través de los puertos VGA
            // Puerto 0x3D4 = registro de selección
            // Puerto 0x3D5 = registro de datos
            IOPort.Out8(0x3D4, 0x0F);
            IOPort.Out8(0x3D5, (byte)(position & 0xFF));
            IOPort.Out8(0x3D4, 0x0E);
            IOPort.Out8(0x3D5, (byte)((position >> 8) & 0xFF));
        }

        /// <summary>
        /// Desplaza el contenido de la pantalla hacia arriba una línea.
        /// </summary>
        private static void ScrollUp()
        {
            // Mover todas las líneas una posición hacia arriba
            for (int y = 0; y < SCREEN_HEIGHT - 1; y++)
            {
                for (int x = 0; x < SCREEN_WIDTH; x++)
                {
                    int currentIdx = y * SCREEN_WIDTH + x;
                    int nextIdx = (y + 1) * SCREEN_WIDTH + x;
                    _buffer[currentIdx] = _buffer[nextIdx];
                }
            }

            // Limpiar la última línea
            int lastLineOffset = (SCREEN_HEIGHT - 1) * SCREEN_WIDTH;
            for (int x = 0; x < SCREEN_WIDTH; x++)
            {
                _buffer[lastLineOffset + x].Character = (byte)' ';
                _buffer[lastLineOffset + x].Attribute = (byte)(((byte)_backgroundColor << 4) | ((byte)_foregroundColor & 0x0F));
            }
        }

        /// <summary>
        /// Escribe múltiples caracteres a la consola.
        /// </summary>
        /// <param name="buffer">Array de caracteres a escribir.</param>
        /// <param name="index">Índice del primer carácter a escribir.</param>
        /// <param name="count">Número de caracteres a escribir.</param>
        public static void Write(char[] buffer, int index, int count)
        {
            if (buffer == null)
                ThrowHelpers.ArgumentNullException("buffer");

            if (index < 0)
                ThrowHelpers.ArgumentOutOfRangeException("index");

            if (count < 0)
                ThrowHelpers.ArgumentOutOfRangeException("count");

            if (index + count > buffer.Length)
                ThrowHelpers.ArgumentException("index + count exceeds buffer length");

            for (int i = 0; i < count; i++)
            {
                Write(buffer[index + i]);
            }
        }

        /// <summary>
        /// Lee una línea de caracteres desde la entrada estándar.
        /// </summary>
        /// <returns>La línea de caracteres leída.</returns>
        public static string ReadLine()
        {
            if (!_initialized)
                Initialize();

            // Buffer temporal para almacenar caracteres
            char[] buffer = new char[256];
            int index = 0;

            while (true)
            {
                // Lee un carácter de teclado (simplificado, en un sistema real usaríamos interrupciones)
                char c = ReadChar();

                // Si es Enter, terminamos
                if (c == '\r' || c == '\n')
                {
                    WriteLine();
                    break;
                }
                // Si es retroceso, borramos el último carácter
                else if (c == '\b')
                {
                    if (index > 0)
                    {
                        index--;
                        Write('\b');
                        Write(' ');
                        Write('\b');
                    }
                }
                // Ignorar caracteres que no se pueden imprimir
                else if (c >= 32 && c < 127)
                {
                    // Si hay espacio en el buffer, añadimos el carácter
                    if (index < buffer.Length - 1)
                    {
                        buffer[index++] = c;
                        Write(c);
                    }
                }
            }

            // Crear string a partir de los caracteres acumulados
            return new string(buffer, 0, index);
        }

        /// <summary>
        /// Lee un carácter de teclado.
        /// Utiliza el driver de teclado del sistema para obtener entrada.
        /// </summary>
        /// <returns>Carácter leído.</returns>
        private static char ReadChar()
        {
            // Reemplazar las llamadas a BIOS con el driver de teclado
            while (true)
            {
                // Verificar si hay teclas disponibles usando el driver de teclado
                if (Kernel.Drivers.Input.Keyboard.IsKeyAvailable())
                {
                    // Leer el caracter usando el driver de teclado
                    char c = Kernel.Drivers.Input.Keyboard.ReadChar();

                    // Si es un carácter válido, devolverlo
                    if (c != '\0')
                    {
                        return c;
                    }
                }

                // Pequeño retraso para no saturar la CPU
                for (int i = 0; i < 1000; i++) { Native.Nop(); }
            }
        }

        /// <summary>
        /// Obtiene el ancho de la pantalla de la consola.
        /// </summary>
        public static int WindowWidth => SCREEN_WIDTH;

        /// <summary>
        /// Obtiene la altura de la pantalla de la consola.
        /// </summary>
        public static int WindowHeight => SCREEN_HEIGHT;

        /// <summary>
        /// Método auxiliar para limitar un valor dentro de un rango.
        /// </summary>
        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }

    /// <summary>
    /// Enumeración de colores soportados por la consola.
    /// </summary>
    public enum ConsoleColor
    {
        Black = 0,
        Blue = 1,
        Green = 2,
        Cyan = 3,
        Red = 4,
        Magenta = 5,
        Brown = 6,
        LightGray = 7,
        DarkGray = 8,
        LightBlue = 9,
        LightGreen = 10,
        LightCyan = 11,
        LightRed = 12,
        LightMagenta = 13,
        Yellow = 14,
        White = 15
    }
}