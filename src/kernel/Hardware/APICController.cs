﻿using Kernel.Diagnostics;
using Kernel.Drivers.IO;

namespace Kernel.Hardware
{
    /// <summary>
    /// Advanced Programmable Interrupt Controller (APIC) Controller
    /// </summary>
    public static unsafe class APICController
    {
        // APIC Local Register Offsets
        private const int APIC_ID_REGISTER = 0x20;
        private const int APIC_VERSION_REGISTER = 0x30;
        private const int APIC_TASK_PRIORITY = 0x80;
        private const int APIC_EOI = 0xB0;
        private const int APIC_LOGICAL_DESTINATION = 0xD0;
        private const int APIC_DESTINATION_FORMAT = 0xE0;
        private const int APIC_SPURIOUS_INTERRUPT_VECTOR = 0xF0;
        private const int APIC_ERROR_STATUS = 0x280;
        private const int APIC_INTERRUPT_COMMAND_LOW = 0x300;
        private const int APIC_INTERRUPT_COMMAND_HIGH = 0x310;
        private const int APIC_LVT_TIMER = 0x320;
        private const int APIC_LVT_THERMAL = 0x330;
        private const int APIC_LVT_PERFORMANCE = 0x340;
        private const int APIC_LVT_LINT0 = 0x350;
        private const int APIC_LVT_LINT1 = 0x360;
        private const int APIC_LVT_ERROR = 0x370;
        private const int APIC_TIMER_INITIAL_COUNT = 0x380;
        private const int APIC_TIMER_CURRENT_COUNT = 0x390;
        private const int APIC_TIMER_DIVIDE_CONFIG = 0x3E0;

        // Spurious Interrupt Vector Register bits
        private const uint APIC_ENABLE = 0x100;
        private const uint APIC_FOCUS_PROCESSOR_CHECKING = 0x200;
        private const uint APIC_EOI_BROADCAST_SUPPRESSION = 0x1000;

        // Interrupt Command Register (ICR) Bits
        private const uint ICR_VECTOR_MASK = 0xFF;              // Bits 0-7
        private const uint ICR_DELIVERY_MODE_FIXED = 0 << 8;    // Fixed - Bits 8-10
        private const uint ICR_DELIVERY_MODE_LOWEST = 1 << 8;   // Lowest Priority
        private const uint ICR_DELIVERY_MODE_SMI = 2 << 8;      // SMI
        private const uint ICR_DELIVERY_MODE_NMI = 4 << 8;      // NMI
        private const uint ICR_DELIVERY_MODE_INIT = 5 << 8;     // INIT
        private const uint ICR_DELIVERY_MODE_STARTUP = 6 << 8;  // Start-up
        private const uint ICR_LOGICAL_DESTINATION = 1 << 11;   // Bit 11
        private const uint ICR_DELIVERY_STATUS = 1 << 12;       // Bit 12 (read-only)
        private const uint ICR_LEVEL_ASSERT = 1 << 14;          // Bit 14
        private const uint ICR_TRIGGER_MODE_LEVEL = 1 << 15;    // Bit 15
        private const uint ICR_DESTINATION_SHORTHAND_NONE = 0 << 18; // No shorthand - Bits 18-19
        private const uint ICR_DESTINATION_SHORTHAND_SELF = 1 << 18; // Self
        private const uint ICR_DESTINATION_SHORTHAND_ALL = 2 << 18;  // All including self
        private const uint ICR_DESTINATION_SHORTHAND_ALL_EX = 3 << 18; // All excluding self

        // LVT Timer Register bits
        private const uint LVT_TIMER_VECTOR_MASK = 0xFF;        // Bits 0-7
        private const uint LVT_TIMER_MASK_INTERRUPT = 1 << 16;  // Bit 16
        private const uint LVT_TIMER_MODE_ONESHOT = 0 << 17;    // One-shot - Bit 17
        private const uint LVT_TIMER_MODE_PERIODIC = 1 << 17;   // Periodic
        private const uint LVT_TIMER_MODE_TSC_DEADLINE = 2 << 17; // TSC-Deadline (newer processors)

