using System.Runtime.InteropServices;

namespace Kernel.Boot
{
    /// <summary>
    /// Constantes de flags para la estructura Multiboot
    /// </summary>
    public static class MultibootFlags
    {
        public const uint MEMORY = 0x1;            // Información de memoria disponible
        public const uint BOOTDEVICE = 0x2;        // Información de dispositivo de arranque disponible
        public const uint CMDLINE = 0x4;           // Línea de comandos disponible
        public const uint MODULES = 0x8;           // Módulos disponibles
        public const uint SYMS_AOUT = 0x10;        // Símbolos a.out disponibles
        public const uint SYMS_ELF = 0x20;         // Símbolos ELF disponibles
        public const uint MMAP = 0x40;             // Mapa de memoria disponible
        public const uint DRIVES = 0x80;           // Información de unidades disponible
        public const uint CONFIG_TABLE = 0x100;    // Tabla de configuración disponible
        public const uint BOOTLOADER_NAME = 0x200; // Nombre del bootloader disponible
        public const uint APM_TABLE = 0x400;       // Tabla APM disponible
        public const uint VBE_INFO = 0x800;        // Información VBE disponible
        public const uint FRAMEBUFFER_INFO = 0x1000; // Información de framebuffer disponible
    }

    /// <summary>
    /// Estructura de información Multiboot proporcionada por el bootloader compatible con Multiboot
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MultibootInfo
    {
        /// <summary>
        /// Flags que indican qué campos son válidos
        /// </summary>
        public uint Flags;
        
        /// <summary>
        /// Memoria baja disponible en KB (si Flags & MEMORY)
        /// </summary>
        public uint MemLow;
        
        /// <summary>
        /// Memoria alta disponible en KB (si Flags & MEMORY)
        /// </summary>
        public uint MemHigh;
        
        /// <summary>
        /// Dispositivo de arranque (si Flags & BOOTDEVICE)
        /// </summary>
        public uint BootDev;
        
        /// <summary>
        /// Puntero a la línea de comandos (si Flags & CMDLINE)
        /// </summary>
        public uint CMDLine;
        
        /// <summary>
        /// Número de módulos cargados (si Flags & MODULES)
        /// </summary>
        public uint ModCount;
        
        /// <summary>
        /// Dirección del primer módulo (si Flags & MODULES)
        /// </summary>
        public uint ModAddr;
        
        /// <summary>
        /// Información de símbolos (si Flags & SYMS_AOUT o SYMS_ELF)
        /// </summary>
        public uint Syms1;
        public uint Syms2;
        public uint Syms3;
        public uint Syms4;
        
        /// <summary>
        /// Longitud del mapa de memoria (si Flags & MMAP)
        /// </summary>
        public uint MMapLen;
        
        /// <summary>
        /// Dirección del mapa de memoria (si Flags & MMAP)
        /// </summary>
        public uint MMapAddr;
        
        /// <summary>
        /// Longitud de la información de unidades (si Flags & DRIVES)
        /// </summary>
        public uint DrvLen;
        
        /// <summary>
        /// Dirección de la información de unidades (si Flags & DRIVES)
        /// </summary>
        public uint DrvAddr;
        
        /// <summary>
        /// Tabla de configuración (si Flags & CONFIG_TABLE)
        /// </summary>
        public uint CfgTable;
        
        /// <summary>
        /// Nombre del cargador de arranque (si Flags & BOOTLOADER_NAME)
        /// </summary>
        public uint LdrName;
        
        /// <summary>
        /// Tabla APM (si Flags & APM_TABLE)
        /// </summary>
        public uint ApmTable;
        
        /// <summary>
        /// Control de información VBE (si Flags & VBE_INFO)
        /// </summary>
        public uint VBECtrlInfo;
        
        /// <summary>
        /// Información del modo VBE (si Flags & VBE_INFO)
        /// </summary>
        public uint VBEInfo;
        
        /// <summary>
        /// Número de modo VBE (si Flags & VBE_INFO)
        /// </summary>
        public ushort VBEMode;
        
        /// <summary>
        /// Segmento de la interfaz VBE (si Flags & VBE_INFO)
        /// </summary>
        public ushort VBEInterfaceSeg;
        
        /// <summary>
        /// Offset de la interfaz VBE (si Flags & VBE_INFO)
        /// </summary>
        public ushort VBEInterfaceOff;
        
        /// <summary>
        /// Longitud de la interfaz VBE (si Flags & VBE_INFO)
        /// </summary>
        public ushort VBEInterfaceLen;
        
        /// <summary>
        /// Dirección del framebuffer (si Flags & FRAMEBUFFER_INFO)
        /// </summary>
        public ulong FramebufferAddr;
        
        /// <summary>
        /// Pitch del framebuffer (si Flags & FRAMEBUFFER_INFO)
        /// </summary>
        public uint FramebufferPitch;
        
        /// <summary>
        /// Ancho del framebuffer (si Flags & FRAMEBUFFER_INFO)
        /// </summary>
        public uint FramebufferWidth;
        
        /// <summary>
        /// Altura del framebuffer (si Flags & FRAMEBUFFER_INFO)
        /// </summary>
        public uint FramebufferHeight;
        
        /// <summary>
        /// Bits por pixel (si Flags & FRAMEBUFFER_INFO)
        /// </summary>
        public byte FramebufferBpp;
        
        /// <summary>
        /// Tipo de framebuffer (si Flags & FRAMEBUFFER_INFO)
        /// </summary>
        public byte FramebufferType;
        
        /// <summary>
        /// Información de color del framebuffer (si Flags & FRAMEBUFFER_INFO)
        /// </summary>
        public fixed byte FramebufferColorInfo[6];

        /// <summary>
        /// Obtiene un puntero a los módulos cargados
        /// </summary>
        public uint* Mods => (uint*)ModAddr;
        
        /// <summary>
        /// Verifica si hay información VBE disponible
        /// </summary>
        public bool HasVBEInfo => (Flags & MultibootFlags.VBE_INFO) != 0;
        
        /// <summary>
        /// Verifica si hay información de framebuffer lineal disponible
        /// </summary>
        public bool HasFramebufferInfo => (Flags & MultibootFlags.FRAMEBUFFER_INFO) != 0;
        
        /// <summary>
        /// Obtiene un puntero a la información VBE si está disponible
        /// </summary>
        public VBEInfo* GetVBEInfo()
        {
            if (!HasVBEInfo)
                return null;
                
            return (VBEInfo*)VBEInfo;
        }
        
        /// <summary>
        /// Obtiene la línea de comandos como string
        /// </summary>
        public string GetCommandLine()
        {
            if ((Flags & MultibootFlags.CMDLINE) == 0)
                return string.Empty;
                
            // Convertir el puntero a string (necesita implementación adicional según tu sistema)
            return null; // TODO: Implementar conversión de puntero a string
        }
        
        /// <summary>
        /// Obtiene el nombre del bootloader
        /// </summary>
        public string GetBootloaderName()
        {
            if ((Flags & MultibootFlags.BOOTLOADER_NAME) == 0)
                return string.Empty;
                
            // Convertir el puntero a string (necesita implementación adicional según tu sistema)
            return null; // TODO: Implementar conversión de puntero a string
        }
    }

