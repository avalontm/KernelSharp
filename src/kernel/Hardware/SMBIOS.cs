using Kernel.Diagnostics;
using System;
using System.Runtime.InteropServices;

namespace Kernel.Hardware
{
    /// <summary>
    /// Basic implementation of the System Management BIOS (SMBIOS)
    /// </summary>
    public static unsafe class SMBIOS
    {
        // Constants
        private const ulong SMBIOS_SEARCH_START = 0xF0000;
        private const ulong SMBIOS_SEARCH_END = 0xFFFFF;
        private const string SMBIOS_ANCHOR_STRING = "_SM_";
        private const string SMBIOS3_ANCHOR_STRING = "_SM3_";

        // SMBIOS table data
        private static IntPtr _tablePtr;
        private static ulong _tableLength;
        private static byte _majorVersion;
        private static byte _minorVersion;
        private static bool _initialized;

        /// <summary>
        /// Standard SMBIOS Entry Point Structure (32-bit)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SMBIOSEntryPoint32
        {
            public fixed byte AnchorString[4];      // '_SM_'
            public byte Checksum;
            public byte Length;
            public byte MajorVersion;
            public byte MinorVersion;
            public ushort MaxStructureSize;
            public byte EntryPointRevision;
            public fixed byte FormattedArea[5];
            public fixed byte IntermediateAnchor[5]; // '_DMI_'
            public byte IntermediateChecksum;
            public ushort StructureTableLength;
            public uint StructureTableAddress;      // Physical address of the table
            public ushort NumberOfStructures;
            public byte BCDRevision;
        }

        /// <summary>
        /// SMBIOS 3.0 Entry Point Structure (64-bit)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SMBIOSEntryPoint64
        {
            public fixed byte AnchorString[5];      // '_SM3_'
            public byte Checksum;
            public byte Length;
            public byte MajorVersion;
            public byte MinorVersion;
            public byte DocRev;
            public byte EntryPointRevision;
            public byte Reserved;
            public uint StructureTableMaxSize;
            public ulong StructureTableAddress;     // Physical address of the table (64-bit)
        }

        /// <summary>
        /// Common header for all SMBIOS structures
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SMBIOSHeader
        {
            public byte Type;
            public byte Length;
            public ushort Handle;
        }

        /// <summary>
        /// Basic SMBIOS structure types
        /// </summary>
        public enum SMBIOSStructureType : byte
        {
            BIOSInformation = 0,
            SystemInformation = 1,
            BaseboardInformation = 2,
            ProcessorInformation = 4,
            MemoryDevice = 17,
            EndOfTable = 127
        }

        // BIOS Information Structure
        struct BIOSInfo
        {
            public SMBIOSHeader Header;
            public byte Vendor;           // String
            public byte Version;          // String
            public ushort StartingSegment;
            public byte ReleaseDate;      // String
            public byte RomSize;
        }

        // System Information Structure
        struct SystemInfo
        {
            public SMBIOSHeader Header;
            public byte Manufacturer;     // String
            public byte ProductName;      // String
            public byte Version;          // String
            public byte SerialNumber;     // String
        }

        // Processor Information Structure
        struct ProcessorInfo
        {
            public SMBIOSHeader Header;
            public byte SocketDesignation;    // String
            public byte ProcessorType;
            public byte ProcessorFamily;
            public byte ProcessorManufacturer; // String
            public ulong ProcessorID;
            public byte ProcessorVersion;     // String
            public byte Voltage;
            public ushort ExternalClock;
            public ushort MaxSpeed;
            public ushort CurrentSpeed;
            public byte Status;
            public byte ProcessorUpgrade;
        }

        /// <summary>
        /// Initializes the SMBIOS subsystem.
        /// </summary>
        public static bool Initialize()
        {
            if (_initialized)
                return true;

            SerialDebug.Info("Initializing SMBIOS...");

            // Search for the SMBIOS entry table.
            if (!FindSMBIOSTable())
            {
                SerialDebug.Warning("SMBIOS table not found");
                return false;
            }

            _initialized = true;
            SerialDebug.Info($"SMBIOS initialized: version {_majorVersion.ToString()}.{_minorVersion.ToString()}");
            return true;
        }

