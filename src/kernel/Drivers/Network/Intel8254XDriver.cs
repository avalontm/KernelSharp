using Kernel.Diagnostics;
using Kernel.Drivers;
using Kernel.Drivers.IO;
using Kernel.Drivers.Network;
using Kernel.Memory;
using System;
using System.Runtime.InteropServices;

namespace Kernel.Drivers.Network
{
    /// <summary>
    /// Driver para controladores de red Intel 8254X (E1000)
    /// Basado en la familia de controladores Intel 82540EM, 82541, 82542, 82543, 82544, 82545, 82546, 82547 entre otros
    /// Documentación: Intel PCI/PCI-X Family of Gigabit Ethernet Controllers Software Developer's Manual
    /// </summary>
    public unsafe class Intel8254XDriver : BaseDriver
    {
        // Direcciones del controlador
        private uint _baseMMIO;            // Base de memoria mapeada I/O
        private PCIDevice _pciDevice;      // Dispositivo PCI
        private MACAddress _macAddress;    // Dirección MAC del adaptador

        // Registros de control y estado
        private const uint REG_CTRL = 0x0000; // Control Register
        private const uint REG_STATUS = 0x0008; // Status Register
        private const uint REG_EEPROM = 0x0014; // EEPROM Register
        private const uint REG_CTRL_EXT = 0x0018; // Extended Control Register
        private const uint REG_ICR = 0x00C0; // Interrupt Cause Read Register
        private const uint REG_IMS = 0x00D0; // Interrupt Mask Set Register
        private const uint REG_IMC = 0x00D8; // Interrupt Mask Clear Register
        private const uint REG_RCTL = 0x0100; // Receive Control Register
        private const uint REG_TCTL = 0x0400; // Transmit Control Register
        private const uint REG_RDBAL = 0x2800; // Rx Descriptor Base Address Low
        private const uint REG_RDBAH = 0x2804; // Rx Descriptor Base Address High
        private const uint REG_RDLEN = 0x2808; // Rx Descriptor Length
        private const uint REG_RDH = 0x2810; // Rx Descriptor Head
        private const uint REG_RDT = 0x2818; // Rx Descriptor Tail
        private const uint REG_TDBAL = 0x3800; // Tx Descriptor Base Address Low
        private const uint REG_TDBAH = 0x3804; // Tx Descriptor Base Address High
        private const uint REG_TDLEN = 0x3808; // Tx Descriptor Length
        private const uint REG_TDH = 0x3810; // Tx Descriptor Head
        private const uint REG_TDT = 0x3818; // Tx Descriptor Tail
        private const uint REG_RAL = 0x5400; // Receive Address Low
        private const uint REG_RAH = 0x5404; // Receive Address High

        // Bits del registro de control (CTRL)
        private const uint CTRL_RESET = (1 << 26); // Reinicio del dispositivo
        private const uint CTRL_SLU = (1 << 6);  // Set Link Up
        private const uint CTRL_ASDE = (1 << 5);  // Auto-Speed Detection Enable
        private const uint CTRL_PHY_RESET = unchecked((uint)(1 << 31)); // PHY Reset

        // Bits del registro Status (STATUS)
        private const uint STATUS_LINK_UP = (1 << 1);  // Link activo

        // Bits del registro de control de recepción (RCTL)
        private const uint RCTL_EN = (1 << 1);  // Receive Enable
        private const uint RCTL_SBP = (1 << 2);  // Store Bad Packets
        private const uint RCTL_UPE = (1 << 3);  // Unicast Promiscuous Enable
        private const uint RCTL_MPE = (1 << 4);  // Multicast Promiscuous Enable
        private const uint RCTL_LBM_NONE = (0 << 6);  // No Loopback
        private const uint RCTL_BAM = (1 << 15); // Broadcast Accept Mode
        private const uint RCTL_BSIZE_2048 = (0 << 16); // Buffer Size 2048
        private const uint RCTL_SECRC = (1 << 26); // Strip Ethernet CRC

        // Bits del registro de control de transmisión (TCTL)
        private const uint TCTL_EN = (1 << 1);  // Transmit Enable
        private const uint TCTL_PSP = (1 << 3);  // Pad Short Packets
        private const uint TCTL_CT_15 = (0xF << 4); // Collision Threshold
        private const uint TCTL_COLD = (0x3F << 12); // Collision Distance

