using Kernel.Diagnostics;
using System;
using System.Runtime.InteropServices;

namespace Kernel.Hardware
{
    /// <summary>
    /// Symmetric Multiprocessing (SMP) Manager for x86_64 systems
    /// </summary>
    public static unsafe class SMPManager
    {
        // ACPI MADT (Multiple APIC Description Table) information
        private const uint MADT_SIGNATURE = 0x43495041; // "APIC"
        private const byte MADT_TYPE_LOCAL_APIC = 0;
        private const byte MADT_TYPE_IO_APIC = 1;
        private const byte MADT_TYPE_INTERRUPT_OVERRIDE = 2;
        private const byte MADT_TYPE_NMI_SOURCE = 3;
        private const byte MADT_TYPE_LOCAL_APIC_NMI = 4;
        private const byte MADT_TYPE_LOCAL_APIC_OVERRIDE = 5;
        private const byte MADT_TYPE_IO_SAPIC = 6;
        private const byte MADT_TYPE_LOCAL_SAPIC = 7;
        private const byte MADT_TYPE_PLATFORM_INT_SOURCE = 8;

        // Limits for ACPI table search
        private const ulong ACPI_SEARCH_START = 0x000E0000;
        private const ulong ACPI_SEARCH_END = 0x000FFFFF;
        private const string RSDP_SIGNATURE = "RSD PTR ";

        // Maximum supported CPUs
        private const int MAX_CPUS = 32;

        // SMP initialization state
        private static bool _initialized;
        private static int _cpuCount;
        private static ulong _localApicAddress;
        private static ulong _ioApicAddress;

        // Information about each CPU
        private static CPUInfo[] _cpuInfos;

        // Structure to store CPU information
        private struct CPUInfo
        {
            public byte ApicId;
            public bool IsBootProcessor;
            public bool IsEnabled;
        }

        // ACPI RSDP (Root System Description Pointer) structure
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct RSDP
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

        // ACPI SDT Header (System Description Table)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ACPISDTHeader
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

        // ACPI MADT (Multiple APIC Description Table)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MADT
        {
            public ACPISDTHeader Header;
            public uint LocalApicAddress;
            public uint Flags;
            // Followed by interrupt controller records
        }

        // MADT Record structure
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MADTRecord
        {
            public byte Type;
            public byte Length;
            // Followed by type-specific data
        }

        // Local APIC Processor Record
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MADTLocalApic
        {
            public MADTRecord Header;
            public byte AcpiProcessorId;
            public byte ApicId;
            public uint Flags;
        }

        // IO APIC Record
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MADTIOApic
        {
            public MADTRecord Header;
            public byte IoApicId;
            public byte Reserved;
            public uint IoApicAddress;
            public uint GlobalSystemInterruptBase;
        }

        /// <summary>
        /// Initializes the SMP subsystem
        /// </summary>
        public static bool Initialize()
        {
            if (_initialized)
                return true;

            SerialDebug.Info("Initializing SMP system...");

            _cpuInfos = new CPUInfo[MAX_CPUS];
            // First try to use ACPI information from ACPIManager if available
            if (ACPIManager._initialized)
            {
                _localApicAddress = ACPIManager.GetLocalApicAddress();
                if (_localApicAddress != 0)
                {
                    SerialDebug.Info($"Using Local APIC address from ACPI: 0x{_localApicAddress.ToStringHex()}");
                }
            }

            // If we couldn't get info from ACPIManager, search for SMP info directly
            if (_localApicAddress == 0 && !FindSMPInfo())
            {
                SerialDebug.Info("Could not find SMP information (ACPI MADT)");

                // Fallback: assume a single processor system with default APIC address
                _cpuCount = 1;
                _localApicAddress = 0xFEE00000; // Default Local APIC base address
                _cpuInfos[0].ApicId = 0;
                _cpuInfos[0].IsEnabled = true;
                _cpuInfos[0].IsBootProcessor = true;

                SerialDebug.Info("Falling back to single CPU configuration with default APIC address");
            }

            // Initialize the Local APIC
            InitializeLocalApic();

            _initialized = true;
            SerialDebug.Info($"SMP system initialized: {_cpuCount.ToString()} processor(s) detected");
            return true;
        }

