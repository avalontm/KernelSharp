using Kernel.Diagnostics;
using Kernel.Drivers.IO;
using Kernel.Hardware;
using System;

namespace Kernel.Drivers.Input
{
    /// <summary>
    /// Representa un evento de teclado
    /// </summary>
    public struct KeyEvent
    {
        public byte ScanCode;     // Código de escaneo original
        public KeyCode Key;       // Tecla interpretada
        public bool Pressed;      // True si la tecla fue presionada, False si fue liberada
        public bool Shift;        // Estado de la tecla Shift
        public bool Ctrl;         // Estado de la tecla Control
        public bool Alt;          // Estado de la tecla Alt
        public char Character;    // Carácter ASCII/Unicode (si es aplicable)
    }

    /// <summary>
    /// Códigos de teclas
    /// </summary>
    public enum KeyCode : byte
    {
        Unknown = 0,

        // Teclas alfanuméricas
        A = 4, B, C, D, E, F, G, H, I, J, K, L, M,
        N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

        // Números
        D1 = 30, D2, D3, D4, D5, D6, D7, D8, D9, D0,

        // Otros caracteres
        Space = 44,
        Minus = 45,       // '-'
        Equals = 46,      // '='
        LeftBracket = 47, // '['
        RightBracket = 48,// ']'
        Backslash = 49,   // '\'
        Semicolon = 51,   // ';'
        Quote = 52,       // '\''
        Grave = 53,       // '`'
        Comma = 54,       // ','
        Period = 55,      // '.'
        Slash = 56,       // '/'

        // Teclas de control
        Enter = 40,
        Escape = 41,
        Backspace = 42,
        Tab = 43,
        CapsLock = 57,

        // Teclas de modificación
        LeftShift = 225,
        RightShift = 229,
        LeftCtrl = 224,
        RightCtrl = 228,
        LeftAlt = 226,
        RightAlt = 230,

        // Teclas de navegación
        Insert = 73,
        Home = 74,
        PageUp = 75,
        Delete = 76,
        End = 77,
        PageDown = 78,

        // Teclas de cursor
        Right = 79,
        Left = 80,
        Down = 81,
        Up = 82,

        // Teclado numérico
        NumLock = 83,
        KpDivide = 84,    // '/'
        KpMultiply = 85,  // '*'
        KpMinus = 86,     // '-'
        KpPlus = 87,      // '+'
        KpEnter = 88,
        Kp1 = 89, Kp2, Kp3, Kp4, Kp5, Kp6, Kp7, Kp8, Kp9, Kp0,
        KpDecimal = 99,   // '.'

        // Teclas de función
        F1 = 58, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12
    }

    /// <summary>
    /// Controlador para el teclado PS/2
    /// </summary>
    public unsafe class KeyboardDriver : BaseDriver
    {
        // Puertos de E/S del controlador de teclado
        private const ushort DATA_PORT = 0x60;
        private const ushort STATUS_PORT = 0x64;
        private const ushort COMMAND_PORT = 0x64;

        // Comandos del controlador
        private const byte CMD_READ_CONFIG = 0x20;
        private const byte CMD_WRITE_CONFIG = 0x60;
        private const byte CMD_DISABLE_SECOND_PORT = 0xA7;
        private const byte CMD_ENABLE_SECOND_PORT = 0xA8;
        private const byte CMD_TEST_SECOND_PORT = 0xA9;
        private const byte CMD_TEST_CONTROLLER = 0xAA;
        private const byte CMD_TEST_FIRST_PORT = 0xAB;
        private const byte CMD_DISABLE_FIRST_PORT = 0xAD;
        private const byte CMD_ENABLE_FIRST_PORT = 0xAE;

        // Comandos del dispositivo
        private const byte DEV_RESET = 0xFF;
        private const byte DEV_ENABLE_SCANNING = 0xF4;
        private const byte DEV_DISABLE_SCANNING = 0xF5;
        private const byte DEV_SET_LEDS = 0xED;

        // Estados de teclas modificadoras
        private bool _leftShift;
        private bool _rightShift;
        private bool _leftCtrl;
        private bool _rightCtrl;
        private bool _leftAlt;
        private bool _rightAlt;
        private bool _capsLock;
        private bool _numLock;
        private bool _scrollLock;

        // Buffer circular para eventos de teclado
        private const int BUFFER_SIZE = 128;
        private KeyEvent[] _eventBuffer = new KeyEvent[BUFFER_SIZE];
        private int _bufferHead = 0;
        private int _bufferTail = 0;

        // Mapa de escaneo para convertir códigos de escaneo a teclas
        private KeyCode[] _scanCodeMap;
        private char[] _characterMap;
        private char[] _shiftCharacterMap;

