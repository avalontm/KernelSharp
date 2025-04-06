using Kernel.Diagnostics;
using Kernel.Drivers.IO;
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

        // MADT entry types
        private const byte MADT_TYPE_LOCAL_APIC = 0;
        private const byte MADT_TYPE_IO_APIC = 1;
        private const byte MADT_TYPE_INT_OVERRIDE = 2;
        private const byte MADT_TYPE_NMI_SOURCE = 3;
        private const byte MADT_TYPE_LOCAL_APIC_NMI = 4;
        private const byte MADT_TYPE_LOCAL_APIC_OVERRIDE = 5;
        private const byte MADT_TYPE_IO_SAPIC = 6;
        private const byte MADT_TYPE_LOCAL_SAPIC = 7;
        private const byte MADT_TYPE_PLATFORM_INT_SOURCE = 8;

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

        // APIC information
        private static ulong _ioApicAddress = 0;
        private static byte _ioApicId = 0;
        private static byte _localApicCount = 0;
        private static byte _ioApicCount = 0;

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
        /// MADT Entry Header
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MADTEntryHeader
        {
            public byte Type;
            public byte Length;
        }

        /// <summary>
        /// MADT Local APIC Entry
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MADTLocalApicEntry
        {
            public MADTEntryHeader Header;
            public byte ProcessorId;
            public byte ApicId;
            public uint Flags; // Bit 0: Processor Enabled, Bit 1: Online Capable
        }

        /// <summary>
        /// MADT I/O APIC Entry
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MADTIOApicEntry
        {
            public MADTEntryHeader Header;
            public byte IOApicId;
            public byte Reserved;
            public uint IOApicAddress;
            public uint GlobalSystemInterruptBase;
        }

        /// <summary>
        /// MADT Interrupt Source Override Entry
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MADTInterruptOverrideEntry
        {
            public MADTEntryHeader Header;
            public byte Bus;          // 0 = ISA
            public byte Source;       // IRQ source
            public uint GlobalSystemInterrupt;
            public ushort Flags;      // Polarity, Trigger Mode
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
                SerialDebug.Info($"Detected ACPI {_rsdp->Revision}.0");

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

            // Parse MADT to find APIC information
            ParseMADT();

            // Detect system reset method
            DetectResetMethod();

            _initialized = true;
            SerialDebug.Info("ACPI subsystem initialized successfully.");
            return true;
        }

        /// <summary>
        /// Parses the MADT table to extract APIC information
        /// </summary>
        private static void ParseMADT()
        {
            if (_madt == null)
            {
                SerialDebug.Info("MADT table not found. Cannot determine APIC configuration.");
                return;
            }

            SerialDebug.Info("Parsing MADT to locate APIC controllers...");

            // Starting address of the first MADT entry
            byte* currentPtr = (byte*)_madt + sizeof(MADT);
            byte* endPtr = (byte*)_madt + _madt->Header.Length;

            // Parse through all entries
            while (currentPtr < endPtr)
            {
                MADTEntryHeader* entryHeader = (MADTEntryHeader*)currentPtr;

                // Validate entry
                if (entryHeader->Length == 0 || currentPtr + entryHeader->Length > endPtr)
                {
                    SerialDebug.Warning("Invalid MADT entry found. Stopping parse.");
                    break;
                }

                switch (entryHeader->Type)
                {
                    case MADT_TYPE_LOCAL_APIC:
                        {
                            MADTLocalApicEntry* localApic = (MADTLocalApicEntry*)currentPtr;
                            bool enabled = (localApic->Flags & 0x1) != 0;

                            if (enabled)
                            {
                                _localApicCount++;
                                //SerialDebug.Info($"Local APIC: ID {localApic->ApicId}, Processor {localApic->ProcessorId}, Enabled: " + enabled);
                            }
                        }
                        break;

                    case MADT_TYPE_IO_APIC:
                        {
                            MADTIOApicEntry* ioApic = (MADTIOApicEntry*)currentPtr;

                            // Store the first IO APIC info
                            if (_ioApicAddress == 0)
                            {
                                _ioApicAddress = ioApic->IOApicAddress;
                                _ioApicId = ioApic->IOApicId;
                            }

                            _ioApicCount++;
                           // SerialDebug.Info($"I/O APIC: ID {ioApic->IOApicId}, Address 0x{((ulong)ioApic->IOApicAddress).ToStringHex()}, GSI Base {ioApic->GlobalSystemInterruptBase}");
                        }
                        break;

                    case MADT_TYPE_INT_OVERRIDE:
                        {
                            MADTInterruptOverrideEntry* intOverride = (MADTInterruptOverrideEntry*)currentPtr;
                            //SerialDebug.Info($"Interrupt Override: IRQ {intOverride->Source} -> GSI {intOverride->GlobalSystemInterrupt}, Flags 0x{((ulong)intOverride->Flags).ToStringHex()}");

                            // Special handling for well-known IRQs
                            if (intOverride->Source == 1) // Keyboard IRQ
                            {
                               // SerialDebug.Info($"Keyboard IRQ override: ISA IRQ 1 -> GSI {intOverride->GlobalSystemInterrupt}");
                            }
                        }
                        break;

                    case MADT_TYPE_NMI_SOURCE:
                    case MADT_TYPE_LOCAL_APIC_NMI:
                    case MADT_TYPE_LOCAL_APIC_OVERRIDE:
                    case MADT_TYPE_IO_SAPIC:
                    case MADT_TYPE_LOCAL_SAPIC:
                    case MADT_TYPE_PLATFORM_INT_SOURCE:
                        // These entry types could be parsed for more advanced interrupt handling
                        break;

                    default:
                        SerialDebug.Info($"Unknown MADT entry type: {entryHeader->Type}");
                        break;
                }

                // Move to the next entry
                currentPtr += entryHeader->Length;
            }

           // SerialDebug.Info($"MADT parsing complete. Found {_localApicCount} Local APICs and {_ioApicCount} I/O APICs.");
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
               // SerialDebug.Info($"Invalid table length: {table->Length.ToString()}");
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
                   // SerialDebug.Info($"Invalid XSDT entry count: {entries.ToString()}");
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
                    //SerialDebug.Info($"Invalid RSDT entry count: {entries.ToString()}");
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
                        IOPort.Out8((ushort)_fadt->ResetReg.Address, _resetValue);
                    }

                    // Always try fallback method
                    SerialDebug.Info("Trying keyboard controller reset as fallback");
                    IOPort.Out8(0x64, 0xFE);
                    break;

                case ResetType.IO:
                    // Reset using keyboard controller
                    IOPort.Out8(0x64, 0xFE);
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
            IOPort.Out8(0xCF9, 0x0E);

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
                IOPort.Out32((ushort)_fadt->PM1aControlBlock, SLP_TYP_S5 | SLP_EN);

                // If there's a second block, write there too
                if (_fadt->PM1bControlBlock != 0)
                {
                    SerialDebug.Info("Writing to PM1b control block");
                    IOPort.Out32((ushort)_fadt->PM1bControlBlock, SLP_TYP_S5 | SLP_EN);
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
        /// Gets the I/O APIC controller address
        /// </summary>
        public static ulong GetIOApicAddress()
        {
            if (_ioApicAddress != 0)
            {
                return _ioApicAddress;
            }

            // Fallback to the default address if not found
            // Most systems use a fixed address for the I/O APIC
            SerialDebug.Info("Using default I/O APIC address 0xFEC00000");
            return 0xFEC00000;
        }

        /// <summary>
        /// Obtiene el ID del APIC Local del procesador actual
        /// </summary>
        /// <returns>ID del APIC Local o 0 si no está disponible</returns>
        public static byte GetLocalAPICId()
        {
            if (!_initialized)
                return 0;

            // Si tenemos un APIC configurado, leer directamente del registro
            if (_madt != null)
            {
                // Obtener la dirección base del APIC Local
                ulong localApicBase = GetLocalAPICBase();

                // La dirección del registro ID del APIC Local es offset 0x20
                uint idRegister = *(uint*)((byte*)localApicBase + 0x20);

                // El ID del APIC Local está en los bits 24-31 del registro
                byte apicId = (byte)((idRegister >> 24) & 0xFF);

                return apicId;
            }

            // Alternativa: utilizar CPUID para obtener el ID del APIC Local
            uint eax = 1; // Leaf 1 para información de procesador
            uint ebx = 0;
            uint ecx = 0;
            uint edx = 0;

            // Usar la versión completa de CPUID que recibe todos los registros
            Native.Cpuid(eax, ref eax, ref ebx, ref ecx, ref edx);

            // El ID del APIC Local está en los bits 24-31 de EBX
            byte apicIdFromCpuid = (byte)((ebx >> 24) & 0xFF);

            return apicIdFromCpuid;
        }

        /// <summary>
        /// Gets the I/O APIC ID
        /// </summary>
        public static byte GetIOApicId()
        {
            return _ioApicId;
        }

        /// <summary>
        /// Obtiene la dirección base del APIC Local
        /// </summary>
        /// <returns>Dirección base del APIC Local</returns>
        public static ulong GetLocalAPICBase()
        {
            if (!_initialized)
                return 0;

            // Si tenemos la dirección desde ACPI MADT, usarla
            if (_madt != null)
            {
                return _madt->LocalApicAddress;
            }

            // Alternativa: leer desde el MSR IA32_APIC_BASE (0x1B)
            ulong msr = Native.ReadMSR(0x1B);

            // La dirección base está en los bits 12-35
            ulong apicBase = msr & 0xFFFFF000;

            return apicBase;
        }

        /// <summary>
        /// Verifica si el APIC Local está habilitado
        /// </summary>
        /// <returns>True si el APIC Local está habilitado, False en caso contrario</returns>
        public static bool IsLocalAPICEnabled()
        {
            if (!_initialized)
                return false;

            // Método 1: Verificar el bit en el MSR IA32_APIC_BASE (0x1B)
            ulong msr = Native.ReadMSR(0x1B);

            // El bit 11 indica si el APIC está habilitado
            bool enabledInMSR = (msr & (1UL << 11)) != 0;

            // Método 2: Si el MADT está presente, verificar los flags
            if (_madt != null)
            {
                // El bit 0 de los flags MADT indica si el sistema está en modo APIC
                // 0 = PIC Mode, 1 = APIC Mode
                bool apicModeInMADT = (_madt->Flags & 1) != 0;

                // Verificar ambas condiciones
                return enabledInMSR && apicModeInMADT;
            }

            return enabledInMSR;
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
        /// Gets the number of detected Local APICs
        /// </summary>
        public static byte GetLocalApicCount()
        {
            return _localApicCount;
        }

        /// <summary>
        /// Gets the number of detected I/O APICs
        /// </summary>
        public static byte GetIOApicCount()
        {
            return _ioApicCount;
        }


        /// <summary>
        /// Gets information about the IRQ override for a specific ISA IRQ
        /// </summary>
        /// <param name="isaIrq">ISA IRQ number (0-15)</param>
        /// <param name="gsi">Output GSI (Global System Interrupt) number</param>
        /// <param name="flags">Output flags (bit 0-1: Polarity, bit 2-3: Trigger Mode)</param>
        /// <returns>True if an override was found, false otherwise</returns>
        public static bool GetIRQOverride(byte isaIrq, out uint gsi, out ushort flags)
        {
            gsi = isaIrq;  // Default: GSI == ISA IRQ
            flags = 0;     // Default: Active High, Edge-triggered

            if (!_initialized || _madt == null)
                return false;

            // Parse MADT to find interrupt overrides
            byte* currentPtr = (byte*)_madt + sizeof(MADT);
            byte* endPtr = (byte*)_madt + _madt->Header.Length;

            while (currentPtr < endPtr)
            {
                MADTEntryHeader* entryHeader = (MADTEntryHeader*)currentPtr;

                if (entryHeader->Length == 0 || currentPtr + entryHeader->Length > endPtr)
                    break;

                if (entryHeader->Type == MADT_TYPE_INT_OVERRIDE)
                {
                    MADTInterruptOverrideEntry* intOverride = (MADTInterruptOverrideEntry*)currentPtr;

                    if (intOverride->Source == isaIrq)
                    {
                        gsi = intOverride->GlobalSystemInterrupt;
                        flags = intOverride->Flags;
                        return true;
                    }
                }

                currentPtr += entryHeader->Length;
            }

            return false;
        }

        /// <summary>
        /// Helper method to check if legacy PIC mode is active according to MADT
        /// </summary>
        public static bool IsLegacyPICModeActive()
        {
            // If MADT flags bit 0 is clear, system is in legacy PIC mode
            return _madt != null && (_madt->Flags & 1) == 0;
        }

        /// <summary>
        /// Gets the PCI Express MMIO base address from the ACPI MCFG table
        /// </summary>
        /// <returns>The base address for PCI Express MMIO configuration space, or 0 if not found</returns>
        public static ulong GetMCFGBaseAddress()
        {
            if (!_initialized)
            {
                SerialDebug.Warning("ACPI subsystem not initialized");
                return 0;
            }

            SerialDebug.Info("Searching for ACPI MCFG table...");

            // Find the MCFG table
            ACPISDTHeader* mcfgTable = FindTable(MCFG_SIGNATURE);
            if (mcfgTable == null)
            {
                SerialDebug.Warning("ACPI MCFG table not found");
                return 0;
            }

            // Verify the table size is at least enough to contain the header and one entry
            if (mcfgTable->Length < 44) // 36 (header) + 8 (reserved bytes) + min 16 (first entry)
            {
               // SerialDebug.Warning("ACPI MCFG table is too small: " + mcfgTable->Length);
                return 0;
            }

            // The MCFG structure has an 8-byte reserved section after the standard ACPI header
            // After that come the MMIO base address entries
            byte* mcfgData = (byte*)mcfgTable + sizeof(ACPISDTHeader) + 8; // Skip header and reserved section

            // Each entry is 16 bytes
            // Offset 0-7: Base Address
            // Offset 8-9: PCI Segment Group
            // Offset 10: Start Bus Number
            // Offset 11: End Bus Number
            // Offset 12-15: Reserved
            ulong baseAddress = *(ulong*)mcfgData;

            SerialDebug.Info("Found MCFG table with base address: 0x" + baseAddress.ToStringHex());

            // For simplicity, we're just taking the first entry's base address
            return baseAddress;
        }
    }
}