        // LVT common bits
        private const uint LVT_DELIVERY_MODE_FIXED = 0 << 8;    // Fixed - Bits 8-10
        private const uint LVT_DELIVERY_MODE_SMI = 2 << 8;      // SMI
        private const uint LVT_DELIVERY_MODE_NMI = 4 << 8;      // NMI
        private const uint LVT_DELIVERY_MODE_EXTINT = 7 << 8;   // ExtINT
        private const uint LVT_MASK = 1 << 16;                  // Bit 16

        // Timer divider values
        private const uint TIMER_DIV_1 = 0xB;    // Divide by 1
        private const uint TIMER_DIV_2 = 0x0;    // Divide by 2
        private const uint TIMER_DIV_4 = 0x1;    // Divide by 4
        private const uint TIMER_DIV_8 = 0x2;    // Divide by 8
        private const uint TIMER_DIV_16 = 0x3;   // Divide by 16
        private const uint TIMER_DIV_32 = 0x8;   // Divide by 32
        private const uint TIMER_DIV_64 = 0x9;   // Divide by 64
        private const uint TIMER_DIV_128 = 0xA;  // Divide by 128

        // Interrupt vector base values
        private const byte APIC_SPURIOUS_VECTOR = 0xFF;      // Vector for spurious interrupts
        private const byte APIC_TIMER_VECTOR = 0x30;         // Vector for APIC timer
        private const byte APIC_ERROR_VECTOR = 0xFE;         // Vector for APIC error handler
        private const byte APIC_THERMAL_VECTOR = 0xFD;       // Vector for thermal sensor
        private const byte APIC_PERFORMANCE_VECTOR = 0xFC;   // Vector for performance counter

        // PIC constants (for remapping and disabling)
        private const ushort PIC1_COMMAND = 0x20;
        private const ushort PIC1_DATA = 0x21;
        private const ushort PIC2_COMMAND = 0xA0;
        private const ushort PIC2_DATA = 0xA1;
        private const byte PIC_ICW1_ICW4 = 0x01;      // ICW4 needed
        private const byte PIC_ICW1_INIT = 0x10;      // Initialization command
        private const byte PIC_ICW4_8086 = 0x01;      // 8086/88 mode
        private const byte PIC_EOI = 0x20;            // End of Interrupt command

        // Controller State
        private static bool _initialized;
        private static bool _apicEnabled;
        private static uint* _localApicAddress;
        private static byte _apicId;
        private static byte _apicVersion;
        private static uint _timerTicksPerMS;  // Calibrated timer ticks per millisecond

        // Safe flag for detected CPU features
        private static bool _cpuidApicSupported;
        private static bool _msrApicEnabled;

        /// <summary>
        /// Initialize APIC Controller
        /// </summary>
        public static bool Initialize()
        {
            if (_initialized)
                return true;

            // Log initialization start
            SerialDebug.Info("Initializing APIC Controller...");

            // First remap PIC to avoid conflicts with exceptions
            RemapPIC();

            // Get Local APIC Address from ACPI
            ulong apicAddr = ACPIManager.GetLocalApicAddress();

            // Validate and fallback to default if needed
            if (apicAddr == 0)
            {
                apicAddr = 0xFEE00000; // Default Local APIC address
            }

            if (apicAddr < 0x1000 || apicAddr > 0xFFFFFFFF)
            {
                SerialDebug.Error("Invalid Local APIC address.");
                return false;
            }

            // Map physical address (assuming 1:1 mapping)
            _localApicAddress = (uint*)apicAddr;

            // Check if APIC is supported and enabled in the CPU
            if (!IsApicSupported())
            {
                SerialDebug.Warning("APIC not supported or disabled by BIOS/firmware");
                return false;
            }

            // Read APIC ID and Version from registers
            uint idReg = ReadApicRegister(APIC_ID_REGISTER);
            uint versionReg = ReadApicRegister(APIC_VERSION_REGISTER);

            _apicId = (byte)(idReg >> 24);
            _apicVersion = (byte)(versionReg & 0xFF);

            SerialDebug.Info("Local APIC ID: " + _apicId + ", Version: " + _apicVersion);

            // Configure APIC
            ConfigureApic();

            // Calibrate timer
            CalibrateTimer();

            _initialized = true;
            _apicEnabled = true;
            SerialDebug.Info("APIC Controller initialized successfully");
            return true;
        }