        /// <summary>
        /// Constructor del controlador de teclado
        /// </summary>
        public KeyboardDriver() : base("keyboard", "PS/2 Keyboard Controller")
        {
            // Inicializar mapas de escaneo
            InitializeScanCodeMap();
            InitializeCharacterMap();
        }

        /// <summary>
        /// Inicializa el controlador de teclado
        /// </summary>
        protected override bool OnInitialize()
        {
            SerialDebug.Info("Initializing keyboard controller with detailed diagnostics...");

            // Diagnóstico previo - lee el estado actual
            byte initialStatus = IOPort.In8(STATUS_PORT);
            SerialDebug.Info($"Initial keyboard controller status: 0x{((ulong)initialStatus).ToStringHex()}");

            // Deshabilitar dispositivos
            SerialDebug.Info("Disabling PS/2 ports...");
            SendCommand(CMD_DISABLE_FIRST_PORT);
            SendCommand(CMD_DISABLE_SECOND_PORT);

            // Vaciar buffer
            SerialDebug.Info("Flushing data buffer...");
            while ((IOPort.In8(STATUS_PORT) & 0x01) != 0)
            {
                byte data = IOPort.In8(DATA_PORT);
                SerialDebug.Info($"Flushed data byte: 0x{((ulong)data).ToStringHex()}");
            }

            // Auto-test del controlador
            SerialDebug.Info("Testing controller...");
            SendCommand(CMD_TEST_CONTROLLER);
            byte testResult = ReadData();
            if (testResult != 0x55)
            {
                SerialDebug.Error($"Controller self-test failed: 0x{((ulong)testResult).ToStringHex()}");
                return false;
            }
            SerialDebug.Info("Controller self-test passed");

            // Leer y modificar configuración
            SerialDebug.Info("Reading controller configuration...");
            SendCommand(CMD_READ_CONFIG);
            byte config = ReadData();
            SerialDebug.Info($"Current configuration: 0x{((ulong)config).ToStringHex()}");

            // Modificar para habilitar IRQ del teclado y deshabilitar traducción de scan codes
            config |= 0x01;  // Habilitar IRQ1
            config &= unchecked((byte)~0x40); // Deshabilitar traducción de scan codes (importante!)

            SerialDebug.Info($"New configuration: 0x{((ulong)config).ToStringHex()}");
            SendCommand(CMD_WRITE_CONFIG);
            SendData(config);

            // Prueba del primer puerto
            SerialDebug.Info("Testing first PS/2 port...");
            SendCommand(CMD_TEST_FIRST_PORT);
            byte portTest = ReadData();
            if (portTest != 0x00)
            {
                SerialDebug.Error($"First port test failed: 0x{((ulong)portTest).ToStringHex()}");
                return false;
            }
            SerialDebug.Info("First port test passed");

            // Habilitar el primer puerto
            SerialDebug.Info("Enabling first PS/2 port...");
            SendCommand(CMD_ENABLE_FIRST_PORT);

            // Resetear el teclado
            SerialDebug.Info("Resetting keyboard...");
            SendData(DEV_RESET);

            // Esperar ACK (0xFA)
            byte response = ReadData();
            if (response != 0xFA)
            {
                SerialDebug.Warning($"Keyboard did not acknowledge reset command: 0x{((ulong)response).ToStringHex()}");
                // Continuar de todos modos, algunos teclados pueden no responder correctamente
            }
            else
            {
                SerialDebug.Info("Reset acknowledged");

                // Esperar resultado del autoprueba (0xAA)
                response = ReadData();
                if (response != 0xAA)
                {
                    SerialDebug.Warning($"Keyboard self-test failed: 0x{((ulong)response).ToStringHex()}");
                    // Continuar de todos modos
                }
                else
                {
                    SerialDebug.Info("Keyboard self-test passed");
                }
            }

            // Habilitar escaneo
            SerialDebug.Info("Enabling keyboard scanning...");
            SendData(DEV_ENABLE_SCANNING);
            response = ReadData();
            if (response != 0xFA)
            {
                SerialDebug.Warning($"Keyboard did not acknowledge enable command: 0x{((ulong)response).ToStringHex()}");
                // Continuar de todos modos
            }
            else
            {
                SerialDebug.Info("Scanning enabled successfully");
            }

            // Registrar manejador
            SerialDebug.Info("Registering interrupt handler...");
            InterruptDelegate handler = new InterruptDelegate(HandleInterrupt);
            InterruptManager.RegisterIRQHandler(1, handler);
            InterruptManager.EnableIRQ(1);

            SerialDebug.Info("Keyboard controller initialized successfully");
            return true;
        }

