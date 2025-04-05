using Internal.Runtime.CompilerHelpers;
using Kernel.Boot;
using Kernel.Diagnostics;
using Kernel.Drivers;
using Kernel.Drivers.Input;
using Kernel.Hardware;
using Kernel.Memory;
using System;
using System.Runtime;

namespace Kernel
{
    static unsafe class Program
    {
        [RuntimeExport("Entry")]
        public static void Entry(MultibootInfo* multibootInfo)
        {
            // Initialize serial debug
            SerialDebug.Initialize();
            // Initialize memory subsystem
            Allocator.Initialize((IntPtr)0x200000);
            PageTable.Initialize();
            StartupCodeHelpers.InitializeModules((IntPtr)0x200000);

            // Show welcome message
            Console.ForegroundColor = ConsoleColor.Green;
            string welcomeMessage = "Welcome to KernelSharp!";
            Console.WriteLine(welcomeMessage);
            Console.ForegroundColor = ConsoleColor.White;

            VBEInfo* info = (VBEInfo*)multibootInfo->VBEInfo;
            if (info->PhysBase != 0)
            {
                Console.WriteLine($"screen: {info->ScreenWidth.ToString()}x{info->ScreenHeight.ToString()}");
            }

            //SerialDebug.Info(cosa);
            if (RuntimeArchitecture.Is32Bit)
            {
                Console.WriteLine("32 bits");
            }
            else if (RuntimeArchitecture.Is64Bit)
            {
                Console.WriteLine("64 bits");
            }

            // Validate Multiboot pointer
            if (multibootInfo == null)
            {
                // Handle null pointer error
                Console.WriteLine("Multiboot info pointer is null!");
                return;
            }

            Console.WriteLine("Multiboot info!");
            Console.WriteLine($"multibootInfo: 0x" + ((ulong)multibootInfo).ToStringHex());

            Console.WriteLine($"Flag: {multibootInfo->Flags}");
            /*
           DriverManager.Initialize();
           DriverManager.RegisterPCIDrivers();
           DriverManager.InitializeAllDrivers();
          */

            // Tablas de descriptores base del sistema
            GDTManager.Initialize();
            IDTManager.Initialize();

            // Detección de hardware básico
            SMBIOS.Initialize();
            SMPManager.Initialize();
            ACPIManager.Initialize();

            // Inicialización de controladores de interrupción
            APICController.Initialize();

            // Inicialización del IOAPIC explícitamente (FALTA)
            IOAPIC.Initialize();  // <-- Añadir esta línea

            // Diagnóstico del IOAPIC para verificar su estado
            IOAPIC.Diagnose();    // <-- Añadir esta línea

            // Verificar que el IOAPIC esté correctamente inicializado
            if (!IOAPIC.IsInitialized())
            {
                SerialDebug.Error("IOAPIC initialization failed, interrupts may not work properly");
            }

            // Gestor de interrupciones y dispositivos PCI
            InterruptManager.Initialize();
            PCIManager.Initialize();  // Mejor mover PCI después del gestor de interrupciones

            // Diagnóstico de interrupciones
            InterruptManager.DiagnoseInterruptSystem();

            // Inicialización de dispositivos de entrada
            Keyboard.Initialize();
  
            // Prueba del teclado
            KeyboardTest.TestTextInput();

            //ThreadPool.Initialize();

            // Show basic system information
            SerialDebug.Info("Initializing system...");

            // Rest of the kernel initialization...
            ArrayExamples.DemoArrays();

            // Initialize other kernel modules
            StartupCodeHelpers.Test();

            // Show initialization information
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\nInitialization completed!");
            Console.ForegroundColor = ConsoleColor.White;


            // Enter the main loop
            Main();
        }

        /// <summary>
        /// Kernel's main method
        /// </summary>
        unsafe static int Main()
        {
            Console.WriteLine("Main Process");

            while (true)
            {
                Native.Halt();
            }
        }


        public static class ArrayExamples
        {
            // Example method to create and use arrays
            public static void DemoArrays()
            {
                SerialDebug.Info("===== ARRAY EXAMPLE =====");

                // Create an array of integers explicitly
                int[] intArray = new int[4];
                intArray[0] = 10;
                intArray[1] = 20;
                intArray[2] = 30;
                intArray[3] = 40;

                SerialDebug.Info("Array of integers created explicitly:");
                for (int i = 0; i < intArray.Length; i++)
                {
                    SerialDebug.Info(intArray[i].ToString());
                }

                // Create an array of strings
                string[] stringArray = new string[] { "Hello", "World", "KernelSharp" };

                SerialDebug.Info("\nArray of strings:");
                for (int i = 0; i < stringArray.Length; i++)
                {
                    SerialDebug.Info(stringArray[i]);
                }

                SerialDebug.Info("==========================");

                Elemento elemento = new Elemento()
                {
                    Name = "AvalonTM",
                    Value = 21,
                };


                SerialDebug.Info(elemento.Name);
                SerialDebug.Info(elemento.Value.ToString());
                SerialDebug.Info(elemento.Value + " " + elemento.Name);
                SerialDebug.Info(elemento.Name + " " + elemento.Value);
                SerialDebug.Info($"{elemento.Name} {elemento.Value}");

                SerialDebug.Info(21 + " " + 534);
            }
        }

    }
}
