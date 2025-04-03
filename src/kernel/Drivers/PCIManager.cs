using Kernel.Diagnostics;
using Kernel.Drivers.IO;
using System.Collections.Generic;

namespace Kernel.Drivers
{
    /// <summary>
    /// Manager for PCI bus operations
    /// </summary>
    public static unsafe class PCIManager
    {
        // PCI configuration space access ports
        private const ushort PCI_CONFIG_ADDRESS = 0xCF8;
        private const ushort PCI_CONFIG_DATA = 0xCFC;

        // PCI configuration space registers
        private const byte PCI_REGISTER_VENDOR_ID = 0x00;
        private const byte PCI_REGISTER_DEVICE_ID = 0x02;
        private const byte PCI_REGISTER_COMMAND = 0x04;
        private const byte PCI_REGISTER_STATUS = 0x06;
        private const byte PCI_REGISTER_REVISION_ID = 0x08;
        private const byte PCI_REGISTER_PROG_IF = 0x09;
        private const byte PCI_REGISTER_SUBCLASS = 0x0A;
        private const byte PCI_REGISTER_CLASS_CODE = 0x0B;
        private const byte PCI_REGISTER_CACHE_LINE_SIZE = 0x0C;
        private const byte PCI_REGISTER_LATENCY_TIMER = 0x0D;
        private const byte PCI_REGISTER_HEADER_TYPE = 0x0E;
        private const byte PCI_REGISTER_BIST = 0x0F;
        private const byte PCI_REGISTER_BAR0 = 0x10;
        private const byte PCI_REGISTER_BAR1 = 0x14;
        private const byte PCI_REGISTER_BAR2 = 0x18;
        private const byte PCI_REGISTER_BAR3 = 0x1C;
        private const byte PCI_REGISTER_BAR4 = 0x20;
        private const byte PCI_REGISTER_BAR5 = 0x24;
        private const byte PCI_REGISTER_CARDBUS_CIS_PTR = 0x28;
        private const byte PCI_REGISTER_SUBSYSTEM_VENDOR_ID = 0x2C;
        private const byte PCI_REGISTER_SUBSYSTEM_ID = 0x2E;
        private const byte PCI_REGISTER_EXPANSION_ROM_BASE = 0x30;
        private const byte PCI_REGISTER_CAPABILITIES_PTR = 0x34;
        private const byte PCI_REGISTER_INTERRUPT_LINE = 0x3C;
        private const byte PCI_REGISTER_INTERRUPT_PIN = 0x3D;
        private const byte PCI_REGISTER_MIN_GNT = 0x3E;
        private const byte PCI_REGISTER_MAX_LAT = 0x3F;

        // PCI bridge specific registers
        private const byte PCI_REGISTER_PRIMARY_BUS = 0x18;
        private const byte PCI_REGISTER_SECONDARY_BUS = 0x19;
        private const byte PCI_REGISTER_SUBORDINATE_BUS = 0x1A;

        // Command register bits
        private const ushort PCI_COMMAND_IO_SPACE = 0x0001;
        private const ushort PCI_COMMAND_MEMORY_SPACE = 0x0002;
        private const ushort PCI_COMMAND_BUS_MASTER = 0x0004;
        private const ushort PCI_COMMAND_SPECIAL_CYCLES = 0x0008;
        private const ushort PCI_COMMAND_MWI_ENABLE = 0x0010;
        private const ushort PCI_COMMAND_VGA_PALETTE_SNOOP = 0x0020;
        private const ushort PCI_COMMAND_PARITY_ERROR_RESPONSE = 0x0040;
        private const ushort PCI_COMMAND_SERR_ENABLE = 0x0100;
        private const ushort PCI_COMMAND_FAST_BACK_TO_BACK = 0x0200;
        private const ushort PCI_COMMAND_INTERRUPT_DISABLE = 0x0400;

        // List of detected PCI devices
        private static List<PCIDevice> _devices;

        /// <summary>
        /// Initialize PCI bus detection
        /// </summary>
        public static void Initialize()
        {
            SerialDebug.Info("Initializing PCI device detection...");
            _devices = new List<PCIDevice>();
            // Scan all buses
            ScanAllBuses();
            // Print detected devices
            PrintDevices();
        }

