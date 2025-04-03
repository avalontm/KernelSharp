using Kernel.Diagnostics;
using Kernel.Memory;
using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Kernel.Hardware
{
    /// <summary>
    /// Programmable Interval Timer (PIT) controller
    /// </summary>
    public static unsafe class PITController
    {
        // PIT I/O ports
        private const ushort PIT_DATA_PORT_0 = 0x40;  // Channel 0 data port
        private const ushort PIT_DATA_PORT_1 = 0x41;  // Channel 1 data port
        private const ushort PIT_DATA_PORT_2 = 0x42;  // Channel 2 data port
        private const ushort PIT_COMMAND_PORT = 0x43; // Command register port

        // PIT command bits
        private const byte PIT_CHANNEL_0 = 0x00;      // Channel 0 select
        private const byte PIT_CHANNEL_1 = 0x40;      // Channel 1 select
        private const byte PIT_CHANNEL_2 = 0x80;      // Channel 2 select
        private const byte PIT_LATCH_COMMAND = 0x00;  // Latch command
        private const byte PIT_ACCESS_BOTH = 0x30;    // Access mode: low byte, then high byte
        private const byte PIT_MODE_SQUARE_WAVE = 0x06; // Mode 3: square wave generator
        private const byte PIT_MODE_RATE_GEN = 0x04;  // Mode 2: rate generator

        // PIT frequency (1.193182 MHz)
        private const uint PIT_BASE_FREQUENCY = 1193182;

        // Default frequency for PIT channel 0 (100 Hz = 10ms per tick)
        private const uint DEFAULT_FREQUENCY = 100;

        // Time-related values
        private static readonly uint _tickPeriodMs;
        private static readonly uint _ticksPerSecond;
        private static readonly uint _ticksPerMs;

        // Counter for system ticks
        private static ulong _ticks;

        // Indicates if the PIT is initialized
        private static bool _initialized;

        /// <summary>
        /// Static constructor to calculate tick values
        /// </summary>
        static PITController()
        {
            _ticksPerSecond = DEFAULT_FREQUENCY;
            _tickPeriodMs = 1000 / DEFAULT_FREQUENCY;
            _ticksPerMs = DEFAULT_FREQUENCY / 1000;
            if (_ticksPerMs == 0) _ticksPerMs = 1; // Ensure at least 1 tick per ms
        }

        /// <summary>
        /// Initializes the PIT controller
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
                return;

            SerialDebug.Info("Initializing PIT (Programmable Interval Timer)...");

            // Calculate the divisor for the desired frequency
            uint divisor = PIT_BASE_FREQUENCY / DEFAULT_FREQUENCY;

            // Ensure the divisor fits in 16 bits
            if (divisor > 0xFFFF)
                divisor = 0xFFFF;

            // Program PIT channel 0 in rate generator mode
            Native.OutByte(PIT_COMMAND_PORT, PIT_CHANNEL_0 | PIT_ACCESS_BOTH | PIT_MODE_RATE_GEN);

            // Set the divisor (low byte, then high byte)
            Native.OutByte(PIT_DATA_PORT_0, (byte)(divisor & 0xFF));
            Native.OutByte(PIT_DATA_PORT_0, (byte)((divisor >> 8) & 0xFF));

            // Reset counter
            _ticks = 0;

            // Register the IRQ handler for PIT (IRQ 0)
            // Note: This assumes you have an InterruptManager or similar mechanism
            if (InterruptManager._initialized)
            {
                RegisterPITHandler();
            }

            _initialized = true;
            SerialDebug.Info($"PIT initialized at {DEFAULT_FREQUENCY} Hz (period: {_tickPeriodMs} ms)");
        }

        /// <summary>
        /// Registers the PIT interrupt handler
        /// </summary>
        private static void RegisterPITHandler()
        {
            // Get PIT interrupt handler address
            IntPtr handler = GetPITHandlerAddress();

            // Register with IDT (interrupt 0x20 = IRQ 0)
            // Note: This assumes you've remapped the PIC to start IRQs at 0x20
            IDTManager.SetIDTEntry(0x20, handler, 0x08, IDTGateType.InterruptGate);

            SerialDebug.Info("PIT interrupt handler registered");
        }

        /// <summary>
        /// PIT interrupt handler - increments the tick counter
        /// </summary>
        [RuntimeExport("HandlePITInterrupt")]
        public static void HandlePITInterrupt()
        {
            // Increment tick counter
            _ticks++;

            // Send EOI (End Of Interrupt) signal to PIC
            // 0x20 is the command port for the master PIC, 0x20 is the EOI command
            Native.OutByte(0x20, 0x20);
        }

        /// <summary>
        /// Gets the PIT handler address for registration with IDT
        /// </summary>
        [DllImport("*", EntryPoint = "_GetPITHandlerAddress")]
        private static extern IntPtr GetPITHandlerAddress();

        /// <summary>
        /// Waits for the specified number of milliseconds using the PIT
        /// </summary>
        /// <param name="ms">Number of milliseconds to wait</param>
        public static void Sleep(uint ms)
        {
            if (!_initialized)
                return;

            ulong targetTicks = _ticks + ((ms * _ticksPerMs) > 0 ? (ms * _ticksPerMs) : 1);

            while (_ticks < targetTicks)
            {
                // If threading is available, yield
                if (Kernel.Threading.Scheduler.IsInitialized)
                {
                    Kernel.Threading.Thread.Yield();
                }
                else
                {
                    // Otherwise, pause to save power
                    Native.Pause();
                }
            }
        }

        /// <summary>
        /// Waits for the specified number of ticks
        /// </summary>
        /// <param name="ticks">Number of ticks to wait</param>
        public static void WaitTicks(ulong ticks)
        {
            if (!_initialized)
                return;

            ulong targetTicks = _ticks + ticks;

            while (_ticks < targetTicks)
            {
                // If threading is available, yield
                if (Kernel.Threading.Scheduler.IsInitialized)
                {
                    Kernel.Threading.Thread.Yield();
                }
                else
                {
                    // Otherwise, pause to save power
                    Native.Pause();
                }
            }
        }

        /// <summary>
        /// Gets the current system tick count
        /// </summary>
        public static ulong Ticks
        {
            get { return _ticks; }
        }

        /// <summary>
        /// Gets the number of ticks per millisecond
        /// </summary>
        public static uint TicksPerMS
        {
            get { return _ticksPerMs; }
        }

        /// <summary>
        /// Gets the number of ticks per second
        /// </summary>
        public static uint TicksPerSecond
        {
            get { return _ticksPerSecond; }
        }

        /// <summary>
        /// Gets the milliseconds per tick
        /// </summary>
        public static uint MSPerTick
        {
            get { return _tickPeriodMs; }
        }

        /// <summary>
        /// Gets whether the PIT is initialized
        /// </summary>
        public static bool IsInitialized
        {
            get { return _initialized; }
        }

        /// <summary>
        /// Gets the current system uptime in milliseconds
        /// </summary>
        public static ulong UptimeMilliseconds
        {
            get { return _ticks * _tickPeriodMs; }
        }

        /// <summary>
        /// Gets the current system uptime in seconds
        /// </summary>
        public static ulong UptimeSeconds
        {
            get { return _ticks / _ticksPerSecond; }
        }
    }
}