        /// <summary>
        /// Check if APIC is supported by the CPU
        /// </summary>
        private static bool IsApicSupported()
        {
            SerialDebug.Info("Checking APIC support using multiple methods...");

            // --- Método 1: Verificar con la función simplificada ---
            uint cpuidValue = Native.CPUID(1);  // Usar tu método simplificado
            bool method1ApicSupported = (cpuidValue & (1 << 9)) != 0;
            SerialDebug.Info($"Method 1 - APIC bit in CPUID(1): {(method1ApicSupported ? "Set" : "Not set")}");

            // --- Método 2: Usar la función completa con diagnósticos detallados ---
            uint maxFunction = 0;
            uint ebx = 0, ecx = 0, edx = 0;

            // Verificar la función máxima soportada
            Native.Cpuid(0, ref maxFunction, ref ebx, ref ecx, ref edx);
            SerialDebug.Info($"Method 2 - Maximum CPUID function: {maxFunction}");

            // Guardar firma del fabricante para diagnóstico
            char[] vendorId = new char[13];
            unsafe
            {
                fixed (char* ptr = vendorId)
                {
                    *(uint*)(ptr + 0) = ebx;
                    *(uint*)(ptr + 4) = edx;
                    *(uint*)(ptr + 8) = ecx;
                    *(ptr + 12) = '\0';
                }
            }
            SerialDebug.Info($"CPU Vendor ID: {new string(vendorId, 0, 12)}");

            bool method2ApicSupported = false;
            // Intentar función 1 si está disponible
            if (maxFunction >= 1)
            {
                uint eax = 1;
                Native.Cpuid(eax, ref eax, ref ebx, ref ecx, ref edx);

               // SerialDebug.Info($"CPUID(1) registers: EAX={((ulong)eax).ToStringHex()}, EBX={((ulong)ebx).ToStringHex()}, ECX={((ulong)ecx).ToStringHex()}, EDX={((ulong)edx).ToStringHex()}");

                method2ApicSupported = (edx & (1 << 9)) != 0;
                SerialDebug.Info($"Method 2 - APIC bit in CPUID(1): {(method2ApicSupported ? "Set" : "Not set")}");
            }
            else
            {
                SerialDebug.Warning("CPUID function 1 not available, falling back to other methods");
            }

            // --- Método 3: Verificar MSR ---
            ulong msr = Native.ReadMSR(0x1B);  // IA32_APIC_BASE MSR
            SerialDebug.Info($"MSR (IA32_APIC_BASE) Value: 0x{msr.ToStringHex()}");

            bool msrApicEnabled = (msr & (1UL << 11)) != 0;
            SerialDebug.Info($"Method 3 - APIC Enabled in MSR: {(msrApicEnabled ? "Yes" : "No")}");

            // Verificar la dirección base del APIC
            ulong apicBaseAddress = msr & 0xFFFFFFFFFFFF000UL;
            SerialDebug.Info($"APIC Base Address from MSR: 0x{apicBaseAddress.ToStringHex()}");

            // --- Método 4: Verificar ACPI/Hardware ---
            ulong acpiApicAddr = ACPIManager.GetLocalApicAddress();
            SerialDebug.Info($"Method 4 - ACPI APIC Address: 0x{acpiApicAddr.ToStringHex()}");

            // Determinar si APIC está soportado basado en todos los métodos
            _cpuidApicSupported = method1ApicSupported || method2ApicSupported;
            _msrApicEnabled = msrApicEnabled;

            // Estrategia de decisión:
            // 1. Si el MSR muestra que APIC está habilitado, lo usamos
            if (msrApicEnabled)
            {
                SerialDebug.Info("Using APIC based on MSR status");
                return true;
            }

            // 2. Si CPUID indica soporte para APIC, intentamos habilitarlo
            if (_cpuidApicSupported)
            {
                SerialDebug.Info("Using APIC based on CPUID feature flag");
                return true;
            }

            // 3. Si ACPI detectó una dirección de APIC, asumimos que está disponible
            if (acpiApicAddr != 0)
            {
                SerialDebug.Info("Using APIC based on ACPI table detection");
                SerialDebug.Warning("APIC not detected by CPU, but present in ACPI tables. Proceeding with caution.");
                return true;
            }

            // 4. Si estamos en un entorno sin APIC, informar y optar por el PIC tradicional
            SerialDebug.Warning("APIC not supported by any detection method");
            return false;
        }

