using Internal.Runtime.CompilerHelpers;
using Kernel.Drivers.IO;
using System;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;

namespace Kernel.Diagnostics
{
    /// <summary>
    /// Sistema de depuración para kernel con soporte minimalista
    /// </summary>
    public unsafe static class SerialDebug
    {
        // Puertos para depuración serie
        private const ushort COM1 = 0x3F8;
        private const ushort COM2 = 0x2F8;
        private const ushort DEBUG_PORT = COM1; // Puerto predeterminado para depuración
        private const ushort DEBUG_E9_PORT = 0xE9; // Puerto especial para emuladores

        // Estado del sistema de depuración
        private static byte currentLogLevel = 2; // Info por defecto
        private static bool isInitialized = false;
        private static bool useSerialPort = true;
        private static bool useDebugPort = true;

        // Niveles de log como constantes en lugar de enum
        public const byte LEVEL_TRACE = 0;
        public const byte LEVEL_DEBUG = 1;
        public const byte LEVEL_INFO = 2;
        public const byte LEVEL_WARN = 3;
        public const byte LEVEL_ERROR = 4;
        public const byte LEVEL_FATAL = 5;
        public const byte LEVEL_NONE = 6;

        // Caracteres comunes como constantes
        private const byte CHAR_BRACKET_OPEN = (byte)'[';
        private const byte CHAR_BRACKET_CLOSE = (byte)']';
        private const byte CHAR_SPACE = (byte)' ';
        private const byte CHAR_NEWLINE = (byte)'\n';
        private const byte CHAR_0 = (byte)'0';
        private const byte CHAR_X = (byte)'x';
        private const byte CHAR_A = (byte)'A';

        /// <summary>
        /// Inicializa el sistema de depuración
        /// </summary>
        public static void Initialize(byte logLevel = LEVEL_INFO, bool enableSerial = true, bool enableDebugPort = true)
        {
            // Guardar configuración
            currentLogLevel = logLevel;
            useSerialPort = enableSerial;
            useDebugPort = enableDebugPort;

            if (useSerialPort)
            {
                // Inicializar el puerto serie para depuración
                InitializeSerialPort(DEBUG_PORT);
            }

            isInitialized = true;

            // Mensaje de inicialización
            WriteLineRaw(LEVEL_INFO, "Kernel Debug System initialized");
        }

        /// <summary>
        /// Inicializa un puerto serie para comunicación
        /// </summary>
        private static void InitializeSerialPort(ushort port)
        {
            // Deshabilitar interrupciones
            IOPort.OutByte((ushort)(port + 1), 0x00);

            // Habilitar DLAB (Divisor Latch Access Bit)
            IOPort.OutByte((ushort)(port + 3), 0x80);

            // Configurar tasa de baudios (115200 bps)
            // Divisor = 115200 / 9600 = 12
            IOPort.OutByte((ushort)(port + 0), 0x0C); // LSB
            IOPort.OutByte((ushort)(port + 1), 0x00); // MSB

            // 8 bits, sin paridad, 1 bit de parada
            IOPort.OutByte((ushort)(port + 3), 0x03);

            // Habilitar FIFO, limpiar y con umbral de 14 bytes
            IOPort.OutByte((ushort)(port + 2), 0xC7);

            // Habilitar interrupciones, RTS/DSR set
            IOPort.OutByte((ushort)(port + 4), 0x0B);

            // Prueba de loopback para verificar que el puerto funciona
            IOPort.OutByte((ushort)(port + 4), 0x1E); // Habilitar loopback
            IOPort.OutByte((ushort)(port + 0), 0x55); // Enviar byte de prueba

            if (IOPort.InByte((ushort)(port + 0)) != 0x55)
            {
                // El puerto no responde correctamente
                useSerialPort = false;
            }

            // Restaurar modo normal
            IOPort.OutByte((ushort)(port + 4), 0x0F);
        }

        /// <summary>
        /// Escribe un mensaje de depuración con formato
        /// </summary>
        private static void WriteLineRaw(byte level, string message)
        {
            if (!isInitialized)
            {
                // Auto-inicializar con configuración predeterminada
                Initialize();
            }

            // Verificar si el nivel de log es suficiente para mostrar este mensaje
            if (level < currentLogLevel)
                return;

            // Enviar formato de inicio: [PREFIX]
            if (useSerialPort)
            {
                WriteByteToSerial(CHAR_BRACKET_OPEN);
                WriteLogLevelPrefixToSerial(level);
                WriteByteToSerial(CHAR_BRACKET_CLOSE);
                WriteByteToSerial(CHAR_SPACE);
            }

            if (useDebugPort)
            {
                WriteByteToDebugPort(CHAR_BRACKET_OPEN);
                WriteLogLevelPrefixToDebugPort(level);
                WriteByteToDebugPort(CHAR_BRACKET_CLOSE);
                WriteByteToDebugPort(CHAR_SPACE);
            }

            // Enviar el mensaje
            WriteStringToOutputs(message);

            // Enviar salto de línea
            if (useSerialPort)
            {
                WriteByteToSerial(CHAR_NEWLINE);
            }

            if (useDebugPort)
            {
                WriteByteToDebugPort(CHAR_NEWLINE);
            }
        }

