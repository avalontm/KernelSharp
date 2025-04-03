using Kernel.Diagnostics;
using Kernel.System;
using Kernel.Threading;
using System;

namespace Kernel.Hardware
{
    /// <summary>
    /// Controlador del Advanced Programmable Interrupt Controller (APIC)
    /// </summary>
    public static unsafe class APICController
    {
        // Registros del APIC Local
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
        private const int APIC_LVT_THERMAL_SENSOR = 0x330;
        private const int APIC_LVT_PERFORMANCE_COUNTER = 0x340;
        private const int APIC_LVT_LINT0 = 0x350;
        private const int APIC_LVT_LINT1 = 0x360;
        private const int APIC_LVT_ERROR = 0x370;
        private const int APIC_TIMER_INITIAL_COUNT = 0x380;
        private const int APIC_TIMER_CURRENT_COUNT = 0x390;
        private const int APIC_TIMER_DIVIDE_CONFIG = 0x3E0;

        // Bits para el registro de comandos de interrupción
        private const uint ICR_DELIVERY_MODE_FIXED = 0 << 8;
        private const uint ICR_DELIVERY_MODE_LOWEST_PRIORITY = 1 << 8;
        private const uint ICR_DELIVERY_MODE_SMI = 2 << 8;
        private const uint ICR_DELIVERY_MODE_NMI = 4 << 8;
        private const uint ICR_DELIVERY_MODE_INIT = 5 << 8;
        private const uint ICR_DELIVERY_MODE_STARTUP = 6 << 8;
        private const uint ICR_PHYSICAL_DESTINATION = 0 << 11;
        private const uint ICR_LOGICAL_DESTINATION = 1 << 11;
        private const uint ICR_DELIVERY_STATUS_PENDING = 1 << 12;
        private const uint ICR_LEVEL_ASSERT = 1 << 14;
        private const uint ICR_TRIGGER_MODE_EDGE = 0 << 15;
        private const uint ICR_TRIGGER_MODE_LEVEL = 1 << 15;
        private const uint ICR_DESTINATION_SHORTHAND_NONE = 0 << 18;
        private const uint ICR_DESTINATION_SHORTHAND_SELF = 1 << 18;
        private const uint ICR_DESTINATION_SHORTHAND_ALL = 2 << 18;
        private const uint ICR_DESTINATION_SHORTHAND_ALL_BUT_SELF = 3 << 18;

        // Estado del controlador
        private static bool _initialized = false;
        private static uint* _localApicAddress = null;
        private static byte _apicId = 0;
        private static byte _apicVersion = 0;

        /// <summary>
        /// Inicializa el controlador APIC
        /// </summary>
        public static bool Initialize()
        {
            if (_initialized)
                return true;

            SerialDebug.Info("Inicializando controlador APIC...");

            // Obtener la dirección del APIC Local desde ACPI
            ulong apicAddr = ACPIManager.GetLocalApicAddress();
  
            if (apicAddr == 0)
            {
                SerialDebug.Info("No se pudo obtener la dirección del APIC Local desde ACPI.");
                return false;
            }

            // Mapear la dirección física a virtual si es necesario (en este caso asumimos mapeo 1:1)
            _localApicAddress = (uint*)apicAddr;

            // Leer ID y versión del APIC
            _apicId = (byte)(ReadAPICRegister(APIC_ID_REGISTER) >> 24);
            _apicVersion = (byte)(ReadAPICRegister(APIC_VERSION_REGISTER) & 0xFF);

            SerialDebug.Info($"APIC Local ID: {_apicId.ToString()}, Versión: {_apicVersion.ToString()}");

            // Configurar el APIC
            ConfigureAPIC();
            InitializeSystemTimer();
            _initialized = true;
            SerialDebug.Info("Controlador APIC inicializado correctamente.");
            return true;
        }

