using Kernel.Diagnostics;
using Kernel.Hardware;

namespace Kernel.System
{
    /// <summary>
    /// System timer interface - provides a unified time source for the kernel
    /// </summary>
    public static class Timer
    {
        // Indicates if the timer subsystem is initialized
        private static bool _initialized = false;

        // The current time source
        private enum TimeSource
        {
            None,
            PITController,
            HPET,
            ACPI
        }

        private static TimeSource _currentSource = TimeSource.None;

        /// <summary>
        /// Initializes the timer subsystem
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
                return;

            SerialDebug.Info("Initializing system timer...");

            // Try to initialize PITController - most reliable and always available
            PITController.Initialize();
            _currentSource = TimeSource.PITController;

            // Note: In a more advanced kernel, you might check for and use HPET or ACPI timer
            // if available, as they provide higher resolution timing

            _initialized = true;
            SerialDebug.Info($"System timer initialized using {_currentSource}");
        }

        /// <summary>
        /// Gets the current tick count from the best available timer
        /// </summary>
        /// <returns>Current tick count</returns>
        public static ulong GetTickCount()
        {
            switch (_currentSource)
            {
                case TimeSource.PITController:
                    return PITController.Ticks;
                // Add cases for other timers when implemented
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Gets the current system uptime in milliseconds
        /// </summary>
        /// <returns>Uptime in milliseconds</returns>
        public static ulong GetUptimeMS()
        {
            switch (_currentSource)
            {
                case TimeSource.PITController:
                    return PITController.UptimeMilliseconds;
                // Add cases for other timers when implemented
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Gets the current system uptime in seconds
        /// </summary>
        /// <returns>Uptime in seconds</returns>
        public static ulong GetUptimeSeconds()
        {
            switch (_currentSource)
            {
                case TimeSource.PITController:
                    return PITController.UptimeSeconds;
                // Add cases for other timers when implemented
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Waits for the specified number of milliseconds
        /// </summary>
        /// <param name="ms">Number of milliseconds to wait</param>
        public static void Sleep(uint ms)
        {
            if (!_initialized)
            {
                // Basic busy waiting if timer not initialized
                for (uint i = 0; i < ms * 10000; i++)
                {
                    Native.Pause();
                }
                return;
            }

            switch (_currentSource)
            {
                case TimeSource.PITController:
                    PITController.Sleep(ms);
                    break;
                // Add cases for other timers when implemented
                default:
                    // Default busy wait
                    for (uint i = 0; i < ms * 10000; i++)
                    {
                        Native.Pause();
                    }
                    break;
            }
        }

        /// <summary>
        /// Gets whether the timer is initialized
        /// </summary>
        public static bool IsInitialized
        {
            get { return _initialized; }
        }
    }
}