        /// <summary>
        /// Searches for the SMBIOS table in memory.
        /// </summary>
        private static bool FindSMBIOSTable()
        {
            // Search within the memory range reserved for SMBIOS.
            byte* current = (byte*)SMBIOS_SEARCH_START;

            while (current < (byte*)SMBIOS_SEARCH_END)
            {
                // Check for SMBIOS 3.0.
                if (IsMatch(current, SMBIOS3_ANCHOR_STRING))
                {
                    SMBIOSEntryPoint64* entry64 = (SMBIOSEntryPoint64*)current;

                    // Verify checksum.
                    if (VerifyChecksum(current, entry64->Length))
                    {
                        _majorVersion = entry64->MajorVersion;
                        _minorVersion = entry64->MinorVersion;
                        _tablePtr = (IntPtr)entry64->StructureTableAddress;
                        _tableLength = entry64->StructureTableMaxSize;

                        SerialDebug.Info($"SMBIOS 3.0: v{_majorVersion.ToString()}.{_minorVersion.ToString()}, " +
                                           $"address: 0x{((ulong)_tablePtr).ToStringHex()}");
                        return true;
                    }
                }
                // Check for SMBIOS 2.x.
                else if (IsMatch(current, SMBIOS_ANCHOR_STRING))
                {
                    SMBIOSEntryPoint32* entry32 = (SMBIOSEntryPoint32*)current;

                    // Verify checksum.
                    if (VerifyChecksum(current, entry32->Length))
                    {
                        _majorVersion = entry32->MajorVersion;
                        _minorVersion = entry32->MinorVersion;
                        _tablePtr = (IntPtr)entry32->StructureTableAddress;
                        _tableLength = entry32->StructureTableLength;

                        SerialDebug.Info($"SMBIOS 2.x: v{_majorVersion.ToString()}.{_minorVersion.ToString()}, " +
                                           $"address: 0x{((ulong)_tablePtr).ToStringHex()}");
                        return true;
                    }
                }

                current += 16; // Search in increments of 16 bytes.
            }

            return false;
        }

