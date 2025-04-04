using Kernel.Diagnostics;
using System;
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
        internal static IDTEntry* _idt;

        // IDT Pointer for LIDT instruction
        internal static IDTPointer _idtPointer;

        /// <summary>
        /// Initializes the IDT
        /// </summary>
        public static void Initialize()
        {
            SerialDebug.Info("Initializing Interrupt Descriptor Table (IDT)...");

            // Allocate memory for the IDT
            _idt = (IDTEntry*)Allocator.malloc((nuint)(sizeof(IDTEntry) * IDT_SIZE));

            // Verificación detallada de la dirección del stub
            IntPtr stubHandler = GetInterruptStubAddress();

            // Log de diagnóstico
            SerialDebug.Info($"Interrupt Stub Address: 0x{((ulong)stubHandler).ToStringHex()}");

            if (stubHandler == IntPtr.Zero)
            {
                // Método de detención de bajo nivel
                SerialDebug.Error("CRITICAL: Cannot obtain interrupt stub address");
                Native.CLI();  // Deshabilitar interrupciones
                while (true)
                {
                    Native.Halt(); // Detener el sistema
                }
            }

            // Configurar todas las entradas de la IDT
            for (int i = 0; i < IDT_SIZE; i++)
            {
                SetIDTEntry(i, stubHandler, 0x08, IDTGateType.InterruptGate);
            }

            // Cargar IDT
            fixed (IDTPointer* idtPtr = &_idtPointer)
            {
                LoadIDT(idtPtr);
            }

            SerialDebug.Info("IDT initialized successfully");
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