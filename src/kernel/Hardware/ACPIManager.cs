using Kernel.Diagnostics;
using System;
using System.Runtime.InteropServices;

namespace Kernel.Hardware
{
    /// <summary>
    /// Basic ACPI (Advanced Configuration and Power Interface) Manager
    /// </summary>
    public static unsafe class ACPIManager
    {
        // Constants for ACPI table search
        private const ulong ACPI_SEARCH_START = 0x000E0000;
        private const ulong ACPI_SEARCH_END = 0x000FFFFF;
        private const string RSDP_SIGNATURE = "RSD PTR ";

        // Common table signatures
        private const uint APIC_SIGNATURE = 0x43495041; // "APIC"
        private const uint FACP_SIGNATURE = 0x50434146; // "FACP"
        private const uint DSDT_SIGNATURE = 0x54445344; // "DSDT"
        private const uint SSDT_SIGNATURE = 0x54445353; // "SSDT"
        private const uint HPET_SIGNATURE = 0x54455048; // "HPET"
        private const uint MCFG_SIGNATURE = 0x4746434D; // "MCFG"

        // Manager state
        internal static bool _initialized = false;
        private static bool _acpiVersion2 = false;

        // Pointers to important tables
        private static RSDP* _rsdp = null;
        private static ACPISDTHeader* _rsdt = null;
        private static ACPISDTHeader* _xsdt = null;
        private static FADT* _fadt = null;
        private static ACPISDTHeader* _dsdt = null;
        private static MADT* _madt = null;

        // System reset information
        private static ResetType _resetType = ResetType.None;
        private static ushort _resetPort = 0;
        private static byte _resetValue = 0;

        /// <summary>
        /// Supported system reset types
        /// </summary>
        public enum ResetType
        {
            None,
            Memory,
            IO,
            Register
        }

        /// <summary>
        /// ACPI RSDP (Root System Description Pointer) Structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RSDP
        {
            public fixed byte Signature[8];
            public byte Checksum;
            public fixed byte OemId[6];
            public byte Revision;
            public uint RsdtAddress;
            // ACPI 2.0+ additional fields
            public uint Length;
            public ulong XsdtAddress;
            public byte ExtendedChecksum;
            public fixed byte Reserved[3];
        }

        /// <summary>
        /// ACPI SDT Header (System Description Table)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ACPISDTHeader
        {
            public uint Signature;
            public uint Length;
            public byte Revision;
            public byte Checksum;
            public fixed byte OemId[6];
            public fixed byte OemTableId[8];
            public uint OemRevision;
            public uint CreatorId;
            public uint CreatorRevision;
        }

        /// <summary>
        /// ACPI FADT (Fixed ACPI Description Table)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FADT
        {
            public ACPISDTHeader Header;
            public uint FirmwareCtrl;
            public uint Dsdt;

            // Reserved field in ACPI 2.0+
            public byte Reserved;

            public byte PreferredPowerManagementProfile;
            public ushort SCI_Interrupt;
            public uint SMI_CommandPort;
            public byte AcpiEnable;
            public byte AcpiDisable;
            public byte S4BIOS_REQ;
            public byte PSTATE_Control;
            public uint PM1aEventBlock;
            public uint PM1bEventBlock;
            public uint PM1aControlBlock;
            public uint PM1bControlBlock;
            public uint PM2ControlBlock;
            public uint PMTimerBlock;
            public uint GPE0Block;
            public uint GPE1Block;
            public byte PM1EventLength;
            public byte PM1ControlLength;
            public byte PM2ControlLength;
            public byte PMTimerLength;
            public byte GPE0Length;
            public byte GPE1Length;
            public byte GPE1Base;
            public byte CStateControl;
            public ushort WorstC2Latency;
            public ushort WorstC3Latency;
            public ushort FlushSize;
            public ushort FlushStride;
            public byte DutyOffset;
            public byte DutyWidth;
            public byte DayAlarm;
            public byte MonthAlarm;
            public byte Century;

            // Reserved fields for ACPI 1.0 systems
            public ushort BootArchitectureFlags;
            public byte Reserved2;
            public uint Flags;

            // Reset Register (ACPI 2.0+)
            public GenericAddressStructure ResetReg;
            public byte ResetValue;

            // Reserved fields for ACPI 2.0+
            public fixed byte Reserved3[3];

            // ACPI 2.0+ fields
            public ulong X_FirmwareControl;
            public ulong X_Dsdt;

            // More ACPI 2.0+ fields for power control not included for simplicity
        }

        /// <summary>
        /// ACPI MADT (Multiple APIC Description Table)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MADT
        {
            public ACPISDTHeader Header;
            public uint LocalApicAddress;
            public uint Flags;
            // Followed by interrupt controller records
        }