        /// <summary>
        /// Remap PIC to avoid conflicts with CPU exceptions
        /// </summary>
        private static void RemapPIC()
        {
            // Save masks
            byte masterMask = IOPort.In8(PIC1_DATA);
            byte slaveMask = IOPort.In8(PIC2_DATA);

            // Start initialization sequence (cascade mode)
            IOPort.Out8(PIC1_COMMAND, PIC_ICW1_INIT | PIC_ICW1_ICW4);
            IOPort.Wait();
            IOPort.Out8(PIC2_COMMAND, PIC_ICW1_INIT | PIC_ICW1_ICW4);
            IOPort.Wait();

            // Set vector offsets: Master PIC IRQ 0-7 -> INT 0x20-0x27, Slave PIC IRQ 8-15 -> INT 0x28-0x2F
            IOPort.Out8(PIC1_DATA, 0x20);  // Master PIC: IRQ 0-7 -> INT 0x20-0x27
            IOPort.Wait();
            IOPort.Out8(PIC2_DATA, 0x28);  // Slave PIC: IRQ 8-15 -> INT 0x28-0x2F
            IOPort.Wait();

            // Set up cascading: Master PIC will communicate with Slave PIC on IRQ2
            IOPort.Out8(PIC1_DATA, 4);     // Master: Slave on IRQ2
            IOPort.Wait();
            IOPort.Out8(PIC2_DATA, 2);     // Slave: Cascade identity
            IOPort.Wait();

            // Set operation mode (8086 mode)
            IOPort.Out8(PIC1_DATA, PIC_ICW4_8086);
            IOPort.Wait();
            IOPort.Out8(PIC2_DATA, PIC_ICW4_8086);
            IOPort.Wait();

            // Restore masks to their original state
            IOPort.Out8(PIC1_DATA, masterMask);
            IOPort.Out8(PIC2_DATA, slaveMask);
        }


        /// <summary>
        /// Configure APIC
        /// </summary>
        private static void ConfigureApic()
        {
            // Read current spurious vector register value for diagnostics
            uint currentSvr = ReadApicRegister(APIC_SPURIOUS_INTERRUPT_VECTOR);
            //SerialDebug.Info("Current Spurious Register Value: 0x" + ((ulong)currentSvr).ToStringHex());

            // Enable APIC in MSR if not already enabled
            EnableApicInMSR();

            // Configure Spurious Interrupt Vector Register
            // Bit 8: Enable APIC, Bit 9: Focus Processor Checking
            uint newSvr = APIC_ENABLE | APIC_SPURIOUS_VECTOR;
            WriteApicRegister(APIC_SPURIOUS_INTERRUPT_VECTOR, newSvr);
            //SerialDebug.Info("New Spurious Register Value: 0x" + ((ulong)newSvr).ToStringHex());

            // Configure Task Priority Register (accept all interrupts)
            WriteApicRegister(APIC_TASK_PRIORITY, 0);

            // Set up error handling
            WriteApicRegister(APIC_LVT_ERROR, APIC_ERROR_VECTOR);

            // Configure LINT0 and LINT1 as normal interrupts (initially masked)
            WriteApicRegister(APIC_LVT_LINT0, LVT_DELIVERY_MODE_EXTINT | LVT_MASK);
            WriteApicRegister(APIC_LVT_LINT1, LVT_DELIVERY_MODE_NMI | LVT_MASK);

            // Configure Performance Counter (if needed)
            WriteApicRegister(APIC_LVT_PERFORMANCE, APIC_PERFORMANCE_VECTOR | LVT_MASK);

            // Configure Thermal Sensor (if needed)
            WriteApicRegister(APIC_LVT_THERMAL, APIC_THERMAL_VECTOR | LVT_MASK);

            // Disable timer initially
            WriteApicRegister(APIC_LVT_TIMER, LVT_TIMER_MASK_INTERRUPT);

            // Disable legacy PIC
            DisablePIC();

            SerialDebug.Info("APIC configured successfully");
        }

