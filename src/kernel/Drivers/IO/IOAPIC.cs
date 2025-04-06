using Kernel.Diagnostics;
using Kernel.Drivers.IO;
using System;

namespace Kernel.Hardware
{
    /// <summary>
    /// Controlador para el IOAPIC (I/O Advanced Programmable Interrupt Controller)
    /// </summary>
    public static unsafe class IOAPIC
    {
        // Registros del IOAPIC
        private const uint IOAPIC_REG_ID = 0x00;         // ID del IOAPIC
        private const uint IOAPIC_REG_VER = 0x01;        // Versión del IOAPIC
        private const uint IOAPIC_REG_ARB = 0x02;        // Registro de arbitraje
        private const uint IOAPIC_REG_REDTBL = 0x10;     // Base para las entradas de redirección

        // Offsets para registros de selección e información
        private const byte IOAPIC_IOREGSEL = 0x00;       // Selector de registro
        private const byte IOAPIC_IOWIN = 0x10;          // Ventana de datos

        // Flags para entradas de redirección
        private const ulong IOAPIC_DELIVERY_FIXED = 0;         // Entrega fija
        private const ulong IOAPIC_DELIVERY_LOW_PRIORITY = 1;  // Entrega a CPU de menor prioridad
        private const ulong IOAPIC_DELIVERY_SMI = 2;           // System Management Interrupt
        private const ulong IOAPIC_DELIVERY_NMI = 4;           // Non-Maskable Interrupt
        private const ulong IOAPIC_DELIVERY_INIT = 5;          // INIT IPI
        private const ulong IOAPIC_DELIVERY_EXTINT = 7;        // Entrega como interrupción externa

        private const ulong IOAPIC_DESTMODE_PHYSICAL = 0;      // Modo de destino físico
        private const ulong IOAPIC_DESTMODE_LOGICAL = 1 << 11; // Modo de destino lógico

        private const ulong IOAPIC_DELIVS = 1 << 12;           // Estado de entrega (sólo lectura)
        private const ulong IOAPIC_INTPOL_LOW = 1 << 13;       // Polaridad (1=activo bajo)
        private const ulong IOAPIC_INTPOL_HIGH = 0;            // Polaridad (0=activo alto)
        private const ulong IOAPIC_TRIGGER_EDGE = 0;           // Disparo por flanco
        private const ulong IOAPIC_TRIGGER_LEVEL = 1 << 15;    // Disparo por nivel
        private const ulong IOAPIC_INT_MASK = 1 << 16;         // Interrupciones enmascaradas
        private const ulong IOAPIC_INT_UNMASK = 0;             // Interrupciones desenmascaradas

        // Dirección física base del IOAPIC
        private static ulong _ioApicBaseAddress;
        private static uint* _ioApicRegisterSelect;
        private static uint* _ioApicWindow;
        private static byte _ioApicId;
        private static byte _ioApicVersion;
        private static byte _maxRedirectionEntries;
        private static bool _initialized;

        /// <summary>
        /// Inicializa el controlador IOAPIC
        /// </summary>
        public static bool Initialize()
        {
            if (_initialized)
                return true;

            SerialDebug.Info("Initializing I/O APIC...");

            // Obtener la dirección base del IOAPIC desde ACPI o usar un valor predeterminado
            _ioApicBaseAddress = ACPIManager.GetIOApicAddress();

            if (_ioApicBaseAddress == 0)
            {
                SerialDebug.Info("Could not obtain I/O APIC address from ACPI. Using default 0xFEC00000");
                _ioApicBaseAddress = 0xFEC00000; // Dirección predeterminada
            }

            // Mapear registros del IOAPIC
            _ioApicRegisterSelect = (uint*)_ioApicBaseAddress;
            _ioApicWindow = (uint*)(_ioApicBaseAddress + IOAPIC_IOWIN);

            // Leer información del IOAPIC
            uint idReg = ReadRegister(IOAPIC_REG_ID);
            uint versionReg = ReadRegister(IOAPIC_REG_VER);

            _ioApicId = (byte)((idReg >> 24) & 0xF);
            _ioApicVersion = (byte)(versionReg & 0xFF);
            _maxRedirectionEntries = (byte)((versionReg >> 16) & 0xFF);

            SerialDebug.Info($"I/O APIC ID: {_ioApicId}, Version: {_ioApicVersion}");
            SerialDebug.Info($"Maximum Redirection Entries: {(_maxRedirectionEntries + 1)}");

            // Inicializar todas las entradas de redirección a enmascaradas
            for (byte i = 0; i <= _maxRedirectionEntries; i++)
            {
                MaskIRQ(i);
            }

            _initialized = true;
            SerialDebug.Info("I/O APIC initialized successfully");
            return true;
        }

        /// <summary>
        /// Lee un registro del IOAPIC
        /// </summary>
        private static uint ReadRegister(uint reg)
        {
            *_ioApicRegisterSelect = reg;
            return *_ioApicWindow;
        }

        /// <summary>
        /// Escribe un valor en un registro del IOAPIC
        /// </summary>
        private static void WriteRegister(uint reg, uint value)
        {
            *_ioApicRegisterSelect = reg;
            *_ioApicWindow = value;
        }