        // Registros adicionales
        private const uint REG_TIPG = 0x0410; // Transmit Inter Packet Gap

        // Número de descriptores
        private const int NUM_RX_DESC = 32;
        private const int NUM_TX_DESC = 8;
        private const int RX_BUFFER_SIZE = 2048;
        private const int TX_BUFFER_SIZE = 2048;

        // Estado de los descriptores
        private ulong _rxDescBase;
        private ulong _txDescBase;
        private int _currentRxDesc;
        private int _currentTxDesc;
        private bool _eepromExists;

        /// <summary>
        /// Estructura para descriptores de recepción (RX)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct RxDesc
        {
            public ulong BufferAddress;   // Dirección del buffer
            public ushort Length;         // Longitud recibida
            public ushort Checksum;       // Checksum
            public byte Status;           // Estado del descriptor
            public byte Errors;           // Errores
            public ushort Special;        // Información especial
        }

        /// <summary>
        /// Estructura para descriptores de transmisión (TX)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TxDesc
        {
            public ulong BufferAddress;   // Dirección del buffer
            public ushort Length;         // Longitud a transmitir
            public byte CSO;              // Checksum Offset
            public byte CMD;              // Comandos
            public byte Status;           // Estado (reservado)
            public byte CSS;              // Checksum Start
            public ushort Special;        // Información especial
        }

        /// <summary>
        /// Constructor del controlador Intel 8254X
        /// </summary>
        /// <param name="device">Dispositivo PCI</param>
        public Intel8254XDriver(PCIDevice device) : base($"intel8254x_{device.Location.GetLocationID()}", "Intel 8254X Gigabit Ethernet Controller")
        {
            _pciDevice = device;
            _currentRxDesc = 0;
            _currentTxDesc = 0;
        }

        /// <summary>
        /// Inicialización del controlador
        /// </summary>
        protected override bool OnInitialize()
        {
            SerialDebug.Info($"Initializing Intel 8254X Network Controller at {_pciDevice.Location}");

            // Habilitar dispositivo PCI (Bus mastering y espacio de memoria)
            EnablePCI();

            // Obtener dirección base del MMIO
            _baseMMIO = (uint)(_pciDevice.BAR[0] & (~0xF));
            SerialDebug.Info($"Intel 8254X Base MMIO: 0x{((ulong)_baseMMIO).ToStringHex()}");

            // Realizar reinicio del hardware
            ResetHardware();

            // Comprobar si existe EEPROM
            _eepromExists = DetectEEPROM();

            // Obtener dirección MAC
            ReadMACAddress();
            SerialDebug.Info($"Intel 8254X MAC Address: {_macAddress}");

            // Inicializar descriptores de recepción
            InitializeRx();

            // Inicializar descriptores de transmisión
            InitializeTx();

            // Configurar y habilitar interrupciones
            SetupInterrupts();

            // Establecer enlace (link up)
            WaitForLinkUp();

            // Habilitar recepción y transmisión
            EnableRxTx();

            SerialDebug.Info($"Intel 8254X Controller initialized successfully");

            return true;
        }

        /// <summary>
        /// Habilita el dispositivo PCI
        /// </summary>
        private void EnablePCI()
        {
            // Habilitar Bus Mastering y espacio de memoria 
            ushort command = PCIManager.ReadConfig16(_pciDevice.Location.Bus, _pciDevice.Location.Device, _pciDevice.Location.Function, 0x04);
            command |= 0x7; // Bus Mastering, Memory Space, I/O Space
            PCIManager.WriteConfig16(_pciDevice.Location.Bus, _pciDevice.Location.Device, _pciDevice.Location.Function, 0x04, command);
        }

        /// <summary>
        /// Reinicia el hardware del controlador
        /// </summary>
        private void ResetHardware()
        {
            SerialDebug.Info("Intel 8254X: Resetting hardware...");

            // Escribir bit de reinicio al registro de control
            WriteRegister(REG_CTRL, CTRL_RESET);

            // Esperar a que el reinicio se complete
            int timeout = 10000;
            while ((ReadRegister(REG_CTRL) & CTRL_RESET) != 0 && timeout > 0)
            {
                Native.Pause();
                timeout--;
            }

            if (timeout <= 0)
            {
                SerialDebug.Warning("Intel 8254X: Reset timeout");
            }

            // Esperar un poco más tras el reinicio
            for (int i = 0; i < 10000; i++)
            {
                Native.Pause();
            }

            SerialDebug.Info("Intel 8254X: Hardware reset complete");
        }

