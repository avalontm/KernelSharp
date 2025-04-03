using Kernel.Diagnostics;
using Kernel.Hardware;
using System.Collections.Generic;

namespace Kernel.Drivers
{
    /// <summary>
    /// Representa un driver básico en el sistema
    /// </summary>
    public abstract class IDriver
    {
        /// <summary>
        /// Identificador único del driver
        /// </summary>
        public string Id { get; internal set; }

        /// <summary>
        /// Nombre descriptivo del driver
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Estado actual del driver
        /// </summary>
        public DriverState State { get; internal set; }

        /// <summary>
        /// Inicializa el driver
        /// </summary>
        /// <returns>True si la inicialización fue exitosa</returns>
        public virtual bool Initialize()
        {
            return false;
        }


        /// <summary>
        /// Detiene y limpia recursos del driver
        /// </summary>
        public virtual void Shutdown()
        {

        }
    }

    /// <summary>
    /// Estados posibles de un driver
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
    /// Tipos de drivers
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
    /// Gestor de drivers del sistema
    /// </summary>
    public static class DriverManager
    {
        // Colección de drivers registrados
        private static Dictionary<string, IDriver> _drivers;

        // Diccionario de drivers por tipo
        private static Dictionary<DriverType, List<IDriver>> _driversByType;

        public static void Initialize()
        {
            SerialDebug.Info("DriverManager Initialize...");
            _drivers = new Dictionary<string, IDriver>();
            _driversByType = new Dictionary<DriverType, List<IDriver>>();
        }

        /// <summary>
        /// Registra un nuevo driver en el sistema
        /// </summary>
        /// <param name="driver">Driver a registrar</param>
        public static void RegisterDriver(IDriver driver)
        {
            if (driver == null)
                return;
            // Prevenir duplicados
            if (_drivers.ContainsKey(driver.Id))
            {
                SerialDebug.Warning($"Driver {driver.Id} already registered. Skipping.");
                return;
            }
            SerialDebug.Info("RegisterDriver " + driver.Name);
            // Agregar al diccionario de drivers
            _drivers[driver.Id] = driver;
            SerialDebug.Info("RegisterDriver PASO 4");

            // Determinar tipo de driver
            DriverType type = DetermineDriverType(driver);

            // Agregar a la lista por tipo
            if (!_driversByType.ContainsKey(type))
                _driversByType[type] = new List<IDriver>();

            _driversByType[type].Add(driver);

            SerialDebug.Info($"Registered driver: {driver.Id} ({driver.Name})");

        }

        /// <summary>
        /// Inicializa todos los drivers registrados
        /// </summary>
        public static void InitializeAllDrivers()
        {
            SerialDebug.Info("Initializing all drivers...");

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

                if (!_driversByType.TryGetValue(type, out List<IDriver> drivers))
                    continue;

                for (int driverIndex = 0; driverIndex < drivers.Count; driverIndex++)
                {
                    IDriver driver = drivers[driverIndex];

                    if (driver.Initialize())
                    {
                        SerialDebug.Info($"Driver initialized: {driver.Id}");
                    }
                    else
                    {
                        SerialDebug.Warning($"Failed to initialize driver: {driver.Id}");
                    }
                }
            }
        }

        /// <summary>
        /// Obtiene un driver por su ID
        /// </summary>
        public static IDriver GetDriver(string id)
        {
            _drivers.TryGetValue(id, out IDriver driver);
            return driver;
        }

        /// <summary>
        /// Obtiene todos los drivers de un tipo específico
        /// </summary>
        public static IDriver[] GetDriversByType(DriverType type)
        {
            if (_driversByType.TryGetValue(type, out List<IDriver> drivers))
            {
                return drivers.ToArray();
            }
            return new IDriver[0];
        }