        /// <summary>
        /// Manejador de interrupciones del teclado
        /// </summary>
        public void HandleInterrupt()
        {
            SerialDebug.Info("Keyboard IRQ triggered");

            // Leer el código de escaneo
            byte scanCode = IOPort.In8(DATA_PORT);

            // Enviar EOI inmediatamente
            //APICController.SendEOI();

            // Log después de enviar EOI
            SerialDebug.Info($"Keyboard scancode: 0x{((ulong)scanCode).ToStringHex()}");

            // Ahora que el EOI está enviado, es seguro procesar el código
            ProcessScanCode(scanCode);
        }
        /// <summary>
        /// Inicializa el mapa de códigos de escaneo
        /// </summary>
        private void InitializeScanCodeMap()
        {
            _scanCodeMap = new KeyCode[256];

            // Inicializar todos a desconocido
            for (int i = 0; i < 256; i++)
            {
                _scanCodeMap[i] = KeyCode.Unknown;
            }

            // Mapear códigos de escaneo Set 2 a códigos de tecla
            // Fila 1: Escape, F1-F12
            _scanCodeMap[0x01] = KeyCode.Escape;
            _scanCodeMap[0x3B] = KeyCode.F1;
            _scanCodeMap[0x3C] = KeyCode.F2;
            _scanCodeMap[0x3D] = KeyCode.F3;
            _scanCodeMap[0x3E] = KeyCode.F4;
            _scanCodeMap[0x3F] = KeyCode.F5;
            _scanCodeMap[0x40] = KeyCode.F6;
            _scanCodeMap[0x41] = KeyCode.F7;
            _scanCodeMap[0x42] = KeyCode.F8;
            _scanCodeMap[0x43] = KeyCode.F9;
            _scanCodeMap[0x44] = KeyCode.F10;
            _scanCodeMap[0x57] = KeyCode.F11;
            _scanCodeMap[0x58] = KeyCode.F12;

            // Fila 2: Números
            _scanCodeMap[0x29] = KeyCode.Grave;    // `
            _scanCodeMap[0x02] = KeyCode.D1;       // 1
            _scanCodeMap[0x03] = KeyCode.D2;       // 2
            _scanCodeMap[0x04] = KeyCode.D3;       // 3
            _scanCodeMap[0x05] = KeyCode.D4;       // 4
            _scanCodeMap[0x06] = KeyCode.D5;       // 5
            _scanCodeMap[0x07] = KeyCode.D6;       // 6
            _scanCodeMap[0x08] = KeyCode.D7;       // 7
            _scanCodeMap[0x09] = KeyCode.D8;       // 8
            _scanCodeMap[0x0A] = KeyCode.D9;       // 9
            _scanCodeMap[0x0B] = KeyCode.D0;       // 0
            _scanCodeMap[0x0C] = KeyCode.Minus;    // -
            _scanCodeMap[0x0D] = KeyCode.Equals;   // =
            _scanCodeMap[0x0E] = KeyCode.Backspace;

            // Fila 3
            _scanCodeMap[0x0F] = KeyCode.Tab;
            _scanCodeMap[0x10] = KeyCode.Q;
            _scanCodeMap[0x11] = KeyCode.W;
            _scanCodeMap[0x12] = KeyCode.E;
            _scanCodeMap[0x13] = KeyCode.R;
            _scanCodeMap[0x14] = KeyCode.T;
            _scanCodeMap[0x15] = KeyCode.Y;
            _scanCodeMap[0x16] = KeyCode.U;
            _scanCodeMap[0x17] = KeyCode.I;
            _scanCodeMap[0x18] = KeyCode.O;
            _scanCodeMap[0x19] = KeyCode.P;
            _scanCodeMap[0x1A] = KeyCode.LeftBracket;   // [
            _scanCodeMap[0x1B] = KeyCode.RightBracket;  // ]
            _scanCodeMap[0x2B] = KeyCode.Backslash;     // \

            // Fila 4
            _scanCodeMap[0x3A] = KeyCode.CapsLock;
            _scanCodeMap[0x1E] = KeyCode.A;
            _scanCodeMap[0x1F] = KeyCode.S;
            _scanCodeMap[0x20] = KeyCode.D;
            _scanCodeMap[0x21] = KeyCode.F;
            _scanCodeMap[0x22] = KeyCode.G;
            _scanCodeMap[0x23] = KeyCode.H;
            _scanCodeMap[0x24] = KeyCode.J;
            _scanCodeMap[0x25] = KeyCode.K;
            _scanCodeMap[0x26] = KeyCode.L;
            _scanCodeMap[0x27] = KeyCode.Semicolon;    // ;
            _scanCodeMap[0x28] = KeyCode.Quote;        // '
            _scanCodeMap[0x1C] = KeyCode.Enter;

            // Fila 5
            _scanCodeMap[0x2A] = KeyCode.LeftShift;
            _scanCodeMap[0x56] = KeyCode.Unknown;      // <> (clave internacional)
            _scanCodeMap[0x2C] = KeyCode.Z;
            _scanCodeMap[0x2D] = KeyCode.X;
            _scanCodeMap[0x2E] = KeyCode.C;
            _scanCodeMap[0x2F] = KeyCode.V;
            _scanCodeMap[0x30] = KeyCode.B;
            _scanCodeMap[0x31] = KeyCode.N;
            _scanCodeMap[0x32] = KeyCode.M;
            _scanCodeMap[0x33] = KeyCode.Comma;        // ,
            _scanCodeMap[0x34] = KeyCode.Period;       // .
            _scanCodeMap[0x35] = KeyCode.Slash;        // /
            _scanCodeMap[0x36] = KeyCode.RightShift;

            // Fila 6
            _scanCodeMap[0x1D] = KeyCode.LeftCtrl;
            // Tecla Windows izquierda: E0 5B
            _scanCodeMap[0x38] = KeyCode.LeftAlt;
            _scanCodeMap[0x39] = KeyCode.Space;
            // Tecla Alt Gr: E0 38
            // Tecla Windows derecha: E0 5C
            // Tecla Menú: E0 5D
            // Tecla Ctrl derecha: E0 1D

            // Teclas de navegación
            _scanCodeMap[0x52] = KeyCode.Insert;       // E0 52
            _scanCodeMap[0x47] = KeyCode.Home;         // E0 47
            _scanCodeMap[0x49] = KeyCode.PageUp;       // E0 49
            _scanCodeMap[0x53] = KeyCode.Delete;       // E0 53
            _scanCodeMap[0x4F] = KeyCode.End;          // E0 4F
            _scanCodeMap[0x51] = KeyCode.PageDown;     // E0 51

            // Teclas de cursor
            _scanCodeMap[0x48] = KeyCode.Up;           // E0 48
            _scanCodeMap[0x4B] = KeyCode.Left;         // E0 4B
            _scanCodeMap[0x50] = KeyCode.Down;         // E0 50
            _scanCodeMap[0x4D] = KeyCode.Right;        // E0 4D

            // Teclado numérico
            _scanCodeMap[0x45] = KeyCode.NumLock;
            _scanCodeMap[0x35] = KeyCode.KpDivide;     // E0 35
            _scanCodeMap[0x37] = KeyCode.KpMultiply;
            _scanCodeMap[0x4A] = KeyCode.KpMinus;
            _scanCodeMap[0x4E] = KeyCode.KpPlus;
            _scanCodeMap[0x1C] = KeyCode.KpEnter;      // E0 1C
            _scanCodeMap[0x4F] = KeyCode.Kp1;
            _scanCodeMap[0x50] = KeyCode.Kp2;
            _scanCodeMap[0x51] = KeyCode.Kp3;
            _scanCodeMap[0x4B] = KeyCode.Kp4;
            _scanCodeMap[0x4C] = KeyCode.Kp5;
            _scanCodeMap[0x4D] = KeyCode.Kp6;
            _scanCodeMap[0x47] = KeyCode.Kp7;
            _scanCodeMap[0x48] = KeyCode.Kp8;
            _scanCodeMap[0x49] = KeyCode.Kp9;
            _scanCodeMap[0x52] = KeyCode.Kp0;
            _scanCodeMap[0x53] = KeyCode.KpDecimal;
        }

