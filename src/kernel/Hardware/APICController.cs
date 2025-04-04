using Kernel.Diagnostics;
using Kernel.Drivers;
using System;

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
        private const int APIC_SPURIOUS_INTERRUPT_VECTOR = 0xF0;
        private const int APIC_INTERRUPT_COMMAND_LOW = 0x300;
        private const int APIC_INTERRUPT_COMMAND_HIGH = 0x310;
        private const int APIC_LVT_TIMER = 0x320;
        private const int APIC_TIMER_INITIAL_COUNT = 0x380;
        private const int APIC_TIMER_CURRENT_COUNT = 0x390;
        private const int APIC_TIMER_DIVIDE_CONFIG = 0x3E0;

        // Interrupt Command Register (ICR) Bits
        private const uint ICR_DELIVERY_MODE_INIT = 5 << 8;
        private const uint ICR_DELIVERY_MODE_STARTUP = 6 << 8;
        private const uint ICR_LEVEL_ASSERT = 1 << 14;

        // Controller State
        private static bool _initialized;
        private static uint* _localApicAddress;
        private static byte _apicId;
        private static byte _apicVersion;

        /// <summary>
        /// Initialize APIC Controller
        /// </summary>
        public static bool Initialize()
        {
            if (_initialized)
                return true;

            // Log initialization start
            SerialDebug.Info("Initializing APIC Controller...");

            // Get Local APIC Address from ACPI
            ulong apicAddr = ACPIManager.GetLocalApicAddress();

            if (apicAddr == 0)
            {
                SerialDebug.Info("Could not obtain Local APIC address from ACPI.");
                return false;
            }

            // Map physical address (assuming 1:1 mapping)
            _localApicAddress = (uint*)apicAddr;

            // Read APIC ID and Version
            _apicId = (byte)(ReadApicRegister(APIC_ID_REGISTER) >> 24);
            _apicVersion = (byte)(ReadApicRegister(APIC_VERSION_REGISTER) & 0xFF);

            SerialDebug.Info($"Local APIC ID: {_apicId.ToString()}, Version: {_apicVersion.ToString()}");

            // Configure APIC
            ConfigureApic();

            _initialized = true;
            SerialDebug.Info("APIC Controller initialized successfully.");
            return true;
        }

        /// <summary>
        /// Configure APIC
        /// </summary>
        private static void ConfigureApic()
        {
            // Enable APIC Mode (disable 8259 PIC)
            EnableApicMode();

            // Configure spurious interrupt handling
            // Bit 8: Enable APIC (1)
            // Bits 0-7: Vector for spurious interrupts (0xFF)
            WriteApicRegister(APIC_SPURIOUS_INTERRUPT_VECTOR, 0x100 | 0xFF);

            // Set task priority to 0 (highest)
            WriteApicRegister(APIC_TASK_PRIORITY, 0);

            // Disable timer initially
            WriteApicRegister(APIC_LVT_TIMER, 0x10000);
        }

        /// <summary>
        /// Enable APIC Mode and Disable 8259 PIC
        /// </summary>
        private static void EnableApicMode()
        {
            // Read MSR IA32_APIC_BASE (0x1B)
            ulong msr = Native.ReadMSR(0x1B);

            // Enable APIC (bit 11)
            msr |= 0x800;

            // Write updated MSR
            Native.WriteMSR(0x1B, msr);

            // Disable 8259 PIC by masking all interrupts
            PICController.SetMasterMask(0xFF);
            PICController.SetSlaveMask(0xFF);
        }

        /// <summary>
        /// Read APIC Local Register
        /// </summary>
        private static uint ReadApicRegister(int reg)
        {
            return _localApicAddress[reg / 4];
        }

        /// <summary>
        /// Write to APIC Local Register
        /// </summary>
        private static void WriteApicRegister(int reg, uint value)
        {
            _localApicAddress[reg / 4] = value;
        }

        /// <summary>
        /// Send End of Interrupt (EOI)
        /// </summary>
        public static void SendEoi()
        {
            WriteApicRegister(APIC_EOI, 0);
        }

        /// <summary>
        /// Send Inter-Processor Interrupt (IPI)
        /// </summary>
        public static void SendIpi(byte destination, uint deliveryMode, byte vector)
        {
            // Wait for previous IPI to complete
            int spinCount = 0;
            while ((ReadApicRegister(APIC_INTERRUPT_COMMAND_LOW) & 0x1000) != 0)
            {
                Native.Pause();
                spinCount++;

                // Prevent infinite loop
                if (spinCount > 1000000)
                    break;
            }

            // Set high part of ICR (destination processor ID)
            WriteApicRegister(APIC_INTERRUPT_COMMAND_HIGH, (uint)destination << 24);

            // Set low part of ICR (delivery mode, vector, etc.)
            uint command = deliveryMode | vector;
            WriteApicRegister(APIC_INTERRUPT_COMMAND_LOW, command);
        }

        /// <summary>
        /// Send INIT IPI to a processor
        /// </summary>
        public static void SendInitIpi(byte destination)
        {
            // INIT IPI: INIT delivery mode, Level Assert
            SendIpi(destination, ICR_DELIVERY_MODE_INIT | ICR_LEVEL_ASSERT, 0);

            // Short delay
            BusyWait(10);

            // De-assert INIT
            SendIpi(destination, ICR_DELIVERY_MODE_INIT, 0);
        }

        /// <summary>
        /// Send Startup IPI to a processor
        /// </summary>
        public static void SendStartupIpi(byte destination, byte vector)
        {
            // STARTUP IPI: STARTUP delivery mode
            SendIpi(destination, ICR_DELIVERY_MODE_STARTUP, vector);
        }

        /// <summary>
        /// Configure APIC Timer
        /// </summary>
        public static void ConfigureTimer(byte vector, uint initialCount, bool periodic)
        {
            // Set timer divider (divide by 16)
            WriteApicRegister(APIC_TIMER_DIVIDE_CONFIG, 0x3);

            // Configure LVT Timer
            uint timerConfig = vector;

            // Periodic mode if specified
            if (periodic)
            {
                timerConfig |= (1 << 17); // Bit 17: Periodic mode
            }

            WriteApicRegister(APIC_LVT_TIMER, timerConfig);

            // Set initial count
            WriteApicRegister(APIC_TIMER_INITIAL_COUNT, initialCount);
        }

        /// <summary>
        /// Stop APIC Timer
        /// </summary>
        public static void StopTimer()
        {
            // Set initial count to 0
            WriteApicRegister(APIC_TIMER_INITIAL_COUNT, 0);

            // Mask timer interrupt
            WriteApicRegister(APIC_LVT_TIMER, 0x10000);
        }

        /// <summary>
        /// Busy wait for a specified number of milliseconds
        /// </summary>
        private static void BusyWait(int ms)
        {
            // Very crude approximation - depends on CPU speed
            int iterations = ms * 1000000;
            for (int i = 0; i < iterations; i++)
            {
                Native.Pause();
            }
        }

        /// <summary>
        /// Initialize Application Processors (APs)
        /// </summary>
        public static void InitializeAps(ulong startupRoutine)
        {
            if (!_initialized)
                return;

            // Address must be 4K aligned
            byte startupVector = (byte)((startupRoutine >> 12) & 0xFF);

            // Get processor count
            int cpuCount = SMPManager.GetProcessorCount();

            // Start from 1 to skip Bootstrap Processor (BSP)
            for (int i = 1; i < cpuCount; i++)
            {
                byte apicId = SMPManager.GetAPICId(i);

                if (apicId != _apicId)
                {
                    SerialDebug.Info($"Starting processor with APIC ID {apicId}...");

                    // Send INIT IPI
                    SendInitIpi(apicId);

                    // Short delay
                    BusyWait(10);

                    // Send STARTUP IPI twice for robustness
                    SendStartupIpi(apicId, startupVector);
                    BusyWait(1);
                    SendStartupIpi(apicId, startupVector);

                    // Wait a bit more
                    BusyWait(10);
                }
            }
        }
    }
}