using Kernel.Diagnostics;
using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Kernel.Hardware
{
    /// <summary>
    /// Gestor básico de ACPI (Advanced Configuration and Power Interface)
    /// </summary>
    public static unsafe class ACPIManager
    {
        // Constantes para búsqueda de tablas ACPI
        private const ulong ACPI_SEARCH_START = 0x000E0000;
        private const ulong ACPI_SEARCH_END = 0x000FFFFF;
        private const string RSDP_SIGNATURE = "RSD PTR ";

        // Firmas de tablas comunes
        private const uint APIC_SIGNATURE = 0x43495041; // "APIC"
        private const uint FACP_SIGNATURE = 0x50434146; // "FACP"
        private const uint DSDT_SIGNATURE = 0x54445344; // "DSDT"
        private const uint SSDT_SIGNATURE = 0x54445353; // "SSDT"
        private const uint HPET_SIGNATURE = 0x54455048; // "HPET"
        private const uint MCFG_SIGNATURE = 0x4746434D; // "MCFG"

        // Estado del gestor
        private static bool _initialized = false;
        private static bool _acpiVersion2 = false;

        // Punteros a tablas importantes
        private static RSDP* _rsdp = null;
        private static ACPISDTHeader* _rsdt = null;
        private static ACPISDTHeader* _xsdt = null;
        private static FADT* _fadt = null;
        private static ACPISDTHeader* _dsdt = null;
        private static MADT* _madt = null;

        // Información sobre reinicio del sistema
        private static ResetType _resetType = ResetType.None;
        private static ushort _resetPort = 0;
        private static byte _resetValue = 0;

        /// <summary>
        /// Tipos de reinicio de sistema soportados
        /// </summary>
        public enum ResetType
        {
            None,
            Memory,
            IO,
            Register
        }

        /// <summary>
        /// Estructura ACPI RSDP (Root System Description Pointer)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RSDP
        {
            public fixed byte Signature[8];
            public byte Checksum;
            public fixed byte OemId[6];
            public byte Revision;
            public uint RsdtAddress;
            // ACPI 2.0+ campos adicionales
            public uint Length;
            public ulong XsdtAddress;
            public byte ExtendedChecksum;
            public fixed byte Reserved[3];
        }

        /// <summary>
        /// Estructura ACPI SDT Header (System Description Table)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ACPISDTHeader
        {
            public uint Signature;
            public uint Length;
            public byte Revision;
            public byte Checksum;
            public fixed byte OemId[6];
            public fixed byte OemTableId[8];
            public uint OemRevision;
            public uint CreatorId;
            public uint CreatorRevision;
        }

        /// <summary>
        /// Estructura ACPI FADT (Fixed ACPI Description Table)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FADT
        {
            public ACPISDTHeader Header;
            public uint FirmwareCtrl;
            public uint Dsdt;

            // Campo reservado en ACPI 2.0+
            public byte Reserved;

            public byte PreferredPowerManagementProfile;
            public ushort SCI_Interrupt;
            public uint SMI_CommandPort;
            public byte AcpiEnable;
            public byte AcpiDisable;
            public byte S4BIOS_REQ;
            public byte PSTATE_Control;
            public uint PM1aEventBlock;
            public uint PM1bEventBlock;
            public uint PM1aControlBlock;
            public uint PM1bControlBlock;
            public uint PM2ControlBlock;
            public uint PMTimerBlock;
            public uint GPE0Block;
            public uint GPE1Block;
            public byte PM1EventLength;
            public byte PM1ControlLength;
            public byte PM2ControlLength;
            public byte PMTimerLength;
            public byte GPE0Length;
            public byte GPE1Length;
            public byte GPE1Base;
            public byte CStateControl;
            public ushort WorstC2Latency;
            public ushort WorstC3Latency;
            public ushort FlushSize;
            public ushort FlushStride;
            public byte DutyOffset;
            public byte DutyWidth;
            public byte DayAlarm;
            public byte MonthAlarm;
            public byte Century;

            // Campos reservados para sistemas ACPI 1.0
            public ushort BootArchitectureFlags;
            public byte Reserved2;
            public uint Flags;

            // Reset Register (ACPI 2.0+)
            public GenericAddressStructure ResetReg;
            public byte ResetValue;

            // Campos reservados para ACPI 2.0+
            public fixed byte Reserved3[3];

            // ACPI 2.0+ fields
            public ulong X_FirmwareControl;
            public ulong X_Dsdt;

            // Más campos ACPI 2.0+ para control de energía que no incluimos por simplicidad
        }

        /// <summary>
        /// Estructura ACPI MADT (Multiple APIC Description Table)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MADT
        {
            public ACPISDTHeader Header;
            public uint LocalApicAddress;
            public uint Flags;
            // Seguido por registros de controladores de interrupción
        }

        /// <summary>
        /// Estructura ACPI Generic Address Structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct GenericAddressStructure
        {
            public byte AddressSpace;
            public byte BitWidth;
            public byte BitOffset;
            public byte AccessSize;
            public ulong Address;
        }

        /// <summary>
        /// Inicializa el subsistema ACPI
        /// </summary>
        public static bool Initialize()
        {
            if (_initialized)
                return true;

            Console.WriteLine("Inicializando subsistema ACPI...");

            // Buscar la tabla RSDP
            if (!FindRSDP())
            {
                Console.WriteLine("No se encontró la tabla RSDP. ACPI no disponible.");
                return false;
            }

            // Determinar la versión de ACPI
            _acpiVersion2 = _rsdp->Revision >= 2;

            if (_acpiVersion2)
            {
                Console.WriteLine($"Detectado ACPI {_rsdp->Revision.ToString()}.0");

                // Usar XSDT para ACPI 2.0+
                if (_rsdp->XsdtAddress != 0)
                {
                    _xsdt = (ACPISDTHeader*)_rsdp->XsdtAddress;
                    if (!ValidateTable(_xsdt))
                    {
                        Console.WriteLine("XSDT inválida, intentando con RSDT...");
                        _xsdt = null;
                    }
                }
            }
            else
            {
                Console.WriteLine("Detectado ACPI 1.0");
            }

            // Si no hay XSDT válida o es ACPI 1.0, usar RSDT
            if (_xsdt == null)
            {
                _rsdt = (ACPISDTHeader*)_rsdp->RsdtAddress;
                if (!ValidateTable(_rsdt))
                {
                    Console.WriteLine("RSDT inválida. No se puede continuar.");
                    return false;
                }
            }

            // Buscar las tablas principales
            if (!FindTables())
            {
                Console.WriteLine("Error al buscar tablas ACPI importantes.");
                return false;
            }

            // Detectar método de reinicio del sistema
            DetectResetMethod();

            _initialized = true;
            Console.WriteLine("Subsistema ACPI inicializado correctamente.");
            return true;
        }

        /// <summary>
        /// Busca y valida la tabla RSDP (Root System Description Pointer)
        /// </summary>
        private static bool FindRSDP()
        {
            byte* current = (byte*)ACPI_SEARCH_START;

            while (current < (byte*)ACPI_SEARCH_END)
            {
                if (IsSignatureMatch(current, RSDP_SIGNATURE))
                {
                    // Verificar checksum
                    byte sum = 0;
                    for (int i = 0; i < 20; i++) // Tamaño de RSDP 1.0
                    {
                        sum += current[i];
                    }

                    if (sum == 0)
                    {
                        _rsdp = (RSDP*)current;

                        // Para ACPI 2.0+, verificar también el checksum extendido
                        if (_rsdp->Revision >= 2)
                        {
                            sum = 0;
                            for (int i = 0; i < _rsdp->Length; i++)
                            {
                                sum += current[i];
                            }

                            if (sum != 0)
                            {
                                Console.WriteLine("RSDP extendido inválido (checksum).");
                                return false;
                            }
                        }

                        return true;
                    }
                }

                current += 16; // Búsqueda alineada a 16 bytes
            }

            return false;
        }

        /// <summary>
        /// Verifica si una cadena en memoria coincide con una firma
        /// </summary>
        private static bool IsSignatureMatch(byte* memory, string signature)
        {
            for (int i = 0; i < signature.Length; i++)
            {
                if (memory[i] != signature[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Valida una tabla ACPI verificando su checksum
        /// </summary>
        private static bool ValidateTable(ACPISDTHeader* table)
        {
            if (table == null)
                return false;

            // Verificar checksum
            byte sum = 0;
            byte* ptr = (byte*)table;
            for (int i = 0; i < table->Length; i++)
            {
                sum += ptr[i];
            }

            return sum == 0;
        }

        /// <summary>
        /// Busca las tablas ACPI importantes
        /// </summary>
        private static bool FindTables()
        {
            // Buscar tablas importantes como FACP (FADT), APIC (MADT), etc.
            _fadt = (FADT*)FindTable(FACP_SIGNATURE);
            _madt = (MADT*)FindTable(APIC_SIGNATURE);

            // DSDT se referencia desde FADT
            if (_fadt != null)
            {
                if (_acpiVersion2 && _fadt->X_Dsdt != 0)
                {
                    _dsdt = (ACPISDTHeader*)_fadt->X_Dsdt;
                }
                else
                {
                    _dsdt = (ACPISDTHeader*)_fadt->Dsdt;
                }

                if (!ValidateTable(_dsdt))
                {
                    Console.WriteLine("DSDT inválida.");
                    _dsdt = null;
                }
            }

            // Verificar que encontramos lo mínimo necesario
            if (_fadt == null)
            {
                Console.WriteLine("No se encontró la tabla FADT.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Busca una tabla ACPI específica por su firma
        /// </summary>
        private static ACPISDTHeader* FindTable(uint signature)
        {
            if (_xsdt != null)
            {
                // Usar XSDT (punteros de 64 bits)
                int entries = (int)(_xsdt->Length - sizeof(ACPISDTHeader)) / 8;
                ulong* tables = (ulong*)((byte*)_xsdt + sizeof(ACPISDTHeader));

                for (int i = 0; i < entries; i++)
                {
                    ACPISDTHeader* table = (ACPISDTHeader*)tables[i];
                    if (table->Signature == signature && ValidateTable(table))
                    {
                        return table;
                    }
                }
            }
            else if (_rsdt != null)
            {
                // Usar RSDT (punteros de 32 bits)
                int entries = (int)(_rsdt->Length - sizeof(ACPISDTHeader)) / 4;
                uint* tables = (uint*)((byte*)_rsdt + sizeof(ACPISDTHeader));

                for (int i = 0; i < entries; i++)
                {
                    ACPISDTHeader* table = (ACPISDTHeader*)tables[i];
                    if (table->Signature == signature && ValidateTable(table))
                    {
                        return table;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Detecta el método disponible para reiniciar el sistema
        /// </summary>
        private static void DetectResetMethod()
        {
            _resetType = ResetType.None;

            // En ACPI 2.0+, verificar el registro de reinicio
            if (_acpiVersion2 && _fadt != null && _fadt->ResetReg.Address != 0)
            {
                _resetType = ResetType.Register;
                _resetValue = _fadt->ResetValue;
                Console.WriteLine("Método de reinicio: Registro ACPI");
                return;
            }

            // Método de reinicio alternativo: Puerto de teclado
            _resetType = ResetType.IO;
            _resetPort = 0x64; // Puerto de control de teclado
            _resetValue = 0xFE; // Valor de reinicio
            Console.WriteLine("Método de reinicio: Puerto de teclado (KB Controller)");
        }

        /// <summary>
        /// Reinicia el sistema usando el método ACPI
        /// </summary>
        public static void ResetSystem()
        {
            if (!_initialized)
            {
                Console.WriteLine("ACPI no inicializado. No se puede reiniciar.");
                return;
            }

            Console.WriteLine("Reiniciando sistema...");

            switch (_resetType)
            {
                case ResetType.Register:
                    // Usar el registro ACPI para reiniciar
                    if (_fadt->ResetReg.AddressSpace == 0) // Memory
                    {
                        *(byte*)_fadt->ResetReg.Address = _resetValue;
                    }
                    else if (_fadt->ResetReg.AddressSpace == 1) // IO
                    {
                        Native.OutByte((ushort)_fadt->ResetReg.Address, _resetValue);
                    }
                    break;

                case ResetType.IO:
                    // Reiniciar usando el controlador de teclado
                    Native.OutByte(0x64, 0xFE);
                    break;

                case ResetType.Memory:
                    // Escribir en memoria para reiniciar
                    *(byte*)_resetPort = _resetValue;
                    break;

                default:
                    Console.WriteLine("No hay método de reinicio disponible.");
                    break;
            }

            // Si llegamos aquí, el reinicio falló
            Console.WriteLine("El reinicio falló. Sistema detenido.");
            while (true) { Native.Halt(); }
        }

        /// <summary>
        /// Apaga el sistema usando métodos ACPI
        /// </summary>
        public static void ShutdownSystem()
        {
            if (!_initialized || _fadt == null)
            {
                Console.WriteLine("ACPI no inicializado. No se puede apagar.");
                return;
            }

            Console.WriteLine("Apagando sistema...");

            // Escribir en los registros PM1a y PM1b para apagar
            if (_fadt->PM1aControlBlock != 0)
            {
                // Valores para SLP_TYP y SLP_EN
                const ushort SLP_EN = 1 << 13;
                const ushort SLP_TYP_S5 = 7 << 10;

                Native.OutWord((ushort)_fadt->PM1aControlBlock, SLP_TYP_S5 | SLP_EN);

                // Si hay un segundo bloque, escribir también allí
                if (_fadt->PM1bControlBlock != 0)
                {
                    Native.OutWord((ushort)_fadt->PM1bControlBlock, SLP_TYP_S5 | SLP_EN);
                }
            }

            // Si llegamos aquí, el apagado falló
            Console.WriteLine("El apagado falló. Sistema detenido.");
            while (true) { Native.Halt(); }
        }

        /// <summary>
        /// Obtiene la dirección del controlador APIC local
        /// </summary>
        public static ulong GetLocalApicAddress()
        {
            if (_madt != null)
            {
                if (_acpiVersion2)
                {
                    // En ACPI 2.0+, podría estar extendido a 64 bits (pero no está en la especificación)
                    return _madt->LocalApicAddress;
                }
                else
                {
                    return _madt->LocalApicAddress;
                }
            }

            return 0;
        }

        /// <summary>
        /// Imprime información sobre las tablas ACPI detectadas
        /// </summary>
        public static void PrintACPIInfo()
        {
            if (!_initialized)
            {
                Console.WriteLine("ACPI no inicializado.");
                return;
            }

            Console.WriteLine("\n=== Información ACPI ===");

            if (_acpiVersion2)
            {
                Console.WriteLine("Versión ACPI: 2.0+");
            }
            else
            {
                Console.WriteLine("Versión ACPI: 1.0");
            }

            // Extraer OEM ID
            string oemId = "";
            for (int i = 0; i < 6; i++)
            {
                oemId += (char)_rsdp->OemId[i];
            }
            Console.WriteLine($"OEM ID: {oemId}");

            // RSDT/XSDT
            if (_xsdt != null)
            {
                //Console.WriteLine($"XSDT: 0x{((ulong)_xsdt).ToStringHex()}");
                int entries = (int)(_xsdt->Length - sizeof(ACPISDTHeader)) / 8;
                Console.WriteLine($"Entradas en XSDT: {entries.ToString()}");
            }

            if (_rsdt != null)
            {
                ///Console.WriteLine($"RSDT: 0x{((ulong)_rsdt).ToStringHex()}");
                int entries = (int)(_rsdt->Length - sizeof(ACPISDTHeader)) / 4;
                Console.WriteLine($"Entradas en RSDT: {entries.ToString()}");
            }

            // Tablas importantes
            if (_fadt != null)
               // Console.WriteLine($"FADT: 0x{((ulong)_fadt).ToStringHex()}");

            if (_dsdt != null)
               // Console.WriteLine($"DSDT: 0x{((ulong)_dsdt).ToStringHex()}");

            if (_madt != null)
            {
                //Console.WriteLine($"MADT: 0x{((ulong)_madt).ToStringHex()}");
                Console.WriteLine($"Local APIC Address: 0x{((ulong)_madt->LocalApicAddress).ToStringHex()}");
                Console.WriteLine($"MADT Flags: 0x{((ulong)_madt->Flags).ToStringHex()}");
            }

            // Método de reinicio
            //Console.WriteLine($"Método de reinicio: {_resetType.ToString()}");

            Console.WriteLine("========================\n");
        }
    }
}