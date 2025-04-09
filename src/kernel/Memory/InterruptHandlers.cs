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
    public unsafe delegate void InterruptDelegate();

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
    }


    /// <summary>
    /// Basic system interrupt manager, APIC version
    /// </summary>
    public static unsafe class InterruptManager
    {  // Constants
        private const int MAX_INTERRUPTS = 256;
        private const int MAX_IRQ_HANDLERS = 24;
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
            _irqHandlers = new InterruptDelegate[MAX_IRQ_HANDLERS]; // APIC típicamente soporta 24 IRQs
            SerialDebug.Info("Allocating memory for handler table...");
            // Allocate memory for the handler table (256 possible interrupts)
            _handlerTable = (IntPtr*)Allocator.malloc((nuint)(sizeof(IntPtr) * MAX_INTERRUPTS));

            // Initialize all pointers to zero
            for (int i = 0; i < MAX_INTERRUPTS; i++)
            {
                _handlerTable[i] = IntPtr.Zero;
            }

            // Initialize IRQ handlers
            for (int i = 0; i < MAX_IRQ_HANDLERS; i++)
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
            for (int i = 0; i < MAX_IRQ_HANDLERS; i++)
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
            IOPort.Out8(0x21, 0xFF);  // PIC maestro
            IOPort.Out8(0xA1, 0xFF);  // PIC esclavo

            SerialDebug.Info("Legacy PIC disabled successfully");
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
            if (irqNumber >= 0 && irqNumber < MAX_IRQ_HANDLERS)
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
            SerialDebug.Info($"Attempting to register IRQ handler:");
            SerialDebug.Info($"IRQ Number: {irq}");
            SerialDebug.Info($"CPU ID: {cpuId}");

            // Verificar que IOAPIC esté inicializado
            if (!IOAPIC.IsInitialized())
            {
                SerialDebug.Error("IOAPIC not initialized before registering IRQ handler");
                return;
            }

            // Verificar que IDT esté configurada
            if (!IDTManager.IsInitialized())
            {
                SerialDebug.Error("IDT not initialized before registering IRQ handler");
                return;
            }

            SerialDebug.Info($"Registering handler for IRQ {irq} on CPU {cpuId}");
            if (irq < 24)
            {
                _irqHandlers[irq] = handler;
                SerialDebug.Info($"Registered handler for IRQ {irq}");

                byte vector = (byte)(irq + 32);

                // Configuración específica según el tipo de dispositivo
                bool activeLow = true;      // La mayoría de los dispositivos en ISA/PS/2
                bool levelTriggered = true; // La mayoría de los dispositivos en ISA/PS/2

                // Configuraciones especiales según el IRQ
                if (irq == 1) // IRQ 1 = Teclado PS/2
                {
                    SerialDebug.Info("Configuring PS/2 keyboard IRQ with ActiveLow and LevelTriggered");
                    activeLow = true;
                    levelTriggered = true;
                }
                else if (irq >= 16) // Típicamente dispositivos PCI usan polaridad activa en alto y disparo por flanco
                {
                    activeLow = false;
                    levelTriggered = false;
                }

                // Configurar el IOAPIC con los parámetros específicos
                IOAPIC.ConfigureIRQ(irq, cpuId, vector, false, activeLow, levelTriggered);
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
            //  SerialDebug.Info($"IDT Base Address: 0x{((ulong)IDTManager._idt).ToStringHex()}");
            // SerialDebug.Info($"IDT Limit: {((ulong)IDTManager._idtPointer.Limit).ToStringHex()}");

            // Verificar manejadores registrados
            int validHandlers = 0;

            for (int i = 0; i < 256; i++)
            {
                if (_handlerTable[i] != IntPtr.Zero)
                {
                    validHandlers++;

                    // Log de dirección de manejadores
                    //  SerialDebug.Info($"Handler for Interrupt {i}: 0x{((ulong)_handlerTable[i]).ToStringHex()}");
                }
            }
            SerialDebug.Info($"Total Valid Interrupt Handlers: {validHandlers}");
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
            SerialDebug.Info("Handling interrupt...");

            //SerialDebug.Info($"Handling interrupt: {frame->InterruptNumber.ToStringHex()}");
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
                //Console.WriteLine($"Unhandled interrupt: 0x{frame->InterruptNumber.ToStringHex()} at address 0x{frame->RIP.ToStringHex()}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        /// <summary>
        /// Handler for divide by zero (INT 0)
        /// </summary>
        public static void DivideByZeroHandler(InterruptFrame* frame)
        {
            KernelPanic($"Divide by zero at address 0x{frame->RIP.ToStringHex()}",
               "InterruptManager.cs", 0, "DivideByZeroHandler");
        }

        /// <summary>
        /// Handler for invalid opcode (INT 6)
        /// </summary>
        public static void InvalidOpcodeHandler(InterruptFrame* frame)
        {
            KernelPanic($"Invalid opcode at address 0x{frame->RIP.ToStringHex()}",
                 "InterruptManager.cs", 0, "InvalidOpcodeHandler");
        }

        /// <summary>
        /// Handler for double fault (INT 8)
        /// </summary>
        public static void DoubleFaultHandler(InterruptFrame* frame)
        {
            KernelPanic("Double fault - System integrity compromised",
                "InterruptManager.cs", 0, "DoubleFaultHandler");
        }

        /// <summary>
        /// Handler for general protection fault (INT 13)
        /// </summary>
        public static void GeneralProtectionHandler(InterruptFrame* frame)
        {
            KernelPanic($"General protection fault at address 0x{frame->RIP.ToStringHex()} - Error code: 0x{frame->ErrorCode.ToStringHex()}",
              "InterruptManager.cs", 0, "GeneralProtectionHandler");
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

            string faultType = present ? "Protection violation" : "Page not present";
            string operation = write ? "Write" : "Read";
            string context = user ? "User" : "Kernel";

            KernelPanic($"Page fault at address 0x{faultAddress.ToStringHex()} - Type: {faultType}, Operation: {operation}, Context: {context}",
                        "InterruptManager.cs", 0, "PageFaultHandler");
        }

        /// <summary>
        /// Muestra un mensaje de kernel panic y detiene el sistema
        /// </summary>
        /// <param name="message">Mensaje de error</param>
        /// <param name="file">Archivo donde ocurrió el error (opcional)</param>
        /// <param name="line">Línea donde ocurrió el error (opcional)</param>
        /// <param name="function">Función donde ocurrió el error (opcional)</param>
        public static void KernelPanic(string message, string file = null, int line = 0, string function = null)
        {
            // Deshabilitar interrupciones para evitar interferencias
            DisableInterrupts();

            // Guardar el color original
            ConsoleColor oldFg = Console.ForegroundColor;
            ConsoleColor oldBg = Console.BackgroundColor;

            // Cambiar a colores de error (fondo rojo, texto blanco)
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;

            // Limpiar pantalla para que el mensaje sea muy visible
            Console.Clear();

            // Mostrar encabezado de kernel panic
            Console.WriteLine("================ KERNEL PANIC ================");
            Console.WriteLine();

            // Mostrar mensaje principal
            Console.WriteLine($"ERROR: {message}");
            Console.WriteLine();

            // Mostrar información de ubicación si está disponible
            if (!string.IsNullOrEmpty(file))
            {
                Console.WriteLine($"Location: {file}");
                if (line > 0)
                    Console.WriteLine($"Line: {line}");
                if (!string.IsNullOrEmpty(function))
                    Console.WriteLine($"Function: {function}");
                Console.WriteLine();
            }

            // Si tenemos un frame de interrupción, mostrar información relevante
            Console.WriteLine("Technical information:");
            Console.WriteLine("System halted due to critical error");

            // Hacer log al puerto serie también
            SerialDebug.Error("KERNEL PANIC: " + message);

            // Detener el sistema
            while (true)
            {
                // Pausar la CPU para reducir el consumo de energía
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