        /// <summary>
        /// Lee una entrada de redirección del IOAPIC
        /// </summary>
        public static ulong ReadRedirectionEntry(byte irq)
        {
            if (irq > _maxRedirectionEntries)
            {
                SerialDebug.Warning($"IRQ {irq} exceeds maximum I/O APIC redirection entries");
                return 0;
            }

            uint lowDword = ReadRegister(IOAPIC_REG_REDTBL + (irq * 2));
            uint highDword = ReadRegister(IOAPIC_REG_REDTBL + (irq * 2) + 1);

            return ((ulong)highDword << 32) | lowDword;
        }

        /// <summary>
        /// Escribe una entrada de redirección en el IOAPIC
        /// </summary>
        public static void WriteRedirectionEntry(byte irq, ulong value)
        {
            if (irq > _maxRedirectionEntries)
            {
                SerialDebug.Warning($"IRQ {irq} exceeds maximum I/O APIC redirection entries");
                return;
            }

            uint lowDword = (uint)(value & 0xFFFFFFFF);
            uint highDword = (uint)(value >> 32);

            WriteRegister(IOAPIC_REG_REDTBL + (irq * 2), lowDword);
            WriteRegister(IOAPIC_REG_REDTBL + (irq * 2) + 1, highDword);
        }

        /// <summary>
        /// Configura una IRQ específica para dirigirse a un procesador y vector específico
        /// </summary>
        /// <param name="irq">Número de IRQ</param>
        /// <param name="cpuId">ID de APIC del procesador destino</param>
        /// <param name="vector">Vector de interrupción (32-255)</param>
        /// <param name="masked">Si la IRQ está enmascarada inicialmente</param>
        public static void SetIRQRedirect(byte irq, byte cpuId, byte vector, bool masked = false)
        {
            if (!_initialized)
            {
                SerialDebug.Warning("I/O APIC not initialized");
                return;
            }

            if (irq > _maxRedirectionEntries)
            {
                SerialDebug.Warning($"IRQ {irq} exceeds maximum I/O APIC redirection entries");
                return;
            }

            // Verificar que el vector sea válido (32-255)
            if (vector < 32 || vector > 255)
            {
                SerialDebug.Warning($"Invalid vector {vector}. Must be between 32 and 255");
                return;
            }

            // Construir la entrada de redirección para la IRQ
            ulong entry =
                (ulong)vector |                  // Vector de interrupción
                IOAPIC_DELIVERY_FIXED |          // Entrega fija
                IOAPIC_DESTMODE_PHYSICAL |       // Modo de destino físico
                IOAPIC_INTPOL_HIGH |             // Polaridad activa alta
                IOAPIC_TRIGGER_EDGE;             // Disparo por flanco

            // Enmascarar si es necesario
            if (masked)
            {
                entry |= IOAPIC_INT_MASK;
            }

            // Establecer el destino (ID del procesador) en los bits altos
            entry |= ((ulong)cpuId << 56);

            // Escribir la entrada
            WriteRedirectionEntry(irq, entry);

            SerialDebug.Info($"Configured IRQ {irq} -> Vector {vector}, CPU {cpuId}, Masked: " + masked);
        }

        /// <summary>
        /// Habilita (desenmascara) una IRQ específica
        /// </summary>
        public static void UnmaskIRQ(byte irq)
        {
            if (!_initialized || irq > _maxRedirectionEntries)
                return;

            ulong entry = ReadRedirectionEntry(irq);
            entry &= ~IOAPIC_INT_MASK; // Eliminar bit de máscara
            WriteRedirectionEntry(irq, entry);

            SerialDebug.Info($"Unmasked IRQ {irq}");
        }

        /// <summary>
        /// Deshabilita (enmascara) una IRQ específica
        /// </summary>
        public static void MaskIRQ(byte irq)
        {
            if (!_initialized || irq > _maxRedirectionEntries)
                return;

            ulong entry = ReadRedirectionEntry(irq);
            entry |= IOAPIC_INT_MASK; // Establecer bit de máscara
            WriteRedirectionEntry(irq, entry);

            SerialDebug.Info($"Masked IRQ {irq}");
        }

        /// <summary>
        /// Verifica si una IRQ está enmascarada
        /// </summary>
        public static bool IsIRQMasked(byte irq)
        {
            if (!_initialized || irq > _maxRedirectionEntries)
                return true;

            ulong entry = ReadRedirectionEntry(irq);
            return (entry & IOAPIC_INT_MASK) != 0;
        }

        /// <summary>
        /// Obtiene el ID del IOAPIC
        /// </summary>
        public static byte GetIOAPICId()
        {
            return _ioApicId;
        }

        /// <summary>
        /// Obtiene la dirección base del IOAPIC
        /// </summary>
        public static ulong GetBaseAddress()
        {
            return _ioApicBaseAddress;
        }

        /// <summary>
        /// Verifica si el IOAPIC está inicializado
        /// </summary>
        public static bool IsInitialized()
        {
            return _initialized;
        }
    }
}