        /// <summary>
        /// ACPI Generic Address Structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct GenericAddressStructure
        {
            public byte AddressSpace;
            public byte BitWidth;
            public byte BitOffset;
            public byte AccessSize;
            public ulong Address;
        }

        /// <summary>
        /// Initializes the ACPI subsystem
        /// </summary>
        public static bool Initialize()
        {
            if (_initialized)
                return true;

            SerialDebug.Info("Initializing ACPI subsystem...");

            // Search for the RSDP table
            if (!FindRSDP())
            {
                SerialDebug.Info("RSDP table not found. ACPI not available.");
                return false;
            }

            // Determine ACPI version
            _acpiVersion2 = _rsdp->Revision >= 2;

            if (_acpiVersion2)
            {
                SerialDebug.Info($"Detected ACPI {_rsdp->Revision.ToString()}.0");

                // Use XSDT for ACPI 2.0+
                if (_rsdp->XsdtAddress != 0)
                {
                    _xsdt = (ACPISDTHeader*)_rsdp->XsdtAddress;
                    if (!ValidateTable(_xsdt))
                    {
                        SerialDebug.Info("Invalid XSDT, trying RSDT...");
                        _xsdt = null;
                    }
                }
            }
            else
            {
                SerialDebug.Info("Detected ACPI 1.0");
            }

            // If no valid XSDT or ACPI 1.0, use RSDT
            if (_xsdt == null)
            {
                _rsdt = (ACPISDTHeader*)_rsdp->RsdtAddress;
                if (!ValidateTable(_rsdt))
                {
                    SerialDebug.Info("Invalid RSDT. Cannot continue.");
                    return false;
                }
            }

            // Find main tables
            if (!FindTables())
            {
                SerialDebug.Info("Error finding important ACPI tables.");
                return false;
            }

            // Detect system reset method
            DetectResetMethod();

            _initialized = true;
            SerialDebug.Info("ACPI subsystem initialized successfully.");
            return true;
        }

        /// <summary>
        /// Finds and validates the RSDP (Root System Description Pointer) table
        /// </summary>
        private static bool FindRSDP()
        {
            byte* current = (byte*)ACPI_SEARCH_START;

            while (current < (byte*)ACPI_SEARCH_END)
            {
                if (IsSignatureMatch(current, RSDP_SIGNATURE))
                {
                    // Verify checksum
                    byte sum = 0;
                    for (int i = 0; i < 20; i++) // Size of RSDP 1.0
                    {
                        sum += current[i];
                    }

                    if (sum == 0)
                    {
                        _rsdp = (RSDP*)current;

                        // For ACPI 2.0+, also verify extended checksum
                        if (_rsdp->Revision >= 2)
                        {
                            sum = 0;
                            for (int i = 0; i < _rsdp->Length; i++)
                            {
                                sum += current[i];
                            }

                            if (sum != 0)
                            {
                                SerialDebug.Info("Invalid extended RSDP (checksum).");
                                return false;
                            }
                        }

                        return true;
                    }
                }

                current += 16; // 16-byte aligned search
            }

            return false;
        }