        /// <summary>
        /// Checks if there is a string match in memory.
        /// </summary>
        private static bool IsMatch(byte* memory, string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (memory[i] != str[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Verifies the checksum of a block of bytes.
        /// </summary>
        private static bool VerifyChecksum(byte* start, byte length)
        {
            byte sum = 0;
            for (int i = 0; i < length; i++)
            {
                sum += start[i];
            }
            return sum == 0; // The sum must be 0 for a valid checksum.
        }

        /// <summary>
        /// Enumerates all SMBIOS structures, calling the callback for each one.
        /// </summary>
        public static bool EnumerateStructures(Func<IntPtr, bool> callback)
        {
            if (!_initialized || _tablePtr == IntPtr.Zero)
                return false;

            byte* current = (byte*)_tablePtr;
            byte* end = current + _tableLength;

            while (current < end)
            {
                SMBIOSHeader* header = (SMBIOSHeader*)current;

                // Check for end-of-table.
                if (header->Type == (byte)SMBIOSStructureType.EndOfTable)
                    break;

                // Call the callback.
                if (!callback((IntPtr)header))
                    break;

                // Move past the formatted area.
                byte* stringSection = current + header->Length;

                // Search for the end of the string section (two consecutive null bytes).
                byte* stringEnd = stringSection;
                while (stringEnd < end)
                {
                    if (stringEnd[0] == 0 && stringEnd[1] == 0)
                    {
                        stringEnd += 2; // Move past the null bytes.
                        break;
                    }
                    stringEnd++;
                }

                // Move to the next record.
                current = stringEnd;
            }

            return true;
        }

        /// <summary>
        /// Finds a specific structure by its type.
        /// </summary>
        public static IntPtr FindStructure(SMBIOSStructureType type)
        {
            if (!_initialized)
                return IntPtr.Zero;

            IntPtr result = IntPtr.Zero;

            EnumerateStructures(ptr =>
            {
                SMBIOSHeader* header = (SMBIOSHeader*)ptr.ToPointer();
                if (header->Type == (byte)type)
                {
                    result = ptr;
                    return false; // Stop enumeration.
                }
                return true; // Continue searching.
            });

            return result;
        }

        /// <summary>
        /// Retrieves a string from an SMBIOS structure.
        /// </summary>
        public static string GetString(IntPtr structPtr, byte stringIndex)
        {
            if (structPtr == IntPtr.Zero || stringIndex == 0)
                return string.Empty;

            SMBIOSHeader* header = (SMBIOSHeader*)structPtr.ToPointer();
            byte* stringStart = (byte*)structPtr.ToPointer() + header->Length;

            // Find the specified string (indices start at 1)
            int currentIndex = 1;

            while (currentIndex < 255) // Safety limit
            {
                if (*stringStart == 0)
                {
                    // End of string
                    if (currentIndex == stringIndex)
                        return string.Empty; // Empty string

                    stringStart++; // Move to the next byte

                    // Check if it is the end of all strings
                    if (*stringStart == 0)
                        return string.Empty; // String not found

                    currentIndex++;
                    continue;
                }

                if (currentIndex == stringIndex)
                {
                    // Found the string, calculate length
                    byte* end = stringStart;
                    while (*end != 0) end++;

                    int length = (int)(end - stringStart);
                    if (length <= 0)
                        return string.Empty;

                    // Convert to a .NET string
                    char[] chars = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        chars[i] = (char)stringStart[i];
                    }

                    return new string(chars);
                }

                // Move to the next character
                stringStart++;
            }

            return string.Empty; // String not found
        }

        /// <summary>
        /// Prints basic system information detected by SMBIOS.
        /// </summary>
        public static void PrintSystemSummary()
        {
            if (!_initialized && !Initialize())
            {
                Console.WriteLine("Failed to initialize SMBIOS");
                return;
            }

            Console.WriteLine("\n===== SMBIOS INFORMATION =====");
            Console.WriteLine($"SMBIOS Version: {_majorVersion.ToString()}.{_minorVersion.ToString()}");

            // BIOS
            IntPtr biosPtr = FindStructure(SMBIOSStructureType.BIOSInformation);
            if (biosPtr != IntPtr.Zero)
            {
                BIOSInfo* biosInfo = (BIOSInfo*)biosPtr.ToPointer();
                Console.WriteLine("\n== BIOS ==");
                Console.WriteLine($"Manufacturer: {GetString(biosPtr, biosInfo->Vendor)}");
                Console.WriteLine($"Version: {GetString(biosPtr, biosInfo->Version)}");
                Console.WriteLine($"Release Date: {GetString(biosPtr, biosInfo->ReleaseDate)}");
            }

            // System
            IntPtr sysPtr = FindStructure(SMBIOSStructureType.SystemInformation);
            if (sysPtr != IntPtr.Zero)
            {
                SystemInfo* sysInfo = (SystemInfo*)sysPtr.ToPointer();
                Console.WriteLine("\n== System ==");
                Console.WriteLine($"Manufacturer: {GetString(sysPtr, sysInfo->Manufacturer)}");
                Console.WriteLine($"Product: {GetString(sysPtr, sysInfo->ProductName)}");
                Console.WriteLine($"Version: {GetString(sysPtr, sysInfo->Version)}");
            }

            // Processor
            IntPtr cpuPtr = FindStructure(SMBIOSStructureType.ProcessorInformation);
            if (cpuPtr != IntPtr.Zero)
            {
                ProcessorInfo* cpuInfo = (ProcessorInfo*)cpuPtr.ToPointer();
                Console.WriteLine("\n== Processor ==");
                Console.WriteLine($"Socket: {GetString(cpuPtr, cpuInfo->SocketDesignation)}");
                Console.WriteLine($"Manufacturer: {GetString(cpuPtr, cpuInfo->ProcessorManufacturer)}");
                Console.WriteLine($"Version: {GetString(cpuPtr, cpuInfo->ProcessorVersion)}");
                Console.WriteLine($"Current Speed: {cpuInfo->CurrentSpeed.ToString()} MHz");
                Console.WriteLine($"Max Speed: {cpuInfo->MaxSpeed.ToString()} MHz");
            }

            Console.WriteLine("\n=============================");
        }
    }
}