        /// <summary>
        /// Enable APIC in the IA32_APIC_BASE MSR
        /// </summary>
        private static void EnableApicInMSR()
        {
            // Read MSR IA32_APIC_BASE (0x1B)
            ulong msr = Native.ReadMSR(0x1B);
            SerialDebug.Info("Initial MSR value: 0x" + msr.ToStringHex());

            // Check if APIC is already enabled (bit 11)
            if ((msr & (1UL << 11)) != 0)
            {
                SerialDebug.Info("APIC already enabled in MSR");
                return;
            }

            // Set the APIC enable bit (bit 11)
            msr |= (1UL << 11);

            // Write updated MSR
            Native.WriteMSR(0x1B, msr);

            SerialDebug.Info("Updated MSR value: 0x" + msr.ToStringHex());
            SerialDebug.Info("APIC enabled in MSR");
        }

        /// <summary>
        /// Calibrate the APIC timer using PIT for reference
        /// </summary>
        private static void CalibrateTimer()
        {
            SerialDebug.Info("Calibrating APIC timer...");

            // Configure PIT for one-shot mode
            IOPort.Out8(0x43, 0x30);   // Channel 0, Mode 0 (one-shot), binary

            // Set PIT to count down from 65535
            IOPort.Out8(0x40, 0xFF);   // Low byte
            IOPort.Out8(0x40, 0xFF);   // High byte

            // Set up APIC timer with maximum count, divide by 16
            WriteApicRegister(APIC_TIMER_DIVIDE_CONFIG, TIMER_DIV_16);
            WriteApicRegister(APIC_TIMER_INITIAL_COUNT, 0xFFFFFFFF);

            // Read initial PIT count
            IOPort.Out8(0x43, 0x00);  // Latch count
            uint startPit = (uint)IOPort.In8(0x40);        // Low byte
            startPit |= (uint)(IOPort.In8(0x40) << 8);     // High byte

            // Calculate target count (1/4 of the way down)
            uint targetCount = startPit - (startPit / 4);

            // Wait for PIT to count down to target
            uint currentPit;
            int waitCounter = 0;
            int maxWait = 1000000;   // Prevent infinite loop

            do
            {
                IOPort.Out8(0x43, 0x00);  // Latch count
                currentPit = (uint)IOPort.In8(0x40);       // Low byte
                currentPit |= (uint)(IOPort.In8(0x40) << 8); // High byte

                // Make sure we don't get stuck in an infinite loop
                waitCounter++;
                if (waitCounter > maxWait)
                {
                    _timerTicksPerMS = 10000; // Default value
                    return;
                }

                // Handle underflow (PIT counts down)
                if (currentPit > startPit)
                {
                    // PIT has wrapped around
                    break;
                }

            } while (currentPit > targetCount);

            // Stop the APIC timer
            WriteApicRegister(APIC_LVT_TIMER, LVT_TIMER_MASK_INTERRUPT);
            uint timerCount = ReadApicRegister(APIC_TIMER_CURRENT_COUNT);
            uint ticksElapsed = 0xFFFFFFFF - timerCount;

            // Calculate elapsed time in microseconds (adjust for PIT frequency 1.193182 MHz)
            uint elapsedMicroseconds;

            // Handle possible PIT wraparound
            if (currentPit > startPit)
            {
                // PIT wrapped around (rare case)
                elapsedMicroseconds = (65536 - startPit + currentPit) * 838;
            }
            else
            {
                // Normal case
                elapsedMicroseconds = (startPit - currentPit) * 838; // 838ns per PIT tick
            }

            // Calculate ticks per millisecond (prevent division by zero)
            if (elapsedMicroseconds >= 1000)
            {
                _timerTicksPerMS = (ticksElapsed * 1000) / (elapsedMicroseconds / 1000);
            }
            else
            {
                // In case elapsed time was too short
                _timerTicksPerMS = ticksElapsed * (1000 / elapsedMicroseconds);
            }

            // Sanity check - if we got a very small or very large value, use a reasonable default
            if (_timerTicksPerMS < 1000 || _timerTicksPerMS > 100000000)
            {
                // SerialDebug.Warning("Unreliable timer calibration result: " + _timerTicksPerMS + " ticks/ms - using default");
                _timerTicksPerMS = 10000; // Default reasonable value
            }

            // SerialDebug.Info("APIC Timer Calibration: " + _timerTicksPerMS + " ticks/ms (div=16)");
        }

