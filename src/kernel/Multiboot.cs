using System.Runtime.InteropServices;

namespace Kernel
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct MultibootInfo
    {
        // Flags que indican qué campos son válidos
        public uint Flags;

        // Información de memoria disponible, válida si (Flags & 0x1) != 0
        public uint MemLower;
        public uint MemUpper;

        // Dispositivo de arranque, válido si (Flags & 0x2) != 0
        public uint BootDevice;

        // Línea de comandos, válida si (Flags & 0x4) != 0
        public uint CommandLine;

        // Módulos cargados, válidos si (Flags & 0x8) != 0
        public uint ModsCount;
        public uint ModsAddr;

        // Información de la tabla de símbolos, válida si (Flags & 0x10) != 0 o (Flags & 0x20) != 0
        public uint Syms1;
        public uint Syms2;
        public uint Syms3;
        public uint Syms4;

        // Mapa de memoria, válido si (Flags & 0x40) != 0
        public uint MmapLength;
        public uint MmapAddr;

        // Información de dispositivos, válida si (Flags & 0x80) != 0
        public uint DrivesLength;
        public uint DrivesAddr;

        // Tabla de configuración BIOS, válida si (Flags & 0x100) != 0
        public uint ConfigTable;

        // Nombre del cargador de arranque, válido si (Flags & 0x200) != 0
        public uint BootLoaderName;

        // Tabla APM, válida si (Flags & 0x400) != 0
        public uint ApmTable;

        // Información de la tarjeta de video, válida si (Flags & 0x800) != 0
        public uint VbeControlInfo;
        public uint VbeModeInfo;
        public ushort VbeMode;
        public ushort VbeInterfaceSeg;
        public ushort VbeInterfaceOff;
        public ushort VbeInterfaceLen;

        // Información de framebuffer, válida si (Flags & 0x1000) != 0
        public ulong FramebufferAddr;
        public uint FramebufferPitch;
        public uint FramebufferWidth;
        public uint FramebufferHeight;
        public byte FramebufferBpp;
        public byte FramebufferType;
        public fixed byte FramebufferColorInfo[6];
    }

    // Estructura para los módulos cargados
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct MultibootModule
    {
        public uint ModStart;
        public uint ModEnd;
        public uint String;
        public uint Reserved;
    }

    // Estructura para una entrada en el mapa de memoria
    [StructLayout(LayoutKind.Sequential)]
    public struct MultibootMmapEntry
    {
        public uint Size;
        public ulong BaseAddr;
        public ulong Length;
        public uint Type;
    }

    // Constantes para las banderas (flags)
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

