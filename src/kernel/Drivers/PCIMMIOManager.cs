using Kernel.Diagnostics;
using Kernel.Hardware;
using Kernel.Memory;
using System.Collections.Generic;

namespace Kernel.Drivers
{
    /// <summary>
    /// PCI Manager implementation using Memory-Mapped I/O (MMIO) for configuration space access
    /// </summary>
    public static unsafe class PCIMMIOManager
    {
        // ACPI enhanced configuration space base address is typically provided by MCFG table
        private static ulong _mmioBaseAddress;
        private static bool _initialized;

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
        /// Initialize PCI detection using Memory-Mapped I/O
        /// </summary>
        public static void Initialize()
        {
            SerialDebug.Info("Initializing PCI MMIO Manager...");

            if (_initialized)
            {
                SerialDebug.Info("PCI MMIO Manager already initialized");
                return;
            }

            _devices = new List<PCIDevice>();

            // Try to get the MMIO base address from ACPI MCFG table
            _mmioBaseAddress = GetPCIMMIOBaseAddress();

            if (_mmioBaseAddress == 0)
            {
                SerialDebug.Warning("Could not obtain PCI MMIO base address from ACPI");
                // Default base address - this may vary by system, so it's better to get it from ACPI
                _mmioBaseAddress = 0xE0000000;
                //SerialDebug.Info("Using default PCI MMIO base address: 0x" + _mmioBaseAddress.ToStringHex());
            }
            else
            {
               // SerialDebug.Info("Found PCI MMIO base address: 0x" + _mmioBaseAddress.ToStringHex());
            }

            // Make sure the MMIO base address is mapped in memory
            // This is necessary only if your memory manager requires explicit mapping
            // In a flat memory model with identity paging, this may not be needed
            // MapPCIMmioRegion(_mmioBaseAddress, 256 * 1024 * 1024);  // 256MB range

            SerialDebug.Info("Scanning PCI buses via MMIO...");
            _initialized = true;

            // Scan all PCI buses
            ScanAllBuses();

            //SerialDebug.Info("MMIO PCI detection complete. Found " + _devices.Count + " devices");
        }

