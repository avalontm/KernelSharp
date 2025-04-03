using Kernel.Diagnostics;
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
    public static class DriverManager
    {
        // Collection of registered drivers
        private static Dictionary<string, IDriver> _drivers;

        // Dictionary of drivers by type
        private static Dictionary<DriverType, List<IDriver>> _driversByType;

        /// <summary>
        /// Initializes the Driver Manager
        /// </summary>
        public static void Initialize()
        {
            SerialDebug.Info("Initializing Driver Manager...");
            _drivers = new Dictionary<string, IDriver>();
            _driversByType = new Dictionary<DriverType, List<IDriver>>();
            SerialDebug.Info("Driver Manager initialized successfully");
        }

        /// <summary>
        /// Registers a new driver in the system
        /// </summary>
        /// <param name="driver">Driver to register</param>
        public static void RegisterDriver(IDriver driver)
        {
            if (driver == null)
                return;

            // Prevent duplicates
            if (_drivers.ContainsKey(driver.Id))
            {
                SerialDebug.Warning($"Driver {driver.Id} already registered. Skipping.");
                return;
            }

            SerialDebug.Info("Registering driver: " + driver.Name);

            // Add to drivers dictionary
            _drivers[driver.Id] = driver;

            // Determine driver type
            DriverType type = DetermineDriverType(driver);

            // Add to list by type
            if (!_driversByType.ContainsKey(type))
            {
                _driversByType[type] = new List<IDriver>();
            }

            _driversByType[type].Add(driver);

            SerialDebug.Info($"Registered driver: {driver.Id} ({driver.Name})");
        }

        /// <summary>
        /// Initializes all registered drivers
        /// </summary>
        public static void InitializeAllDrivers()
        {
            SerialDebug.Info("Initializing all drivers...");

            // Initialization order by driver type priority
            DriverType[] initOrder = new DriverType[]
            {
                DriverType.Graphics,
                DriverType.Storage,
                DriverType.Network,
                DriverType.Input,
                DriverType.Audio,
                DriverType.Other
            };

            for (int typeIndex = 0; typeIndex < initOrder.Length; typeIndex++)
            {
                DriverType type = initOrder[typeIndex];
                List<IDriver> drivers = null;

                if (!_driversByType.TryGetValue(type, out drivers))
                    continue;

                for (int driverIndex = 0; driverIndex < drivers.Count; driverIndex++)
                {
                    IDriver driver = drivers[driverIndex];

                    SerialDebug.Info($"Initializing driver: {driver.Id}");
                    if (driver.Initialize())
                    {
                        SerialDebug.Info($"Driver initialized successfully: {driver.Id}");
                    }
                    else
                    {
                        SerialDebug.Warning($"Failed to initialize driver: {driver.Id}");
                    }
                }
            }

            SerialDebug.Info("All drivers initialization complete");
        }

        /// <summary>
        /// Gets a driver by its ID
        /// </summary>
        public static IDriver GetDriver(string id)
        {
            IDriver driver = null;
            _drivers.TryGetValue(id, out driver);
            return driver;
        }

        /// <summary>
        /// Gets all drivers of a specific type
        /// </summary>
        public static IDriver[] GetDriversByType(DriverType type)
        {
            List<IDriver> drivers = null;
            if (_driversByType.TryGetValue(type, out drivers))
            {
                return drivers.ToArray();
            }
            return new IDriver[0];
        }

        /// <summary>
        /// Determines the type of driver
        /// </summary>
        private static DriverType DetermineDriverType(IDriver driver)
        {
            // Logic to determine driver type
            // Can be expanded as needed
            string name = driver.Name.ToUpper();

            if (name.Contains("GRAPHICS") || name.Contains("VIDEO") || name.Contains("DISPLAY"))
                return DriverType.Graphics;

            if (name.Contains("NETWORK") || name.Contains("ETHERNET") || name.Contains("WIFI"))
                return DriverType.Network;

            if (name.Contains("STORAGE") || name.Contains("DISK") || name.Contains("IDE") ||
                name.Contains("SATA") || name.Contains("NVME"))
                return DriverType.Storage;

            if (name.Contains("INPUT") || name.Contains("KEYBOARD") || name.Contains("MOUSE"))
                return DriverType.Input;

            if (name.Contains("AUDIO") || name.Contains("SOUND"))
                return DriverType.Audio;

            return DriverType.Other;
        }

        /// <summary>
        /// Shuts down all drivers
        /// </summary>
        public static void ShutdownAllDrivers()
        {
            SerialDebug.Info("Shutting down all drivers...");

            // Get all drivers from dictionary
            int count = _drivers.Count;
            IDriver[] driversArray = new IDriver[count];
            int index = 0;

            // Extract all driver values directly without using CopyTo
            string[] keysArray = _drivers.Keys;
            for (int i = 0; i < keysArray.Length; i++)
            {
                IDriver driver = null;
                if (_drivers.TryGetValue(keysArray[i], out driver))
                {
                    driversArray[index++] = driver;
                }
            }

            // Shutdown each driver
            for (int i = 0; i < index; i++)
            {
                IDriver driver = driversArray[i];
                SerialDebug.Info($"Shutting down driver: {driver.Id}");
                driver.Shutdown();
                SerialDebug.Info($"Driver shut down: {driver.Id}");
            }

            SerialDebug.Info("All drivers shutdown complete");
        }

        /// <summary>
        /// Enables memory space for a PCI device
        /// </summary>
        public static void EnableMemorySpace(PCIDevice pciDevice)
        {
            if (pciDevice == null)
                return;

            // Read current command register
            ushort command = PCIManager.ReadConfig16(
                pciDevice.Location.Bus,
                pciDevice.Location.Device,
                pciDevice.Location.Function,
                0x04 // Command register
            );

            // Enable memory space bit (bit 1)
            command |= 0x02;

            // Write updated command register
            PCIManager.WriteConfig16(
                pciDevice.Location.Bus,
                pciDevice.Location.Device,
                pciDevice.Location.Function,
                0x04, // Command register
                command
            );

            SerialDebug.Info($"Memory space enabled for device {pciDevice.Location.ToString()}");
        }

        /// <summary>
        /// Enables bus mastering for a PCI device
        /// </summary>
        public static void EnableBusMastering(PCIDevice pciDevice)
        {
            if (pciDevice == null)
                return;

            // Read current command register
            ushort command = PCIManager.ReadConfig16(
                pciDevice.Location.Bus,
                pciDevice.Location.Device,
                pciDevice.Location.Function,
                0x04 // Command register
            );

            // Enable bus mastering bit (bit 2)
            command |= 0x04;

            // Write updated command register
            PCIManager.WriteConfig16(
                pciDevice.Location.Bus,
                pciDevice.Location.Device,
                pciDevice.Location.Function,
                0x04, // Command register
                command
            );

            SerialDebug.Info($"Bus mastering enabled for device {pciDevice.Location.ToString()}");
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
        protected abstract bool OnInitialize();

        /// <summary>
        /// Method to be implemented by specific drivers for shutdown
        /// </summary>
        protected abstract void OnShutdown();
    }
}