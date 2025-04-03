using Kernel.Diagnostics;
using System.Runtime.InteropServices;

namespace Kernel.Memory
{
    /// <summary>
    /// Global Descriptor Table (GDT) manager para arquitectura x86_64
    /// </summary>
    public static unsafe class GDTManager
    {
        // Constantes para tipos de entrada GDT
        private const byte GDT_TYPE_CODE = 0x9A;    // Segmento de código ejecutable de solo lectura
        private const byte GDT_TYPE_DATA = 0x92;    // Segmento de datos de lectura/escritura
        private const byte GDT_TYPE_TSS = 0x89;     // Task State Segment

        // Flags de descriptores
        private const byte GDT_FLAG_LONG_MODE = 0x20;   // Flag de modo de 64-bit
        private const byte GDT_FLAG_PROTECTED = 0x80;   // Bit de modo protegido de 32-bit
        private const byte GDT_FLAG_4K = 0x40;          // Granularidad de 4K (páginas)

        // Número máximo de entradas GDT
        private const int MAX_DESCRIPTORS = 8;

        // Estructura para una entrada en la GDT de 64 bits
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct GDTEntry
        {
            public ushort LimitLow;     // Límite bits 0-15
            public ushort BaseLow;      // Dirección base bits 0-15
            public byte BaseMiddle;     // Dirección base bits 16-23
            public byte Type;           // Tipo y atributos
            public byte LimitHighFlags; // Límite bits 16-19 y flags
            public byte BaseHigh;       // Dirección base bits 24-31
            public uint BaseUpper;      // Dirección base bits 32-63 (solo usado en descriptores del sistema como TSS)
            public uint Reserved;       // Reservado, debe ser 0
        }

        // Estructura para el puntero GDT que se carga en el registro GDTR
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct GDTPointer
        {
            public ushort Limit;  // Tamaño de GDT menos uno
            public ulong Base;    // Dirección base de GDT (64-bit)
        }

        // Entradas GDT estáticas
        private static GDTEntry _nullEntry;    // Descriptor nulo
        private static GDTEntry _kernelCode;   // Segmento de código del kernel
        private static GDTEntry _kernelData;   // Segmento de datos del kernel
        private static GDTEntry _userCode;     // Segmento de código de usuario (opcional, para multitarea)
        private static GDTEntry _userData;     // Segmento de datos de usuario (opcional, para multitarea)
        private static GDTPointer _gdtPointer; // Puntero a la GDT

        /// <summary>
        /// Inicializa la GDT básica con un modelo de memoria plana para modo de 64 bits
        /// </summary>
        public static void Initialize()
        {
            SerialDebug.Info("Inicializando GDT para 64-bit...");

            // Configurar descriptor nulo (todos los valores en 0)
            _nullEntry = new GDTEntry();

            // Configurar segmento de código para modo de 64 bits (kernel)
            // En modo de 64 bits, base y límite son ignorados excepto para descriptores del sistema
            _kernelCode = new GDTEntry
            {
                LimitLow = 0xFFFF,
                BaseLow = 0,
                BaseMiddle = 0,
                Type = GDT_TYPE_CODE,  // Segmento de código ejecutable y legible
                LimitHighFlags = (byte)(0x0F | GDT_FLAG_LONG_MODE | GDT_FLAG_4K),
                BaseHigh = 0,
                BaseUpper = 0,          // 32 bits superiores para base de 64 bits
                Reserved = 0            // Debe ser cero
            };

            // Configurar segmento de datos (kernel)
            _kernelData = new GDTEntry
            {
                LimitLow = 0xFFFF,
                BaseLow = 0,
                BaseMiddle = 0,
                Type = GDT_TYPE_DATA,  // Segmento de datos de lectura/escritura
                LimitHighFlags = (byte)(0x0F | GDT_FLAG_4K),  // Sin flag LONG_MODE para segmentos de datos
                BaseHigh = 0,
                BaseUpper = 0,
                Reserved = 0
            };

            // Opcional: Configurar segmentos para espacio de usuario si se implementa multitarea
            _userCode = new GDTEntry
            {
                LimitLow = 0xFFFF,
                BaseLow = 0,
                BaseMiddle = 0,
                Type = GDT_TYPE_CODE,  // Código ejecutable y legible
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
                Type = GDT_TYPE_DATA,  // Datos de lectura/escritura
                LimitHighFlags = (byte)(0x0F | GDT_FLAG_4K),
                BaseHigh = 0,
                Reserved = 0
            };

            // Configurar el puntero GDT
            _gdtPointer.Limit = (ushort)(5 * sizeof(GDTEntry) - 1);

            // Crear un array de GDTEntry para pasar a la función nativa
            GDTEntry* gdtEntries = stackalloc GDTEntry[5];
            gdtEntries[0] = _nullEntry;
            gdtEntries[1] = _kernelCode;
            gdtEntries[2] = _kernelData;
            gdtEntries[3] = _userCode;
            gdtEntries[4] = _userData;

            fixed (GDTPointer* gdtPtr = &_gdtPointer)
            {
                _gdtPointer.Base = (ulong)gdtEntries;

                SerialDebug.Info("LoadGDT");
                // Cargar la GDT
                LoadGDT(gdtPtr);

                SerialDebug.Info($"Cargando GDT en dirección: 0x{((ulong)gdtEntries).ToStringHex()}");
                SerialDebug.Info($"Tamaño GDT: {_gdtPointer.Limit.ToString()} bytes");
            }


            SerialDebug.Info("GDT de 64 bits inicializada correctamente.");
        }

        /// <summary>
        /// Carga la GDT en el registro GDTR del procesador
        /// </summary>
        [DllImport("*", EntryPoint = "_LoadGDT")]
        private static extern void LoadGDT(GDTPointer* gdtPtr);

        /// <summary>
        /// Recarga los registros de segmento después de cargar la GDT
        /// </summary>
        [DllImport("*", EntryPoint = "_ReloadSegments")]
        private static extern void ReloadSegments();

        /// <summary>
        /// Cambia el valor de los registros de segmento DS, ES, FS, GS y SS
        /// </summary>
        [DllImport("*", EntryPoint = "_SetSegmentRegisters")]
        private static extern void SetSegmentRegisters(ushort selector);

        /// <summary>
        /// Configura una Task State Segment (TSS) para gestión de tareas
        /// Nota: Esta función se debe implementar si se va a utilizar cambios de tarea
        /// </summary>
        public static void SetupTSS(ulong tssAddress)
        {
            // Implementación de configuración de TSS
            // La TSS en modo 64-bit requiere un descriptor de 16 bytes (dos entradas GDT)

            // Esta implementación se añadiría en una fase posterior cuando se implemente
            // la gestión de tareas y manejo de interrupciones avanzado
        }
    }
}