        // In your kernel initialization code, after APIC is initialized
        public static void InitializeSystemTimer()
        {
            SerialDebug.Info("Calibrating APIC timer...");

            // Calibrate the timer to find how many ticks represent 10 milliseconds
            uint ticksPer10ms = CalibrateTimer(10);

            // Calculate ticks per millisecond
            uint ticksPerMs = ticksPer10ms / 10;

            SerialDebug.Info($"APIC Timer calibration: {ticksPerMs} ticks per millisecond");

            // Configure the timer to fire an interrupt (e.g., 0x20) every 10ms
            byte timerInterruptVector = 0x20;
            uint timerInterval = ticksPer10ms;

            // Set up the timer in periodic mode
            ConfigureTimer(timerInterruptVector, timerInterval, true);

            SerialDebug.Info("APIC Timer configured for 10ms intervals");
        }

        /// <summary>
        /// Configura el controlador APIC
        /// </summary>
        private static void ConfigureAPIC()
        {
            // Habilitar el modo APIC (deshabilitar el modo PIC 8259)
            EnableAPICMode();

            // Configurar manejo de interrupciones espurias
            // Bit 8: Habilitar APIC (1)
            // Bits 0-7: Vector para interrupciones espurias (usamos 0xFF)
            WriteAPICRegister(APIC_SPURIOUS_INTERRUPT_VECTOR, 0x100 | 0xFF);

            // Establecer la prioridad de tarea a 0 (más alta)
            WriteAPICRegister(APIC_TASK_PRIORITY, 0);

            // Configurar entradas LVT
            // Deshabilitar el temporizador inicialmente
            WriteAPICRegister(APIC_LVT_TIMER, 0x10000);

            // Configurar LINT0 como ExtINT (modo compatible PIC 8259)
            WriteAPICRegister(APIC_LVT_LINT0, 0x700);

            // Configurar LINT1 como NMI
            WriteAPICRegister(APIC_LVT_LINT1, 0x400 | 0x2);

            // Configurar sensor térmico (enmascarado)
            WriteAPICRegister(APIC_LVT_THERMAL_SENSOR, 0x10000);

            // Configurar contador de rendimiento (enmascarado)
            WriteAPICRegister(APIC_LVT_PERFORMANCE_COUNTER, 0x10000);

            // Configurar manejo de errores
            WriteAPICRegister(APIC_LVT_ERROR, 0xFE);
        }

        /// <summary>
        /// Habilita el modo APIC y deshabilita el PIC 8259
        /// </summary>
        private static void EnableAPICMode()
        {
            // Leer MSR IA32_APIC_BASE (0x1B)
            ulong msr = Native.ReadMSR(0x1B);

            // Habilitar APIC (bit 11)
            msr |= 0x800;

            // Escribir MSR actualizado
            Native.WriteMSR(0x1B, msr);

            // Deshabilitar el PIC 8259 enmascarando todas las interrupciones
            PICController.SetMasterMask(0xFF);
            PICController.SetSlaveMask(0xFF);
        }

        /// <summary>
        /// Lee un registro del controlador APIC Local
        /// </summary>
        /// <param name="reg">Offset del registro (en bytes)</param>
        private static uint ReadAPICRegister(int reg)
        {
            return _localApicAddress[reg / 4];
        }

        /// <summary>
        /// Escribe un valor en un registro del controlador APIC Local
        /// </summary>
        /// <param name="reg">Offset del registro (en bytes)</param>
        /// <param name="value">Valor a escribir</param>
        private static void WriteAPICRegister(int reg, uint value)
        {
            _localApicAddress[reg / 4] = value;
        }

        /// <summary>
        /// Envía un comando EOI (End of Interrupt) al APIC
        /// </summary>
        public static void SendEOI()
        {
            WriteAPICRegister(APIC_EOI, 0);
        }

        /// <summary>
        /// Obtiene el ID del APIC Local
        /// </summary>
        public static byte GetApicId()
        {
            return _apicId;
        }