        /// <summary>
        /// Escribe un string a los puertos configurados
        /// </summary>
        private static void WriteStringToOutputs(string text)
        {
            if (text == null)
                return;

            int len = text.Length;
            for (int i = 0; i < len; i++)
            {
                char c = text[i];
                byte b = (byte)c;

                if (useSerialPort)
                {
                    WriteByteToSerial(b);
                }

                if (useDebugPort)
                {
                    WriteByteToDebugPort(b);
                }
            }
        }

        /// <summary>
        /// Escribe el prefijo del nivel a puerto serie
        /// </summary>
        private static void WriteLogLevelPrefixToSerial(byte level)
        {
            // Escribir prefijo según nivel sin usar arrays
            switch (level)
            {
                case LEVEL_TRACE:
                    WriteByteToSerial((byte)'T');
                    WriteByteToSerial((byte)'R');
                    WriteByteToSerial((byte)'A');
                    WriteByteToSerial((byte)'C');
                    WriteByteToSerial((byte)'E');
                    break;
                case LEVEL_DEBUG:
                    WriteByteToSerial((byte)'D');
                    WriteByteToSerial((byte)'E');
                    WriteByteToSerial((byte)'B');
                    WriteByteToSerial((byte)'U');
                    WriteByteToSerial((byte)'G');
                    break;
                case LEVEL_INFO:
                    WriteByteToSerial((byte)'I');
                    WriteByteToSerial((byte)'N');
                    WriteByteToSerial((byte)'F');
                    WriteByteToSerial((byte)'O');
                    break;
                case LEVEL_WARN:
                    WriteByteToSerial((byte)'W');
                    WriteByteToSerial((byte)'A');
                    WriteByteToSerial((byte)'R');
                    WriteByteToSerial((byte)'N');
                    break;
                case LEVEL_ERROR:
                    WriteByteToSerial((byte)'E');
                    WriteByteToSerial((byte)'R');
                    WriteByteToSerial((byte)'R');
                    WriteByteToSerial((byte)'O');
                    WriteByteToSerial((byte)'R');
                    break;
                case LEVEL_FATAL:
                    WriteByteToSerial((byte)'F');
                    WriteByteToSerial((byte)'A');
                    WriteByteToSerial((byte)'T');
                    WriteByteToSerial((byte)'A');
                    WriteByteToSerial((byte)'L');
                    break;
                default:
                    WriteByteToSerial((byte)'?');
                    WriteByteToSerial((byte)'?');
                    WriteByteToSerial((byte)'?');
                    WriteByteToSerial((byte)'?');
                    WriteByteToSerial((byte)'?');
                    break;
            }
        }

        /// <summary>
        /// Escribe el prefijo del nivel a puerto debug
        /// </summary>
        private static void WriteLogLevelPrefixToDebugPort(byte level)
        {
            // Mismo enfoque que arriba pero para puerto debug
            switch (level)
            {
                case LEVEL_TRACE:
                    WriteByteToDebugPort((byte)'T');
                    WriteByteToDebugPort((byte)'R');
                    WriteByteToDebugPort((byte)'A');
                    WriteByteToDebugPort((byte)'C');
                    WriteByteToDebugPort((byte)'E');
                    break;
                case LEVEL_DEBUG:
                    WriteByteToDebugPort((byte)'D');
                    WriteByteToDebugPort((byte)'E');
                    WriteByteToDebugPort((byte)'B');
                    WriteByteToDebugPort((byte)'U');
                    WriteByteToDebugPort((byte)'G');
                    break;
                case LEVEL_INFO:
                    WriteByteToDebugPort((byte)'I');
                    WriteByteToDebugPort((byte)'N');
                    WriteByteToDebugPort((byte)'F');
                    WriteByteToDebugPort((byte)'O');
                    break;
                case LEVEL_WARN:
                    WriteByteToDebugPort((byte)'W');
                    WriteByteToDebugPort((byte)'A');
                    WriteByteToDebugPort((byte)'R');
                    WriteByteToDebugPort((byte)'N');
                    break;
                case LEVEL_ERROR:
                    WriteByteToDebugPort((byte)'E');
                    WriteByteToDebugPort((byte)'R');
                    WriteByteToDebugPort((byte)'R');
                    WriteByteToDebugPort((byte)'O');
                    WriteByteToDebugPort((byte)'R');
                    break;
                case LEVEL_FATAL:
                    WriteByteToDebugPort((byte)'F');
                    WriteByteToDebugPort((byte)'A');
                    WriteByteToDebugPort((byte)'T');
                    WriteByteToDebugPort((byte)'A');
                    WriteByteToDebugPort((byte)'L');
                    break;
                default:
                    WriteByteToDebugPort((byte)'?');
                    WriteByteToDebugPort((byte)'?');
                    WriteByteToDebugPort((byte)'?');
                    WriteByteToDebugPort((byte)'?');
                    WriteByteToDebugPort((byte)'?');
                    break;
            }
        }