        /// <summary>
        /// Inicializa los mapas de caracteres para traducir teclas a caracteres
        /// </summary>
        private void InitializeCharacterMap()
        {
            _characterMap = new char[256];
            _shiftCharacterMap = new char[256];

            // Inicializar todos a valor nulo
            for (int i = 0; i < 256; i++)
            {
                _characterMap[i] = '\0';
                _shiftCharacterMap[i] = '\0';
            }

            // Mapear teclas a caracteres (US layout)
            // Números
            _characterMap[(int)KeyCode.D1] = '1';
            _characterMap[(int)KeyCode.D2] = '2';
            _characterMap[(int)KeyCode.D3] = '3';
            _characterMap[(int)KeyCode.D4] = '4';
            _characterMap[(int)KeyCode.D5] = '5';
            _characterMap[(int)KeyCode.D6] = '6';
            _characterMap[(int)KeyCode.D7] = '7';
            _characterMap[(int)KeyCode.D8] = '8';
            _characterMap[(int)KeyCode.D9] = '9';
            _characterMap[(int)KeyCode.D0] = '0';

            // Mayúsculas con Shift para números
            _shiftCharacterMap[(int)KeyCode.D1] = '!';
            _shiftCharacterMap[(int)KeyCode.D2] = '@';
            _shiftCharacterMap[(int)KeyCode.D3] = '#';
            _shiftCharacterMap[(int)KeyCode.D4] = '$';
            _shiftCharacterMap[(int)KeyCode.D5] = '%';
            _shiftCharacterMap[(int)KeyCode.D6] = '^';
            _shiftCharacterMap[(int)KeyCode.D7] = '&';
            _shiftCharacterMap[(int)KeyCode.D8] = '*';
            _shiftCharacterMap[(int)KeyCode.D9] = '(';
            _shiftCharacterMap[(int)KeyCode.D0] = ')';

            // Letras
            _characterMap[(int)KeyCode.A] = 'a';
            _characterMap[(int)KeyCode.B] = 'b';
            _characterMap[(int)KeyCode.C] = 'c';
            _characterMap[(int)KeyCode.D] = 'd';
            _characterMap[(int)KeyCode.E] = 'e';
            _characterMap[(int)KeyCode.F] = 'f';
            _characterMap[(int)KeyCode.G] = 'g';
            _characterMap[(int)KeyCode.H] = 'h';
            _characterMap[(int)KeyCode.I] = 'i';
            _characterMap[(int)KeyCode.J] = 'j';
            _characterMap[(int)KeyCode.K] = 'k';
            _characterMap[(int)KeyCode.L] = 'l';
            _characterMap[(int)KeyCode.M] = 'm';
            _characterMap[(int)KeyCode.N] = 'n';
            _characterMap[(int)KeyCode.O] = 'o';
            _characterMap[(int)KeyCode.P] = 'p';
            _characterMap[(int)KeyCode.Q] = 'q';
            _characterMap[(int)KeyCode.R] = 'r';
            _characterMap[(int)KeyCode.S] = 's';
            _characterMap[(int)KeyCode.T] = 't';
            _characterMap[(int)KeyCode.U] = 'u';
            _characterMap[(int)KeyCode.V] = 'v';
            _characterMap[(int)KeyCode.W] = 'w';
            _characterMap[(int)KeyCode.X] = 'x';
            _characterMap[(int)KeyCode.Y] = 'y';
            _characterMap[(int)KeyCode.Z] = 'z';

            // Mayúsculas para letras
            _shiftCharacterMap[(int)KeyCode.A] = 'A';
            _shiftCharacterMap[(int)KeyCode.B] = 'B';
            _shiftCharacterMap[(int)KeyCode.C] = 'C';
            _shiftCharacterMap[(int)KeyCode.D] = 'D';
            _shiftCharacterMap[(int)KeyCode.E] = 'E';
            _shiftCharacterMap[(int)KeyCode.F] = 'F';
            _shiftCharacterMap[(int)KeyCode.G] = 'G';
            _shiftCharacterMap[(int)KeyCode.H] = 'H';
            _shiftCharacterMap[(int)KeyCode.I] = 'I';
            _shiftCharacterMap[(int)KeyCode.J] = 'J';
            _shiftCharacterMap[(int)KeyCode.K] = 'K';
            _shiftCharacterMap[(int)KeyCode.L] = 'L';
            _shiftCharacterMap[(int)KeyCode.M] = 'M';
            _shiftCharacterMap[(int)KeyCode.N] = 'N';
            _shiftCharacterMap[(int)KeyCode.O] = 'O';
            _shiftCharacterMap[(int)KeyCode.P] = 'P';
            _shiftCharacterMap[(int)KeyCode.Q] = 'Q';
            _shiftCharacterMap[(int)KeyCode.R] = 'R';
            _shiftCharacterMap[(int)KeyCode.S] = 'S';
            _shiftCharacterMap[(int)KeyCode.T] = 'T';
            _shiftCharacterMap[(int)KeyCode.U] = 'U';
            _shiftCharacterMap[(int)KeyCode.V] = 'V';
            _shiftCharacterMap[(int)KeyCode.W] = 'W';
            _shiftCharacterMap[(int)KeyCode.X] = 'X';
            _shiftCharacterMap[(int)KeyCode.Y] = 'Y';
            _shiftCharacterMap[(int)KeyCode.Z] = 'Z';

            // Símbolos especiales
            _characterMap[(int)KeyCode.Space] = ' ';
            _characterMap[(int)KeyCode.Minus] = '-';
            _characterMap[(int)KeyCode.Equals] = '=';
            _characterMap[(int)KeyCode.LeftBracket] = '[';
            _characterMap[(int)KeyCode.RightBracket] = ']';
            _characterMap[(int)KeyCode.Backslash] = '\\';
            _characterMap[(int)KeyCode.Semicolon] = ';';
            _characterMap[(int)KeyCode.Quote] = '\'';
            _characterMap[(int)KeyCode.Grave] = '`';
            _characterMap[(int)KeyCode.Comma] = ',';
            _characterMap[(int)KeyCode.Period] = '.';
            _characterMap[(int)KeyCode.Slash] = '/';

            // Símbolos especiales con Shift
            _shiftCharacterMap[(int)KeyCode.Space] = ' ';
            _shiftCharacterMap[(int)KeyCode.Minus] = '_';
            _shiftCharacterMap[(int)KeyCode.Equals] = '+';
            _shiftCharacterMap[(int)KeyCode.LeftBracket] = '{';
            _shiftCharacterMap[(int)KeyCode.RightBracket] = '}';
            _shiftCharacterMap[(int)KeyCode.Backslash] = '|';
            _shiftCharacterMap[(int)KeyCode.Semicolon] = ':';
            _shiftCharacterMap[(int)KeyCode.Quote] = '"';
            _shiftCharacterMap[(int)KeyCode.Grave] = '~';
            _shiftCharacterMap[(int)KeyCode.Comma] = '<';
            _shiftCharacterMap[(int)KeyCode.Period] = '>';
            _shiftCharacterMap[(int)KeyCode.Slash] = '?';

            // Teclado numérico
            _characterMap[(int)KeyCode.Kp0] = '0';
            _characterMap[(int)KeyCode.Kp1] = '1';
            _characterMap[(int)KeyCode.Kp2] = '2';
            _characterMap[(int)KeyCode.Kp3] = '3';
            _characterMap[(int)KeyCode.Kp4] = '4';
            _characterMap[(int)KeyCode.Kp5] = '5';
            _characterMap[(int)KeyCode.Kp6] = '6';
            _characterMap[(int)KeyCode.Kp7] = '7';
            _characterMap[(int)KeyCode.Kp8] = '8';
            _characterMap[(int)KeyCode.Kp9] = '9';
            _characterMap[(int)KeyCode.KpDecimal] = '.';
            _characterMap[(int)KeyCode.KpDivide] = '/';
            _characterMap[(int)KeyCode.KpMultiply] = '*';
            _characterMap[(int)KeyCode.KpMinus] = '-';
            _characterMap[(int)KeyCode.KpPlus] = '+';
            _characterMap[(int)KeyCode.KpEnter] = '\n';

            // Copia los valores del teclado numérico a las mayúsculas
            for (int i = (int)KeyCode.Kp0; i <= (int)KeyCode.KpEnter; i++)
            {
                _shiftCharacterMap[i] = _characterMap[i];
            }

            // Caracteres especiales
            _characterMap[(int)KeyCode.Enter] = '\n';
            _characterMap[(int)KeyCode.Tab] = '\t';
            _shiftCharacterMap[(int)KeyCode.Enter] = '\n';
            _shiftCharacterMap[(int)KeyCode.Tab] = '\t';
        }