        /// <summary>
        /// Envía un mensaje de interrupción Inter-Processor (IPI)
        /// </summary>
        /// <param name="destination">ID APIC del procesador destino</param>
        /// <param name="deliveryMode">Modo de entrega (Fixed, NMI, etc.)</param>
        /// <param name="vector">Vector de interrupción (solo para modo Fixed)</param>
        public static void SendIPI(byte destination, uint deliveryMode, byte vector)
        {
            // Esperar a que cualquier IPI anterior termine
            while ((ReadAPICRegister(APIC_INTERRUPT_COMMAND_LOW) & ICR_DELIVERY_STATUS_PENDING) != 0)
            {
                // Espera activa (en un sistema real usaríamos un mecanismo más eficiente)
                Native.Pause();
            }

            // Configurar parte alta del ICR (ID del procesador destino)
            WriteAPICRegister(APIC_INTERRUPT_COMMAND_HIGH, (uint)destination << 24);

            // Configurar parte baja del ICR (modo de entrega, vector, etc.)
            uint command = deliveryMode | vector;
            WriteAPICRegister(APIC_INTERRUPT_COMMAND_LOW, command);
        }

        /// <summary>
        /// Envía un mensaje de inicialización (INIT IPI) a un procesador
        /// </summary>
        /// <param name="destination">ID APIC del procesador destino</param>
        public static void SendInitIPI(byte destination)
        {
            // INIT IPI: Modo de entrega INIT (5), Asserción Nivel, Sin acceso corto
            SendIPI(destination, ICR_DELIVERY_MODE_INIT | ICR_LEVEL_ASSERT, 0);

            // Esperar un poco
            for (int i = 0; i < 10000; i++) Native.Pause();

            // De-asertar INIT
            SendIPI(destination, ICR_DELIVERY_MODE_INIT, 0);
        }

        /// <summary>
        /// Envía un mensaje de inicio (STARTUP IPI) a un procesador
        /// </summary>
        /// <param name="destination">ID APIC del procesador destino</param>
        /// <param name="vector">Vector (dirección de inicio / 4096)</param>
        public static void SendStartupIPI(byte destination, byte vector)
        {
            // STARTUP IPI: Modo de entrega STARTUP (6)
            SendIPI(destination, ICR_DELIVERY_MODE_STARTUP, vector);
        }

        /// <summary>
        /// Configura el temporizador APIC
        /// </summary>
        /// <param name="vector">Vector de interrupción para el temporizador</param>
        /// <param name="initialCount">Cuenta inicial del temporizador</param>
        /// <param name="periodic">true para modo periódico, false para una sola vez</param>
        public static void ConfigureTimer(byte vector, uint initialCount, bool periodic)
        {
            // Configurar divisor del temporizador (divide por 16)
            WriteAPICRegister(APIC_TIMER_DIVIDE_CONFIG, 0x3);

            // Configurar LVT Timer
            uint timerConfig = vector;

            // Modo periódico si se especifica
            if (periodic)
            {
                timerConfig |= (1 << 17); // Bit 17: Modo periódico
            }

            WriteAPICRegister(APIC_LVT_TIMER, timerConfig);

            // Establecer cuenta inicial
            WriteAPICRegister(APIC_TIMER_INITIAL_COUNT, initialCount);
        }

        /// <summary>
        /// Detiene el temporizador APIC
        /// </summary>
        public static void StopTimer()
        {
            // Establecer cuenta inicial a 0
            WriteAPICRegister(APIC_TIMER_INITIAL_COUNT, 0);

            // Enmascarar la interrupción del temporizador
            WriteAPICRegister(APIC_LVT_TIMER, 0x10000);
        }

        /// <summary>
        /// Obtiene el valor actual del contador del temporizador
        /// </summary>
        public static uint GetTimerCurrentCount()
        {
            return ReadAPICRegister(APIC_TIMER_CURRENT_COUNT);
        }

        /// <summary>
        /// Calibra el temporizador APIC usando un retraso conocido
        /// </summary>
        /// <param name="msTarget">Tiempo objetivo en milisegundos</param>
        /// <returns>Valor de cuenta para el tiempo objetivo</returns>
        public static uint CalibrateTimer(uint msTarget)
        {
            // Disable timer while we calibrate
            WriteAPICRegister(APIC_LVT_TIMER, 0x10000);

            // Set divide value
            WriteAPICRegister(APIC_TIMER_DIVIDE_CONFIG, 0x3); // Divide by 16

            // Start with maximum count
            WriteAPICRegister(APIC_TIMER_INITIAL_COUNT, 0xFFFFFFFF);

            // Wait for a fixed amount of time (e.g., using PITController)
            DelayUsingPITController(msTarget);

            // Read current count
            uint remaining = ReadAPICRegister(APIC_TIMER_CURRENT_COUNT);

            // Calculate ticks elapsed
            uint elapsed = 0xFFFFFFFF - remaining;

            // Stop timer
            WriteAPICRegister(APIC_TIMER_INITIAL_COUNT, 0);

            return elapsed;
        }