        /// <summary>
        /// Get the MMIO base address from ACPI MCFG table
        /// This is a simplified implementation - in a real system, you'd parse the ACPI tables
        /// </summary>
        private static ulong GetPCIMMIOBaseAddress()
        {
            // Try to get the base address from ACPI manager
            ulong mcfgAddr = ACPIManager.GetMCFGBaseAddress();
            if (mcfgAddr != 0)
            {
                return mcfgAddr;
            }

            // If ACPI detection didn't work, try some common values
            // These are used by various virtualization platforms
            ulong[] commonAddresses = {
                0xE0000000,  // Common in many systems
                0xF0000000,  // Another common value
                0xC0000000,  // Seen in some virtual machines
                0x80000000   // Another possibility
            };

            // Test each address by trying to read from a known device location (0:0:0)
            foreach (ulong addr in commonAddresses)
            {
                // Create a temporary pointer to the potential MMIO region
                byte* testPtr = (byte*)(addr + GetDeviceOffset(0, 0, 0));

                // Try to read the vendor ID
                ushort vendorID = *(ushort*)testPtr;

                // If we get a valid vendor ID (not 0xFFFF), it might be a working MMIO region
                if (vendorID != 0xFFFF && vendorID != 0)
                {
                   // SerialDebug.Info("Detected potential PCI MMIO base at 0x" + addr.ToStringHex() + " (VendorID: 0x" + ((ulong)vendorID).ToStringHex() + ")");
                    return addr;
                }
            }

            return 0;
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
        /// Calculate the device offset in MMIO space
        /// </summary>
        private static ulong GetDeviceOffset(byte bus, byte device, byte function)
        {
            return ((ulong)bus << 20) | ((ulong)device << 15) | ((ulong)function << 12);
        }

        /// <summary>
        /// Read 8-bit value from device's PCI configuration space using MMIO
        /// </summary>
        public static byte ReadConfig8(byte bus, byte device, byte function, byte offset)
        {
            if (!_initialized)
                return 0xFF;

            ulong deviceAddress = _mmioBaseAddress + GetDeviceOffset(bus, device, function);
            byte* ptr = (byte*)(deviceAddress + offset);
            return *ptr;
        }

        /// <summary>
        /// Read 16-bit value from device's PCI configuration space using MMIO
        /// </summary>
        public static ushort ReadConfig16(byte bus, byte device, byte function, byte offset)
        {
            if (!_initialized)
                return 0xFFFF;

            ulong deviceAddress = _mmioBaseAddress + GetDeviceOffset(bus, device, function);
            ushort* ptr = (ushort*)(deviceAddress + offset);
            return *ptr;
        }

        /// <summary>
        /// Read 32-bit value from device's PCI configuration space using MMIO
        /// </summary>
        public static uint ReadConfig32(byte bus, byte device, byte function, byte offset)
        {
            if (!_initialized)
                return 0xFFFFFFFF;

            ulong deviceAddress = _mmioBaseAddress + GetDeviceOffset(bus, device, function);
            uint* ptr = (uint*)(deviceAddress + offset);
            return *ptr;
        }

        /// <summary>
        /// Write 8-bit value to device's PCI configuration space using MMIO
        /// </summary>
        public static void WriteConfig8(byte bus, byte device, byte function, byte offset, byte value)
        {
            if (!_initialized)
                return;

            ulong deviceAddress = _mmioBaseAddress + GetDeviceOffset(bus, device, function);
            byte* ptr = (byte*)(deviceAddress + offset);
            *ptr = value;
        }

        /// <summary>
        /// Write 16-bit value to device's PCI configuration space using MMIO
        /// </summary>
        public static void WriteConfig16(byte bus, byte device, byte function, byte offset, ushort value)
        {
            if (!_initialized)
                return;

            ulong deviceAddress = _mmioBaseAddress + GetDeviceOffset(bus, device, function);
            ushort* ptr = (ushort*)(deviceAddress + offset);
            *ptr = value;
        }

        /// <summary>
        /// Write 32-bit value to device's PCI configuration space using MMIO
        /// </summary>
        public static void WriteConfig32(byte bus, byte device, byte function, byte offset, uint value)
        {
            if (!_initialized)
                return;

            ulong deviceAddress = _mmioBaseAddress + GetDeviceOffset(bus, device, function);
            uint* ptr = (uint*)(deviceAddress + offset);
            *ptr = value;
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
        /// Check if device exists at the specified location
        /// </summary>
        private static bool DeviceExists(byte bus, byte device, byte function)
        {
            // Read the vendor ID and check if it's a valid value
            ushort vendorID = ReadConfig16(bus, device, function, PCI_REGISTER_VENDOR_ID);
            return vendorID != 0xFFFF && vendorID != 0;
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
            //SerialDebug.Info("Scanning PCI bus " + bus + " via MMIO");

            // In MMIO mode, we can efficiently check multiple devices
            for (byte device = 0; device < 32; device++)
            {
                // Check if device exists
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
            PCIDevice pciDevice = GetDeviceInfo(bus, device, function);

            // Log device information
           // SerialDebug.Info("Found PCI device at " + bus + ":" + device + ":" + function + " - VID=0x" +
                       //    ((ulong)pciDevice.ID.VendorID).ToStringHex() + ", DID=0x" + ((ulong)pciDevice.ID.DeviceID).ToStringHex() + ", " +
                       //    "Class=0x" + ((ulong)pciDevice.ID.ClassCode).ToStringHex() + ", Subclass=0x" + ((ulong)pciDevice.ID.Subclass).ToStringHex());

            _devices.Add(pciDevice);

            // If this is a PCI-to-PCI bridge, scan the secondary bus
            if (pciDevice.IsBridge)
            {
                //SerialDebug.Info("PCI bridge found, scanning secondary bus " + pciDevice.SecondaryBus);
                ScanBus(pciDevice.SecondaryBus);
            }
        }

        /// <summary>
        /// Scan all PCI buses
        /// </summary>
        private static void ScanAllBuses()
        {
            if (!_initialized)
            {
                SerialDebug.Warning("PCI MMIO Manager not initialized");
                return;
            }

            SerialDebug.Info("Scanning all PCI buses via MMIO");

            // First, check if we can access bus 0, device 0, function 0
            if (!DeviceExists(0, 0, 0))
            {
                SerialDebug.Warning("Cannot access PCI configuration space via MMIO");

                // Perform a more comprehensive search across multiple buses
                bool foundAnyDevice = false;

                // Check buses 0-15 for any devices
                for (byte bus = 0; bus < 16 && !foundAnyDevice; bus++)
                {
                    SerialDebug.Info("Trying PCI bus " + bus);

                    for (byte device = 0; device < 32 && !foundAnyDevice; device++)
                    {
                        // Only check first function to save time
                        if (DeviceExists(bus, device, 0))
                        {
                            foundAnyDevice = true;
                            //SerialDebug.Info("Found first PCI device at " + bus + ":" + device + ":0");

                            // Scan from this point
                            ScanBus(bus);
                        }
                    }
                }

                if (!foundAnyDevice)
                {
                    SerialDebug.Warning("No PCI devices found via MMIO after comprehensive search");
                }

                return;
            }

            // Check if this host has multiple PCI domains
            byte headerType = ReadConfig8(0, 0, 0, PCI_REGISTER_HEADER_TYPE);

            if (IsMultiFunction(headerType))
            {
                // Multiple PCI host controllers/domains
                SerialDebug.Info("Multiple PCI domains detected");

                for (byte function = 0; function < 8; function++)
                {
                    if (DeviceExists(0, 0, function))
                    {
                        // Each function represents a separate PCI domain with its own bus 0
                        ScanBus(function);
                    }
                }
            }
            else
            {
                // Single PCI domain - scan bus 0
                SerialDebug.Info("Single PCI domain detected");
                ScanBus(0);
            }
        }

        /// <summary>
        /// Dump the raw PCI configuration space for a specific device for diagnostic purposes
        /// </summary>
        public static void DumpDeviceConfigSpace(byte bus, byte device, byte function)
        {
            if (!_initialized)
            {
                SerialDebug.Warning("PCI MMIO Manager not initialized");
                return;
            }

            if (!DeviceExists(bus, device, function))
            {
               // SerialDebug.Warning("No device exists at " + bus + ":" + device + ":" + function);
                return;
            }

            //SerialDebug.Info("Dumping PCI configuration space for device " + bus + ":" + device + ":" + function);

            // Read the first 64 bytes (header)
           // SerialDebug.Info("Header:");
            for (byte offset = 0; offset < 64; offset += 4)
            {
                uint value = ReadConfig32(bus, device, function, offset);
               // SerialDebug.Info("  Offset 0x" + ((ulong)offset).ToStringHex() + ": 0x" + ((ulong)value).ToStringHex());
            }

            // The rest of the configuration space (typically up to 256 bytes)
            SerialDebug.Info("Extended Configuration:");
            for (byte offset = 64; offset < 192; offset += 16)
            {
                string line = "  Offset 0x" + ((ulong)offset).ToStringHex() + ":";

                for (byte j = 0; j < 16; j += 4)
                {
                    uint value = ReadConfig32(bus, device, function, (byte)(offset + j));
                    line += " 0x" + ((ulong)value).ToStringHex();
                }

              //  SerialDebug.Info(line);
            }
        }

    }
}