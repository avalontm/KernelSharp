using System.Runtime.InteropServices;


namespace Kernel.Boot
{
    /// <summary>
    /// Multiboot header structure and VBE/VESA information for x86 boot process
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MultibootInfo
    {
        public uint Flags;
        public uint MemLow;
        public uint MemHigh;
        public uint BootDev;
        public uint CMDLine;
        public uint ModCount;
        public uint ModAddr;
        public uint Syms1;
        public uint Syms2;
        public uint Syms3;
        public uint Syms4;
        public uint MMapLen;
        public uint MMapAddr;
        public uint DrvLen;
        public uint DrvAddr;
        public uint CfgTable;
        public uint LdrName;
        public uint ApmTable;
        public uint VBECtrlInfo;
        public uint VBEInfo;
        public uint VBEMode;
        public uint VBEInterfaceSeg;
        public uint VBEInterfaceOff;
        public uint VBEInterfaceLen;

        public uint* Mods
        {
            get
            {
                return (uint*)ModAddr;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct VESAInfo
    {
        public fixed char Signature[4]; // must be "VESA" to indicate valid VBE support

        public ushort Version;         // VBE version; high byte is major version, low byte is minor version
        public uint Oem;                // segment:offset pointer to OEM
        public uint Capabilities;       // bitfield that describes card capabilities
        public uint Video_Modes;        // segment:offset pointer to list of supported video modes
        public ushort Video_Memory;     // amount of video memory in 64KB blocks
        public ushort Software_Rev;     // software revision
        public uint Vendor;             // segment:offset to card vendor string
        public uint Product_Name;       // segment:offset to card model name
        public uint Product_Rev;        // segment:offset pointer to product revision

        public fixed char Reserved[222];         // reserved for future expansion

        public fixed char Oem_Data[256];         // OEM BIOSes store their strings in this area
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VBEInfo
    {
        public ushort Attributes;
        public byte WindowA;
        public byte WindowB;
        public ushort Granularity;
        public ushort WindowSize;
        public ushort SegmentA;
        public ushort SegmentB;
        public uint WinFuncPtr;
        public ushort Pitch;
        public ushort ScreenWidth;
        public ushort ScreenHeight;
        public byte WChar;
        public byte YChar;
        public byte Planes;
        public byte BitsPerPixel;
        public byte Banks;
        public byte MemoryModel;
        public byte BankSize;
        public byte ImagePages;
        public byte Reserved0;
        public byte RedMask;
        public byte RedPosition;
        public byte GreenMask;
        public byte GreenPosition;
        public byte BlueMask;
        public byte BluePosition;
        public byte ReservedMask;
        public byte ReservedPosition;
        public byte DirectColorAttributes;
        public uint PhysBase;
        public uint OffScreenMemoryOff;
        public ushort OffScreenMemorySize;
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