        /// <summary>
        /// Método para enviar un comando al controlador de teclado
        /// </summary>
        private void SendCommand(byte command)
        {
            // Esperar a que el controlador esté listo para recibir comandos
            while ((IOPort.In8(STATUS_PORT) & 0x02) != 0)
            {
                Native.Nop();
            }

            // Enviar comando
            IOPort.Out8(COMMAND_PORT, command);
        }

        /// <summary>
        /// Método para enviar datos al teclado
        /// </summary>
        private void SendData(byte data)
        {
            // Esperar a que el controlador esté listo para recibir datos
            while ((IOPort.In8(STATUS_PORT) & 0x02) != 0)
            {
                Native.Nop();
            }

            // Enviar datos
            IOPort.Out8(DATA_PORT, data);
        }

        /// <summary>
        /// Método para leer datos del controlador de teclado
        /// </summary>
        private byte ReadData()
        {
            // Esperar a que haya datos disponibles
            for (int timeout = 0; timeout < 1000; timeout++)
            {
                if ((IOPort.In8(STATUS_PORT) & 0x01) != 0)
                {
                    return IOPort.In8(DATA_PORT);
                }
                Native.Nop();
            }

            // Timeout
            SerialDebug.Warning("Keyboard read timeout");
            return 0;
        }

