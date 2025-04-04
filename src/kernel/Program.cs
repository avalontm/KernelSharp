using Internal.Runtime.CompilerHelpers;
using Kernel.Boot;
using Kernel.Diagnostics;
using Kernel.Drivers;
using Kernel.Drivers.Network;
using Kernel.Hardware;
using Kernel.Memory;
using Kernel.Threading;
using System;
using System.Collections.Generic;
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

            GDTManager.Initialize();
            IDTManager.Initialize();
            SMBIOS.Initialize();
            SMPManager.Initialize();
            ACPIManager.Initialize();
            APICController.Initialize();

            PCIManager.Initialize();
            DriverManager.Initialize();
            RegisterPCIDrivers();
            DriverManager.InitializeAllDrivers();

            InterruptManager.Initialize();

            ThreadPool.Initialize();

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

        private static void RegisterPCIDrivers()
        {
            // Obtener lista de dispositivos PCI detectados
            List<PCIDevice> pciDevices = PCIManager.GetDevices();

            SerialDebug.Info($"pciDevices: {pciDevices.Count}");
            // Iterar dispositivos y registrar drivers según su clase
            for (int i = 0; i < pciDevices.Count; i++)
            {
                PCIDevice device = pciDevices[i];

                switch (device.ID.ClassCode)
                {
                    case 0x02: // Dispositivos de red
                        RegisterNetworkDriver(device);
                        break;

                    case 0x03: // Controladores de gráficos
                        //RegisterGraphicsDriver(device);
                        break;

                    case 0x01: // Controladores de almacenamiento
                               // RegisterStorageDriver(device);
                        break;

                    default:
                        // Registrar drivers genéricos para otros tipos de dispositivos
                        // RegisterGenericDriver(device);
                        break;
                }
            }
        }

        private static void RegisterNetworkDriver(PCIDevice device)
        {
            // Drivers específicos para diferentes vendedores de red
            switch (device.ID.VendorID)
            {
                case 0x8086: // Intel
                    if (device.ID.DeviceID == 0x100E) // E1000
                    {
                        DriverManager.RegisterDriver(new E1000NetworkDriver(device));
                    }
                    break;

                case 0x10EC: // Realtek
                             // Agregar soporte para otros dispositivos Realtek
                    break;

                // Otros vendedores de red
                default:
                    // Driver genérico de red
                   // DriverManager.RegisterDriver(new GenericNetworkDriver(device));
                    break;
            }
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

                SerialDebug.Info("\nList of numbers:");
                List<PCIDevice> devices = new List<PCIDevice>();

                devices.Add(new PCIDevice(new PCIDeviceID(), new PCILocation(), 0, 0,0, new uint[6], true, 0, 0));
                devices.Add(new PCIDevice(new PCIDeviceID(), new PCILocation(), 0, 0, 0, new uint[6], true, 0, 0));
                devices.Add(new PCIDevice(new PCIDeviceID(), new PCILocation(), 0, 0, 0, new uint[6], true, 0, 0));

                for (int i = 0; i < devices.Count; i++)
                {
                    SerialDebug.Info(devices[i].ID.ToString());
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