        /// <summary>
        /// Searches for SMP information in ACPI tables
        /// </summary>
        private static bool FindSMPInfo()
        {

            // Search for RSDP (Root System Description Pointer)
            byte* current = (byte*)ACPI_SEARCH_START;
            RSDP* rsdp = null;

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
                        rsdp = (RSDP*)current;

                        // For ACPI 2.0+, also verify extended checksum
                        if (rsdp->Revision >= 2)
                        {
                            sum = 0;
                            for (int i = 0; i < rsdp->Length; i++)
                            {
                                if (current + i < (byte*)ACPI_SEARCH_END)
                                {
                                    sum += current[i];
                                }
                            }

                            if (sum != 0)
                            {
                                SerialDebug.Info("RSDP extended checksum invalid, continuing with caution");
                            }
                        }

                        break;
                    }
                }

                current += 16; // 16-byte aligned search
            }

            if (rsdp == null)
            {
                SerialDebug.Info("ACPI RSDP not found");
                return false;
            }

            // Depending on revision, use RSDT or XSDT
            bool result = false;
            if (rsdp->Revision >= 2 && rsdp->XsdtAddress != 0)
            {
                // ACPI 2.0+, use XSDT
                result = ParseXSDT((ACPISDTHeader*)rsdp->XsdtAddress);
            }

            if (!result && rsdp->RsdtAddress != 0)
            {
                // ACPI 1.0 or XSDT parsing failed, use RSDT
                result = ParseRSDT((ACPISDTHeader*)(ulong)rsdp->RsdtAddress);
            }

            return result;
        }

        /// <summary>
        /// Compares a memory signature with an expected string
        /// </summary>
        private static bool IsSignatureMatch(byte* memory, string signature)
        {
            if (memory == null)
                return false;

            for (int i = 0; i < signature.Length; i++)
            {
                if (memory[i] != signature[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Parses the RSDT (Root System Description Table) looking for MADT
        /// </summary>
        private static bool ParseRSDT(ACPISDTHeader* rsdt)
        {

            // Verify pointer and length
            if (rsdt == null || rsdt->Length < sizeof(ACPISDTHeader))
            {
                SerialDebug.Info("Invalid RSDT pointer or length");
                return false;
            }

            // Check for reasonable length to prevent issues
            if (rsdt->Length > 0x10000) // 64KB max as sanity check
            {
                SerialDebug.Info($"RSDT length too large: {rsdt->Length} bytes");
                return false;
            }

            // Number of entries
            int entries = (int)(rsdt->Length - sizeof(ACPISDTHeader)) / 4;

            if (entries <= 0 || entries > 1000) // Sanity check for entry count
            {
                SerialDebug.Info($"Unreasonable RSDT entry count: {entries}");
                return false;
            }

            // Find the MADT table
            uint* tableAddresses = (uint*)((byte*)rsdt + sizeof(ACPISDTHeader));
            for (int i = 0; i < entries; i++)
            {
                if (tableAddresses[i] == 0 || tableAddresses[i] >= 0xFFFFFFFF)
                    continue; // Skip invalid addresses

                ACPISDTHeader* table = (ACPISDTHeader*)(ulong)tableAddresses[i];

                // Basic validation before accessing
                if (table == null)
                    continue;

                // Check if it's MADT
                if (table->Signature == MADT_SIGNATURE)
                {
                    return ParseMADT((MADT*)table);
                }

            }

            return true;
        }

        /// <summary>
        /// Parses the XSDT (Extended System Description Table) looking for MADT
        /// </summary>
        private static bool ParseXSDT(ACPISDTHeader* xsdt)
        {
            // Verify pointer and length
            if (xsdt == null || xsdt->Length < sizeof(ACPISDTHeader))
            {
                SerialDebug.Info("Invalid XSDT pointer or length");
                return false;
            }

            // Check for reasonable length to prevent issues
            if (xsdt->Length > 0x10000) // 64KB max as sanity check
            {
                SerialDebug.Info($"XSDT length too large: {xsdt->Length} bytes");
                return false;
            }

            // Number of entries
            int entries = (int)(xsdt->Length - sizeof(ACPISDTHeader)) / 8;

            if (entries <= 0 || entries > 1000) // Sanity check for entry count
            {
                SerialDebug.Info($"Unreasonable XSDT entry count: {entries}");
                return false;
            }

            // Find the MADT table
            ulong* tableAddresses = (ulong*)((byte*)xsdt + sizeof(ACPISDTHeader));
            for (int i = 0; i < entries; i++)
            {
                if (tableAddresses[i] == 0 || tableAddresses[i] >= 0xFFFFFFFFFFFF)
                    continue; // Skip invalid addresses

                ACPISDTHeader* table = (ACPISDTHeader*)tableAddresses[i];

                // Basic validation before accessing
                if (table == null)
                    continue;

                // Check if it's MADT
                if (table->Signature == MADT_SIGNATURE)
                {
                    return ParseMADT((MADT*)table);
                }

            }


            return false;
        }

        /// <summary>
        /// Parses the MADT (Multiple APIC Description Table) to find CPUs and APICs
        /// </summary>
        private static bool ParseMADT(MADT* madt)
        {

            // Verify pointer and length
            if (madt == null || madt->Header.Length < sizeof(MADT))
            {
                SerialDebug.Info("Invalid MADT pointer or length");
                return false;
            }

            // Save Local APIC address
            _localApicAddress = madt->LocalApicAddress;
            SerialDebug.Info($"Found Local APIC address: 0x{_localApicAddress.ToStringHex()}");

            // Process interrupt controller records
            byte* current = (byte*)madt + sizeof(MADT);
            byte* end = (byte*)madt + madt->Header.Length;

            _cpuCount = 0;

            while (current < end)
            {
                // Ensure we have at least 2 bytes to read the record header
                if (current + 2 > end)
                    break;

                MADTRecord* record = (MADTRecord*)current;

                // Ensure length is valid
                if (record->Length < 2 || current + record->Length > end)
                {
                    SerialDebug.Info($"Invalid MADT record length: {record->Length}");
                    break;
                }

                // Process according to type
                switch (record->Type)
                {
                    case MADT_TYPE_LOCAL_APIC:
                        if (record->Length >= sizeof(MADTLocalApic))
                        {
                            MADTLocalApic* localApic = (MADTLocalApic*)record;

                            // Check if processor is enabled
                            bool enabled = (localApic->Flags & 1) != 0;
                            if (enabled && _cpuCount < MAX_CPUS)
                            {
                                _cpuInfos[_cpuCount].ApicId = localApic->ApicId;
                                _cpuInfos[_cpuCount].IsEnabled = true;
                                _cpuInfos[_cpuCount].IsBootProcessor = (_cpuCount == 0);
                                _cpuCount++;

                                SerialDebug.Info($"Found CPU with APIC ID: {localApic->ApicId}");
                            }
                        }
                        break;

                    case MADT_TYPE_IO_APIC:
                        if (record->Length >= sizeof(MADTIOApic))
                        {
                            MADTIOApic* ioApic = (MADTIOApic*)record;
                            _ioApicAddress = ioApic->IoApicAddress;
                            SerialDebug.Info($"Found IO APIC at address: 0x{_ioApicAddress.ToStringHex()}, ID: {ioApic->IoApicId}");
                        }
                        break;
                }

                // Move to next record
                current += record->Length;
            }

            // If no CPUs were found, assume at least one
            if (_cpuCount == 0)
            {
                SerialDebug.Info("No CPUs found in MADT, assuming single CPU system");
                _cpuCount = 1;
                _cpuInfos[0].ApicId = 0;
                _cpuInfos[0].IsEnabled = true;
                _cpuInfos[0].IsBootProcessor = true;
            }

            return true;

        }

        /// <summary>
        /// Initializes the Local APIC
        /// </summary>
        private static void InitializeLocalApic()
        {

            if (_localApicAddress == 0)
            {
                SerialDebug.Info("Cannot initialize Local APIC - address is 0");
                return;
            }

            SerialDebug.Info($"Initializing Local APIC at 0x{_localApicAddress.ToStringHex()}");

            // Map the physical APIC address to a virtual address if needed
            // In a simple system, we could use identity mapping

            // Enable the Local APIC (only for BSP for now)
            uint* apicBase = (uint*)_localApicAddress;

            // Spurious Interrupt Vector Register (offset 0xF0)
            uint* spuriousReg = apicBase + (0xF0 / 4);

            // Read current value
            uint spuriousValue = *spuriousReg;
            SerialDebug.Info($"Current Spurious Register Value: 0x{((ulong)spuriousValue).ToStringHex()}");

            // Enable APIC (bit 8) and set spurious vector to 0xFF
            *spuriousReg = (spuriousValue | 0x100) | 0xFF;

            // Verify the write
            uint newValue = *spuriousReg;
            SerialDebug.Info($"New Spurious Register Value: 0x{((ulong)newValue).ToStringHex()}");

            // In a complete implementation, you would initialize APs (Application Processors) here

        }

        /// <summary>
        /// Gets the number of available CPUs
        /// </summary>
        public static int GetProcessorCount()
        {
            return _initialized ? _cpuCount : 1;
        }

        /// <summary>
        /// Gets the APIC ID of the current processor
        /// </summary>
        public static byte GetCurrentApicId()
        {
            // In a real system, you would get the APIC ID of the current processor
            // Through CPUID or reading the corresponding register from the Local APIC

            if (_initialized && _localApicAddress != 0)
            {

                // Read APIC ID register (offset 0x20)
                uint* apicIdReg = (uint*)_localApicAddress + (0x20 / 4);
                return (byte)((*apicIdReg >> 24) & 0xFF);
            }

            // To simplify, assume we're on the BSP (ID 0)
            return 0;
        }

        /// <summary>
        /// Gets the APIC ID of a specific processor
        /// </summary>
        /// <param name="index">Processor index (0-based)</param>
        /// <returns>APIC ID for the processor or 0xFF if invalid</returns>
        public static byte GetAPICId(int index)
        {
            if (_initialized && index >= 0 && index < _cpuCount)
            {
                return _cpuInfos[index].ApicId;
            }
            return 0xFF; // Invalid value
        }

        /// <summary>
        /// Prints information about detected processors
        /// </summary>
        public static void PrintProcessorInfo()
        {
            if (!_initialized)
            {
                SerialDebug.Info("SMP system not initialized");
                return;
            }

            SerialDebug.Info("\n=== Processor Information ===");
            SerialDebug.Info($"Total CPU count: {_cpuCount}");
            SerialDebug.Info($"Local APIC Address: 0x{_localApicAddress.ToStringHex()}");
            if (_ioApicAddress != 0)
                SerialDebug.Info($"IO APIC Address: 0x{_ioApicAddress.ToStringHex()}");

            for (int i = 0; i < _cpuCount; i++)
            {
                string processorType = _cpuInfos[i].IsBootProcessor ? "BSP" : "AP";
                string enabledStatus = _cpuInfos[i].IsEnabled ? "Enabled" : "Disabled";
                SerialDebug.Info($"CPU {i}: APIC ID {_cpuInfos[i].ApicId}, {processorType}, {enabledStatus}");
            }
            SerialDebug.Info("=============================\n");
        }
    }
}