        private static void DelayUsingPITController(uint ms)
        {
            // In a real implementation, you would use the PITController as a reference timer
            // For now, we'll use a conservative busy-wait
            for (uint i = 0; i < ms * 1000000; i++)
            {
                Native.Pause();
            }
        }

        /// <summary>
        /// Inicializa los procesadores de aplicación (APs)
        /// </summary>
        /// <param name="startupRoutine">Dirección física de la rutina de inicio</param>
        public static void InitializeAPs(ulong startupRoutine)
        {
            if (!_initialized)
                return;

            // La dirección debe estar alineada a 4K (página)
            byte startupVector = (byte)((startupRoutine >> 12) & 0xFF);

            // Obtener información de procesadores del gestor SMP
            int cpuCount = SMPManager.GetProcessorCount();

            for (int i = 1; i < cpuCount; i++) // Empezamos en 1 para omitir el BSP
            {
                byte apicId = SMPManager.GetAPICId(i);

                if (apicId != _apicId) // Asegurar que no enviamos IPIs a nosotros mismos
                {
                    SerialDebug.Info($"Iniciando procesador con APIC ID {apicId.ToString()}...");

                    // Enviar INIT IPI
                    SendInitIPI(apicId);

                    // Pequeño retraso (10 ms)
                    Wait(10);

                    // Enviar STARTUP IPI (intentar dos veces por robustez)
                    SendStartupIPI(apicId, startupVector);

                    // Pequeño retraso (1 ms)
                    Wait(1);

                    SendStartupIPI(apicId, startupVector);

                    // Esperar un poco más para darle tiempo al AP para iniciar
                    Wait(10);
                }
            }
        }

        /// <summary>
        /// Performs a timed wait using the system timer
        /// </summary>
        /// <param name="ms">Milliseconds to wait</param>
        /// <summary>
        /// Performs a timed wait using the best available time source
        /// </summary>
        /// <param name="ms">Milliseconds to wait</param>
        private static void Wait(int ms)
        {
            // If timer subsystem is initialized, use it (most accurate)
            if (Timer.IsInitialized)
            {
                ulong startTick = Timer.GetTickCount();
                ulong endTick = startTick + (ulong)ms;

                while (Timer.GetTickCount() < endTick)
                {
                    // If scheduler is available, yield to other threads
                    if (Scheduler.IsInitialized)
                    {
                        Thread.Yield();
                    }
                    else
                    {
                        // Otherwise, pause to save power
                        Native.Pause();
                    }
                }
            }
            // If ACPI timer is available, use that
            else if (ACPIManager._initialized && ACPIManager.GetPMTimerAddress() != 0)
            {
                // ACPI PM timer is typically a 24/32-bit timer running at 3.579545 MHz
                ulong start = ReadPMTimer();
                ulong end = start + ((ulong)ms * 3580) / 1000; // Convert ms to timer ticks

                while (ReadPMTimer() < end)
                {
                    Native.Pause();
                }
            }
            // If PITController (Programmable Interval Timer) is initialized, use that
            else if (PITController.IsInitialized)
            {
                ulong currentTick = PITController.Ticks;
                ulong targetTick = currentTick + ((ulong)ms * PITController.TicksPerMS);

                while (PITController.Ticks < targetTick)
                {
                    Native.Pause();
                }
            }
            // Last resort: use busy waiting with CPU pauses
            else
            {
                // Very crude approximation - depends heavily on CPU speed
                // This should be calibrated if possible
                for (int i = 0; i < ms * 1000000; i++)
                {
                    Native.Pause();
                }
            }
        }

        // Helper method to read the ACPI PM Timer
        private static ulong ReadPMTimer()
        {
            // Read from the PM Timer register
            return (ulong)Native.InDWord((ushort)ACPIManager.GetPMTimerAddress());
        }
    }
}