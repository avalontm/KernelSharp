using Kernel.Diagnostics;
using Kernel.Drivers.IO;
using Kernel.Hardware;
using Kernel.Memory;
using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Kernel
{
    /// <summary>
    /// Delegate para manejadores de interrupciones de hardware (IRQ)
    /// </summary>
    public delegate void InterruptDelegate();

    /// <summary>
    /// Structure representing CPU state during an interrupt
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InterruptFrame
    {
        public ulong RDI;
        public ulong RSI;
        public ulong RBP;
        public ulong RSP;
        public ulong RBX;
        public ulong RDX;
        public ulong RCX;
        public ulong RAX;

        // Información de la interrupción
        public ulong InterruptNumber;
        public ulong ErrorCode;

        // Registros automáticamente guardados por la CPU
        public ulong RIP;
        public ulong CS;
        public ulong RFLAGS;
        public ulong UserRSP;
        public ulong SS;

        // Agregar el ScanCode (si lo necesitas para el teclado)
        public byte ScanCode;
    }


    /// <summary>
    /// Basic system interrupt manager, APIC version
    /// </summary>
    public static unsafe class InterruptManager
    {
        // Table of pointers to interrupt handlers
        private static IntPtr* _handlerTable;
        internal static bool _initialized;

        // Table de manejadores de IRQ para hardware
        private static InterruptDelegate[] _irqHandlers;

        // Base vector para interrupciones de IRQ
        private const byte IRQ_BASE_VECTOR = 32;

        // Número del procesador al que enviar las interrupciones por defecto (típicamente 0)
        private const byte DEFAULT_CPU_ID = 0;

        /// <summary>
        /// Inicializa el gestor de interrupciones
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            SerialDebug.Info("Initializing interrupt manager (APIC mode)...");
            _irqHandlers = new InterruptDelegate[24]; // APIC típicamente soporta 24 IRQs

            // Allocate memory for the handler table (256 possible interrupts)
            _handlerTable = (IntPtr*)Allocator.malloc((nuint)(sizeof(IntPtr) * 256));

            // Initialize all pointers to zero
            for (int i = 0; i < 256; i++)
            {
                _handlerTable[i] = IntPtr.Zero;
            }

            // Initialize IRQ handlers
            for (int i = 0; i < 24; i++)
            {
                _irqHandlers[i] = null;
            }

            // Register handlers for important exceptions
            RegisterHandler(0, &DivideByZeroHandler);       // Divide by zero
            RegisterHandler(6, &InvalidOpcodeHandler);      // Invalid opcode
            RegisterHandler(8, &DoubleFaultHandler);        // Double fault
            RegisterHandler(13, &GeneralProtectionHandler); // General protection
            RegisterHandler(14, &PageFaultHandler);         // Page fault

            // Registrar manejadores para las IRQs básicas (32-55 en APIC típico)
            for (int i = 0; i < 24; i++)
            {
                RegisterHandler(IRQ_BASE_VECTOR + i, &IRQHandler);
            }

            // Deshabilitar el PIC clásico cuando usamos APIC
            DisableLegacyPIC();

            _initialized = true;
            // Report that the manager is initialized
            SerialDebug.Info("Interrupt manager initialized successfully (APIC mode)");
        }

        /// <summary>
        /// Deshabilita el PIC clásico (8259) cuando se usa exclusivamente APIC
        /// </summary>
        private static void DisableLegacyPIC()
        {
            SerialDebug.Info("Disabling legacy PIC (8259)...");

            // Enmascarar todas las IRQs en ambos PICs
            IOPort.OutByte(0x21, 0xFF);  // PIC maestro
            IOPort.OutByte(0xA1, 0xFF);  // PIC esclavo

            SerialDebug.Info("Legacy PIC disabled successfully");
        }

        public static void DiagnoseInterruptSystem()
        {
            SerialDebug.Info("Starting Interrupt System Diagnosis (APIC mode)");

            // Verificar inicialización
            if (!_initialized)
            {
                SerialDebug.Warning("Interrupt manager not initialized");
                return;
            }

            // Verificar tabla de manejadores
            int validHandlers = 0;
            for (int i = 0; i < 256; i++)
            {
                if (_handlerTable[i] != IntPtr.Zero)
                {
                    validHandlers++;
                }
            }

            SerialDebug.Info($"Total valid interrupt handlers: {validHandlers}");

            // Verificar manejadores de IRQ
            int activeIRQHandlers = 0;
            for (int i = 0; i < 24; i++)
            {
                if (_irqHandlers[i] != null)
                {
                    activeIRQHandlers++;
                    SerialDebug.Info($"Active handler for IRQ {i}");
                }
            }

            SerialDebug.Info($"Active IRQ handlers: {activeIRQHandlers}");

            // Obtener información del APIC
            DiagnoseAPICConfiguration();

            // Prueba de habilitación/deshabilitación de interrupciones
            SerialDebug.Info("Testing interrupt enable/disable");

            DisableInterrupts();
            SerialDebug.Info("Interrupts disabled");

            EnableInterrupts();
            SerialDebug.Info("Interrupts enabled");
        }

        /// <summary>
        /// Diagnóstico específico para la configuración APIC
        /// </summary>
        private static void DiagnoseAPICConfiguration()
        {
            SerialDebug.Info("APIC Configuration Diagnosis");

            // Obtener identificación del procesador local
            uint apicId = ACPIManager.GetLocalAPICId();
            SerialDebug.Info($"Local APIC ID: {apicId}");

            // Obtener y mostrar estado del APIC
            ulong apicBase = ACPIManager.GetLocalAPICBase();
            SerialDebug.Info($"Local APIC Base: 0x{apicBase.ToStringHex()}");

            // Verificar si el APIC local está habilitado
            bool apicEnabled = ACPIManager.IsLocalAPICEnabled();
            SerialDebug.Info($"Local APIC Enabled: " + apicEnabled);

            // Obtener información del IOAPIC
            uint ioApicId = IOAPIC.GetIOAPICId();
            SerialDebug.Info($"IO APIC ID: {ioApicId}");

            // Verificar entradas de redirección para IRQs importantes
            SerialDebug.Info("Checking IRQ redirection entries:");
            SerialDebug.Info($"Timer (IRQ 0): 0x{IOAPIC.ReadRedirectionEntry(0).ToStringHex()}");
            SerialDebug.Info($"Keyboard (IRQ 1): 0x{IOAPIC.ReadRedirectionEntry(1).ToStringHex()}");
        }

        /// <summary>
        /// Función genérica para manejar las IRQs de hardware
        /// </summary>
        /// <param name="frame">Frame con información de la interrupción</param>
        public static void IRQHandler(InterruptFrame* frame)
        {
            // Calcular el número de IRQ (0-23) a partir del número de interrupción (32-55)
            int irqNumber = (int)(frame->InterruptNumber - IRQ_BASE_VECTOR);

            // Verificar que sea un IRQ válido
            if (irqNumber >= 0 && irqNumber < 24)
            {
                // Llamar al manejador específico si existe
                if (_irqHandlers[irqNumber] != null)
                {
                    _irqHandlers[irqNumber]();
                }

                // Enviar End-Of-Interrupt al APIC
                APICController.SendEOI();
            }
        }

        /// <summary>
        /// Registra un manejador para una IRQ específica (0-23)
        /// </summary>
        /// <param name="irq">Número de IRQ (0-23)</param>
        /// <param name="handler">Delegado que maneja la IRQ</param>
        /// <param name="cpuId">ID del procesador al que dirigir la interrupción (opcional)</param>
        public static void RegisterIRQHandler(byte irq, InterruptDelegate handler, byte cpuId = DEFAULT_CPU_ID)
        {
            if (irq < 24)
            {
                _irqHandlers[irq] = handler;
                SerialDebug.Info($"Registered handler for IRQ {irq}");

                // Configurar el IOAPIC para dirigir esta IRQ al vector y procesador correctos
                IOAPIC.SetIRQRedirect(irq, cpuId);
            }
            else
            {
                SerialDebug.Warning($"Invalid IRQ number: {irq}. Must be between 0 and 23.");
            }
        }

        /// <summary>
        /// Habilita una IRQ específica en el I/O APIC.
        /// </summary>
        /// <param name="irq">Número de IRQ</param>
        public static void EnableIRQ(byte irq)
        {
            // Deshabilitar interrupciones globalmente durante la configuración
            DisableInterrupts();

            SerialDebug.Info($"Enabling IRQ {irq} in IOAPIC");

            // Verificar si la IRQ es válida en APIC
            if (irq >= 24)
            {
                SerialDebug.Warning($"Invalid IRQ number for APIC: {irq}");
                EnableInterrupts();
                return;
            }

            // Leer el registro actual del I/O APIC para la IRQ
            ulong currentEntry = IOAPIC.ReadRedirectionEntry(irq);

            // Habilitar la IRQ en el APIC (desmascararla)
            ulong newEntry = currentEntry & ~(1UL << 16); // Bit 16 = Mask (0 para habilitar)

            // Escribir la nueva configuración en el I/O APIC
            IOAPIC.WriteRedirectionEntry(irq, newEntry);

            SerialDebug.Info($"IRQ {irq} enabled successfully in IOAPIC");

            // Volver a habilitar las interrupciones globalmente
            EnableInterrupts();
        }

        /// <summary>
        /// Deshabilita una IRQ específica en el I/O APIC.
        /// </summary>
        /// <param name="irq">Número de IRQ</param>
        public static void DisableIRQ(byte irq)
        {
            // Deshabilitar interrupciones globalmente durante la configuración
            DisableInterrupts();

            SerialDebug.Info($"Disabling IRQ {irq} in IOAPIC");

            // Verificar si la IRQ es válida en APIC
            if (irq >= 24)
            {
                SerialDebug.Warning($"Invalid IRQ number for APIC: {irq}");
                EnableInterrupts();
                return;
            }

            // Leer el registro actual del I/O APIC para la IRQ
            ulong currentEntry = IOAPIC.ReadRedirectionEntry(irq);

            // Deshabilitar la IRQ en el APIC (mascararla)
            ulong newEntry = currentEntry | (1UL << 16); // Bit 16 = Mask (1 para deshabilitar)

            // Escribir la nueva configuración en el I/O APIC
            IOAPIC.WriteRedirectionEntry(irq, newEntry);

            SerialDebug.Info($"IRQ {irq} disabled successfully in IOAPIC");

            // Volver a habilitar las interrupciones globalmente
            EnableInterrupts();
        }

        /// <summary>
        /// Verifica si una IRQ específica está habilitada en el IOAPIC
        /// </summary>
        private static bool IsIRQEnabled(byte irq)
        {
            if (irq >= 24)
                return false;

            // Leer la entrada de redirección
            ulong entry = IOAPIC.ReadRedirectionEntry(irq);

            // Comprobar el bit de máscara (bit 16)
            // Si es 0, la IRQ está habilitada; si es 1, está deshabilitada
            return (entry & (1UL << 16)) == 0;
        }

        public static void DiagnoseInterruptInitialization()
        {
            SerialDebug.Info("Comprehensive Interrupt System Diagnosis (APIC mode)");

            // Verificar configuración de IDT
            SerialDebug.Info($"IDT Base Address: 0x{((ulong)IDTManager._idt).ToStringHex()}");
            SerialDebug.Info($"IDT Limit: {((ulong)IDTManager._idtPointer.Limit).ToStringHex()}");

            // Verificar manejadores registrados
            int validHandlers = 0;
            for (int i = 0; i < 256; i++)
            {
                if (_handlerTable[i] != IntPtr.Zero)
                {
                    validHandlers++;

                    // Log de dirección de manejadores
                    SerialDebug.Info($"Handler for Interrupt {i}: 0x{((ulong)_handlerTable[i]).ToStringHex()}");
                }
            }
            SerialDebug.Info($"Total Valid Interrupt Handlers: {validHandlers}");

            // Verificar configuración del APIC
            SerialDebug.Info("APIC Configuration:");

            // Verificar el estado de habilitación de IRQs importantes
            SerialDebug.Info($"Timer IRQ (0) enabled: " + IsIRQEnabled(0));
            SerialDebug.Info($"Keyboard IRQ (1) enabled: " + IsIRQEnabled(1));
            SerialDebug.Info($"Mouse IRQ (12) enabled: " + IsIRQEnabled(12));
        }

        /// <summary>
        /// Registra un manejador para una interrupción específica
        /// </summary>
        /// <param name="interruptNumber">Número de interrupción</param>
        /// <param name="handler">Puntero a la función manejadora</param>
        private static void RegisterHandler(int interruptNumber, delegate*<InterruptFrame*, void> handler)
        {
            if (interruptNumber >= 0 && interruptNumber < 256)
            {
                _handlerTable[interruptNumber] = (IntPtr)handler;

                // Call the external function that sets the handler in the IDT
                WriteInterruptHandler(interruptNumber, (IntPtr)handler);
            }
        }

        /// <summary>
        /// Main entry point for all interrupts
        /// This function is called from assembly code
        /// </summary>
        [RuntimeExport("HandleInterrupt")]
        public static void HandleInterrupt(InterruptFrame* frame)
        {
            SerialDebug.Info($"Handling interrupt: {frame->InterruptNumber.ToStringHex()}");
            int interruptNumber = (int)frame->InterruptNumber;

            // Handle the interrupt based on its number
            if (interruptNumber >= 0 && interruptNumber < 256)
            {
                // Check if there is a registered handler
                IntPtr handlerPtr = _handlerTable[interruptNumber];

                if (handlerPtr != IntPtr.Zero)
                {
                    // Call the specific handler
                    ((delegate*<InterruptFrame*, void>)handlerPtr.ToPointer())(frame);
                }
                else
                {
                    // Use the default handler
                    DefaultHandler(frame);
                }
            }
        }

        /// <summary>
        /// External function to register a handler in the interrupt descriptor table
        /// </summary>
        [RuntimeExport("_WriteInterruptHandler")]
        public static void WriteInterruptHandler(int index, IntPtr handler)
        {
            if (index >= 0 && index < 256)
            {
                _handlerTable[index] = handler;
            }
        }

        /// <summary>
        /// Default handler for interrupts without a specific handler
        /// </summary>
        public static void DefaultHandler(InterruptFrame* frame)
        {
            // Only display on console if it's an unexpected interrupt (not normal hardware)
            if (frame->InterruptNumber < 32)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Unhandled interrupt: 0x{frame->InterruptNumber.ToStringHex()} at address 0x{frame->RIP.ToStringHex()}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        /// <summary>
        /// Handler for divide by zero (INT 0)
        /// </summary>
        public static void DivideByZeroHandler(InterruptFrame* frame)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: Divide by zero at address 0x{frame->RIP.ToStringHex()}");
            Console.ForegroundColor = ConsoleColor.White;
            HaltSystem();
        }

        /// <summary>
        /// Handler for invalid opcode (INT 6)
        /// </summary>
        public static void InvalidOpcodeHandler(InterruptFrame* frame)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: Invalid opcode at address 0x{frame->RIP.ToStringHex()}");
            Console.ForegroundColor = ConsoleColor.White;
            HaltSystem();
        }

        /// <summary>
        /// Handler for double fault (INT 8)
        /// </summary>
        public static void DoubleFaultHandler(InterruptFrame* frame)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("CRITICAL ERROR: Double fault. System will halt.");
            Console.ForegroundColor = ConsoleColor.White;
            HaltSystem();
        }

        /// <summary>
        /// Handler for general protection fault (INT 13)
        /// </summary>
        public static void GeneralProtectionHandler(InterruptFrame* frame)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: General protection fault at address 0x{frame->RIP.ToStringHex()}");
            Console.WriteLine($"Error code: 0x{frame->ErrorCode.ToStringHex()}");
            Console.ForegroundColor = ConsoleColor.White;
            HaltSystem();
        }

        /// <summary>
        /// Handler for page fault (INT 14)
        /// </summary>
        public static void PageFaultHandler(InterruptFrame* frame)
        {
            // Read the CR2 register that contains the address that caused the fault
            ulong faultAddress = Native.ReadCR2();

            // Error code analysis
            bool present = (frame->ErrorCode & 1) != 0;      // Page present
            bool write = (frame->ErrorCode & 2) != 0;        // Write operation
            bool user = (frame->ErrorCode & 4) != 0;         // User mode
            bool reserved = (frame->ErrorCode & 8) != 0;     // Reserved bits
            bool instruction = (frame->ErrorCode & 16) != 0; // Instruction fetch

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: Page fault at address 0x{faultAddress.ToStringHex()}");
            Console.WriteLine($"Type: {(present ? "Protection violation" : "Page not present")}");
            Console.WriteLine($"Operation: {(write ? "Write" : "Read")}");
            Console.WriteLine($"Context: {(user ? "User" : "Kernel")}");
            if (reserved) Console.WriteLine("Reserved bits error");
            if (instruction) Console.WriteLine("Caused by instruction fetch");
            Console.ForegroundColor = ConsoleColor.White;

            HaltSystem();
        }

        /// <summary>
        /// Halts the system after a critical exception
        /// </summary>
        private static void HaltSystem()
        {
            // Disable interrupts
            Native.CLI();

            Console.WriteLine("System halted");

            // Infinite loop
            while (true)
            {
                // Pause the CPU to save power
                Native.Halt();
            }
        }

        /// <summary>
        /// Habilita todas las interrupciones (STI)
        /// </summary>
        public static void EnableInterrupts()
        {
            Native.STI();
        }

        /// <summary>
        /// Deshabilita todas las interrupciones (CLI)
        /// </summary>
        public static void DisableInterrupts()
        {
            Native.CLI();
        }
    }
}