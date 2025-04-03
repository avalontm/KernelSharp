using Kernel.Diagnostics;
using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Kernel.Hardware
{
    /// <summary>
    /// Gestor de Multiprocesamiento Simétrico (SMP) para sistemas x86_64
    /// </summary>
    public static unsafe class SMPManager
    {
        // Información sobre la tabla ACPI MADT (Multiple APIC Description Table)
        private const ulong MADT_SIGNATURE = 0x43495041; // "APIC"
        private const byte MADT_TYPE_LOCAL_APIC = 0;
        private const byte MADT_TYPE_IO_APIC = 1;

        // Límites para la búsqueda de tablas ACPI
        private const ulong ACPI_SEARCH_START = 0x000E0000;
        private const ulong ACPI_SEARCH_END = 0x000FFFFF;
        private const string RSDP_SIGNATURE = "RSD PTR ";

        // Límite máximo de CPUs soportadas
        private const int MAX_CPUS = 32;

        // Información sobre el estado de inicialización SMP
        private static bool _initialized;
        private static int _cpuCount;
        private static ulong _localApicAddress;
        private static ulong _ioApicAddress;

        // Información sobre cada CPU
        private static CPUInfo[] _cpuInfos = new CPUInfo[MAX_CPUS];

        // Estructura para almacenar información de cada CPU
        private struct CPUInfo
        {
            public byte ApicId;
            public bool IsBootProcessor;
            public bool IsEnabled;
        }

        // Estructura ACPI RSDP (Root System Description Pointer)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct RSDP
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

        // Estructura ACPI SDT Header (System Description Table)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ACPISDTHeader
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

        // Estructura ACPI MADT (Multiple APIC Description Table)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MADT
        {
            public ACPISDTHeader Header;
            public uint LocalApicAddress;
            public uint Flags;
            // Seguido por registros de controladores de interrupción
        }

        // Estructura de un registro MADT
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MADTRecord
        {
            public byte Type;
            public byte Length;
            // Seguido por datos específicos del tipo
        }

        // Registro de Procesador Local APIC
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MADTLocalApic
        {
            public MADTRecord Header;
            public byte AcpiProcessorId;
            public byte ApicId;
            public uint Flags;
        }

        // Registro de IO APIC
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MADTIOApic
        {
            public MADTRecord Header;
            public byte IoApicId;
            public byte Reserved;
            public uint IoApicAddress;
            public uint GlobalSystemInterruptBase;
        }

        /// <summary>
        /// Inicializa el subsistema SMP
        /// </summary>
        public static bool Initialize()
        {
            if (_initialized)
                return true;

            Console.WriteLine("Inicializando sistema SMP...");

            // Buscar tablas ACPI para información SMP
            if (!FindSMPInfo())
            {
                Console.WriteLine("No se encontró información SMP (ACPI MADT)");
                return false;
            }

            // Inicializar el APIC local
            InitializeLocalApic();

            _initialized = true;
            Console.WriteLine($"Sistema SMP inicializado: {_cpuCount.ToString()} procesador(es) detectado(s)");
            return true;
        }

        /// <summary>
        /// Busca información SMP en las tablas ACPI
        /// </summary>
        private static bool FindSMPInfo()
        {
            // Buscar RSDP (Root System Description Pointer)
            byte* current = (byte*)ACPI_SEARCH_START;
            RSDP* rsdp = null;

            while (current < (byte*)ACPI_SEARCH_END)
            {
                if (IsSignatureMatch(current, RSDP_SIGNATURE))
                {
                    // Verificar checksum
                    byte sum = 0;
                    for (int i = 0; i < sizeof(RSDP); i++)
                    {
                        sum += current[i];
                    }

                    if (sum == 0)
                    {
                        rsdp = (RSDP*)current;
                        break;
                    }
                }

                current += 16; // Búsqueda alineada a 16 bytes
            }

            if (rsdp == null)
            {
                Console.WriteLine("No se encontró ACPI RSDP");
                return false;
            }

            // Dependiendo de la revisión, usar RSDT o XSDT
            bool result = false;
            if (rsdp->Revision >= 2 && rsdp->XsdtAddress != 0)
            {
                // ACPI 2.0+, usar XSDT
                result = ParseXSDT((ACPISDTHeader*)(ulong)rsdp->XsdtAddress);
            }
            else
            {
                // ACPI 1.0, usar RSDT
                result = ParseRSDT((ACPISDTHeader*)(ulong)rsdp->RsdtAddress);
            }

            return result;
        }

        /// <summary>
        /// Compara una firma en memoria con una cadena esperada
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
        /// Analiza la RSDT (Root System Description Table) buscando la MADT
        /// </summary>
        private static bool ParseRSDT(ACPISDTHeader* rsdt)
        {
            // Verificar longitud
            if (rsdt->Length < sizeof(ACPISDTHeader))
                return false;

            // Número de entradas
            int entries = (int)(rsdt->Length - sizeof(ACPISDTHeader)) / 4;

            // Buscar la tabla MADT
            uint* tableAddresses = (uint*)(rsdt + 1);
            for (int i = 0; i < entries; i++)
            {
                ACPISDTHeader* table = (ACPISDTHeader*)(ulong)tableAddresses[i];

                // Verificar si es MADT
                if (table->Signature == MADT_SIGNATURE)
                {
                    return ParseMADT((MADT*)table);
                }
            }

            return false;
        }

        /// <summary>
        /// Analiza la XSDT (Extended System Description Table) buscando la MADT
        /// </summary>
        private static bool ParseXSDT(ACPISDTHeader* xsdt)
        {
            // Verificar longitud
            if (xsdt->Length < sizeof(ACPISDTHeader))
                return false;

            // Número de entradas
            int entries = (int)(xsdt->Length - sizeof(ACPISDTHeader)) / 8;

            // Buscar la tabla MADT
            ulong* tableAddresses = (ulong*)(xsdt + 1);
            for (int i = 0; i < entries; i++)
            {
                ACPISDTHeader* table = (ACPISDTHeader*)tableAddresses[i];

                // Verificar si es MADT
                if (table->Signature == MADT_SIGNATURE)
                {
                    return ParseMADT((MADT*)table);
                }
            }

            return false;
        }

        /// <summary>
        /// Analiza la MADT (Multiple APIC Description Table) para encontrar CPUs y APICs
        /// </summary>
        private static bool ParseMADT(MADT* madt)
        {
            // Verificar longitud
            if (madt->Header.Length < sizeof(MADT))
                return false;

            // Guardar dirección del APIC local
            _localApicAddress = madt->LocalApicAddress;

            // Procesar registros de controladores de interrupción
            byte* current = (byte*)(madt + 1);
            byte* end = (byte*)madt + madt->Header.Length;

            _cpuCount = 0;

            while (current < end)
            {
                MADTRecord* record = (MADTRecord*)current;

                // Asegurarse de que la longitud sea válida
                if (record->Length < 2 || current + record->Length > end)
                    break;

                // Procesar según el tipo
                switch (record->Type)
                {
                    case MADT_TYPE_LOCAL_APIC:
                        if (record->Length >= sizeof(MADTLocalApic))
                        {
                            MADTLocalApic* localApic = (MADTLocalApic*)record;

                            // Verificar si el procesador está habilitado
                            bool enabled = (localApic->Flags & 1) != 0;
                            if (enabled && _cpuCount < MAX_CPUS)
                            {
                                _cpuInfos[_cpuCount].ApicId = localApic->ApicId;
                                _cpuInfos[_cpuCount].IsEnabled = true;
                                _cpuInfos[_cpuCount].IsBootProcessor = (_cpuCount == 0);
                                _cpuCount++;
                            }
                        }
                        break;

                    case MADT_TYPE_IO_APIC:
                        if (record->Length >= sizeof(MADTIOApic))
                        {
                            MADTIOApic* ioApic = (MADTIOApic*)record;
                            _ioApicAddress = ioApic->IoApicAddress;
                        }
                        break;
                }

                // Avanzar al siguiente registro
                current += record->Length;
            }

            return _cpuCount > 0;
        }

        /// <summary>
        /// Inicializa el APIC local
        /// </summary>
        private static void InitializeLocalApic()
        {
            if (_localApicAddress == 0)
                return;

            // Mapear la dirección física del APIC local a una dirección virtual si es necesario
            // En un sistema simple, podríamos usar la identidad de mapeo

            // Habilitar el APIC local (solo para el BSP por ahora)
            uint* apicBase = (uint*)_localApicAddress;

            // Registro Spurious Interrupt Vector (offset 0xF0)
            uint* spuriousReg = apicBase + (0xF0 / 4);

            // Habilitar el APIC (bit 8)
            *spuriousReg = *spuriousReg | 0x100;

            // En una implementación completa, aquí inicializarías los APs (Application Processors)
        }

        /// <summary>
        /// Obtiene el número de CPUs disponibles
        /// </summary>
        public static int GetProcessorCount()
        {
            return _cpuCount;
        }

        /// <summary>
        /// Obtiene el ID APIC del procesador actual
        /// </summary>
        public static byte GetCurrentApicId()
        {
            // En un sistema real, obtendrías el ID APIC del procesador actual
            // A través del CPUID o leyendo el registro correspondiente del APIC local

            // Para simplificar, asumimos que estamos en el BSP (ID 0)
            return 0;
        }

        /// <summary>
        /// Obtiene el ID APIC de un procesador específico
        /// </summary>
        /// <param name="index">Índice del procesador (0-based)</param>
        /// <returns>ID del APIC para el procesador o 0xFF si no es válido</returns>
        public static byte GetAPICId(int index)
        {
            if (index >= 0 && index < _cpuCount)
            {
                return _cpuInfos[index].ApicId;
            }
            return 0xFF; // Valor inválido
        }

        /// <summary>
        /// Imprime información sobre los procesadores detectados
        /// </summary>
        public static void PrintProcessorInfo()
        {
            if (!_initialized)
            {
                Console.WriteLine("Sistema SMP no inicializado");
                return;
            }

            Console.WriteLine("\n=== Información de Procesadores ===");
            Console.WriteLine($"Número total de CPUs: {_cpuCount.ToString()}");
            Console.WriteLine($"Dirección de APIC Local: 0x{((ulong)_localApicAddress).ToStringHex()}");
            if (_ioApicAddress != 0)
                Console.WriteLine($"Dirección de IO APIC: 0x{((ulong)_ioApicAddress).ToStringHex()}");

            for (int i = 0; i < _cpuCount; i++)
            {
                string processorType = _cpuInfos[i].IsBootProcessor ? "BSP" : "AP";
                string enabledStatus = _cpuInfos[i].IsEnabled ? "Habilitado" : "Deshabilitado";
                Console.WriteLine("CPU " + i.ToString() + ": APIC ID " + _cpuInfos[i].ApicId.ToString() + ", " +
                                  processorType + ", " + enabledStatus);
            }
            Console.WriteLine("==================================\n");
        }
    }
}