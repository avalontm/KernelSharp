using Internal.Runtime.CompilerHelpers;
using Kernel.Boot;
using Kernel.Diagnostics;
using Kernel.Drivers.Video;
using Kernel.Memory;
using System;
using System.Runtime;

namespace Kernel
{
    unsafe class Program
    {
        [RuntimeExport("Entry")]
        unsafe static void EntryPoint(MultibootInfo* info, IntPtr modules, IntPtr trampoline)
        {
            // Inicializar depuración por puerto serie
            SerialDebug.Initialize();
            SerialDebug.Info("Kernel iniciando...");

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

            // Inicializar sistema de gestión de memoria
            SerialDebug.Info("\nInicializando sistema de memoria...");
            InitializeMemorySystem();

            // Inicializar otros módulos del kernel
            StartupCodeHelpers.InitializeModules(modules);

            // Pruebas de memoria
            TestMemorySystem();

            // El resto de la inicialización del kernel...
            ArrayExamples.DemoArrays();

            // Mostrar información de la inicialización
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\nInicialización completada!");
            Console.ForegroundColor = ConsoleColor.White;

            // Entrar en el bucle principal
            Main();
        }

        /// <summary>
        /// Inicializa el sistema de gestión de memoria
        /// </summary>
        static void InitializeMemorySystem()
        {
            // Inicializar el sistema de memoria sin depender de MultibootInfo
            SerialDebug.Info("==== Iniciando gestor de memoria ====");

            // Inicializar todos los componentes
            MemoryManager.Initialize();

            // Mostrar información del sistema de memoria
            MemoryManager.PrintMemoryInfo();

            SerialDebug.Info("==== Sistema de memoria inicializado correctamente ==== ");
        }

