using Corlib.Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerHelpers;
using System;
using System.Runtime;
using System.Runtime.InteropServices;

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

        // Al inicio de tu clase Program
        [DllImport("*", EntryPoint = "malloc")]
        private static extern IntPtr Malloc(int size);

        // Método que puedes llamar al inicio de EntryPoint
        private static unsafe void ConfigureMemoryAllocator()
        {
            console.PrintLine("Inicializando sistema de memoria para arrays...");

            // Asignar un bloque grande para el heap del sistema
            void* heapMemory = (void*)MemoryHelpers.Malloc(1024 * 1024); // 1MB inicial
            if (heapMemory == null)
            {
                console.PrintLine("ERROR: No se pudo asignar memoria para el heap!");
                return;
            }

            // Inicializar el heap del runtime
            RuntimeImports.RhpInitializeHeap(heapMemory, 1024 * 1024);

            console.PrintLine("Sistema de memoria para arrays inicializado correctamente");
        }


        [RuntimeExport("Entry")]
        unsafe static void EntryPoint(MultibootInfo* Info, IntPtr Modules, IntPtr Trampoline)
        {
            var frameBuffer = new FrameBuffer((byte*)VideoBaseAddress);
            console = new Console(Width, Height, Color.Green, frameBuffer);
            console.Clear();

            // Display kernel logo
            DisplayKernelLogo(console);

            ConfigureMemoryAllocator();

            // Inicializar el allocator
            console.PrintLine("Initializing memory allocator...");
            Allocator.Initialize((IntPtr)0x20000000);
            console.PrintLine("Memory initialization [OK]");

            // Inicializar módulos runtime
            console.PrintLine("Initializing runtime modules...");
            StartupCodeHelpers.InitializeModules(Modules);
            console.PrintLine("Runtime modules initialized [OK]");

            console.PrintLine("Initializing page table...");
            PageTable.Initialize();
            console.PrintLine("Page table initialized [OK]");

            // Mostrar mensaje de bienvenida
            string welcomeMessage = "Welcome to the Kernel!";
            console.PrintLine(welcomeMessage);
            console.Print("String length: ");
            console.PrintLine((3243234448).ToString());
          
            string model = "hola";

            for (int i=0; i < model.Length; i++)
            {
                console.PrintLine(model[i].ToString());
            }
            
            // Crear un array de enteros
            var arrays = new int[] { 10, 20, 30, 40 };

            for (int i = 0; i < arrays.Length; i++)
            {
                console.PrintLine(arrays[i].ToString());
            }

            Elemento elemento = new Elemento();
            elemento.Name = "AvalonTM";
            elemento.Value = 21;

            console.PrintLine(elemento.Name);
            console.PrintLine(elemento.Value.ToString());
            console.PrintLine((15.5).ToString());

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
}