        /// <summary>
        /// Detecta la presencia de una EEPROM
        /// </summary>
        private bool DetectEEPROM()
        {
            SerialDebug.Info("Intel 8254X: Detecting EEPROM...");

            // Iniciar detección escribiendo 1 en el registro EEPROM
            WriteRegister(REG_EEPROM, 1);

            for (int i = 0; i < 1000; i++)
            {
                uint value = ReadRegister(REG_EEPROM);
                if ((value & 0x10) != 0)
                {
                    SerialDebug.Info("Intel 8254X: EEPROM detected");
                    return true;
                }
                Native.Pause();
            }

            SerialDebug.Info("Intel 8254X: No EEPROM detected, using registers");
            return false;
        }

        /// <summary>
        /// Lee un valor de la EEPROM
        /// </summary>
        private ushort ReadEEPROM(uint address)
        {
            uint temp;
            WriteRegister(REG_EEPROM, 1 | (address << 8));

            while (((temp = ReadRegister(REG_EEPROM)) & 0x10) == 0)
            {
                Native.Pause();
            }

            return (ushort)((temp >> 16) & 0xFFFF);
        }

        /// <summary>
        /// Lee la dirección MAC del controlador
        /// </summary>
        private void ReadMACAddress()
        {
            byte[] macBytes = new byte[6];

            if (_eepromExists)
            {
                // Leer MAC desde EEPROM
                ushort temp;
                temp = ReadEEPROM(0);
                macBytes[0] = (byte)(temp & 0xFF);
                macBytes[1] = (byte)(temp >> 8);

                temp = ReadEEPROM(1);
                macBytes[2] = (byte)(temp & 0xFF);
                macBytes[3] = (byte)(temp >> 8);

                temp = ReadEEPROM(2);
                macBytes[4] = (byte)(temp & 0xFF);
                macBytes[5] = (byte)(temp >> 8);
            }
            else
            {
                // Leer MAC desde registros
                uint low = ReadRegister(REG_RAL);
                uint high = ReadRegister(REG_RAH);

                macBytes[0] = (byte)(low & 0xFF);
                macBytes[1] = (byte)((low >> 8) & 0xFF);
                macBytes[2] = (byte)((low >> 16) & 0xFF);
                macBytes[3] = (byte)((low >> 24) & 0xFF);
                macBytes[4] = (byte)(high & 0xFF);
                macBytes[5] = (byte)((high >> 8) & 0xFF);
            }

            _macAddress = new MACAddress(macBytes);
        }

        /// <summary>
        /// Inicializa los descriptores de recepción
        /// </summary>
        private void InitializeRx()
        {
            SerialDebug.Info("Intel 8254X: Initializing RX descriptors...");

            // Asignar memoria para los descriptores de recepción
            _rxDescBase = (ulong)Allocator.malloc((ulong)(NUM_RX_DESC * sizeof(RxDesc)));
            if (_rxDescBase == 0)
            {
                SerialDebug.Error("Intel 8254X: Failed to allocate RX descriptors");
                return;
            }

            // Inicializar descriptores
            for (int i = 0; i < NUM_RX_DESC; i++)
            {
                RxDesc* desc = (RxDesc*)(_rxDescBase + (ulong)(i * sizeof(RxDesc)));

                // Asignar buffer de recepción
                ulong buffer = (ulong)Allocator.malloc((ulong)RX_BUFFER_SIZE);
                if (buffer == 0)
                {
                    SerialDebug.Error("Intel 8254X: Failed to allocate RX buffer");
                    return;
                }

                // Limpiar buffer
                Native.Stosb((void*)buffer, 0, (ulong)RX_BUFFER_SIZE);

                // Configurar descriptor
                desc->BufferAddress = buffer;
                desc->Length = 0;
                desc->Status = 0;
                desc->Errors = 0;
            }

            // Configurar registros de descriptores de recepción
            WriteRegister(REG_RDBAL, (uint)(_rxDescBase & 0xFFFFFFFF));       // Dirección base (parte baja)
            WriteRegister(REG_RDBAH, (uint)(_rxDescBase >> 32));              // Dirección base (parte alta)
            WriteRegister(REG_RDLEN, NUM_RX_DESC * sizeof(RxDesc));           // Longitud total de los descriptores

            // Indices de cabecera y cola (head/tail)
            WriteRegister(REG_RDH, 0);
            WriteRegister(REG_RDT, NUM_RX_DESC - 1);                          // Tail apunta al último descriptor

            // Configurar control de recepción
            uint rctl = RCTL_EN |        // Habilitar recepción
                        RCTL_SBP |       // Almacenar paquetes malos
                        RCTL_UPE |       // Modo promiscuo unicast
                        RCTL_MPE |       // Modo promiscuo multicast
                        RCTL_LBM_NONE |  // Sin loopback
                        RCTL_BAM |       // Aceptar paquetes broadcast
                        RCTL_BSIZE_2048 | // Tamaño de buffer 2048
                        RCTL_SECRC;      // Eliminar CRC

            WriteRegister(REG_RCTL, rctl);
        }

