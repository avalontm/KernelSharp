using Kernel.Diagnostics;
using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Kernel.Memory
{
    /// <summary>
    /// IDT Entry Type
    /// </summary>
    public enum IDTGateType : byte
    {
        Task = 0x5,             // 80386 Task Gate
        Interrupt16 = 0x6,      // 16-bit Interrupt Gate
        Trap16 = 0x7,           // 16-bit Trap Gate
        Interrupt32 = 0xE,      // 32-bit Interrupt Gate
        Trap32 = 0xF,           // 32-bit Trap Gate
        InterruptGate = 0x8E,   // Interrupt Gate (with flags)
        TrapGate = 0x8F         // Trap Gate (with flags)
    }

    /// <summary>
    /// Structure for an entry in the Interrupt Descriptor Table (IDT)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IDTEntry
    {
        public ushort BaseLow;    // Lower part of handler address
        public ushort Selector;   // Code segment selector
        public byte IST;          // Interrupt Stack Table index (0-7)
        public byte TypeAttr;     // Type and attributes
        public ushort BaseMid;    // Middle part of handler address
        public uint BaseHigh;     // Upper part of handler address
        public uint Reserved;     // Reserved, must be 0
    }

    /// <summary>
    /// IDT Pointer for loading with LIDT instruction
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IDTPointer
    {
        public ushort Limit;      // Size of IDT - 1
        public ulong Base;        // Base address of IDT
    }

    /// <summary>
    /// Interrupt Descriptor Table manager for x86_64
    /// </summary>
    public static unsafe class IDTManager
    {
        // IDT Size (256 possible interrupts)
        private const int IDT_SIZE = 256;

        // The IDT
        private static IDTEntry* _idt;

        // IDT Pointer for LIDT instruction
        private static IDTPointer _idtPointer;

        /// <summary>
        /// Initializes the IDT
        /// </summary>
        public static void Initialize()
        {
            SerialDebug.Info("Initializing Interrupt Descriptor Table (IDT)...");

            // Allocate memory for the IDT
            _idt = (IDTEntry*)Allocator.malloc((nuint)(sizeof(IDTEntry) * IDT_SIZE));

            // Initialize all entries to zero
            for (int i = 0; i < IDT_SIZE; i++)
            {
                _idt[i] = new IDTEntry();
            }

            // Configure IDT pointer
            _idtPointer.Limit = (ushort)(IDT_SIZE * sizeof(IDTEntry) - 1);
            _idtPointer.Base = (ulong)_idt;

            // Configure interrupt stub handler that redirects to HandleInterrupt
            IntPtr stubHandler = GetInterruptStubAddress();

            if (stubHandler != IntPtr.Zero)
            {
                // Register the stub for all entries
                for (int i = 0; i < IDT_SIZE; i++)
                {
                    SetIDTEntry(i, stubHandler, 0x08, IDTGateType.InterruptGate);
                }

                // Load the IDT
                fixed (IDTPointer* idtPtr = &_idtPointer)
                {
                    LoadIDT(idtPtr);
                }
                SerialDebug.Info("IDT initialized successfully");
            }
            else
            {
                SerialDebug.Info("ERROR: Could not get interrupt stub address");
            }
        }

        /// <summary>
        /// Configures an entry in the IDT
        /// </summary>
        /// <param name="index">Entry index (0-255)</param>
        /// <param name="handler">Handler address</param>
        /// <param name="selector">Code segment selector (0x08 for kernel code)</param>
        /// <param name="type">Gate type</param>
        /// <param name="ist">Interrupt Stack Table index (0 for normal stack)</param>
        public static void SetIDTEntry(int index, IntPtr handler, ushort selector, IDTGateType type, byte ist = 0)
        {
            if (index < 0 || index >= IDT_SIZE)
                return;

            ulong handlerAddr = (ulong)handler;

            _idt[index].BaseLow = (ushort)(handlerAddr & 0xFFFF);
            _idt[index].BaseMid = (ushort)((handlerAddr >> 16) & 0xFFFF);
            _idt[index].BaseHigh = (uint)((handlerAddr >> 32) & 0xFFFFFFFF);
            _idt[index].Selector = selector;
            _idt[index].TypeAttr = (byte)type;
            _idt[index].IST = ist;
            _idt[index].Reserved = 0;
        }

        internal static void Disable()
        {
            Native.CLI();
        }

        internal static void Enable()
        {
            Native.STI();
        }

        /// <summary>
        /// Gets the interrupt stub address
        /// </summary>
        [DllImport("*", EntryPoint = "_GetInterruptStubAddress")]
        private static extern IntPtr GetInterruptStubAddress();

        /// <summary>
        /// Loads the IDT into the IDTR register
        /// </summary>
        [DllImport("*", EntryPoint = "_LoadIDT")]
        private static extern void LoadIDT(IDTPointer* idtPtr);
    }
}