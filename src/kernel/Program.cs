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

        [RuntimeExport("Entry")]
        unsafe static void EntryPoint(MultibootInfo* Info, IntPtr Modules, IntPtr Trampoline)
        {
            // Display kernel logo
            DisplayKernelLogo();

            // Mostrar mensaje de bienvenida
            string welcomeMessage = "Welcome to the KernelSharp!";
            Console.WriteLine(welcomeMessage);

            StartupCodeHelpers.InitializeModules(Modules);
            // Detectar arquitectura
            if (RuntimeArchitecture.Is32Bit)
            {
                Console.WriteLine("Ejecutando en 32 bits");
            }
            else if (RuntimeArchitecture.Is64Bit)
            {
                Console.WriteLine("Ejecutando en 64 bits");
            }

            ArrayExamples.DemoArrays();


            Console.WriteLine("Inicialización completada!");

            // Entrar en el bucle principal
            Main();
        }

        unsafe static int Main()
        {
            for (; ; );
        }

        static void DisplayKernelLogo()
        {
            Console.Write(@"                                                                                ");
            Console.Write(@"   **  **     ____     ____     _____                                           ");
            Console.Write(@"  |  \/  |   / ** \   / ** \   / ____|                                          ");
            Console.Write(@"  | \  / |  | |  | | | |  | | | (___                                            ");
            Console.Write(@"  | |\/| |  | |  | | | |  | |  \___ \                                           ");
            Console.Write(@"  | |  | |  | |__| | | |__| |  ____) |                                          ");
            Console.Write(@"  |_|  |_|   \____/   \____/  |_____/                                           ");
            Console.Write(@"                                                                                ");
            Console.WriteLine("");
        }
    }

    public static class ArrayExamples
    {
        // Método de ejemplo para crear y usar arrays
        public static void DemoArrays()
        {
            Console.WriteLine("===== EJEMPLO DE ARRAYS =====");

            // Crear un array de enteros de forma explícita
            int[] intArray = new int[4];
            intArray[0] = 10;
            intArray[1] = 20;
            intArray[2] = 30;
            intArray[3] = 40;

            Console.WriteLine("Array de enteros creado explícitamente:");
            for (int i = 0; i < intArray.Length; i++)
            {
                Console.WriteLine(intArray[i].ToString());
            }

            // Crear un array de strings
            string[] stringArray = new string[] { "Hola", "Mundo", "KernelSharp" };

            Console.WriteLine("\nArray de strings:");
            for (int i = 0; i < stringArray.Length; i++)
            {
                Console.WriteLine(stringArray[i]);
            }


            Console.WriteLine("==========================");

            Elemento elemento = new Elemento()
            {
                Name = "AvalonTM",
                Value = 21,
            };

            Console.WriteLine($"{elemento.Name} | {elemento.Value.ToString()}");
        }
    }
}