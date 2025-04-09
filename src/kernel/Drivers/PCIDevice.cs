using Kernel.Diagnostics;
using System;
using System.Diagnostics;

namespace Kernel.Drivers
{
    /// <summary>
    /// PCI device class identifiers
    /// </summary>
    public enum PCIClassID : byte
    {
        PCIDevice_2_0 = 0x00,
        MassStorageController = 0x01,
        NetworkController = 0x02,
        DisplayController = 0x03,
        MultimediaDevice = 0x04,
        MemoryController = 0x05,
        BridgeDevice = 0x06,
        SimpleCommController = 0x07,
        BaseSystemPeripheral = 0x08,
        InputDevice = 0x09,
        DockingStations = 0x0A,
        Processors = 0x0B,
        SerialBusController = 0x0C,
        WirelessController = 0x0D,
        IntelligentController = 0x0E,
        SatelliteCommController = 0x0F,
        EncryptionController = 0x10,
        SignalProcessingController = 0x11,
        ProcessingAccelerators = 0x12,
        NonEssentialInstrumentation = 0x13,
        Coprocessor = 0x40,
        Unclassified = 0xFF
    }

    /// <summary>
    /// PCI vendor identifiers
    /// </summary>
    public enum PCIVendorID : ushort
    {
        Intel = 0x8086,
        AMD = 0x1022,
        VMWare = 0x15AD,
        Bochs = 0x1234,
        VirtualBox = 0x80EE
    }

    /// <summary>
    /// Structure representing a PCI device identifier
    /// </summary>
    public struct PCIDeviceID
    {
        public ushort VendorID;
        public ushort DeviceID;
        public ushort SubsystemVendorID;
        public ushort SubsystemID;
        public byte RevisionID;
        public byte ProgIF;
        public byte Subclass;
        public byte ClassCode;

        public override string ToString()
        {
            return $"PCI Device {((ulong)ClassCode).ToStringHex()}.{((ulong)Subclass).ToStringHex()}.{((ulong)ProgIF).ToStringHex()} - Vendor: {((ulong)VendorID).ToStringHex()}, Device: {((ulong)DeviceID).ToStringHex()}";
        }
    }

    /// <summary>
    /// Structure representing a PCI function's location
    /// </summary>
    public struct PCILocation
    {
        public byte Bus;
        public byte Device;
        public byte Function;

        public override string ToString()
        {
            return $"{((ulong)Bus).ToString()}:{((ulong)Device).ToString()}.{((ulong)Function).ToString()}";
        }

        public string GetLocationID()
        {
            return $"{Bus}:{Device}.{Function}";
        }
    }

    /// <summary>
    /// Base class for PCI device information
    /// </summary>
    public class PCIDevice
    {
        public PCIDeviceID ID { get; }
        public PCILocation Location { get; }
        public byte HeaderType { get; }
        public byte InterruptLine { get; }
        public byte InterruptPin { get; }
        public uint[] BAR { get;}
        public bool IsBridge { get; }
        public byte SecondaryBus { get; }
        public byte SubordinateBus { get; }

        public PCIDevice(PCIDeviceID id, PCILocation location, byte headerType, byte interruptLine, byte interruptPin, uint[] bars, bool isBridge, byte secondaryBus, byte subordinateBus)
        {
            BAR = new uint[6];
            ID = id;
            Location = location;
            HeaderType = headerType;
            InterruptLine = interruptLine;
            InterruptPin = interruptPin;
            if (bars != null && bars.Length == 6)
            {
                for (int i = 0; i < bars.Length; i++)
                {
                    BAR[i] = bars[i];
                }
            }

            IsBridge = isBridge;
            SecondaryBus = secondaryBus;
            SubordinateBus = subordinateBus;
        }

        public override string ToString()
        {
            return Location.ToString() + " - " + GetDeviceTypeName() + " " + ID.ToString();
        }

