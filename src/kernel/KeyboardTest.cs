using Kernel.Drivers.Input;
using Kernel.Diagnostics;
using System;

namespace Kernel
{
    /// <summary>
    /// Clase de prueba para el controlador de teclado
    /// </summary>
    public static class KeyboardTest
    {
        /// <summary>
        /// Inicializa y prueba el teclado
        /// </summary>
        public static void TestKeyboard()
        {
            SerialDebug.Info("Iniciando prueba de teclado...");

            // Inicializar el subsistema de teclado
            Keyboard.Initialize();

            Console.WriteLine("Prueba de teclado - Presione teclas para ver los eventos");
            Console.WriteLine("Presiona ESC para salir");

            // Procesar teclas hasta que se presione Escape
            while (true)
            {
                // Esperar a que haya una tecla disponible
                while (!Keyboard.IsKeyAvailable())
                {
                    Native.Pause();
                }

                // Leer la tecla
                KeyEvent keyEvent = Keyboard.ReadKey();

                // Solo mostrar las teclas presionadas, no las liberadas
                if (keyEvent.Pressed)
                {
                    Console.Write("Tecla: ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"{keyEvent.Key}");
                    Console.ForegroundColor = ConsoleColor.White;

                    if (keyEvent.Character != '\0')
                    {
                        Console.Write($" ('{keyEvent.Character}')");
                    }

                    // Mostrar modificadores
                    string modifiers = "";
                    if (keyEvent.Shift) modifiers += "Shift+";
                    if (keyEvent.Ctrl) modifiers += "Ctrl+";
                    if (keyEvent.Alt) modifiers += "Alt+";

                    if (modifiers.Length > 0)
                    {
                        Console.Write($" [{modifiers.Substring(0, modifiers.Length - 1)}]");
                    }

                    Console.WriteLine();

                    // Salir si se presiona Escape
                    if (keyEvent.Key == KeyCode.Escape)
                    {
                        break;
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("Ahora probaremos la lectura de líneas.");
            Console.WriteLine("Escribe algo y presiona Enter:");

            string line = Keyboard.ReadLine();

            Console.WriteLine();
            Console.Write("Has escrito: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(line);
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("Prueba de teclado finalizada.");
        }

        /// <summary>
        /// Prueba simple de entrada de texto con diagnóstico
        /// </summary>
        public static void TestTextInput()
        {
            // Diagnóstico inicial
            SerialDebug.Info("Starting keyboard test function");

            // Verificar estado antes de inicializar
            bool keyboardAvailable = Keyboard.IsKeyAvailable();
            SerialDebug.Info("Keyboard available before init: " + (keyboardAvailable ? "true" : "false"));

            // Asegurarse de que el teclado esté inicializado
            if (!keyboardAvailable)
            {
                SerialDebug.Info("Initializing keyboard from test function");
                Keyboard.Initialize();
            }

            // Verificar estado después de inicializar
            SerialDebug.Info("Keyboard available after init: " + (Keyboard.IsKeyAvailable() ? "true" : "false"));

            Console.WriteLine("==== Editor de texto simple ====");
            Console.WriteLine("Escribe texto. Línea vacía para terminar.");
            Console.WriteLine();

            // Prueba de detección de teclas simple
            SerialDebug.Info("Waiting for keypress...");
            Console.Write("Presiona cualquier tecla para continuar... ");

            // Bloquear hasta que se presione una tecla (con timeout)
            int timeout = 10000000; // Ajustar según sea necesario
            while (!Keyboard.IsKeyAvailable() && timeout > 0)
            {
                Native.Pause();
                timeout--;
            }

            if (timeout > 0)
            {
                // Se detectó una tecla
                KeyEvent key = Keyboard.ReadKey();
                SerialDebug.Info("Key detected! Code: " + key.Key.ToString() + ", Char: " + key.Character);
                SerialDebug.Info("Key pressed: " + (key.Pressed ? "true" : "false"));
                Console.WriteLine("[" + key.Key.ToString() + "]");
            }
            else
            {
                // No se detectó ninguna tecla en el tiempo especificado
                SerialDebug.Warning("No key detected within timeout period");
                Console.WriteLine("[Timeout]");
            }

            // Continuar con la prueba de entrada de líneas
            string[] lines = new string[20];
            int lineCount = 0;

            while (lineCount < 20)
            {
                Console.Write("> ");
                SerialDebug.Info("Waiting for line input...");

                string line = Keyboard.ReadLine();
                SerialDebug.Info("ReadLine returned: '" + line + "'");

                // Terminar si la línea está vacía
                if (line.Length == 0)
                {
                    SerialDebug.Info("Empty line detected, ending input");
                    break;
                }

                lines[lineCount] = line;
                SerialDebug.Info("Stored line " + lineCount.ToString() + ": '" + line + "'");
                lineCount++;
            }

            Console.WriteLine();
            Console.WriteLine("Texto ingresado:");
            Console.WriteLine("----------------");
            for (int i = 0; i < lineCount; i++)
            {
                Console.WriteLine(lines[i]);
            }
            Console.WriteLine("----------------");

            SerialDebug.Info("Text input test completed");
        }
    }
}