        /// <summary>
        /// Inicializa los descriptores de transmisión
        /// </summary>
        private void InitializeTx()
        {
            SerialDebug.Info("Intel 8254X: Initializing TX descriptors...");

            // Asignar memoria para los descriptores de transmisión
            _txDescBase = (ulong)Allocator.malloc((ulong)(NUM_TX_DESC * sizeof(TxDesc)));
            if (_txDescBase == 0)
            {
                SerialDebug.Error("Intel 8254X: Failed to allocate TX descriptors");
                return;
            }

            // Inicializar descriptores
            for (int i = 0; i < NUM_TX_DESC; i++)
            {
                TxDesc* desc = (TxDesc*)(_txDescBase + (ulong)(i * sizeof(TxDesc)));

                // Asignar buffer de transmisión
                ulong buffer = (ulong)Allocator.malloc((ulong)TX_BUFFER_SIZE);
                if (buffer == 0)
                {
                    SerialDebug.Error("Intel 8254X: Failed to allocate TX buffer");
                    return;
                }

                // Limpiar buffer
                Native.Stosb((void*)buffer, 0, (ulong)TX_BUFFER_SIZE);

                // Configurar descriptor
                desc->BufferAddress = buffer;
                desc->Length = 0;
                desc->CMD = 0;
                desc->Status = 0;
            }

            // Configurar registros de descriptores de transmisión
            WriteRegister(REG_TDBAL, (uint)(_txDescBase & 0xFFFFFFFF));       // Dirección base (parte baja)
            WriteRegister(REG_TDBAH, (uint)(_txDescBase >> 32));              // Dirección base (parte alta)
            WriteRegister(REG_TDLEN, NUM_TX_DESC * sizeof(TxDesc));           // Longitud total de los descriptores

            // Indices de cabecera y cola (head/tail)
            WriteRegister(REG_TDH, 0);
            WriteRegister(REG_TDT, 0);

            // Configurar TIPG (Transmit Inter Packet Gap)
            WriteRegister(REG_TIPG, 10);

            // Configurar control de transmisión
            uint tctl = TCTL_EN |        // Habilitar transmisión
                        TCTL_PSP |       // Pad short packets
                        TCTL_CT_15 |     // Collision Threshold
                        TCTL_COLD;       // Collision Distance

            WriteRegister(REG_TCTL, tctl);
        }

        /// <summary>
        /// Configura las interrupciones del controlador
        /// </summary>
        private void SetupInterrupts()
        {
            SerialDebug.Info($"Intel 8254X: Setting up interrupts, IRQ: {_pciDevice.InterruptLine}");

            // Limpiar todas las causas de interrupción
            uint icr = ReadRegister(REG_ICR);

            // Habilitar interrupciones deseadas
            uint ims = (1 << 0) |  // Tx Descriptor Written Back
                       (1 << 1) |  // Tx Queue Empty
                       (1 << 2) |  // Link Status Change
                       (1 << 4) |  // Rx Min Threshold Reached
                       (1 << 6) |  // Rx Timer
                       (1 << 7);   // Rx Descriptor Written Back

            WriteRegister(REG_IMS, ims);

            // Asignar manejador de interrupciones
            //InterruptManager.RegisterHandler(_pciDevice.InterruptLine, HandleInterrupt);
        }