        /// <summary>
        /// Realiza pruebas básicas del sistema de memoria
        /// </summary>
        static void TestMemorySystem()
        {
            SerialDebug.Info("\n===== PRUEBAS DEL SISTEMA DE MEMORIA =====");

            // Prueba 1: Asignación y liberación de memoria simple
            SerialDebug.Info("Prueba 1: Asignación básica...");
            void* mem1 = MemoryManager.Allocate(1024);
            if (mem1 != null)
            {
                 SerialDebug.Info($"  Memoria asignada en: 0x{(uint)mem1:X8}");

                // Escribir datos en la memoria
                byte* bytePtr = (byte*)mem1;
                for (int i = 0; i < 1024; i++)
                {
                    bytePtr[i] = (byte)(i & 0xFF);
                }

                // Verificar los datos
                bool dataValid = true;
                for (int i = 0; i < 1024; i++)
                {
                    if (bytePtr[i] != (byte)(i & 0xFF))
                    {
                        dataValid = false;
                        break;
                    }
                }
                Console.WriteLine($"  Validación de datos: {(dataValid ? "OK" : "ERROR")}");

                // Liberar
                MemoryManager.Free(mem1);
                Console.WriteLine("  Memoria liberada correctamente");
            }
            else
            {
                Console.WriteLine("  ERROR: Fallo al asignar memoria");
            }

            // Prueba 2: Múltiples asignaciones y liberaciones
            Console.WriteLine("\nPrueba 2: Múltiples asignaciones...");
            const int NUM_ALLOCS = 10;
            void*[] allocations = new void*[NUM_ALLOCS];

            for (int i = 0; i < NUM_ALLOCS; i++)
            {
                uint size = (uint)((i + 1) * 512); // 512B, 1KB, 1.5KB, etc.
                allocations[i] = MemoryManager.Allocate(size);

                if (allocations[i] != null)
                {
                    Console.WriteLine($"  Asignación #{i + 1}: {size} bytes en 0x{(uint)allocations[i]:X8}");
                }
                else
                {
                    Console.WriteLine($"  ERROR: Fallo al asignar {size} bytes");
                }
            }

            // Liberar en orden inverso
            for (int i = NUM_ALLOCS - 1; i >= 0; i--)
            {
                if (allocations[i] != null)
                {
                    MemoryManager.Free(allocations[i]);
                    Console.WriteLine($"  Liberada asignación #{i + 1}");
                }
            }

            // Prueba 3: Asignación de páginas físicas
            Console.WriteLine("\nPrueba 3: Asignación de páginas físicas...");
            uint pageIndex = PhysicalMemoryManager.AllocatePage();
            if (pageIndex != uint.MaxValue)
            {
                uint pageAddress = PhysicalMemoryManager.PageToAddress(pageIndex);
                Console.WriteLine($"  Página asignada: #{pageIndex}, dirección física: 0x{pageAddress:X8}");

                // Liberar la página
                PhysicalMemoryManager.FreePage(pageIndex);
                Console.WriteLine("  Página liberada correctamente");
            }
            else
            {
                Console.WriteLine("  ERROR: No se pudo asignar página física");
            }

            // Prueba 4: Memoria virtual
            Console.WriteLine("\nPrueba 4: Memoria virtual...");
            uint virtAddr = MemoryManager.AllocateVirtualMemory(0, 2, false);  // 2 páginas, acceso kernel

            if (virtAddr != 0)
            {
                Console.WriteLine($"  Memoria virtual asignada en: 0x{virtAddr}");

                // Probar escribir en la memoria virtual
                byte* vptr = (byte*)virtAddr;
                vptr[0] = 0xAA;
                vptr[1] = 0xBB;
                vptr[2] = 0xCC;
                vptr[3] = 0xDD;

                Console.WriteLine($"  Escritura en memoria virtual: 0x{vptr[0]}{vptr[1]}{vptr[2]}{vptr[3]}");

                // Liberar la memoria virtual
                MemoryManager.FreeVirtualMemory(virtAddr, 2);
                Console.WriteLine("  Memoria virtual liberada correctamente");
            }
            else
            {
                Console.WriteLine("  ERROR: No se pudo asignar memoria virtual");
            }

            // Estadísticas finales
            Console.WriteLine("\nEstadísticas de memoria tras las pruebas:");
            MemoryManager.PrintMemoryInfo();
            Console.WriteLine("=========================================");
        }

        /// <summary>
        /// Método principal del kernel
        /// </summary>
        unsafe static int Main()
        {
            // Bucle principal del kernel
            uint idleCounter = 0;

            while (true)
            {
                // Aquí se implementaría el planificador de tareas, manejo de interrupciones, etc.
                // Para este ejemplo, simplemente mostramos un indicador de actividad cada cierto tiempo

                if (++idleCounter >= 10000000)
                {
                    idleCounter = 0;

                    // Mostrar uso de memoria cada cierto tiempo
                    ShowMemoryUsage();
                }
            }
        }

        /// <summary>
        /// Muestra información sobre el uso de memoria
        /// </summary>
        static void ShowMemoryUsage()
        {
            // Obtener porcentaje de memoria libre
            int freePercent = PhysicalMemoryManager.FreeMemoryPercentage();

            // Elegir color según el porcentaje libre
            ConsoleColor color;
            if (freePercent > 50)
                color = ConsoleColor.Green;
            else if (freePercent > 20)
                color = ConsoleColor.Yellow;
            else
                color = ConsoleColor.Red;

            // Guardar posición actual
            int oldX = Console.CursorLeft;
            int oldY = Console.CursorTop;

            // Mostrar en la esquina superior derecha
            Console.SetCursorPosition(Console.WindowWidth - 20, 0);
            Console.ForegroundColor = color;
            Console.Write($"Mem: {PhysicalMemoryManager.FreeMemoryMB()}MB free");
            Console.ForegroundColor = ConsoleColor.White;

            // Restaurar posición
            Console.SetCursorPosition(oldX, oldY);
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
            SerialDebug.Info(21 + " " + 534);
        }
    }

    public class Elemento
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }
}