using Internal.Runtime.CompilerHelpers;
using System;
using System.Runtime;

namespace Kernel
{
    public class Elemento
    {
        public int Value { get; set; }
        public string Name { get; set; }
    }
    unsafe class Program
    {
        private const int VideoBaseAddress = 0xb8000;

        // Assumes VGA text mode 7 (80 x 25)
        // ref: https://en.wikipedia.org/wiki/VGA_text_mode
        private const int Width = 80;
        private const int Height = 25;
        public static Console console;


        [RuntimeExport("Entry")]
        unsafe static void EntryPoint(MultibootInfo* Info, IntPtr Modules, IntPtr Trampoline)
        {
            var frameBuffer = new FrameBuffer((byte*)VideoBaseAddress);
            console = new Console(Width, Height, Color.Green, frameBuffer);
            console.Clear();

            // Display kernel logo
            DisplayKernelLogo(console);

            // Mostrar mensaje de bienvenida
            string welcomeMessage = "Welcome to the KernelSharp!";
            console.PrintLine(welcomeMessage);

            StartupCodeHelpers.InitializeModules(Modules);
            // Detectar arquitectura
            if (RuntimeArchitecture.Is32Bit)
            {
                console.PrintLine("Ejecutando en 32 bits");
            }
            else if (RuntimeArchitecture.Is64Bit)
            {
                console.PrintLine("Ejecutando en 64 bits");
            }

            ArrayExamples.DemoArrays(console);


            console.PrintLine("Inicialización completada!");

            // Entrar en el bucle principal
            Main();
        }

        unsafe static int Main()
        {
            for (; ; );
        }

        static void DisplayKernelLogo(Console console)
        {
            console.Print(@"                                                                                ");
            console.Print(@"   **  **     ____     ____     _____                                           ");
            console.Print(@"  |  \/  |   / ** \   / ** \   / ____|                                          ");
            console.Print(@"  | \  / |  | |  | | | |  | | | (___                                            ");
            console.Print(@"  | |\/| |  | |  | | | |  | |  \___ \                                           ");
            console.Print(@"  | |  | |  | |__| | | |__| |  ____) |                                          ");
            console.Print(@"  |_|  |_|   \____/   \____/  |_____/                                           ");
            console.Print(@"                                                                                ");
            console.PrintLine("");
        }
    }

    public static class ArrayExamples
    {
        // Método de ejemplo para crear y usar arrays
        public static void DemoArrays(Console console)
        {
            console.PrintLine("===== EJEMPLO DE ARRAYS =====");

            // Crear un array de enteros de forma explícita
            int[] intArray = new int[4];
            intArray[0] = 10;
            intArray[1] = 20;
            intArray[2] = 30;
            intArray[3] = 40;

            console.PrintLine("Array de enteros creado explícitamente:");
            for (int i = 0; i < intArray.Length; i++)
            {
                console.PrintLine(intArray[i].ToString());
            }

            // Crear un array de strings
            string[] stringArray = new string[] { "Hola", "Mundo", "KernelSharp" };

            console.PrintLine("\nArray de strings:");
            for (int i = 0; i < stringArray.Length; i++)
            {
                console.PrintLine(stringArray[i]);
            }


            console.PrintLine("==========================");

            Elemento elemento = new Elemento()
            {
                Name = "AvalonTM",
                Value = 21,
            };

            console.PrintLine($"{elemento.Name} | {elemento.Value.ToString()}");


        }
    }
}
