using Kernel.Diagnostics;
using Kernel.Drivers.Audio;
using Kernel.Drivers.Input;
using Kernel.Drivers.Network;
using System;
using System.Collections.Generic;

namespace Kernel.Drivers
{
    /// <summary>
    /// Represents a basic driver in the system
    /// </summary>
    public abstract class IDriver
    {
        /// <summary>
        /// Unique identifier for the driver
        /// </summary>
        public string Id { get; internal set; }

        /// <summary>
        /// Descriptive name of the driver
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Current state of the driver
        /// </summary>
        public DriverState State { get; internal set; }

        /// <summary>
        /// Initializes the driver
        /// </summary>
        /// <returns>True if initialization was successful</returns>
        public virtual bool Initialize()
        {
            return false;
        }

        /// <summary>
        /// Stops and cleans up driver resources
        /// </summary>
        public virtual void Shutdown()
        {
            // Base implementation does nothing
        }
    }

    /// <summary>
    /// Possible states for a driver
    /// </summary>
    public enum DriverState 
    {
        Unloaded,
        Initializing,
        Running,
        Error,
        Stopped
    }

    /// <summary>
    /// Types of drivers
    /// </summary>
    public enum DriverType 
    {
        Graphics,
        Network,
        Storage,
        Input,
        Audio,
        Other
    }

    /// <summary>
    /// System driver manager
    /// </summary>
    public unsafe static class DriverManager
    {
        // Arrays to store drivers
        private static IDriver[] _registeredDrivers;
        private static int _driverCount = 0;

        // Parallel arrays for driver type management
        private static int[] _driverTypes;

        /// <summary>
        /// Initializes the Driver Manager
        /// </summary>
        public static void Initialize()
        {
            SerialDebug.Info("Initializing Lightweight Driver Manager...");
            // Reset arrays
            _registeredDrivers = new IDriver[64];
            SerialDebug.Info("Driver array initialized successfully");
            _driverTypes = new int[64];
            _driverCount = 0;
            SerialDebug.Info("Lightweight Driver Manager initialized successfully");
        }

        /// <summary>
        /// Registers a new driver in the system
        /// </summary>
        /// <param name="driver">Driver to register</param>
        public static bool RegisterDriver(IDriver driver)
        {
            if (driver == null)
                return false;

            // Check for duplicate
            for (int i = 0; i < _driverCount; i++)
            {
                if (_registeredDrivers[i] != null)
                {
                    if (_registeredDrivers[i].Id == driver.Id)
                    {
                        SerialDebug.Warning($"Driver {driver.Id} already registered. Skipping.");
                        return false;
                    }
                }
            }

            // Ensure capacity
            if (_driverCount >= _registeredDrivers.Length)
            {
                SerialDebug.Warning("Maximum driver capacity reached. Cannot register more drivers.");
                return false;
            }

            // Register driver
            _registeredDrivers[_driverCount] = driver;
            _driverTypes[_driverCount] = DetermineDriverType(driver);
            _driverCount++;

            SerialDebug.Info($"Registered driver: {driver.Id} ({driver.Name})");
            return true;
        }