    /// <summary>
    /// Información sobre la tarjeta VBE/VESA
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct VESAInfo
    {
        /// <summary>
        /// Firma "VESA" que indica soporte VBE válido
        /// </summary>
        public fixed byte Signature[4];
        
        /// <summary>
        /// Versión VBE; byte alto es versión mayor, byte bajo es versión menor
        /// </summary>
        public ushort Version;
        
        /// <summary>
        /// Puntero segmento:offset al OEM
        /// </summary>
        public uint Oem;
        
        /// <summary>
        /// Campo de bits que describe capacidades de la tarjeta
        /// </summary>
        public uint Capabilities;
        
        /// <summary>
        /// Puntero segmento:offset a lista de modos de video soportados
        /// </summary>
        public uint Video_Modes;
        
        /// <summary>
        /// Cantidad de memoria de video en bloques de 64KB
        /// </summary>
        public ushort Video_Memory;
        
        /// <summary>
        /// Revisión de software
        /// </summary>
        public ushort Software_Rev;
        
        /// <summary>
        /// Puntero segmento:offset a string del vendedor de la tarjeta
        /// </summary>
        public uint Vendor;
        
        /// <summary>
        /// Puntero segmento:offset al nombre del modelo de la tarjeta
        /// </summary>
        public uint Product_Name;
        
        /// <summary>
        /// Puntero segmento:offset a la revisión del producto
        /// </summary>
        public uint Product_Rev;
        
        /// <summary>
        /// Reservado para expansión futura
        /// </summary>
        public fixed byte Reserved[222];
        
        /// <summary>
        /// BIOSes OEM almacenan sus strings en esta área
        /// </summary>
        public fixed byte Oem_Data[256];
    }