        /// <summary>
        /// Determina el tipo de driver
        /// </summary>
        private static DriverType DetermineDriverType(IDriver driver)
        {
            // Lógica para determinar el tipo de driver
            // Puede ser expandida según sea necesario
            if (driver.Name.Contains("Graphics") || driver.Name.Contains("Video"))
                return DriverType.Graphics;

            if (driver.Name.Contains("Network") || driver.Name.Contains("Ethernet"))
                return DriverType.Network;

            if (driver.Name.Contains("Storage") || driver.Name.Contains("Disk") || driver.Name.Contains("IDE"))
                return DriverType.Storage;

            return DriverType.Other;
        }

        /// <summary>
        /// Apaga todos los drivers
        /// </summary>
        public static void ShutdownAllDrivers()
        {
            SerialDebug.Info("Shutting down all drivers...");

            // Convertir valores del diccionario a array
            IDriver[] drivers = new IDriver[_drivers.Count];
            int driverCount = 0;
            
            // Usar for para iterar claves del diccionario
            for (int i = 0; i < _drivers.Keys.Length; i++)
            {
                string key = _drivers.Keys[i];
                drivers[driverCount++] = _drivers[key];
            }
            
            // Apagar drivers usando for
            for (int i = 0; i < driverCount; i++)
            {
                IDriver driver = drivers[i];
                driver.Shutdown();
                SerialDebug.Info($"Driver shut down: {driver.Id}");
            }
        }

        public static void EnableMemorySpace(PCIDevice pciDevice)
        {
            if (pciDevice == null)
                return;

            // Leer registro de comando actual
            ushort command = PCIManager.ReadConfig16(
                pciDevice.Location.Bus,
                pciDevice.Location.Device,
                pciDevice.Location.Function,
                0x04 // Registro de comando
            );

            // Habilitar bit de espacio de memoria (bit 1)
            command |= 0x02;

            // Escribir registro de comando actualizado
            PCIManager.WriteConfig16(
                pciDevice.Location.Bus,
                pciDevice.Location.Device,
                pciDevice.Location.Function,
                0x04, // Registro de comando
                command
            );

            SerialDebug.Info($"Memory space enabled for device {pciDevice.Location.ToString()}");
        }

        public static void EnableBusMastering(PCIDevice pciDevice)
        {
            if (pciDevice == null)
                return;

            // Leer registro de comando actual
            ushort command = PCIManager.ReadConfig16(
                pciDevice.Location.Bus,
                pciDevice.Location.Device,
                pciDevice.Location.Function,
                0x04 // Registro de comando
            );

            // Habilitar bit de bus mastering (bit 2)
            command |= 0x04;

            // Escribir registro de comando actualizado
            PCIManager.WriteConfig16(
                pciDevice.Location.Bus,
                pciDevice.Location.Device,
                pciDevice.Location.Function,
                0x04, // Registro de comando
                command
            );

            SerialDebug.Info($"Bus mastering enabled for device {pciDevice.Location.ToString()}");
        }
    }

    /// <summary>
    /// Clase base abstracta para implementación básica de drivers
    /// </summary>
    public abstract class BaseDriver : IDriver
    {
        /// <summary>
        /// Constructor base para drivers
        /// </summary>
        /// <param name="id">Identificador único</param>
        /// <param name="name">Nombre descriptivo</param>
        protected BaseDriver(string id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <summary>
        /// Método de inicialización por defecto
        /// </summary>
        public override bool Initialize()
        {
            if (State == DriverState.Running)
                return true;

            State = DriverState.Initializing;
            // Lógica de inicialización específica en clases derivadas
            OnInitialize();
            State = DriverState.Running;
            return true;
        }

        /// <summary>
        /// Método de apagado por defecto
        /// </summary>
        public override void Shutdown()
        {
            if (State == DriverState.Stopped)
                return;

            OnShutdown();
            State = DriverState.Stopped;
        }

        /// <summary>
        /// Método a implementar por drivers específicos para inicialización
        /// </summary>
        protected abstract void OnInitialize();

        /// <summary>
        /// Método a implementar por drivers específicos para apagado
        /// </summary>
        protected abstract void OnShutdown();
    }
}