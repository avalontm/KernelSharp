using Internal.Runtime.CompilerHelpers;
using Kernel.Applications;
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
        /// <summary>
        /// Punto de entrada del kernel
        /// </summary>
        /// <param name="multibootInfo">Información de Multiboot proporcionada por el loader</param>
        /// <param name="modules">Puntero a módulos cargados (si hay alguno)</param>
        /// <param name="gdtInfo">Información sobre la GDT configurada por el loader</param>
        [RuntimeExport("Entry")]
        public static void Entry(MultibootInfo* multibootInfo, IntPtr modules, IntPtr gdtInfo)
        {
            // Initialize serial debug
            SerialDebug.Initialize();

            SerialDebug.Info($"Información Multiboot: 0x{((ulong)multibootInfo).ToStringHex()}");
            SerialDebug.Info($"Puntero a módulos: 0x{((ulong)modules).ToStringHex()}");
            SerialDebug.Info($"Información GDT: 0x{((ulong)gdtInfo).ToStringHex()}");

            // Initialize memory subsystem
            Allocator.Initialize((IntPtr)0x200000);
            PageTable.Initialize();
            //StartupCodeHelpers.InitializeModules((IntPtr)0x200000);

            SerialDebug.Info($"modules: {(int)modules}");
            // Show welcome message
            Console.ForegroundColor = ConsoleColor.Green;
            string welcomeMessage = "Welcome to KernelSharp!";
            Console.WriteLine(welcomeMessage);
            Console.ForegroundColor = ConsoleColor.White;

            VBEInfo* info = (VBEInfo*)multibootInfo->VBEInfo;
            if (info->PhysBase != 0)
            {
                Console.WriteLine($"screen: {info->ScreenWidth}x{info->ScreenHeight}");
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
            // Si tienes información de GDT, usarla en lugar de crear una nueva
            if (gdtInfo != IntPtr.Zero)
            {
                // Inicializar GDT aprovechando la configuración del loader
                GDTManager.InitializeFromExisting(gdtInfo);
            }
            else
            {
                // Configurar GDT desde cero si no hay información disponible
                GDTManager.Initialize();
            }

            IDTManager.Initialize();

            // Detecci�n de hardware b�sico
            SMBIOS.Initialize();
            SMPManager.Initialize();
            byte apic_id= SMPManager.GetCurrentApicId();
            Console.WriteLine($"APIC ID: {apic_id}");
            ACPIManager.Initialize();

            // Inicializaci�n de controladores de interrupci�n
            APICController.Initialize();   
            IOAPIC.Initialize();

            // Gestor de interrupciones y dispositivos PCI
            InterruptManager.Initialize();
            PCIMMIOManager.Initialize();

            DriverManager.Initialize();
            DriverManager.RegisterPCIDrivers();
            DriverManager.InitializeAllDrivers();

           // AudioTest.Test();

            Keyboard.Initialize();

            Console.WriteLine("Press any key to continue...");
            SerialDebug.Info("Press any key to continue...");
            string read = Console.ReadLine();
            SerialDebug.Info("You typed: " + read);
            Console.WriteLine("You typed: " + read);
            //ThreadPool.Initialize();

            // Show basic system information
            SerialDebug.Info("Initializing system...");

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
    }
}
