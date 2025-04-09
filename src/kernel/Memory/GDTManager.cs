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
        // Constants for access byte fields
        private const byte ACCESS_PRESENT = 0x80;
        private const byte ACCESS_RING0 = 0x00;
        private const byte ACCESS_RING3 = 0x60;
        private const byte ACCESS_SYSTEM = 0x00;
        private const byte ACCESS_CODE_DATA = 0x10;
        private const byte ACCESS_EXECUTABLE = 0x08;
        private const byte ACCESS_DIRECTION_DOWN = 0x04;
        private const byte ACCESS_RW = 0x02;
        private const byte ACCESS_ACCESSED = 0x01;

        // Constants for flags byte fields
        private const byte FLAG_GRANULARITY = 0x80;
        private const byte FLAG_32BIT = 0x40;
        private const byte FLAG_64BIT = 0x20;

        // Predefined access byte values
        private const byte ACCESS_KERNEL_CODE = ACCESS_PRESENT | ACCESS_RING0 | ACCESS_CODE_DATA | ACCESS_EXECUTABLE | ACCESS_RW;
        private const byte ACCESS_KERNEL_DATA = ACCESS_PRESENT | ACCESS_RING0 | ACCESS_CODE_DATA | ACCESS_RW;
        private const byte ACCESS_USER_CODE = ACCESS_PRESENT | ACCESS_RING3 | ACCESS_CODE_DATA | ACCESS_EXECUTABLE | ACCESS_RW;
        private const byte ACCESS_USER_DATA = ACCESS_PRESENT | ACCESS_RING3 | ACCESS_CODE_DATA | ACCESS_RW;
        private const byte ACCESS_TSS = 0x89;

        // Structure for a 64-bit GDT entry
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct GDTEntry
        {
            public ushort LimitLow;     // Limit bits 0-15
            public ushort BaseLow;      // Base address bits 0-15
            public byte BaseMiddle;     // Base address bits 16-23
            public byte Access;         // Access byte
            public byte FlagsAndLimitHigh; // Limit bits 16-19 and flags
            public byte BaseHigh;       // Base address bits 24-31
            public uint BaseUpper;      // Base address bits 32-63 (used for TSS)
            public uint Reserved;       // Reserved, must be 0
        }

        // Structure for GDT pointer
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct GDTPointer
        {
            public ushort Limit;  // Size of GDT minus one
            public ulong Base;    // Base address of GDT (64-bit)
        }

        // Task State Segment structure
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TSS
        {
            public uint Reserved0;
            public ulong RSP0;        // Stack pointer for privilege level 0
            public ulong RSP1;        // Stack pointer for privilege level 1
            public ulong RSP2;        // Stack pointer for privilege level 2
            public ulong Reserved1;
            public ulong IST1;        // Interrupt Stack Table pointer 1
            public ulong IST2;        // Interrupt Stack Table pointer 2
            public ulong IST3;        // Interrupt Stack Table pointer 3
            public ulong IST4;        // Interrupt Stack Table pointer 4
            public ulong IST5;        // Interrupt Stack Table pointer 5
            public ulong IST6;        // Interrupt Stack Table pointer 6
            public ulong IST7;        // Interrupt Stack Table pointer 7
            public ulong Reserved2;
            public ushort Reserved3;
            public ushort IOMapBase;  // I/O Map Base Address
        }

        // Define segment selectors for easier reference
        public enum SegmentSelector : ushort
        {
            KernelNullSelector = 0x00,
            KernelCodeSelector = 0x08,
            KernelDataSelector = 0x10,
            UserCodeSelector = 0x18,
            UserDataSelector = 0x20,
            TssSelector = 0x28
        }

        // Memory for GDT entries
        private static IntPtr _gdtMemory;
        private static GDTPointer _gdtPointer;
        private static TSS _tss; 
        private static byte[] _interruptStack; // 16KB stack for interrupts  

        /// <summary>
        /// Initializes the GDT for 64-bit mode
        /// </summary>
        public static void Initialize()
        {
            SerialDebug.Info("Initializing GDT for 64-bit mode...");

            // Check existing GDT first
            GDTPointer currentGdt = new GDTPointer();
            StoreGDT(&currentGdt);
            SerialDebug.Info($"Current GDT at 0x{currentGdt.Base.ToStringHex()}, limit: {currentGdt.Limit}");

            // Allocate memory for the new GDT (6 entries)
            _gdtMemory = (IntPtr)Allocator.malloc(6 * sizeof(GDTEntry));
            GDTEntry* gdtEntries = (GDTEntry*)_gdtMemory;

            // Initialize TSS
            InitializeTSS();

            // Set up GDT entries
            // 0: Null descriptor
            gdtEntries[0] = CreateEntry(0, 0, 0, 0);

            // 1: Kernel code segment (64-bit)
            gdtEntries[1] = CreateEntry(0, 0, ACCESS_KERNEL_CODE, FLAG_64BIT);

            // 2: Kernel data segment
            gdtEntries[2] = CreateEntry(0, 0, ACCESS_KERNEL_DATA, 0);

            // 3: User code segment (64-bit)
            gdtEntries[3] = CreateEntry(0, 0, ACCESS_USER_CODE, FLAG_64BIT);

            // 4: User data segment
            gdtEntries[4] = CreateEntry(0, 0, ACCESS_USER_DATA, 0);

            // 5: TSS entry
            fixed (TSS* tssPtr = &_tss)
            {
                gdtEntries[5] = CreateTSSEntry((ulong)tssPtr);
            }

            // Set up GDT pointer
            _gdtPointer.Limit = (ushort)(6 * sizeof(GDTEntry) - 1);
            _gdtPointer.Base = (ulong)gdtEntries;

            SerialDebug.Info($"New GDT at address: 0x{_gdtPointer.Base.ToStringHex()}");
            SerialDebug.Info($"GDT size: {_gdtPointer.Limit + 1} bytes");

            // Apply the new GDT safely
            ApplyGDT();

            SerialDebug.Info("64-bit GDT initialized successfully.");
        }

        /// <summary>
        /// Creates a GDT entry with the specified parameters
        /// </summary>
        private static GDTEntry CreateEntry(ulong baseAddress, uint limit, byte access, byte flags)
        {
            // In 64-bit mode, most base and limit values are ignored
            // But we'll set them properly anyway for completeness
            return new GDTEntry
            {
                BaseLow = (ushort)(baseAddress & 0xFFFF),
                BaseMiddle = (byte)((baseAddress >> 16) & 0xFF),
                BaseHigh = (byte)((baseAddress >> 24) & 0xFF),
                BaseUpper = (uint)((baseAddress >> 32) & 0xFFFFFFFF),
                LimitLow = (ushort)(limit & 0xFFFF),
                FlagsAndLimitHigh = (byte)(((limit >> 16) & 0x0F) | flags),
                Access = access,
                Reserved = 0
            };
        }

        /// <summary>
        /// Creates a TSS entry
        /// </summary>
        private static GDTEntry CreateTSSEntry(ulong baseAddress)
        {
            uint limit = (uint)sizeof(TSS) - 1;

            return new GDTEntry
            {
                BaseLow = (ushort)(baseAddress & 0xFFFF),
                BaseMiddle = (byte)((baseAddress >> 16) & 0xFF),
                BaseHigh = (byte)((baseAddress >> 24) & 0xFF),
                BaseUpper = (uint)((baseAddress >> 32) & 0xFFFFFFFF),
                LimitLow = (ushort)(limit & 0xFFFF),
                FlagsAndLimitHigh = (byte)((limit >> 16) & 0x0F),
                Access = ACCESS_TSS,
                Reserved = 0
            };
        }

        /// <summary>
        /// Initializes the Task State Segment
        /// </summary>
        private static void InitializeTSS()
        {
            SerialDebug.Info("Initializing TSS...");
            // Clear TSS structure
            _tss = new TSS();

            SerialDebug.Info("Setting up TSS...");
            // Set up stack for privilege level 0
            fixed (byte* stackPtr = &_interruptStack[_interruptStack.Length - 1])
            {
                SerialDebug.Info($"Stack pointer for TSS: 0x{((ulong)stackPtr).ToStringHex()}");
                _tss.RSP0 = (ulong)stackPtr;

                // Set up interrupt stack table entries
                _tss.IST1 = (ulong)stackPtr;
            }

            // No I/O permission map
            _tss.IOMapBase = (ushort)sizeof(TSS);
            SerialDebug.Info($"TSS initialized with RSP0: 0x{_tss.RSP0.ToStringHex()}");
        }

        /// <summary>
        /// Safely applies the new GDT and updates segment registers
        /// </summary>
        private static void ApplyGDT()
        {
            fixed (GDTPointer* gdtPtr = &_gdtPointer)
            {
                // Load GDT
                SerialDebug.Info("Loading GDT...");
                LoadGDT(gdtPtr);

                // Update data segments
                SerialDebug.Info("Updating data segments...");
                SetDataSegments((ushort)SegmentSelector.KernelDataSelector);

                // Special handling for SS (stack segment)
                SerialDebug.Info("Updating stack segment...");
                SetStackSegment((ushort)SegmentSelector.KernelDataSelector);

                // Update CS with far return
                SerialDebug.Info("Updating code segment...");
                ReloadCodeSegment((ushort)SegmentSelector.KernelCodeSelector);

                // Load TSS
                SerialDebug.Info("Loading TSS...");
                LoadTSS((ushort)SegmentSelector.TssSelector);
            }
        }

        /// <summary>
        /// Gets the current GDT information
        /// </summary>
        [DllImport("*", EntryPoint = "_StoreGDT")]
        private static extern void StoreGDT(GDTPointer* gdtPtr);

        /// <summary>
        /// Loads a new GDT
        /// </summary>
        [DllImport("*", EntryPoint = "_LoadGDT")]
        private static extern void LoadGDT(GDTPointer* gdtPtr);

        /// <summary>
        /// Sets data segment registers (DS, ES)
        /// </summary>
        [DllImport("*", EntryPoint = "_SetDataSegments")]
        private static extern void SetDataSegments(ushort selector);

        /// <summary>
        /// Sets stack segment register (SS)
        /// </summary>
        [DllImport("*", EntryPoint = "_SetStackSegment")]
        private static extern void SetStackSegment(ushort selector);

        /// <summary>
        /// Reloads code segment register (CS) with a far return
        /// </summary>
        [DllImport("*", EntryPoint = "_ReloadCodeSegment")]
        private static extern void ReloadCodeSegment(ushort selector);

        /// <summary>
        /// Loads the Task State Segment
        /// </summary>
        [DllImport("*", EntryPoint = "_LoadTSS")]
        private static extern void LoadTSS(ushort selector);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct GDTInfo
        {
            public ushort Limit;    // Tamaño de la GDT menos 1
            public ulong Base;      // Dirección base de la GDT
        }

        /// <summary>
        /// Inicializa la GDT basándose en la configuración existente del loader
        /// </summary>
        public static unsafe void InitializeFromExisting(IntPtr gdtInfoPtr)
        {
            SerialDebug.Info("Inicializando GDT desde configuración existente...");

            // Convertir el puntero a GDTInfo
            GDTInfo* gdtInfo = (GDTInfo*)gdtInfoPtr;
            _interruptStack = new byte[16384];
            SerialDebug.Info($"GDT existente en: 0x{gdtInfo->Base.ToStringHex()}, tamaño: {gdtInfo->Limit + 1} bytes");


            InitializeTSS();

           // LoadTSS((ushort)SegmentSelector.TssSelector);

            SerialDebug.Info("GDT extendida inicializada correctamente.");
        }
    }
}