        /// <summary>
        /// Checks if a memory string matches a signature
        /// </summary>
        private static bool IsSignatureMatch(byte* memory, string signature)
        {
            for (int i = 0; i < signature.Length; i++)
            {
                if (memory[i] != signature[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Validates an ACPI table by verifying its checksum
        /// </summary>
        private static bool ValidateTable(ACPISDTHeader* table)
        {
            if (table == null)
                return false;

            // Verify checksum
            byte sum = 0;
            byte* ptr = (byte*)table;

            // Add explicit bounds check to prevent potential overflow
            if (table->Length < sizeof(ACPISDTHeader) || table->Length > 0x100000) // 1MB max table size as sanity check
            {
                SerialDebug.Info($"Invalid table length: {table->Length.ToString()}");
                return false;
            }

            for (int i = 0; i < table->Length; i++)
            {
                sum += ptr[i];
            }

            return sum == 0;
        }

        /// <summary>
        /// Finds important ACPI tables
        /// </summary>
        private static bool FindTables()
        {
            // Find important tables like FACP (FADT), APIC (MADT), etc.
            _fadt = (FADT*)FindTable(FACP_SIGNATURE);
            _madt = (MADT*)FindTable(APIC_SIGNATURE);

            // DSDT is referenced from FADT
            if (_fadt != null)
            {
                if (_acpiVersion2 && _fadt->X_Dsdt != 0)
                {
                    _dsdt = (ACPISDTHeader*)_fadt->X_Dsdt;
                }
                else
                {
                    _dsdt = (ACPISDTHeader*)_fadt->Dsdt;
                }

                if (!ValidateTable(_dsdt))
                {
                    SerialDebug.Info("Invalid DSDT.");
                    _dsdt = null;
                }
            }

            // Verify that we found the minimum required
            if (_fadt == null)
            {
                SerialDebug.Info("FADT table not found.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Finds a specific ACPI table by its signature
        /// </summary>
        private static ACPISDTHeader* FindTable(uint signature)
        {
            if (_xsdt != null)
            {
                // Use XSDT (64-bit pointers)
                int entries = (int)(_xsdt->Length - sizeof(ACPISDTHeader)) / 8;

                // Validate entry count for security
                if (entries <= 0 || entries > 1000) // Sanity check - no more than 1000 tables
                {
                    SerialDebug.Info($"Invalid XSDT entry count: {entries.ToString()}");
                    return null;
                }

                ulong* tables = (ulong*)((byte*)_xsdt + sizeof(ACPISDTHeader));

                for (int i = 0; i < entries; i++)
                {
                    // Add extra validation for pointer
                    if (tables[i] == 0 || tables[i] > 0xFFFFFFFFFFFF) // Sanity check - address in reasonable range
                    {
                        continue;
                    }

                    ACPISDTHeader* table = (ACPISDTHeader*)tables[i];

                    // Basic validation before full checksum
                    if (table->Length < sizeof(ACPISDTHeader))
                    {
                        continue;
                    }

                    if (table->Signature == signature && ValidateTable(table))
                    {
                        return table;
                    }
                }
            }
            else if (_rsdt != null)
            {
                // Use RSDT (32-bit pointers)
                int entries = (int)(_rsdt->Length - sizeof(ACPISDTHeader)) / 4;

                // Validate entry count for security
                if (entries <= 0 || entries > 1000) // Sanity check
                {
                    SerialDebug.Info($"Invalid RSDT entry count: {entries.ToString()}");
                    return null;
                }

                uint* tables = (uint*)((byte*)_rsdt + sizeof(ACPISDTHeader));

                for (int i = 0; i < entries; i++)
                {
                    // Add extra validation for pointer
                    if (tables[i] == 0 || tables[i] > 0xFFFFFFFF) // Sanity check
                    {
                        continue;
                    }

                    ACPISDTHeader* table = (ACPISDTHeader*)tables[i];

                    // Basic validation before full checksum
                    if (table->Length < sizeof(ACPISDTHeader))
                    {
                        continue;
                    }

                    if (table->Signature == signature && ValidateTable(table))
                    {
                        return table;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Detects the available method for system reset
        /// </summary>
        private static void DetectResetMethod()
        {
            _resetType = ResetType.None;

            // In ACPI 2.0+, check the reset register
            if (_acpiVersion2 && _fadt != null && _fadt->ResetReg.Address != 0)
            {
                _resetType = ResetType.Register;
                _resetValue = _fadt->ResetValue;
                SerialDebug.Info("Reset method: ACPI Register");
                return;
            }

            // Alternative reset method: Keyboard port
            _resetType = ResetType.IO;
            _resetPort = 0x64; // Keyboard controller port
            _resetValue = 0xFE; // Reset value
            SerialDebug.Info("Reset method: Keyboard Controller Port");
        }

        /// <summary>
        /// Resets the system using the ACPI method
        /// </summary>
        public static void ResetSystem()
        {
            if (!_initialized)
            {
                SerialDebug.Info("ACPI not initialized. Cannot reset.");
                return;
            }

            SerialDebug.Info("Resetting system...");

            // Try multiple reset methods in fallback sequence
            switch (_resetType)
            {
                case ResetType.Register:
                    // Use ACPI register to reset
                    if (_fadt->ResetReg.AddressSpace == 0) // Memory
                    {
                        *(byte*)_fadt->ResetReg.Address = _resetValue;
                    }
                    else if (_fadt->ResetReg.AddressSpace == 1) // IO
                    {
                        Native.OutByte((ushort)_fadt->ResetReg.Address, _resetValue);
                    }

                    // Always try fallback method
                    SerialDebug.Info("Trying keyboard controller reset as fallback");
                    Native.OutByte(0x64, 0xFE);
                    break;

                case ResetType.IO:
                    // Reset using keyboard controller
                    Native.OutByte(0x64, 0xFE);
                    break;

                case ResetType.Memory:
                    // Write to memory to reset
                    *(byte*)_resetPort = _resetValue;
                    SerialDebug.Info("Memory reset attempted");
                    break;

                default:
                    SerialDebug.Info("No reset method available.");
                    break;
            }

            // Last resort - try the PCI reset method
            // PCI reset through CF9 port (common in modern systems)
            SerialDebug.Info("Attempting PCI reset through CF9 port");
            Native.OutByte(0xCF9, 0x0E);

            // If we get here, reset failed
            SerialDebug.Info("Reset failed. System halted.");
            while (true) { Native.Halt(); }
        }

        /// <summary>
        /// Shuts down the system using ACPI methods
        /// </summary>
        public static void ShutdownSystem()
        {
            if (!_initialized || _fadt == null)
            {
                SerialDebug.Info("ACPI not initialized. Cannot shutdown.");
                return;
            }

            SerialDebug.Info("Shutting down system...");

            // Write to PM1a and PM1b registers to shut down
            if (_fadt->PM1aControlBlock != 0)
            {
                // Values for SLP_TYP and SLP_EN
                const ushort SLP_EN = 1 << 13;
                const ushort SLP_TYP_S5 = 7 << 10;

                SerialDebug.Info("Writing to PM1a control block");
                Native.OutWord((ushort)_fadt->PM1aControlBlock, SLP_TYP_S5 | SLP_EN);

                // If there's a second block, write there too
                if (_fadt->PM1bControlBlock != 0)
                {
                    SerialDebug.Info("Writing to PM1b control block");
                    Native.OutWord((ushort)_fadt->PM1bControlBlock, SLP_TYP_S5 | SLP_EN);
                }
            }

            // If we get here, shutdown failed
            SerialDebug.Info("Shutdown failed. System halted.");
            while (true) { Native.Halt(); }
        }

        /// <summary>
        /// Gets the Local APIC controller address
        /// </summary>
        public static ulong GetLocalApicAddress()
        {
            if (_madt != null)
            {
                // ACPI 2.0+ might have extended address (but not in the specification)
                return _madt->LocalApicAddress;
            }

            // Fallback to the default address if MADT not found
            // Most systems use a fixed address for the Local APIC
            return 0xFEE00000;
        }

        /// <summary>
        /// Gets the ACPI Power Management Timer address
        /// </summary>
        /// <returns>The address of the PM Timer, or 0 if not available</returns>
        public static uint GetPMTimerAddress()
        {
            if (!_initialized || _fadt == null)
                return 0;

            // Check if PM Timer is available
            if (_fadt->PMTimerBlock != 0 && _fadt->PMTimerLength > 0)
            {
                return _fadt->PMTimerBlock;
            }

            return 0;
        }

        /// <summary>
        /// Prints information about detected ACPI tables
        /// </summary>
        public static void PrintACPIInfo()
        {
            if (!_initialized)
            {
                SerialDebug.Info("ACPI not initialized.");
                return;
            }

            SerialDebug.Info("\n=== ACPI Information ===");

            if (_acpiVersion2)
            {
                SerialDebug.Info("ACPI Version: 2.0+");
            }
            else
            {
                SerialDebug.Info("ACPI Version: 1.0");
            }

            // Extract OEM ID
            string oemId = "";
            for (int i = 0; i < 6; i++)
            {
                oemId += (char)_rsdp->OemId[i];
            }
            SerialDebug.Info($"OEM ID: {oemId.ToString()}");

            // RSDT/XSDT
            if (_xsdt != null)
            {
                int entries = (int)(_xsdt->Length - sizeof(ACPISDTHeader)) / 8;
                SerialDebug.Info($"XSDT Entries: {entries.ToString()}");
            }

            if (_rsdt != null)
            {
                int entries = (int)(_rsdt->Length - sizeof(ACPISDTHeader)) / 4;
                SerialDebug.Info($"RSDT Entries: {entries.ToString()}");
            }

            // Important tables
            // Para FADT
            if (_fadt != null)
            {
                SerialDebug.Info("FADT: Found");
            }
            else
            {
                SerialDebug.Info("FADT: Not found");
            }

            // Para DSDT
            if (_dsdt != null)
            {
                SerialDebug.Info("DSDT: Found");
            }
            else
            {
                SerialDebug.Info("DSDT: Not found");
            }


            if (_madt != null)
            {
                SerialDebug.Info($"MADT: Found");
                SerialDebug.Info($"Local APIC Address: 0x{((ulong)_madt->LocalApicAddress).ToStringHex()}");
                SerialDebug.Info($"MADT Flags: 0x{((ulong)_madt->Flags).ToStringHex()}");
            }
            else
            {
                SerialDebug.Info("MADT: Not found");
            }

            // Reset method
            SerialDebug.Info($"Reset Method: {_resetType}");

            SerialDebug.Info("========================\n");
        }


    }
}