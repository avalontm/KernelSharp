using Kernel.Diagnostics;
using System.Runtime.InteropServices;

namespace Kernel.Memory
{
    /// <summary>
    /// Administrador de la Global Descriptor Table (GDT)
    /// </summary>
    public static unsafe class GDTManager
    {
        // Constantes para tipos de entrada GDT
        private const byte GDT_TYPE_CODE = 0x9A;    // Segmento de código ejecutable-legible
        private const byte GDT_TYPE_DATA = 0x92;    // Segmento de datos legible-escribible
        private const byte GDT_TYPE_TSS = 0x89;     // Segmento para TSS (Task State Segment)

        // Flags para descriptor
        private const byte GDT_FLAG_PROTECTED = 0x80;   // Bit para modo protegido de 32 bits
        private const byte GDT_FLAG_4K = 0x40;          // Granularidad de 4K (páginas)

        // Tamaño máximo de entradas GDT (normalmente no necesitamos tantas)
        private const int MAX_DESCRIPTORS = 8;

        // Array que contiene los descriptores de la GDT
        private static GDTEntry[] _gdt = new GDTEntry[MAX_DESCRIPTORS];
        // Define las entradas GDT estáticamente
        private static GDTEntry _nullEntry;      // Descriptor nulo
        private static GDTEntry _codeEntry;      // Segmento de código
        private static GDTEntry _dataEntry;      // Segmento de datos
        private static GDTPointer _gdtPointer;

        /// <summary>
        /// Inicializa la GDT básica con segmentos planos.
        /// </summary>
        public static void Initialize()
        {
            //SendString.Info("Inicializando GDT...");
            // Configurar descriptor nulo (todos los valores en 0)
            _nullEntry = new GDTEntry();

            // Configurar segmento de código
            _codeEntry = new GDTEntry
            {
                LimitLow = 0xFFFF,
                BaseLow = 0,
                BaseMiddle = 0,
                Type = GDT_TYPE_CODE,
                LimitHighFlags = (byte)(0x0F | GDT_FLAG_PROTECTED | GDT_FLAG_4K),
                BaseHigh = 0
            };

            // Configurar segmento de datos
            _dataEntry = new GDTEntry
            {
                LimitLow = 0xFFFF,
                BaseLow = 0,
                BaseMiddle = 0,
                Type = GDT_TYPE_DATA,
                LimitHighFlags = (byte)(0x0F | GDT_FLAG_PROTECTED | GDT_FLAG_4K),
                BaseHigh = 0
            };

            // Configurar puntero GDT
            _gdtPointer.Limit = (ushort)(3 * sizeof(GDTEntry) - 1);

            fixed (GDTEntry* gdtPtr = &_nullEntry)
            {
                _gdtPointer.Base = (uint)gdtPtr;

                fixed (GDTPointer* gdtPointerPtr = &_gdtPointer)
                {
                    LoadGDT(gdtPointerPtr);
                }
            }

            ReloadSegments();

            //SendString.Info("GDT inicializada.");
        }

        /// <summary>
        /// Configura una entrada de la GDT.
        /// </summary>
        private static void SetEntry(int index, uint base_addr, uint limit, byte type, byte flags)
        {
            if (index >= MAX_DESCRIPTORS)
                return;

            // Configura la base
            _gdt[index].BaseLow = (ushort)(base_addr & 0xFFFF);
            _gdt[index].BaseMiddle = (byte)((base_addr >> 16) & 0xFF);
            _gdt[index].BaseHigh = (byte)((base_addr >> 24) & 0xFF);

            // Configura el límite
            _gdt[index].LimitLow = (ushort)(limit & 0xFFFF);
            _gdt[index].LimitHighFlags = (byte)((limit >> 16) & 0x0F);

            // Configura el tipo y flags
            _gdt[index].Type = type;
            _gdt[index].LimitHighFlags |= (byte)(flags & 0xF0);
        }

        /// <summary>
        /// Carga la GDT en el registro GDTR del procesador.
        /// </summary>
        [DllImport("*", EntryPoint = "_LoadGDT")]
        private static extern void LoadGDT(GDTPointer* gdtPtr);

        /// <summary>
        /// Recarga los registros de segmento después de cargar la GDT.
        /// </summary>
        [DllImport("*", EntryPoint = "_ReloadSegments")]
        private static extern void ReloadSegments();
    }

    /// <summary>
    /// Estructura para una entrada en la tabla de descriptores globales (GDT).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GDTEntry
    {
        public ushort LimitLow;     // Bits 0-15 del límite
        public ushort BaseLow;      // Bits 0-15 de la dirección base
        public byte BaseMiddle;     // Bits 16-23 de la dirección base
        public byte Type;           // Tipo y atributos
        public byte LimitHighFlags; // Bits 16-19 del límite y flags
        public byte BaseHigh;       // Bits 24-31 de la dirección base
    }

    /// <summary>
    /// Estructura para el puntero a la GDT que se carga en el registro GDTR.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GDTPointer
    {
        public ushort Limit;  // Tamaño de la GDT menos uno
        public uint Base;     // Dirección base de la GDT
    }
}