        /// <summary>
        /// Get list of all detected PCI devices
        /// </summary>
        public static List<PCIDevice> GetDevices()
        {
            return _devices;
        }

        /// <summary>
        /// Find devices by class and subclass
        /// </summary>
        public static List<PCIDevice> FindDevicesByClass(byte classCode, byte subclass)
        {
            List<PCIDevice> result = new List<PCIDevice>();

            for (int i = 0; i < _devices.Count; i++)
            {
                if (_devices[i].ID.ClassCode == classCode && _devices[i].ID.Subclass == subclass)
                {
                    result.Add(_devices[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Find device by vendor and device ID
        /// </summary>
        public static PCIDevice FindDevice(ushort vendorID, ushort deviceID)
        {
            for (int i = 0; i < _devices.Count; i++)
            {
                if (_devices[i].ID.VendorID == vendorID && _devices[i].ID.DeviceID == deviceID)
                {
                    return _devices[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Find device by bus, device, and function numbers
        /// </summary>
        public static PCIDevice FindDevice(byte bus, byte device, byte function)
        {
            for (int i = 0; i < _devices.Count; i++)
            {
                if (_devices[i].Location.Bus == bus &&
                    _devices[i].Location.Device == device &&
                    _devices[i].Location.Function == function)
                {
                    return _devices[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Find device by class ID and subclass ID
        /// </summary>
        public static PCIDevice FindDeviceByClass(PCIClassID classID, byte subclassID)
        {
            for (int i = 0; i < _devices.Count; i++)
            {
                if (_devices[i].ID.ClassCode == (byte)classID &&
                    _devices[i].ID.Subclass == subclassID)
                {
                    return _devices[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Read 8-bit value from device's PCI configuration space
        /// </summary>
        public static byte ReadConfig8(byte bus, byte device, byte function, byte offset)
        {
            uint address = GetAddressBase(bus, device, function) | (uint)(offset & 0xFC);
            IOPort.OutDword(PCI_CONFIG_ADDRESS, address);
            byte shift = (byte)((offset & 3) * 8);
            return (byte)(IOPort.InDword(PCI_CONFIG_DATA) >> shift & 0xFF);
        }

        /// <summary>
        /// Read 16-bit value from device's PCI configuration space
        /// </summary>
        public static ushort ReadConfig16(byte bus, byte device, byte function, byte offset)
        {
            uint address = GetAddressBase(bus, device, function) | (uint)(offset & 0xFC);
            IOPort.OutDword(PCI_CONFIG_ADDRESS, address);
            byte shift = (byte)((offset & 2) * 8);
            return (ushort)(IOPort.InDword(PCI_CONFIG_DATA) >> shift & 0xFFFF);
        }

        /// <summary>
        /// Read 32-bit value from device's PCI configuration space
        /// </summary>
        public static uint ReadConfig32(byte bus, byte device, byte function, byte offset)
        {
            uint address = GetAddressBase(bus, device, function) | (uint)(offset & 0xFC);
            IOPort.OutDword(PCI_CONFIG_ADDRESS, address);
            return IOPort.InDword(PCI_CONFIG_DATA);
        }

        /// <summary>
        /// Write 8-bit value to device's PCI configuration space
        /// </summary>
        public static void WriteConfig8(byte bus, byte device, byte function, byte offset, byte value)
        {
            uint address = GetAddressBase(bus, device, function) | (uint)(offset & 0xFC);
            IOPort.OutDword(PCI_CONFIG_ADDRESS, address);

            uint data = IOPort.InDword(PCI_CONFIG_DATA);
            byte shift = (byte)((offset & 3) * 8);
            uint mask = ~(0xFFU << shift);
            data = data & mask | (uint)value << shift;

            IOPort.OutDword(PCI_CONFIG_DATA, data);
        }

        /// <summary>
        /// Write 16-bit value to device's PCI configuration space
        /// </summary>
        public static void WriteConfig16(byte bus, byte device, byte function, byte offset, ushort value)
        {
            uint address = GetAddressBase(bus, device, function) | (uint)(offset & 0xFC);
            IOPort.OutDword(PCI_CONFIG_ADDRESS, address);

            uint data = IOPort.InDword(PCI_CONFIG_DATA);
            byte shift = (byte)((offset & 2) * 8);
            uint mask = ~(0xFFFFU << shift);
            data = data & mask | (uint)value << shift;

            IOPort.OutDword(PCI_CONFIG_DATA, data);
        }

        /// <summary>
        /// Write 32-bit value to device's PCI configuration space
        /// </summary>
        public static void WriteConfig32(byte bus, byte device, byte function, byte offset, uint value)
        {
            uint address = GetAddressBase(bus, device, function) | (uint)(offset & 0xFC);
            IOPort.OutDword(PCI_CONFIG_ADDRESS, address);
            IOPort.OutDword(PCI_CONFIG_DATA, value);
        }

        /// <summary>
        /// Enable bus mastering for the device
        /// </summary>
        public static void EnableBusMastering(PCIDevice device)
        {
            ushort command = ReadConfig16(device.Location.Bus, device.Location.Device, device.Location.Function, PCI_REGISTER_COMMAND);
            command |= PCI_COMMAND_BUS_MASTER;
            WriteConfig16(device.Location.Bus, device.Location.Device, device.Location.Function, PCI_REGISTER_COMMAND, command);
        }

        /// <summary>
        /// Enable memory space for the device
        /// </summary>
        public static void EnableMemorySpace(PCIDevice device)
        {
            ushort command = ReadConfig16(device.Location.Bus, device.Location.Device, device.Location.Function, PCI_REGISTER_COMMAND);
            command |= PCI_COMMAND_MEMORY_SPACE;
            WriteConfig16(device.Location.Bus, device.Location.Device, device.Location.Function, PCI_REGISTER_COMMAND, command);
        }

        /// <summary>
        /// Enable I/O space for the device
        /// </summary>
        public static void EnableIOSpace(PCIDevice device)
        {
            ushort command = ReadConfig16(device.Location.Bus, device.Location.Device, device.Location.Function, PCI_REGISTER_COMMAND);
            command |= PCI_COMMAND_IO_SPACE;
            WriteConfig16(device.Location.Bus, device.Location.Device, device.Location.Function, PCI_REGISTER_COMMAND, command);
        }

        /// <summary>
        /// Get address base for a PCI device
        /// </summary>
        private static uint GetAddressBase(byte bus, byte device, byte function)
        {
            return (uint)(0x80000000 | bus << 16 | (device & 0x1F) << 11 | (function & 0x07) << 8);
        }

        /// <summary>
        /// Check if device exists at the specified location
        /// </summary>
        private static bool DeviceExists(byte bus, byte device, byte function)
        {
            ushort vendorID = ReadConfig16(bus, device, function, PCI_REGISTER_VENDOR_ID);
            return vendorID != 0xFFFF;
        }

        /// <summary>
        /// Get information about the device at the specified location
        /// </summary>
        private static PCIDevice GetDeviceInfo(byte bus, byte device, byte function)
        {
            PCIDeviceID id = new PCIDeviceID
            {
                VendorID = ReadConfig16(bus, device, function, PCI_REGISTER_VENDOR_ID),
                DeviceID = ReadConfig16(bus, device, function, PCI_REGISTER_DEVICE_ID),
                RevisionID = ReadConfig8(bus, device, function, PCI_REGISTER_REVISION_ID),
                ProgIF = ReadConfig8(bus, device, function, PCI_REGISTER_PROG_IF),
                Subclass = ReadConfig8(bus, device, function, PCI_REGISTER_SUBCLASS),
                ClassCode = ReadConfig8(bus, device, function, PCI_REGISTER_CLASS_CODE),
                SubsystemVendorID = ReadConfig16(bus, device, function, PCI_REGISTER_SUBSYSTEM_VENDOR_ID),
                SubsystemID = ReadConfig16(bus, device, function, PCI_REGISTER_SUBSYSTEM_ID)
            };

            PCILocation location = new PCILocation
            {
                Bus = bus,
                Device = device,
                Function = function
            };

            byte headerType = ReadConfig8(bus, device, function, PCI_REGISTER_HEADER_TYPE);
            byte interruptLine = ReadConfig8(bus, device, function, PCI_REGISTER_INTERRUPT_LINE);
            byte interruptPin = ReadConfig8(bus, device, function, PCI_REGISTER_INTERRUPT_PIN);

            // Read Base Address Registers (BARs)
            uint[] bars = new uint[6];
            bars[0] = ReadConfig32(bus, device, function, PCI_REGISTER_BAR0);
            bars[1] = ReadConfig32(bus, device, function, PCI_REGISTER_BAR1);
            bars[2] = ReadConfig32(bus, device, function, PCI_REGISTER_BAR2);
            bars[3] = ReadConfig32(bus, device, function, PCI_REGISTER_BAR3);
            bars[4] = ReadConfig32(bus, device, function, PCI_REGISTER_BAR4);
            bars[5] = ReadConfig32(bus, device, function, PCI_REGISTER_BAR5);

            // Get bridge-specific information
            bool isBridge = id.ClassCode == 0x06 && id.Subclass == 0x04; // PCI-to-PCI bridge
            byte secondaryBus = 0;
            byte subordinateBus = 0;

            if (isBridge && (headerType & 0x7F) == 1) // Header type 1 is PCI-to-PCI bridge
            {
                secondaryBus = ReadConfig8(bus, device, function, PCI_REGISTER_SECONDARY_BUS);
                subordinateBus = ReadConfig8(bus, device, function, PCI_REGISTER_SUBORDINATE_BUS);
            }

            return new PCIDevice(id, location, headerType, interruptLine, interruptPin, bars, isBridge, secondaryBus, subordinateBus);
        }

        /// <summary>
        /// Check if the device has multiple functions
        /// </summary>
        private static bool IsMultiFunction(byte headerType)
        {
            return (headerType & 0x80) != 0;
        }

        /// <summary>
        /// Scan a PCI bus for devices
        /// </summary>
        private static void ScanBus(byte bus)
        {
            for (byte device = 0; device < 32; device++)
            {
                // Check if device
                if (!DeviceExists(bus, device, 0))
                    continue;

                // Process function 0
                ProcessFunction(bus, device, 0);

                // Check if this is a multi-function device
                byte headerType = ReadConfig8(bus, device, 0, PCI_REGISTER_HEADER_TYPE);
                if (IsMultiFunction(headerType))
                {
                    // Process other functions
                    for (byte function = 1; function < 8; function++)
                    {
                        if (DeviceExists(bus, device, function))
                        {
                            ProcessFunction(bus, device, function);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process a PCI function
        /// </summary>
        private static void ProcessFunction(byte bus, byte device, byte function)
        {
            // Create a unique device location ID
            PCIDevice pciDevice = GetDeviceInfo(bus, device, function);   

            _devices.Add(pciDevice);

            // If this is a PCI-to-PCI bridge, scan the secondary bus if not already processed
            if (pciDevice.IsBridge)
            {
                ScanBus(pciDevice.SecondaryBus);
            }
        }

        /// <summary>
        /// Scan all PCI buses
        /// </summary>
        private static void ScanAllBuses()
        {
            SerialDebug.Info("Scanning all PCI buses");

            // Check if PCI bus exists
            if (!DeviceExists(0, 0, 0))
            {
                SerialDebug.Warning("No PCI bus found!");
                return;
            }

            // Check if this is a multi-function host controller
            byte headerType = ReadConfig8(0, 0, 0, PCI_REGISTER_HEADER_TYPE);
            if (IsMultiFunction(headerType))
            {
                // Multiple PCI host controllers - scan each one's function as a separate bus
                for (byte function = 0; function < 8; function++)
                {
                    if (DeviceExists(0, 0, function))
                    {
                        ScanBus(function);
                    }
                }
            }
            else
            {
                // Single PCI host controller - scan bus 0
                ScanBus(0);
            }
        }

        /// <summary>
        /// Print detected PCI devices
        /// </summary>
        private static void PrintDevices()
        {
            SerialDebug.Info($"Detected {_devices.Count.ToString()} PCI devices:");

            for (int i = 0; i < _devices.Count; i++)
            {
                PCIDevice device = _devices[i];
                SerialDebug.Info($"  {device.ToString()}");
            }
        }
    }
}