        /// <summary>
        /// Disable legacy 8259 PIC
        /// </summary>
        private static void DisablePIC()
        {
            // Mask all interrupts on both PICs
            IOPort.Out8(PIC1_DATA, 0xFF);
            IOPort.Out8(PIC2_DATA, 0xFF);
            SerialDebug.Info("Legacy PIC disabled");
        }

        /// <summary>
        /// Read APIC Local Register with safety checks
        /// </summary>
        private static uint ReadApicRegister(int reg)
        {
            if (_localApicAddress == null)
            {
                // SerialDebug.Warning("Attempted to read APIC register 0x" + ((ulong)reg).ToStringHex() + " but APIC address is null");
                return 0;
            }

            // Make sure the register offset is aligned (should be divisible by 4)
            if ((reg & 0x3) != 0)
            {
                reg &= ~0x3; // Force alignment
            }

            return _localApicAddress[reg / 4];
        }

        /// <summary>
        /// Write to APIC Local Register with safety checks
        /// </summary>
        private static void WriteApicRegister(int reg, uint value)
        {
            if (_localApicAddress == null)
            {
                // SerialDebug.Warning("Attempted to write 0x" + ((ulong)value).ToStringHex() + " to APIC register 0x" + ((ulong)reg).ToStringHex() + " but APIC address is null");
                return;
            }

            // Make sure the register offset is aligned (should be divisible by 4)
            if ((reg & 0x3) != 0)
            {
                reg &= ~0x3; // Force alignment
            }

            _localApicAddress[reg / 4] = value;
        }

        /// <summary>
        /// Send End of Interrupt (EOI) command to APIC
        /// </summary>
        public static void SendEOI()
        {
            if (!_initialized || !_apicEnabled)
                return;

            WriteApicRegister(APIC_EOI, 0);
        }

        /// <summary>
        /// Configure and enable APIC Timer
        /// </summary>
        /// <param name="vector">Interrupt vector (32-255)</param>
        /// <param name="milliseconds">Timer interval in milliseconds</param>
        /// <param name="periodic">True for periodic mode, false for one-shot</param>
        /// <returns>True if successful</returns>
        public static bool StartTimer(byte vector, uint milliseconds, bool periodic)
        {
            if (!_initialized || !_apicEnabled || vector < 32)
                return false;

            // Set timer divider (divide by 16)
            WriteApicRegister(APIC_TIMER_DIVIDE_CONFIG, TIMER_DIV_16);

            // Calculate initial count
            uint initialCount = milliseconds * _timerTicksPerMS;

            // Configure timer LVT entry
            uint config = vector;
            if (periodic)
                config |= LVT_TIMER_MODE_PERIODIC;

            // Set timer configuration
            WriteApicRegister(APIC_LVT_TIMER, config);

            // Set initial count to start the timer
            WriteApicRegister(APIC_TIMER_INITIAL_COUNT, initialCount);

            //SerialDebug.Info("APIC Timer started: vector=" + vector + ", period=" + milliseconds + "ms, count=" + initialCount);
            return true;
        }

        /// <summary>
        /// Stop APIC Timer
        /// </summary>
        public static void StopTimer()
        {
            if (!_initialized || !_apicEnabled)
                return;

            // Mask timer and set count to 0
            WriteApicRegister(APIC_LVT_TIMER, LVT_TIMER_MASK_INTERRUPT);
            WriteApicRegister(APIC_TIMER_INITIAL_COUNT, 0);

            SerialDebug.Info("APIC Timer stopped");
        }

        /// <summary>
        /// Send Inter-Processor Interrupt (IPI)
        /// </summary>
        /// <param name="destination">Destination APIC ID</param>
        /// <param name="deliveryMode">Delivery mode (FIXED, NMI, etc.)</param>
        /// <param name="vector">Interrupt vector (0-255)</param>
        /// <returns>True if IPI was sent successfully</returns>
        public static bool SendIpi(byte destination, uint deliveryMode, byte vector)
        {
            if (!_initialized || !_apicEnabled)
                return false;

            // Wait for any previous IPI to complete (check delivery status bit)
            int spinCount = 0;
            int maxSpinCount = 1000000;

            while ((ReadApicRegister(APIC_INTERRUPT_COMMAND_LOW) & ICR_DELIVERY_STATUS) != 0)
            {
                Native.Pause();
                spinCount++;

                // Timeout after too many attempts
                if (spinCount > maxSpinCount)
                {
                    return false;
                }
            }

            // Set high part of ICR (destination processor ID)
            WriteApicRegister(APIC_INTERRUPT_COMMAND_HIGH, (uint)destination << 24);

            // Set low part of ICR (delivery mode, vector, etc.)
            uint command = deliveryMode | (uint)vector;
            WriteApicRegister(APIC_INTERRUPT_COMMAND_LOW, command);

            return true;
        }

