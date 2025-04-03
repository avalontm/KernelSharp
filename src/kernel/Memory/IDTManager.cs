using Kernel.Diagnostics;
using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Kernel.Memory
{
    /// <summary>
    /// Tipo de entrada de la IDT
    /// </summary>
    public enum IDTGateType : byte
    {
        Task = 0x5,             // Compuerta de tarea de 80386
        Interrupt16 = 0x6,      // Compuerta de interrupción de 16 bits
        Trap16 = 0x7,           // Compuerta de trampa de 16 bits
        Interrupt32 = 0xE,      // Compuerta de interrupción de 32 bits
        Trap32 = 0xF,           // Compuerta de trampa de 32 bits
        InterruptGate = 0x8E,   // Compuerta de interrupción (con banderas)
        TrapGate = 0x8F         // Compuerta de trampa (con banderas)
    }

    /// <summary>
    /// Estructura para una entrada en la tabla de descriptores de interrupciones (IDT)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IDTEntry
    {
        public ushort BaseLow;    // Parte baja de la dirección del manejador
        public ushort Selector;   // Selector de segmento de código
        public byte IST;          // Índice de pila de interrupciones (0-7)
        public byte TypeAttr;     // Tipo y atributos
        public ushort BaseMid;    // Parte media de la dirección del manejador
        public uint BaseHigh;     // Parte alta de la dirección del manejador
        public uint Reserved;     // Reservado, debe ser 0
    }

    /// <summary>
    /// Puntero a la IDT para cargar con la instrucción LIDT
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IDTPointer
    {
        public ushort Limit;      // Tamaño de la IDT - 1
        public ulong Base;        // Dirección base de la IDT
    }

    /// <summary>
    /// Gestor de la tabla de descriptores de interrupciones para x86_64
    /// </summary>
    public static unsafe class IDTManager
    {
        // Tamaño de la IDT (256 posibles interrupciones)
        private const int IDT_SIZE = 256;

        // La IDT
        private static IDTEntry* _idt;

        // Puntero a la IDT para la instrucción LIDT
        private static IDTPointer _idtPointer;

        /// <summary>
        /// Inicializa la IDT
        /// </summary>
        public static void Initialize()
        {
            SerialDebug.Info("Inicializando tabla de descriptores de interrupciones (IDT)...");

            // Asignar memoria para la IDT
            _idt = (IDTEntry*)Allocator.malloc((nuint)(sizeof(IDTEntry) * IDT_SIZE));

            // Inicializar todas las entradas a cero
            for (int i = 0; i < IDT_SIZE; i++)
            {
                _idt[i] = new IDTEntry();
            }

            // Configurar el puntero a la IDT
            _idtPointer.Limit = (ushort)(IDT_SIZE * sizeof(IDTEntry) - 1);
            _idtPointer.Base = (ulong)_idt;

            // Configurar controlador de interrupción de stub que redirige a HandleInterrupt
            IntPtr stubHandler = GetInterruptStubAddress();
            if (stubHandler != IntPtr.Zero)
            {
                // Registrar el stub para todas las entradas
                for (int i = 0; i < IDT_SIZE; i++)
                {
                    SetIDTEntry(i, stubHandler, 0x08, IDTGateType.InterruptGate);
                }

                // Cargar la IDT
                fixed (IDTPointer* idtPtr = &_idtPointer)
                {
                    LoadIDT(idtPtr);
                }
                SerialDebug.Info("IDT inicializada correctamente");
            }
            else
            {
                SerialDebug.Info("ERROR: No se pudo obtener la dirección del stub de interrupción");
            }
        }

        /// <summary>
        /// Configura una entrada en la IDT
        /// </summary>
        /// <param name="index">Índice de la entrada (0-255)</param>
        /// <param name="handler">Dirección del manejador</param>
        /// <param name="selector">Selector de segmento de código (0x08 para código de kernel)</param>
        /// <param name="type">Tipo de puerta</param>
        /// <param name="ist">Índice de pila de interrupciones (0 para pila normal)</param>
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
        /// Obtiene la dirección del stub de interrupción
        /// </summary>
        [DllImport("*", EntryPoint = "_GetInterruptStubAddress")]
        private static extern IntPtr GetInterruptStubAddress();

        /// <summary>
        /// Carga la IDT en el registro IDTR
        /// </summary>
        [DllImport("*", EntryPoint = "_LoadIDT")]
        private static extern void LoadIDT(IDTPointer* idtPtr);
    }
}