        /// <summary>
        /// Gets a driver by its ID
        /// </summary>
        public static IDriver GetDriver(string id)
        {
            for (int i = 0; i < _driverCount; i++)
            {
                if (_registeredDrivers[i].Id == id)
                {
                    return _registeredDrivers[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Gets all drivers of a specific type
        /// </summary>
        public static IDriver[] GetDriversByType(DriverType type)
        {
            // Count matching drivers first
            int matchCount = 0;
            for (int i = 0; i < _driverCount; i++)
            {
                if (_driverTypes[i] == type)
                {
                    matchCount++;
                }
            }

            // Create and populate result array
            IDriver[] result = new IDriver[matchCount];
            int index = 0;
            for (int i = 0; i < _driverCount; i++)
            {
                if (_driverTypes[i] == type)
                {
                    result[index++] = _registeredDrivers[i];
                }
            }

            return result;
        }

        /// <summary>
        /// Initializes all registered drivers
        /// </summary>
        public static void InitializeAllDrivers()
        {
            SerialDebug.Info("Initializing all drivers...");


            // Iterate through priority order
            for (int o = 0; o < 6; o++)
            {
                for (int i = 0; i < _driverCount; i++)
                {
                    // Check if driver matches current type
                    if (_driverTypes[i] == o)
                    {
                        IDriver driver = _registeredDrivers[i];
                        //SerialDebug.Info($"Initializing driver: {driver.Id}");

                        if (driver.Initialize())
                        {
                           // SerialDebug.Info($"Driver initialized successfully: {driver.Id}");
                        }
                        else
                        {
                            //SerialDebug.Warning($"Failed to initialize driver: {driver.Id}");
                        }
                    }
                }
            }

            SerialDebug.Info("All drivers initialization complete");
        }

        /// <summary>
        /// Shuts down all drivers
        /// </summary>
        public static void ShutdownAllDrivers()
        {
            SerialDebug.Info("Shutting down all drivers...");

            // Shutdown in reverse order
            for (int i = _driverCount - 1; i >= 0; i--)
            {
                IDriver driver = _registeredDrivers[i];
                SerialDebug.Info($"Shutting down driver: {driver.Id}");
                driver.Shutdown();
            }

            SerialDebug.Info("All drivers shutdown complete");
        }

        /// <summary>
        /// Determines the type of driver
        /// </summary>
        private static int DetermineDriverType(IDriver driver)
        {
            // Logic to determine driver type (same as previous implementation)
            string name = driver.Name.ToUpper();

            if (name.Contains("GRAPHICS") || name.Contains("VIDEO") || name.Contains("DISPLAY"))
                return (int)DriverType.Graphics;

            if (name.Contains("NETWORK") || name.Contains("ETHERNET") || name.Contains("WIFI"))
                return (int)DriverType.Network;

            if (name.Contains("STORAGE") || name.Contains("DISK") || name.Contains("IDE") ||
                name.Contains("SATA") || name.Contains("NVME"))
                return (int)DriverType.Storage;

            if (name.Contains("INPUT") || name.Contains("KEYBOARD") || name.Contains("MOUSE"))
                return (int)DriverType.Input;

            if (name.Contains("AUDIO") || name.Contains("SOUND"))
                return (int)DriverType.Audio;

            return (int)DriverType.Other;
        }


        public static void RegisterPCIDrivers()
        {
            SerialDebug.Info("Registering PCI drivers...");
            // Obtener lista de dispositivos PCI detectados
            List<PCIDevice> pciDevices = PCIMMIOManager.GetDevices();

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
                               // RegisterGraphicsDriver(device);
                        break;

                    case 0x01: // Controladores de almacenamiento
                               // RegisterStorageDriver(device);
                        break;
                    case 0x04: // Dispositivos multimedia (audio)
                        RegisterAudioDriver(device);
                        break;
                    case 0x08: // Dispositivos de entrada
                               // Los dispositivos de entrada generalmente tienen el ClassCode 0x08
                        RegisterInputDriver(device);
                        break;
                    default:
                        // Registrar drivers genéricos para otros tipos de dispositivos
                        // RegisterGenericDriver(device);
                        break;
                }
            }
        }

        /// <summary>
        /// Función para registrar un driver de dispositivos de entrada (teclados, ratones, etc.)
        /// </summary>
        private static void RegisterInputDriver(PCIDevice device)
        {
            SerialDebug.Info($"Registering input device driver for device ID: {device.ID.DeviceID}");

            // Implementar el registro del driver de dispositivo de entrada aquí
            // Esto podría incluir la inicialización de un driver de teclado o mouse,
            // dependiendo del dispositivo PCI.

            // Ejemplo de cómo se podría registrar un driver de teclado
            if (device.ID.DeviceID == 0x1000) // Ejemplo de ID para un dispositivo de teclado
            {
                RegisterKeyboardDriver(device);
            }
            else if (device.ID.DeviceID == 0x2000) // Ejemplo de ID para un dispositivo de ratón
            {
                RegisterMouseDriver(device);
            }
            else
            {
                //SerialDebug.Warning($"Input device with unrecognized ID: {device.ID.DeviceID}. Registering a generic input driver.");
                RegisterGenericInputDriver(device);
            }
        }

        /// <summary>
        /// Función para registrar el driver de un teclado.
        /// </summary>
        /// <summary>
        /// Función para registrar el driver de un teclado.
        /// </summary>
        private static void RegisterKeyboardDriver(PCIDevice device)
        {
            //SerialDebug.Info($"Registering keyboard driver for device ID: {device.ID.DeviceID}");

            // Verificar si el dispositivo es un teclado PS/2 (opcional, según el tipo de teclado que quieras registrar)
            if (IsPS2Keyboard(device))
            {
                // Crear una instancia del driver para el teclado
                KeyboardDriver driver = new KeyboardDriver();

                // Registrar el driver con el DriverManager
                DriverManager.RegisterDriver(driver);

                SerialDebug.Info("PS/2 Keyboard driver successfully registered.");
            }
            else
            {
                SerialDebug.Warning("The device is not a PS/2 keyboard. Skipping driver registration.");
            }
        }

        /// <summary>
        /// Función para verificar si el dispositivo es un teclado PS/2.
        /// </summary>
        private static bool IsPS2Keyboard(PCIDevice device)
        {
            // Este es un ejemplo de cómo podrías verificar si el dispositivo es un teclado PS/2.
            // Necesitarías comprobar el ID del dispositivo, y posiblemente otros parámetros
            // para asegurar que se trata de un teclado PS/2.
            return device.ID.DeviceID == 0x0001;  // Ejemplo de ID para un teclado PS/2 (debe ser verificado según el hardware).
        }

        /// <summary>
        /// Función para registrar el driver de un ratón.
        /// </summary>
        private static void RegisterMouseDriver(PCIDevice device)
        {
            //SerialDebug.Info($"Registering mouse driver for device ID: {device.ID.DeviceID}");
            // Implementar la inicialización del driver de ratón aquí.
            // Esto podría incluir la configuración de interrupciones o puertos.
        }

        /// <summary>
        /// Función para registrar un driver de dispositivo de entrada genérico.
        /// </summary>
        private static void RegisterGenericInputDriver(PCIDevice device)
        {
           // SerialDebug.Info($"Registering generic input driver for device ID: {device.ID.DeviceID}");
            // Implementar la inicialización del driver genérico de entrada aquí.
            // Esto podría involucrar el registro de un controlador genérico que maneje entradas estándar.

        }
        /// <summary>
        /// Registra controladores de dispositivos de audio
        /// </summary>
        /// <param name="device">Dispositivo PCI de audio</param>
        static void RegisterAudioDriver(PCIDevice device)
        {
            //SerialDebug.Info($"Detecting audio device: VendorID=0x{((ulong)device.ID.VendorID).ToStringHex()}, DeviceID=0x{((ulong)device.ID.DeviceID).ToStringHex()}");

            // Intel AC97 Audio Controller
            if (device.ID.VendorID == 0x8086 && device.ID.DeviceID == 0x2415)
            {
                SerialDebug.Info("Detected Intel AC97 Audio Controller");
                AC97AudioDriver driver = new AC97AudioDriver(device);
                DriverManager.RegisterDriver(driver);
            }
            // Intel High Definition Audio Controller
            else if (device.ID.VendorID == 0x8086 &&
                    (device.ID.DeviceID == 0x2668 || device.ID.DeviceID == 0x27d8 ||
                     device.ID.DeviceID == 0x269a || device.ID.DeviceID == 0x284b))
            {
                SerialDebug.Info("Detected Intel HD Audio Controller (not supported yet)");
                // HDAAudioDriver driver = new HDAAudioDriver(device);
                // DriverManager.RegisterDriver(driver);
            }
            // Ensoniq AudioPCI (ES1370)
            else if (device.ID.VendorID == 0x1274 && device.ID.DeviceID == 0x5000)
            {
                SerialDebug.Info("Detected Ensoniq AudioPCI (ES1370)");
                // ES1370Driver driver = new ES1370Driver(device);
                // DriverManager.RegisterDriver(driver);
            }
            // Creative Sound Blaster
            else if (device.ID.VendorID == 0x1102)
            {
                SerialDebug.Info("Detected Creative Sound Blaster device");
                // SoundBlasterDriver driver = new SoundBlasterDriver(device);
                // DriverManager.RegisterDriver(driver);
            }
            // Genérico para cualquier otro dispositivo de audio
            else
            {
                //SerialDebug.Info($"Detected unknown audio device (Subclass: 0x{((ulong)device.ID.Subclass).ToStringHex()})");
                // Podrías intentar cargar un driver genérico según la subclase

                // Audio genérico
                if (device.ID.Subclass == 0x01)
                {
                    // GenericAudioDriver driver = new GenericAudioDriver(device);
                    // DriverManager.RegisterDriver(driver);
                }
            }
        }

        static void RegisterNetworkDriver(PCIDevice device)
        {
            // Drivers específicos para diferentes vendedores de red
            switch (device.ID.VendorID)
            {
                case 0x8086: // Intel
                    if (device.ID.DeviceID == 0x100E) // E1000
                    {
                       // RegisterDriver(new Intel8254XDriver(device));
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

        internal static void EnableMemorySpace(PCIDevice pciDevice)
        {
          
        }

        internal static void EnableBusMastering(PCIDevice pciDevice)
        {
         
        }
    }

    
    /// <summary>
    /// Abstract base class for basic driver implementation
    /// </summary>
    public abstract class BaseDriver : IDriver
    {
        /// <summary>
        /// Base constructor for drivers
        /// </summary>
        /// <param name="id">Unique identifier</param>
        /// <param name="name">Descriptive name</param>
        protected BaseDriver(string id, string name)
        {
            Id = id;
            Name = name;
            State = DriverState.Unloaded;
        }

        /// <summary>
        /// Default initialization method
        /// </summary>
        public override bool Initialize()
        {
            if (State == DriverState.Running)
                return true;

            State = DriverState.Initializing;

            // Specific initialization logic in derived classes
            bool success = OnInitialize();

            if (success)
            {
                State = DriverState.Running;
            }
            else
            {
                State = DriverState.Error;
            }

            return success;
        }

        /// <summary>
        /// Default shutdown method
        /// </summary>
        public override void Shutdown()
        {
            if (State == DriverState.Stopped)
                return;

            OnShutdown();
            State = DriverState.Stopped;
        }

        /// <summary>
        /// Method to be implemented by specific drivers for initialization
        /// </summary>
        protected virtual bool OnInitialize()
        {
            // Default implementation does nothing
            return true;
        }

        /// <summary>
        /// Method to be implemented by specific drivers for shutdown
        /// </summary>
        protected virtual void OnShutdown()
        {
            // Default implementation does nothing
        }
    }
}