        /// <summary>
        /// Send INIT IPI to a processor
        /// </summary>
        /// <param name="destination">Destination APIC ID</param>
        /// <returns>True if successful</returns>
        public static bool SendInitIpi(byte destination)
        {
            if (!_initialized || !_apicEnabled)
                return false;

            // Assert INIT IPI
            bool result = SendIpi(destination, ICR_DELIVERY_MODE_INIT | ICR_LEVEL_ASSERT, 0);

            // Wait 10ms
            BusyWait(10);

            // Deassert INIT (not required on many newer systems)
            SendIpi(destination, ICR_DELIVERY_MODE_INIT, 0);

            return result;
        }

        /// <summary>
        /// Send STARTUP IPI to a processor
        /// </summary>
        /// <param name="destination">Destination APIC ID</param>
        /// <param name="vector">Start-up vector (0-255, typically points to trampoline code)</param>
        /// <returns>True if successful</returns>
        public static bool SendStartupIpi(byte destination, byte vector)
        {
            if (!_initialized || !_apicEnabled)
                return false;

            return SendIpi(destination, ICR_DELIVERY_MODE_STARTUP, vector);
        }

        /// <summary>
        /// Initialize Application Processors (APs) using the MP startup protocol
        /// </summary>
        /// <param name="trampolineAddress">Physical address of the trampoline code</param>
        /// <returns>True if at least one AP was started</returns>
        public static bool InitializeAPs(ulong trampolineAddress)
        {
            if (!_initialized || !_apicEnabled)
                return false;

            // Address must be 4K aligned and below 1MB
            if ((trampolineAddress & 0xFFF) != 0 || trampolineAddress >= 0x100000)
            {
                // SerialDebug.Warning("Invalid trampoline address: 0x" + trampolineAddress.ToStringHex());
                return false;
            }

            // Convert address to a startup vector (4K aligned, bits 12-19)
            byte startupVector = (byte)((trampolineAddress >> 12) & 0xFF);

            // Get processor count from SMP manager
            int cpuCount = SMPManager.GetProcessorCount();
            int startedCount = 0;

            // SerialDebug.Info("Starting " + (cpuCount - 1) + " APs with trampoline at 0x" + trampolineAddress.ToStringHex() + " (vector 0x" + ((ulong)startupVector).ToStringHex() + ")");

            // Skip BSP (index 0) and start APs
            for (int i = 1; i < cpuCount; i++)
            {
                byte apicId = SMPManager.GetAPICId(i);

                // Skip self
                if (apicId == _apicId)
                    continue;

                // SerialDebug.Info("Starting processor with APIC ID " + apicId + "...");

                // Send INIT IPI
                SendInitIpi(apicId);

                // Wait at least 10ms
                BusyWait(10);

                // Send STARTUP IPI (best practice is to send twice)
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    if (SendStartupIpi(apicId, startupVector))
                    {
                        BusyWait(1);  // Wait a short time between attempts
                    }
                }

                startedCount++;
            }

            // Allow time for APs to initialize
            BusyWait(50);

            return startedCount > 0;
        }

        /// <summary>
        /// Busy wait for a specified number of milliseconds
        /// </summary>
        /// <param name="ms">Number of milliseconds to wait</param>
        private static void BusyWait(int ms)
        {
            // This is a crude delay - in a real system fIsApicSupportedyou'd use a timer
            // Adjust the loop count based on CPU speed
            uint loopCount = (uint)ms * 100000;  // Adjust this multiplier based on CPU speed
            for (uint i = 0; i < loopCount; i++)
            {
                Native.Pause();  // Pause instruction for efficiency
            }
        }