        /// <summary>
        /// Actualiza los LEDs del teclado según el estado actual
        /// </summary>
        private void UpdateLEDs()
        {
            byte ledStatus = 0;

            if (_scrollLock) ledStatus |= 0x01;
            if (_numLock) ledStatus |= 0x02;
            if (_capsLock) ledStatus |= 0x04;

            // Enviar comando para actualizar LEDs
            SendData(DEV_SET_LEDS);

            // Esperar ACK
            if (ReadData() != 0xFA)
            {
                SerialDebug.Warning("Keyboard did not acknowledge LED command");
                return;
            }

            // Enviar estado de los LEDs
            SendData(ledStatus);

            // Esperar ACK
            if (ReadData() != 0xFA)
            {
                SerialDebug.Warning("Keyboard did not acknowledge LED status");
            }
        }

        /// <summary>
        /// Procesa un código de escaneo
        /// </summary>
        private void ProcessScanCode(byte scanCode)
        {
            static bool IsBreakCode(byte scanCode)
            {
                return (scanCode & 0x80) != 0;
            }

            // Determinar si es código de pulsación o liberación
            bool isBreak = IsBreakCode(scanCode);

            // Normalizar el código de escaneo (eliminar bit de liberación)
            byte normalizedScanCode = isBreak ? (byte)(scanCode & 0x7F) : scanCode;

            // Convertir código de escaneo a tecla
            KeyCode key = _scanCodeMap[normalizedScanCode];

            // Actualizar estado de teclas modificadoras
            if (key == KeyCode.LeftShift)
                _leftShift = !isBreak;
            else if (key == KeyCode.RightShift)
                _rightShift = !isBreak;
            else if (key == KeyCode.LeftCtrl)
                _leftCtrl = !isBreak;
            else if (key == KeyCode.RightCtrl)
                _rightCtrl = !isBreak;
            else if (key == KeyCode.LeftAlt)
                _leftAlt = !isBreak;
            else if (key == KeyCode.RightAlt)
                _rightAlt = !isBreak;
            else if (key == KeyCode.CapsLock && !isBreak)
            {
                // Toggle CapsLock en pulsación, no en liberación
                _capsLock = !_capsLock;
                UpdateLEDs();
            }
            else if (key == KeyCode.NumLock && !isBreak)
            {
                // Toggle NumLock en pulsación, no en liberación
                _numLock = !_numLock;
                UpdateLEDs();
            }

            // Crear evento de teclado
            KeyEvent keyEvent = new KeyEvent
            {
                ScanCode = scanCode,
                Key = key,
                Pressed = !isBreak,
                Shift = _leftShift || _rightShift,
                Ctrl = _leftCtrl || _rightCtrl,
                Alt = _leftAlt || _rightAlt,
                Character = GetCharacterForKey(key)
            };

            // Añadir el evento al buffer
            if (AddEventToBuffer(keyEvent))
            {
                SerialDebug.Info($"Teclado Evento");
            }
        }