        /// <summary>
        /// Gets a descriptive device type based on class codes
        /// </summary>
        public string GetDeviceTypeName()
        {
            return (ID.ClassCode, ID.Subclass) switch
            {
                (0x00, 0x00) => "Unknown/Pre-PCI 2.0",
                (0x00, 0x01) => "VGA-Compatible Device",

                (0x01, 0x00) => "SCSI Controller",
                (0x01, 0x01) => "IDE Controller",
                (0x01, 0x02) => "Floppy Controller",
                (0x01, 0x03) => "IPI Controller",
                (0x01, 0x04) => "RAID Controller",
                (0x01, 0x05) => "ATA Controller",
                (0x01, 0x06) => "SATA Controller",
                (0x01, 0x07) => "SAS Controller",
                (0x01, 0x08) => "NVMe Controller",
                (0x01, 0x80) => "Other Storage Controller",

                (0x02, 0x00) => "Ethernet Controller",
                (0x02, 0x01) => "Token Ring Controller",
                (0x02, 0x02) => "FDDI Controller",
                (0x02, 0x03) => "ATM Controller",
                (0x02, 0x04) => "ISDN Controller",
                (0x02, 0x05) => "WorldFip Controller",
                (0x02, 0x06) => "PICMG Controller",
                (0x02, 0x07) => "InfiniBand Controller",
                (0x02, 0x80) => "Other Network Controller",

                (0x03, 0x00) => "VGA Controller",
                (0x03, 0x01) => "XGA Controller",
                (0x03, 0x02) => "3D Controller",
                (0x03, 0x80) => "Other Display Controller",

                (0x04, 0x00) => "Video Controller",
                (0x04, 0x01) => "Audio Controller",
                (0x04, 0x02) => "Computer Telephony Device",
                (0x04, 0x03) => "HD Audio Controller",
                (0x04, 0x80) => "Other Multimedia Controller",

                (0x05, 0x00) => "RAM Controller",
                (0x05, 0x01) => "Flash Controller",
                (0x05, 0x80) => "Other Memory Controller",

                (0x06, 0x00) => "Host Bridge",
                (0x06, 0x01) => "ISA Bridge",
                (0x06, 0x02) => "EISA Bridge",
                (0x06, 0x03) => "MCA Bridge",
                (0x06, 0x04) => "PCI-to-PCI Bridge",
                (0x06, 0x05) => "PCMCIA Bridge",
                (0x06, 0x06) => "NuBus Bridge",
                (0x06, 0x07) => "CardBus Bridge",
                (0x06, 0x08) => "RACEway Bridge",
                (0x06, 0x09) => "Semi-Transparent PCI-to-PCI Bridge",
                (0x06, 0x0A) => "InfiniBand-to-PCI Bridge",
                (0x06, 0x80) => "Other Bridge Device",

                (0x07, 0x00) => "Generic Serial Controller",
                (0x07, 0x01) => "16450 Serial Controller",
                (0x07, 0x02) => "16550 Serial Controller",
                (0x07, 0x03) => "16650 Serial Controller",
                (0x07, 0x04) => "16750 Serial Controller",
                (0x07, 0x05) => "16850 Serial Controller",
                (0x07, 0x06) => "16950 Serial Controller",

                (0x08, 0x00) => "Generic System Peripheral",
                (0x08, 0x01) => "PIC",
                (0x08, 0x02) => "DMA Controller",
                (0x08, 0x03) => "Timer",
                (0x08, 0x04) => "RTC Controller",
                (0x08, 0x05) => "PCI Hot-Plug Controller",
                (0x08, 0x06) => "SD Host Controller",
                (0x08, 0x80) => "Other System Peripheral",

                (0x0C, 0x00) => "FireWire (IEEE 1394) Controller",
                (0x0C, 0x01) => "ACCESS Bus Controller",
                (0x0C, 0x02) => "SSA Controller",
                (0x0C, 0x03) => "USB Controller",
                (0x0C, 0x04) => "Fibre Channel Controller",
                (0x0C, 0x05) => "SMBus Controller",
                (0x0C, 0x06) => "InfiniBand Controller",
                (0x0C, 0x07) => "IPMI Interface",
                (0x0C, 0x08) => "SERCOS Interface",
                (0x0C, 0x09) => "CANbus Controller",
                (0x0C, 0x80) => "Other Serial Bus Controller",

                _ => $"Unknown ({ID.ClassCode.ToString()}:{ID.Subclass.ToString()})"
            };
        }
    }

}