    /// <summary>
    /// Información sobre un modo VBE específico
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VBEInfo
    {
        /// <summary>
        /// Atributos del modo
        /// </summary>
        public ushort Attributes;
        
        /// <summary>
        /// Atributos de la ventana A
        /// </summary>
        public byte WindowA;
        
        /// <summary>
        /// Atributos de la ventana B
        /// </summary>
        public byte WindowB;
        
        /// <summary>
        /// Granularidad de la ventana en KB
        /// </summary>
        public ushort Granularity;
        
        /// <summary>
        /// Tamaño de la ventana en KB
        /// </summary>
        public ushort WindowSize;
        
        /// <summary>
        /// Segmento de la ventana A
        /// </summary>
        public ushort SegmentA;
        
        /// <summary>
        /// Segmento de la ventana B
        /// </summary>
        public ushort SegmentB;
        
        /// <summary>
        /// Puntero a la función de cambio de ventana
        /// </summary>
        public uint WinFuncPtr;
        
        /// <summary>
        /// Bytes por línea de escaneo
        /// </summary>
        public ushort Pitch;
        
        /// <summary>
        /// Ancho de la pantalla en píxeles
        /// </summary>
        public ushort ScreenWidth;
        
        /// <summary>
        /// Altura de la pantalla en píxeles
        /// </summary>
        public ushort ScreenHeight;
        
        /// <summary>
        /// Ancho del carácter en píxeles
        /// </summary>
        public byte WChar;
        
        /// <summary>
        /// Altura del carácter en píxeles
        /// </summary>
        public byte YChar;
        
        /// <summary>
        /// Número de planos de memoria
        /// </summary>
        public byte Planes;
        
        /// <summary>
        /// Bits por píxel
        /// </summary>
        public byte BitsPerPixel;
        
        /// <summary>
        /// Número de bancos
        /// </summary>
        public byte Banks;
        
        /// <summary>
        /// Modelo de memoria
        /// </summary>
        public byte MemoryModel;
        
        /// <summary>
        /// Tamaño del banco en KB
        /// </summary>
        public byte BankSize;
        
        /// <summary>
        /// Número de páginas de imagen
        /// </summary>
        public byte ImagePages;
        
        /// <summary>
        /// Reservado para compatibilidad con VBE 1.0
        /// </summary>
        public byte Reserved0;
        
        // Campos de color directo
        public byte RedMask;
        public byte RedPosition;
        public byte GreenMask;
        public byte GreenPosition;
        public byte BlueMask;
        public byte BluePosition;
        public byte ReservedMask;
        public byte ReservedPosition;
        public byte DirectColorAttributes;
        
        /// <summary>
        /// Dirección física del framebuffer
        /// </summary>
        public uint PhysBase;
        
        /// <summary>
        /// Desplazamiento de la memoria fuera de pantalla
        /// </summary>
        public uint OffScreenMemoryOff;
        
        /// <summary>
        /// Tamaño de la memoria fuera de pantalla en KB
        /// </summary>
        public ushort OffScreenMemorySize;
    }
}