        /// <summary>
        /// Espera a que el enlace (link) esté activo
        /// </summary>
        private void WaitForLinkUp()
        {
            SerialDebug.Info("Intel 8254X: Waiting for link up...");

            // Asegurar que SLU (Set Link Up) está habilitado
            uint ctrl = ReadRegister(REG_CTRL);
            WriteRegister(REG_CTRL, ctrl | CTRL_SLU | CTRL_ASDE);

            // Esperar a que el link esté activo
            int timeout = 100000;
            while (timeout > 0)
            {
                uint status = ReadRegister(REG_STATUS);
                if ((status & STATUS_LINK_UP) != 0)
                {
                    SerialDebug.Info("Intel 8254X: Link is up");

                    // Detectar velocidad y duplex
                    bool fullDuplex = (status & (1 << 0)) != 0;
                    int speed;

                    if ((status & (3 << 6)) == 0)
                        speed = 10;
                    else if ((status & (2 << 6)) != 0)
                        speed = 1000;
                    else
                        speed = 100;

                    SerialDebug.Info($"Intel 8254X: Speed: {speed} Mbps, {(fullDuplex ? "Full" : "Half")} Duplex");
                    return;
                }

                timeout--;
                Native.Pause();
            }

            SerialDebug.Warning("Intel 8254X: Timeout waiting for link up");
        }

        /// <summary>
        /// Habilita la recepción y transmisión
        /// </summary>
        private void EnableRxTx()
        {
            SerialDebug.Info("Intel 8254X: Enabling RX/TX...");

            // Asegurar que RCTL.EN y TCTL.EN están activados
            uint rctl = ReadRegister(REG_RCTL);
            WriteRegister(REG_RCTL, rctl | RCTL_EN);

            uint tctl = ReadRegister(REG_TCTL);
            WriteRegister(REG_TCTL, tctl | TCTL_EN);
        }

        /// <summary>
        /// Manejador de interrupciones
        /// </summary>
        public void HandleInterrupt()
        {
            // Leer y limpiar las causas de interrupción
            uint icr = ReadRegister(REG_ICR);

            if ((icr & (1 << 2)) != 0)
            {
                // Cambio en el estado del enlace
                SerialDebug.Info("Intel 8254X: Link status changed");
                WaitForLinkUp();
            }

            if ((icr & (1 << 7)) != 0 || (icr & (1 << 4)) != 0)
            {
                // Paquete recibido
                ProcessReceivedPackets();
            }

            if ((icr & (1 << 0)) != 0)
            {
                // Descriptor de transmisión escrito
                ProcessTransmitComplete();
            }
        }

        /// <summary>
        /// Procesa los paquetes recibidos
        /// </summary>
        private void ProcessReceivedPackets()
        {
            int rxCurrent = _currentRxDesc;

            RxDesc* desc = (RxDesc*)(_rxDescBase + (ulong)(_currentRxDesc * sizeof(RxDesc)));

            // Verificar si hay paquetes disponibles (bit DD - Descriptor Done)
            while ((desc->Status & 0x1) != 0)
            {
                // Longitud del paquete recibido
                ushort length = desc->Length;

                if (length > 0)
                {
                    SerialDebug.Info($"Intel 8254X: Received packet of size {length}");

                    // Aquí procesarías el paquete: desc->BufferAddress contiene los datos
                    // NetworkManager.HandlePacket((byte*)desc->BufferAddress, length);
                }

                // Marcar descriptor como disponible
                desc->Status = 0;

                // Avanzar al siguiente descriptor
                _currentRxDesc = (_currentRxDesc + 1) % NUM_RX_DESC;
                desc = (RxDesc*)(_rxDescBase + (ulong)(_currentRxDesc * sizeof(RxDesc)));
            }

            // Actualizar el registro tail si se procesaron descriptores
            if (rxCurrent != _currentRxDesc)
            {
                WriteRegister(REG_RDT, _currentRxDesc);
            }
        }