        /// <summary>
        /// Obtiene el carácter correspondiente a una tecla, teniendo en cuenta el estado de Shift y CapsLock
        /// </summary>
        private char GetCharacterForKey(KeyCode key)
        {
            bool useShift = _leftShift || _rightShift;

            // Para letras, tener en cuenta CapsLock
            if (key >= KeyCode.A && key <= KeyCode.Z)
            {
                // Si CapsLock está activo, invierte el efecto de Shift
                useShift = useShift != _capsLock;
            }

            // Teclas de función y control no producen caracteres
            if (key == KeyCode.Unknown ||
                (key >= KeyCode.F1 && key <= KeyCode.F12) ||
                key == KeyCode.LeftShift || key == KeyCode.RightShift ||
                key == KeyCode.LeftCtrl || key == KeyCode.RightCtrl ||
                key == KeyCode.LeftAlt || key == KeyCode.RightAlt ||
                key == KeyCode.CapsLock || key == KeyCode.NumLock ||
                key == KeyCode.Escape ||
                key == KeyCode.Insert || key == KeyCode.Delete ||
                key == KeyCode.Home || key == KeyCode.End ||
                key == KeyCode.PageUp || key == KeyCode.PageDown ||
                key == KeyCode.Up || key == KeyCode.Down ||
                key == KeyCode.Left || key == KeyCode.Right)
            {
                return '\0';
            }

            // Para el teclado numérico, tener en cuenta NumLock
            if (key >= KeyCode.Kp0 && key <= KeyCode.Kp9)
            {
                if (!_numLock)
                {
                    // Si NumLock está desactivado, las teclas numéricas actúan como teclas de navegación
                    return '\0';
                }
            }

            // Usar el mapa de caracteres correspondiente
            if (useShift)
                return _shiftCharacterMap[(int)key];
            else
                return _characterMap[(int)key];
        }

