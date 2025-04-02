using Internal.Runtime.CompilerHelpers;
using Kernel.Boot;
using Kernel.Diagnostics;
using Kernel.Drivers;
using Kernel.Memory;
using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Kernel
{
    unsafe class Program
    {
        [RuntimeExport("Entry")]
        public static void Entry(MultibootInfo* multibootInfo, IntPtr trampoline)
        {
            // Initialize memory subsystem
            Allocator.Initialize((IntPtr)0x20000000);
            StartupCodeHelpers.InitializeModules((IntPtr)0x20000000);
            PageTable.Initialize();

            // Show welcome message
            Console.ForegroundColor = ConsoleColor.Green;
            string welcomeMessage = "Welcome to KernelSharp!";
            Console.WriteLine(welcomeMessage);
            Console.ForegroundColor = ConsoleColor.White;

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

            // Initialize serial debug
            SerialDebug.Initialize();

            // Rest of the kernel initialization...
            ArrayExamples.DemoArrays();


            Console.WriteLine("Multiboot info!");
            Console.WriteLine($"multibootInfo: 0x" + ((ulong)multibootInfo).ToStringHex());

            Console.WriteLine($"Flag: {multibootInfo->Flags.ToString()}");

            // Initialize GDT first
            SerialDebug.Info("Initializing GDT...");
            //GDTManager.Initialize();

            // Show basic system information
            SerialDebug.Info("Initializing system...");

            if (CPUMode.IsInProtectedMode())
            {
                Console.WriteLine("System in protected mode");
            }
            else
            {
                Console.WriteLine("System in real mode");
            }

            if (CPUMode.IsPagingEnabled())
            {
                Console.WriteLine("Paging enabled");
            }

            // Memory tests
            MemoryManager.Initialize(multibootInfo);

            // Initialize IDT after memory
            //IDTManager.Initialize();

            // Initialize interrupt handlers
            //InterruptHandlers.Initialize();

            // Initialize the PIC
            //PICController.Initialize();


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
            // Kernel's main loop
            uint idleCounter = 0;

            while (true)
            {
                Native.Halt();
                idleCounter++;

                if (idleCounter >= 1)
                {
                    idleCounter = 0;
                    // Show memory usage after a certain period
                }

            }
        }


        public static class ArrayExamples
        {
            // Example method to create and use arrays
            public static void DemoArrays()
            {
                Console.WriteLine("===== ARRAY EXAMPLE =====");

                // Create an array of integers explicitly
                int[] intArray = new int[4];
                intArray[0] = 10;
                intArray[1] = 20;
                intArray[2] = 30;
                intArray[3] = 40;

                Console.WriteLine("Array of integers created explicitly:");
                for (int i = 0; i < intArray.Length; i++)
                {
                    Console.WriteLine(intArray[i].ToString());
                }

                // Create an array of strings
                string[] stringArray = new string[] { "Hello", "World", "KernelSharp" };

                Console.WriteLine("\nArray of strings:");
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