        /// <summary>
        /// Wrapper for CPUID instruction
        /// </summary>
        private static void CPUID(uint function, ref uint eax, ref uint ebx, ref uint ecx, ref uint edx)
        {
            eax = function;  // Set function number in eax
            Native.Cpuid(function, ref eax, ref ebx, ref ecx, ref edx);
        }

        /// <summary>
        /// Enable or disable specified Local Vector Table entry
        /// </summary>
        /// <param name="lvtRegister">LVT register offset</param>
        /// <param name="enable">True to enable, false to disable</param>
        /// <param name="vector">Interrupt vector (0-255)</param>
        /// <returns>True if successful</returns>
        public static bool ConfigureLvtEntry(int lvtRegister, bool enable, byte vector)
        {
            if (!_initialized || !_apicEnabled)
                return false;

            // Validate vector if enabling
            if (enable && vector < 32)
            {
                SerialDebug.Warning("Invalid vector " + vector + " for LVT entry (must be >= 32)");
                return false;
            }

            // Validate LVT register offset
            bool validRegister = false;

            if (lvtRegister == APIC_LVT_TIMER ||
                lvtRegister == APIC_LVT_THERMAL ||
                lvtRegister == APIC_LVT_PERFORMANCE ||
                lvtRegister == APIC_LVT_LINT0 ||
                lvtRegister == APIC_LVT_LINT1 ||
                lvtRegister == APIC_LVT_ERROR)
            {
                validRegister = true;
            }

            if (!validRegister)
            {
                //SerialDebug.Warning("Invalid LVT register offset: 0x" + ((ulong)lvtRegister).ToStringHex());
                return false;
            }

            // Read current value
            uint value = ReadApicRegister(lvtRegister);

            // Clear vector and mask bit
            value &= ~0xFF;         // Clear vector bits
            value &= ~(1 << 16);    // Clear mask bit

            // Set new vector
            value |= vector;

            // Set mask bit if disabling
            if (!enable)
                value |= (1 << 16);

            // Write back
            WriteApicRegister(lvtRegister, value);

            return true;
        }

        public static byte GetValidCPUId()
        {
            // Función 1: Información del procesador
            uint eax = 1;
            uint ebx = 0, ecx = 0, edx = 0;
            Native.Cpuid(eax, ref eax, ref ebx, ref ecx, ref edx);

            // Extraer ID de APIC de los bits superiores de EBX
            byte apicId = (byte)((ebx >> 24) & 0xFF);

            // Validación usando la lógica de tu APIC Controller
            byte currentApicId = GetCurrentApicId();

            // Estrategias de validación
            if (apicId < 8)
            {
                SerialDebug.Info($"Using APIC ID from CPUID: {apicId}");
                return apicId;
            }

            if (currentApicId < 8)
            {
                //SerialDebug.Info($"Using APIC ID from APIC Controller: {currentApicId}");
                return currentApicId;
            }

            // Intentar extracción alternativa de EBX
            apicId = (byte)((ebx >> 16) & 0x0F);

            return (byte)((apicId < 8) ? apicId : 0);
        }

        /// <summary>
        /// Gets the Local APIC ID of the current processor
        /// </summary>
        /// <returns>APIC ID of current processor</returns>
        public static byte GetCurrentApicId()
        {
            if (!_initialized || !_apicEnabled)
                return 0;

            uint idReg = ReadApicRegister(APIC_ID_REGISTER);
            return (byte)(idReg >> 24);
        }

        /// <summary>
        /// Gets whether the APIC is initialized and enabled
        /// </summary>
        public static bool IsApicEnabled
        {
            get { return _initialized && _apicEnabled; }
        }

        /// <summary>
        /// Gets the address of the Local APIC memory-mapped registers
        /// </summary>
        public static ulong LocalApicAddress
        {
            get { return (ulong)_localApicAddress; }
        }

        /// <summary>
        /// Gets the ID of the Local APIC
        /// </summary>
        public static byte ApicId
        {
            get { return _apicId; }
        }

        /// <summary>
        /// Gets the version of the Local APIC
        /// </summary>
        public static byte ApicVersion
        {
            get { return _apicVersion; }
        }

        /// <summary>
        /// Gets the calibrated timer ticks per millisecond
        /// </summary>
        public static uint TimerTicksPerMS
        {
            get { return _timerTicksPerMS; }
        }
    }
}