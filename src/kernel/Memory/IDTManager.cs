using Kernel.Diagnostics;
using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Kernel.Memory
{
    /// <summary>
    /// Administrador de la Interrupt Descriptor Table (IDT)
    /// </summary>
    public static unsafe class IDTManager
    {
        // Constantes para tipos de entradas
        private const byte IDT_TYPE_INTERRUPT = 0x8E;    // Puerta de interrupción de 32 bits
        private const byte IDT_TYPE_TRAP = 0x8F;         // Puerta de trampa de 32 bits
        private const byte IDT_TYPE_TASK = 0x85;         // Puerta de tarea de 32 bits

        // Número máximo de entradas en la IDT (256 interrupciones posibles)
        private const int IDT_ENTRIES = 256;

        // La IDT en sí
        private static IDTEntry* _idt;

        // Puntero a la IDT
        private static IDTPointer _idtPointer;


        // En vez de usar delegados, definir punteros a funciones directamente
        // Esto evita problemas con el escáner de IL
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void RawInterruptHandler();

        // Tabla de punteros a funciones (inicialmente nulos)
        private static RawInterruptHandler* _handlerTable;

        /// <summary>
        /// Inicializa la IDT
        /// </summary>
        /// <summary>
        /// Inicializa la IDT
        /// </summary>
        public static void Initialize()
        {
            SerialDebug.Info("Inicializando IDT...");

            // Asignar memoria para la IDT
            _idt = (IDTEntry*)NativeMemory.Alloc((nuint)(sizeof(IDTEntry) * IDT_ENTRIES));

            // Asignar memoria para la tabla de manejadores
            _handlerTable = (RawInterruptHandler*)NativeMemory.Alloc((nuint)(sizeof(RawInterruptHandler) * IDT_ENTRIES));

            // Inicializar todos los manejadores a null (reemplazar con alguna implementación por defecto)
            for (int i = 0; i < IDT_ENTRIES; i++)
            {
                _handlerTable[i] = null;
            }

            // Configurar el puntero a la IDT
            _idtPointer.Limit = (ushort)(IDT_ENTRIES * sizeof(IDTEntry) - 1);
            _idtPointer.Base = (uint)_idt;

            // Cargar la IDT - necesitamos usar fixed para obtener la dirección
            fixed (IDTPointer* idtPtr = &_idtPointer)
            {
                LoadIDT(idtPtr);
            }

            SerialDebug.Info("IDT inicializada correctamente.");
        }

        // Método para obtener la dirección del manejador por defecto
        private static IntPtr GetDefaultInterruptHandlerAddress()
        {
            // Obtener la dirección del método DefaultInterruptHandler
            // Esto asume que DefaultInterruptHandler tiene un atributo RuntimeExport
            return GetExportedMethodAddress("DefaultInterruptHandler");
        }

        // Método auxiliar para obtener direcciones de métodos exportados
        [DllImport("*", EntryPoint = "GetExportedMethodAddress")]
        private static extern IntPtr GetExportedMethodAddress(string methodName);

        /// <summary>
        /// Configura una entrada en la IDT
        /// </summary>
        /// <param name="index">Índice de la interrupción</param>
        /// <param name="handler">Dirección del manejador</param>
        /// <param name="selector">Selector de segmento</param>
        /// <param name="type">Tipo de entrada</param>
        public static void SetEntry(int index, uint handler, ushort selector, byte type)
        {
            if (index >= IDT_ENTRIES)
                return;

            _idt[index].BaseLow = (ushort)(handler & 0xFFFF);
            _idt[index].BaseHigh = (ushort)((handler >> 16) & 0xFFFF);
            _idt[index].Reserved = 0;
            _idt[index].Flags = type;
            _idt[index].Selector = selector;
        }

        /// <summary>
        /// Registra un manejador de interrupción personalizado
        /// </summary>
        /// <param name="index">Número de interrupción</param>
        /// <param name="handler">Función manejadora</param>
        public static void RegisterHandler(int interruptNumber, RawInterruptHandler handler)
        {
            if (interruptNumber >= 0 && interruptNumber < IDT_ENTRIES)
            {
                _handlerTable[interruptNumber] = handler;

                // Aquí necesitaríamos registrar un nuevo manejador en la IDT
                // que luego llame a _handlers[index]
                // Esto requeriría código de assembly adicional para crear un stub
            }
        }

        /// <summary>
        /// Manejador por defecto para interrupciones no manejadas
        /// </summary>
        [RuntimeExport("DefaultInterruptHandler")]
        public static void DefaultInterruptHandler()
        {
            // Manejador por defecto que simplemente ignora la interrupción
            SerialDebug.Warning("Interrupción no manejada recibida.");
        }

        /// <summary>
        /// Carga la IDT en el registro IDTR del procesador
        /// </summary>
        [DllImport("*", EntryPoint = "_LoadIDT")]
        private static extern void LoadIDT(IDTPointer* idtPtr);
    }

    /// <summary>
    /// Estructura para una entrada en la tabla de descriptores de interrupción (IDT)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IDTEntry
    {
        public ushort BaseLow;    // Bits 0-15 de la dirección del manejador
        public ushort Selector;   // Selector de segmento de código
        public byte Reserved;     // Reservado, debe ser 0
        public byte Flags;        // Flags y tipo
        public ushort BaseHigh;   // Bits 16-31 de la dirección del manejador
    }

    /// <summary>
    /// Estructura para el puntero a la IDT que se carga en el registro IDTR
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IDTPointer
    {
        public ushort Limit;  // Tamaño de la IDT menos uno
        public uint Base;     // Dirección base de la IDT
    }
}