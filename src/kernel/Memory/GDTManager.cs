using Kernel.Diagnostics;
using System;
using System.Runtime.InteropServices;

namespace Kernel.Memory
{
    /// <summary>
    /// Global Descriptor Table (GDT) manager for x86_64 architecture
    /// </summary>
    public static unsafe class GDTManager
    {
        // Constants for GDT entry types
        private const byte GDT_TYPE_CODE = 0x9A;    // Read-only executable code segment
        private const byte GDT_TYPE_DATA = 0x92;    // Read/write data segment
        private const byte GDT_TYPE_TSS = 0x89;     // Task State Segment

        // Descriptor flags
        private const byte GDT_FLAG_LONG_MODE = 0x20;   // 64-bit mode flag
        private const byte GDT_FLAG_PROTECTED = 0x80;   // 32-bit protected mode bit
        private const byte GDT_FLAG_4K = 0x40;          // 4K granularity (pages)

        // Maximum number of GDT entries
        private const int MAX_DESCRIPTORS = 8;

        // Structure for a 64-bit GDT entry
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct GDTEntry
        {
            public ushort LimitLow;     // Limit bits 0-15
            public ushort BaseLow;      // Base address bits 0-15
            public byte BaseMiddle;     // Base address bits 16-23
            public byte Type;           // Type and attributes
            public byte LimitHighFlags; // Limit bits 16-19 and flags
            public byte BaseHigh;       // Base address bits 24-31
            public uint BaseUpper;      // Base address bits 32-63 (only used in system descriptors like TSS)
            public uint Reserved;       // Reserved, must be 0
        }

        // Structure for the GDT pointer loaded into the GDTR register
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct GDTPointer
        {
            public ushort Limit;  // Size of GDT minus one
            public ulong Base;    // Base address of GDT (64-bit)
        }

        // Static GDT entries
        private static GDTEntry _nullEntry;    // Null descriptor
        private static GDTEntry _kernelCode;   // Kernel code segment
        private static GDTEntry _kernelData;   // Kernel data segment
        private static GDTEntry _userCode;     // User code segment (optional, for multitasking)
        private static GDTEntry _userData;     // User data segment (optional, for multitasking)
        private static GDTPointer _gdtPointer; // Pointer to the GDT

        /// <summary>
        /// Initializes the basic GDT with a flat memory model for 64-bit mode
        /// </summary>
        public static void Initialize()
        {
            SerialDebug.Info("Initializing GDT for 64-bit mode...");
            // Configure null descriptor (all values set to 0)
            _nullEntry = new GDTEntry();

            // Configure code segment for 64-bit mode (kernel)
            // In 64-bit mode, base and limit are ignored except for system descriptors
            _kernelCode = new GDTEntry
            {
                LimitLow = 0xFFFF,
                BaseLow = 0,
                BaseMiddle = 0,
                Type = GDT_TYPE_CODE,  // Executable and readable code segment
                LimitHighFlags = (byte)(0x0F | GDT_FLAG_LONG_MODE | GDT_FLAG_4K),
                BaseHigh = 0,
                BaseUpper = 0,          // Upper 32 bits for 64-bit base
                Reserved = 0            // Must be zero
            };

            // Configure data segment (kernel)
            _kernelData = new GDTEntry
            {
                LimitLow = 0xFFFF,
                BaseLow = 0,
                BaseMiddle = 0,
                Type = GDT_TYPE_DATA,  // Read/write data segment
                LimitHighFlags = (byte)(0x0F | GDT_FLAG_4K),  // No LONG_MODE flag for data segments
                BaseHigh = 0,
                BaseUpper = 0,
                Reserved = 0
            };

            // Optional: Configure segments for user space if multitasking is implemented
            _userCode = new GDTEntry
            {
                LimitLow = 0xFFFF,
                BaseLow = 0,
                BaseMiddle = 0,
                Type = GDT_TYPE_CODE,  // Executable and readable code
                LimitHighFlags = (byte)(0x0F | GDT_FLAG_LONG_MODE | GDT_FLAG_4K),
                BaseHigh = 0,
                BaseUpper = 0,
                Reserved = 0
            };

            _userData = new GDTEntry
            {
                LimitLow = 0xFFFF,
                BaseLow = 0,
                BaseMiddle = 0,
                Type = GDT_TYPE_DATA,  // Read/write data
                LimitHighFlags = (byte)(0x0F | GDT_FLAG_4K),
                BaseHigh = 0,
                BaseUpper = 0,
                Reserved = 0
            };

            // Configure the GDT pointer
            _gdtPointer.Limit = (ushort)(5 * sizeof(GDTEntry) - 1);

            // Create a GDTEntry array to pass to the native function
            GDTEntry* gdtEntries = stackalloc GDTEntry[5];
            gdtEntries[0] = _nullEntry;
            gdtEntries[1] = _kernelCode;
            gdtEntries[2] = _kernelData;
            gdtEntries[3] = _userCode;
            gdtEntries[4] = _userData;

            fixed (GDTPointer* gdtPtr = &_gdtPointer)
            {
                _gdtPointer.Base = (ulong)gdtEntries;

                // Load the GDT
                LoadGDT(gdtPtr);

                SerialDebug.Info($"Loading GDT at address: 0x{((ulong)gdtEntries).ToStringHex()}");
                SerialDebug.Info($"GDT size: {_gdtPointer.Limit.ToString()} bytes");
            }

            SerialDebug.Info("64-bit GDT initialized successfully.");
        }

        /// <summary>
        /// Loads the GDT into the processor's GDTR register
        /// </summary>
        [DllImport("*", EntryPoint = "_LoadGDT")]
        private static extern void LoadGDT(GDTPointer* gdtPtr);

        /// <summary>
        /// Reloads segment registers after loading the GDT
        /// </summary>
        [DllImport("*", EntryPoint = "_ReloadSegments")]
        private static extern void ReloadSegments();

        /// <summary>
        /// Changes the value of segment registers DS, ES, FS, GS, and SS
        /// </summary>
        [DllImport("*", EntryPoint = "_SetSegmentRegisters")]
        private static extern void SetSegmentRegisters(ushort selector);

    }
}