        /// <summary>
        /// Envía un byte a través del puerto serie
        /// </summary>
        private static void WriteByteToSerial(byte value)
        {
            // Esperar hasta que el puerto esté listo para enviar
            while ((IOPort.InByte((ushort)(DEBUG_PORT + 5)) & 0x20) == 0)
            {
                // Pequeña pausa para no saturar el bus
                int j = 1000;
                while (j > 0) j--;
            }

            // Enviar byte
            IOPort.OutByte((ushort)(DEBUG_PORT), value);
        }

        /// <summary>
        /// Envía un byte a través del puerto de depuración especial (0xE9)
        /// </summary>
        private static void WriteByteToDebugPort(byte value)
        {
            // El puerto 0xE9 es un puerto especial en emuladores
            IOPort.OutByte(DEBUG_E9_PORT, value);
        }

        /// <summary>
        /// Escribe un mensaje de depuración de nivel Info
        /// </summary>
        public static void Info(string message)
        {
            WriteLineRaw(LEVEL_INFO, message);
        }

        /// <summary>
        /// Escribe un mensaje de depuración de nivel Warning
        /// </summary>
        public static void Warning(string message)
        {
            WriteLineRaw(LEVEL_WARN, message);
        }

        /// <summary>
        /// Escribe un mensaje de depuración de nivel Error
        /// </summary>
        public static void Error(string message)
        {
            WriteLineRaw(LEVEL_ERROR, message);
        }

        /// <summary>
        /// Escribe un mensaje de depuración de nivel Fatal
        /// </summary>
        public static void Fatal(string message)
        {
            WriteLineRaw(LEVEL_FATAL, message);
        }

        /// <summary>
        /// Escribe un mensaje de depuración de nivel Debug
        /// </summary>
        public static void DebugMessage(string message)
        {
            WriteLineRaw(LEVEL_DEBUG, message);
        }

        /// <summary>
        /// Escribe un mensaje de depuración de nivel Trace
        /// </summary>
        public static void Trace(string message)
        {
            WriteLineRaw(LEVEL_TRACE, message);
        }

        /// <summary>
        /// Escribe un número hexadecimal con prefijo 0x
        /// </summary>
        public static void WriteHex(uint value)
        {
            if (useSerialPort)
            {
                // Escribir prefijo "0x"
                WriteByteToSerial(CHAR_0);
                WriteByteToSerial(CHAR_X);

                // Escribir dígitos hexadecimales
                for (int i = 28; i >= 0; i -= 4)
                {
                    byte digit = (byte)((value >> i) & 0xF);
                    byte hexChar = digit < 10 ?
                                  (byte)(CHAR_0 + digit) :
                                  (byte)(CHAR_A + (digit - 10));
                    WriteByteToSerial(hexChar);
                }
            }

            if (useDebugPort)
            {
                // Escribir prefijo "0x"
                WriteByteToDebugPort(CHAR_0);
                WriteByteToDebugPort(CHAR_X);

                // Escribir dígitos hexadecimales
                for (int i = 28; i >= 0; i -= 4)
                {
                    byte digit = (byte)((value >> i) & 0xF);
                    byte hexChar = digit < 10 ?
                                  (byte)(CHAR_0 + digit) :
                                  (byte)(CHAR_A + (digit - 10));
                    WriteByteToDebugPort(hexChar);
                }
            }
        }

        [RuntimeExport("_DebugWrite")]
        public static unsafe void WriteCharToSerial(byte* value, int length)
        {
            if (value == null || length <= 0)
                return;

            // Opcional: puedes comentar el prefijo si está causando confusión
            // En su lugar, podemos agregar un contador para debug
            WriteByteToSerial((byte)'[');
            WriteByteToSerial((byte)length); // Enviar la longitud real como un byte
            WriteByteToSerial((byte)']');
            WriteByteToSerial((byte)' ');

            // Enviar los bytes uno por uno
            for (int i = 0; i < length; i++)
            {
                byte b = value[i];
                if (useSerialPort)
                {
                    WriteByteToSerial(b);
                }
                if (useDebugPort)
                {
                    WriteByteToDebugPort(b);
                }
            }

            // Agregar nueva línea al final
            if (useSerialPort)
            {
                WriteByteToSerial(CHAR_NEWLINE);
            }
            if (useDebugPort)
            {
                WriteByteToDebugPort(CHAR_NEWLINE);
            }
        }
    }
}