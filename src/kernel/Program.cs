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
        // Método para habilitar interrupciones (invocará STI)
        [DllImport("*", EntryPoint = "_STI")]
        private static extern void EnableInterrupts();

        [RuntimeExport("Entry")]
        unsafe static void KernelEntry(MultibootInfo* multibootInfo, uint magicNumber)
        {
            // Inicializar depuración por puerto serie
            SerialDebug.Initialize();
            SerialDebug.Info("Kernel iniciando... ");
  
        // Verificar el magic number de Multiboot
            if (magicNumber != 0x2BADB002)
            {
                SerialDebug.Error($"Magic number inválido: 0x{magicNumber.ToHexString()}");
                SerialDebug.Error("No se recibió información correcta del bootloader");
                // Continuar con valores predeterminados
            }
            else
            {
                SerialDebug.Info($"Magic number correcto: 0x{magicNumber.ToHexString()}");
            }
    

            if (multibootInfo == null)
            {
                SerialDebug.Error("MultibootInfo es NULL - Error crítico");
                SerialDebug.Info("Usando valores predeterminados para memoria");
                return;
            }

            SerialDebug.Info(multibootInfo->MemLower.ToString());

            //MemoryDetection.InitializeMemoryDetection();

            // Inicializar GDT antes que nada
            SerialDebug.Info("Inicializando GDT...");
            GDTManager.Initialize();

            // Mostrar mensaje de bienvenida
            Console.ForegroundColor = ConsoleColor.Green;
            string welcomeMessage = "Bienvenido a KernelSharp!";
            Console.WriteLine(welcomeMessage);
            Console.ForegroundColor = ConsoleColor.White;

            // Mostrar información básica del sistema
            SerialDebug.Info("Inicializando sistema...");

            // Detectar arquitectura
            SerialDebug.Info("Arquitectura del sistema: ");
            if (RuntimeArchitecture.Is32Bit)
            {
                SerialDebug.Info("32 bits");
            }
            else if (RuntimeArchitecture.Is64Bit)
            {
                SerialDebug.Info("64 bits");
            }

            if (CPUMode.IsInProtectedMode())
            {
                Console.WriteLine("Sistema en modo protegido");
            }
            else
            {
                Console.WriteLine("Sistema en modo real");
            }

            if (CPUMode.IsPagingEnabled())
            {
                Console.WriteLine("Paginación habilitada");
            }

            // Pruebas de memoria
            MemoryManager.Initialize(multibootInfo);

            // Inicializar IDT después de la memoria
            IDTManager.Initialize();

            // Inicializar manejadores de interrupción
            InterruptHandlers.Initialize();

            // Inicializar el PIC
            PICController.Initialize();

            // Habilitar interrupciones
            SerialDebug.Info("Habilitando interrupciones...");
            EnableInterrupts();
            SerialDebug.Info("interrupciones [OK]");

            // Inicializar otros módulos del kernel
            StartupCodeHelpers.Test();

            // El resto de la inicialización del kernel...
            ArrayExamples.DemoArrays();

            // Mostrar información de la inicialización
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\nInicializacion completada!");
            Console.ForegroundColor = ConsoleColor.White;

            // Entrar en el bucle principal
            Main();
        }

        /// <summary>
        /// Método principal del kernel
        /// </summary>
        unsafe static int Main()
        {
            Console.WriteLine("Main Process");
            // Bucle principal del kernel
            uint idleCounter = 0;

            while (true)
            {
                Native.Halt();
                idleCounter++;

                if (idleCounter >= 1)
                {

                    idleCounter = 0;

                    // Mostrar uso de memoria cada cierto tiempo
                    SerialDebug.Info($"Mem: {MemoryManager.FreeMemory.ToString()}MB free");
                }

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

                SerialDebug.Info(elemento.Name);
                SerialDebug.Info(elemento.Value.ToString());
                SerialDebug.Info(elemento.Value + " " + elemento.Name);
                SerialDebug.Info(elemento.Name + " " + elemento.Value);
                SerialDebug.Info($"{elemento.Name} {elemento.Value.ToString()}");
                SerialDebug.Info(21 + " " + 534);
            }
        }

    }
}