        /// <summary>
        /// Procesa la finalización de transmisiones
        /// </summary>
        private void ProcessTransmitComplete()
        {
            TxDesc* desc = (TxDesc*)(_txDescBase + (ulong)(_currentTxDesc * sizeof(TxDesc)));

            // Solo procesar si el descriptor tiene el bit DD (Descriptor Done)
            if ((desc->Status & 0x1) != 0)
            {
                // Limpiar descriptor
                desc->Status = 0;

                // Manejar siguientes paquetes pendientes si existen
                // ...

                // Avanzar al siguiente descriptor
                _currentTxDesc = (_currentTxDesc + 1) % NUM_TX_DESC;
            }
        }

        /// <summary>
        /// Transmite un paquete
        /// </summary>
        public bool TransmitPacket(byte[] data, int offset, int length)
        {
            if (length <= 0 || length > TX_BUFFER_SIZE)
            {
                SerialDebug.Warning($"Intel 8254X: Invalid packet size: {length}");
                return false;
            }

            // Obtener el descriptor de transmisión actual
            TxDesc* desc = (TxDesc*)(_txDescBase + (ulong)(_currentTxDesc * sizeof(TxDesc)));

            // Verificar si el descriptor está disponible
            if ((desc->CMD & 0x1) != 0)
            {
                SerialDebug.Warning("Intel 8254X: No TX descriptors available");
                return false;
            }

            // Copiar datos al buffer
            byte* buffer = (byte*)desc->BufferAddress;
            if (buffer == null)
            {
                SerialDebug.Error("Intel 8254X: TX buffer is null");
                return false;
            }

            // Copiar datos al buffer
            for (int i = 0; i < length; i++)
            {
                buffer[i] = data[offset + i];
            }

            // Configurar descriptor
            desc->Length = (ushort)length;

            // Comandos:
            // EOP - End of Packet
            // IFCS - Insert FCS (CRC)
            // RS - Report Status
            desc->CMD = 0x1 | 0x2 | 0x8;
            desc->Status = 0;

            // Actualizar índice
            int newIndex = (_currentTxDesc + 1) % NUM_TX_DESC;
            _currentTxDesc = newIndex;

            // Actualizar registro tail
            WriteRegister(REG_TDT, newIndex);

            return true;
        }

        /// <summary>
        /// Lee un valor de un registro
        /// </summary>
        private uint ReadRegister(uint reg)
        {
            return *(uint*)(_baseMMIO + reg);
        }

        /// <summary>
        /// Escribe un valor a un registro
        /// </summary>
        private void WriteRegister(uint reg, uint value)
        {
            *(uint*)(_baseMMIO + reg) = value;
        }

        /// <summary>
        /// Método de apagado
        /// </summary>
        protected override void OnShutdown()
        {
            SerialDebug.Info("Intel 8254X: Shutting down...");

            // Desactivar interrupciones
            WriteRegister(REG_IMC, 0xFFFFFFFF);

            // Desactivar recepción y transmisión
            uint rctl = ReadRegister(REG_RCTL);
            WriteRegister(REG_RCTL, rctl & ~RCTL_EN);

            uint tctl = ReadRegister(REG_TCTL);
            WriteRegister(REG_TCTL, tctl & ~TCTL_EN);

            // Reiniciar hardware
            WriteRegister(REG_CTRL, CTRL_RESET);

            // Liberar recursos
            // En un kernel real, aquí liberarías la memoria de los descriptores
        }

        /// <summary>
        /// Obtiene la dirección MAC del dispositivo
        /// </summary>
        public MACAddress GetMACAddress()
        {
            return _macAddress;
        }
    }

    /// <summary>
    /// Estructura para representar una dirección MAC
    /// </summary>
    public struct MACAddress
    {
        private byte[] _bytes;

        public MACAddress(byte[] address)
        {
            _bytes = new byte[6];
            for (int i = 0; i < 6 && i < address.Length; i++)
            {
                _bytes[i] = address[i];
            }
        }

        public override string ToString()
        {
            return $"{_bytes[0].ToString("X2")}:{_bytes[1].ToString("X2")}:{_bytes[2].ToString("X2")}:{_bytes[3].ToString("X2")}:{_bytes[4].ToString("X2")}:{_bytes[5].ToString("X2")}";
        }

        public byte[] GetBytes()
        {
            return _bytes;
        }

        public byte this[int index]
        {
            get
            {
                if (index < 0 || index >= _bytes.Length)
                    return 0;
                return _bytes[index];
            }
        }
    }
}