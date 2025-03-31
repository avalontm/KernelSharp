using System.Runtime.InteropServices;


namespace Kernel.Boot
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct MultibootInfo
    {
        public uint Flags;        // Flags que indican qué información está disponible
        public uint MemLower;     // Cantidad de memoria inferior en KB
        public uint MemUpper;     // Cantidad de memoria superior en KB
        public uint BootDevice;   // Dispositivo de arranque
        public uint CmdLine;      // Puntero a la línea de comandos
        public uint ModsCount;    // Número de módulos cargados
        public uint ModsAddr;     // Dirección de la lista de módulos

        // Información de la tabla de símbolos
        public uint Syms1;        // Tamaño de la tabla de símbolos
        public uint Syms2;        // Dirección de la tabla de símbolos
        public uint Syms3;        // Tamaño de la tabla de strings
        public uint Syms4;        // Dirección de la tabla de strings

        public uint MmapLength;   // Longitud del mapa de memoria
        public uint MmapAddr;     // Dirección del mapa de memoria

        public uint DrivesLength; // Longitud de la información de drives
        public uint DrivesAddr;   // Dirección de la información de drives

        public uint ConfigTable;  // Tabla de configuración de ROM

        public uint BootloaderName; // Puntero al nombre del bootloader

        public uint ApmTable;     // Tabla APM

        public uint VbeControlInfo; // Información de control VBE
        public uint VbeModeInfo;    // Información de modo VBE
        public ushort VbeMode;      // Modo VBE actual
        public ushort VbeInterfaceSeg; // Segmento de interfaz VBE
        public ushort VbeInterfaceOff;  // Offset de interfaz VBE
        public ushort VbeInterfaceLen;  // Longitud de interfaz VBE

        // Campo adicional que no está en la especificación Multiboot 1 pero es útil tener
       // public uint Magic;        // Magic number (debería ser 0x2BADB002)
    }

    /// <summary>
    /// Multiboot flags constants
    /// </summary>
    public static class MultibootFlags
    {
        public const uint MEMORY = 0x1;
        public const uint BOOTDEVICE = 0x2;
        public const uint CMDLINE = 0x4;
        public const uint MODULES = 0x8;
        public const uint SYMS_AOUT = 0x10;
        public const uint SYMS_ELF = 0x20;
        public const uint MMAP = 0x40;
        public const uint DRIVES = 0x80;
        public const uint CONFIG_TABLE = 0x100;
        public const uint BOOTLOADER_NAME = 0x200;
        public const uint APM_TABLE = 0x400;
        public const uint VBE_INFO = 0x800;
        public const uint FRAMEBUFFER_INFO = 0x1000;
    }
}