        /// <summary>
        /// Añade un evento al buffer circular
        /// </summary>
        private bool AddEventToBuffer(KeyEvent keyEvent)
        {
            // Calcular la siguiente posición del tail
            int nextTail = (_bufferTail + 1) % BUFFER_SIZE;

            // Verificar si el buffer está lleno
            if (nextTail == _bufferHead)
            {
                return false; // Buffer lleno
            }

            // Añadir el evento
            _eventBuffer[_bufferTail] = keyEvent;
            _bufferTail = nextTail;

            return true;
        }

        /// <summary>
        /// Verifica si hay un evento de teclado disponible
        /// </summary>
        public bool IsKeyAvailable()
        {
            return _bufferHead != _bufferTail;
        }

        /// <summary>
        /// Lee un evento de teclado del buffer
        /// </summary>
        public KeyEvent ReadKey()
        {
            // Verificar si hay eventos disponibles
            if (_bufferHead == _bufferTail)
            {
                // Buffer vacío, devolver evento nulo
                return new KeyEvent();
            }

            // Leer el evento
            KeyEvent keyEvent = _eventBuffer[_bufferHead];

            // Avanzar el head
            _bufferHead = (_bufferHead + 1) % BUFFER_SIZE;

            return keyEvent;
        }

        /// <summary>
        /// Lee un carácter del teclado (si está disponible)
        /// </summary>
        public char ReadChar()
        {
            while (IsKeyAvailable())
            {
                KeyEvent keyEvent = ReadKey();

                // Sólo procesar teclas presionadas (no liberaciones)
                if (keyEvent.Pressed && keyEvent.Character != '\0')
                {
                    return keyEvent.Character;
                }
            }

            return '\0'; // No hay caracteres disponibles
        }

        /// <summary>
        /// Lee una línea de texto desde el teclado
        /// </summary>
        public string ReadLine()
        {
            char[] buffer = new char[256];
            int position = 0;

            while (position < 255)
            {
                // Esperar a que haya una tecla disponible
                while (!IsKeyAvailable())
                {
                    Native.Nop();
                }

                // Leer la tecla
                KeyEvent keyEvent = ReadKey();

                // Sólo procesar teclas presionadas (no liberaciones)
                if (!keyEvent.Pressed)
                    continue;

                // Manejar casos especiales
                if (keyEvent.Key == KeyCode.Enter)
                {
                    // Terminar la línea
                    Console.WriteLine(); // Nueva línea en la consola
                    break;
                }
                else if (keyEvent.Key == KeyCode.Backspace)
                {
                    // Retroceso
                    if (position > 0)
                    {
                        position--;
                        Console.Write("\b \b"); // Borrar el carácter en la consola
                    }
                }
                else if (keyEvent.Character != '\0')
                {
                    // Carácter normal
                    buffer[position++] = keyEvent.Character;
                    Console.Write(keyEvent.Character); // Mostrar en la consola
                }
            }

            // Crear y devolver la cadena
            return new string(buffer, 0, position);
        }

        /// <summary>
        /// Método que se llama al apagar el driver
        /// </summary>
        protected override void OnShutdown()
        {
            // Deshabilitar escaneo del teclado
            SendData(DEV_DISABLE_SCANNING);

            // Deshabilitar IRQ del teclado
            InterruptManager.DisableIRQ(1);
        }
    }

    /// <summary>
    /// Clase estática para funciones de teclado globales
    /// </summary>
    public static class Keyboard
    {
        private static KeyboardDriver _driver;
        private static bool _initialized;

        /// <summary>
        /// Inicializa el subsistema de teclado
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
                return;

            SerialDebug.Info("Initializing keyboard system...");

            // Crear e inicializar el driver de teclado
            _driver = new KeyboardDriver();
            if (_driver.Initialize())
            {
                _initialized = true;
                SerialDebug.Info("Keyboard system initialized successfully");
            }
            else
            {
                SerialDebug.Error("Failed to initialize keyboard driver");
            }
        }

        /// <summary>
        /// Verifica si hay una tecla disponible
        /// </summary>
        public static bool IsKeyAvailable()
        {
            return _initialized && _driver.IsKeyAvailable();
        }

        /// <summary>
        /// Lee un evento de teclado
        /// </summary>
        public static KeyEvent ReadKey()
        {
            SerialDebug.Info("Reading key event...");
            if (!_initialized)
                return new KeyEvent();

            return _driver.ReadKey();
        }

        /// <summary>
        /// Lee un carácter del teclado
        /// </summary>
        public static char ReadChar()
        {
            if (!_initialized)
                return '\0';

            return _driver.ReadChar();
        }

        /// <summary>
        /// Lee una línea de texto desde el teclado
        /// </summary>
        public static string ReadLine()
        {
            if (!_initialized)
                return string.Empty;

            return _driver.ReadLine();
        }
    }
}