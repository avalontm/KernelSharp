using Kernel.Diagnostics;
using Kernel.Drivers.IO;
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
        // Registers saved by the interrupt routine
        public ulong RDI;
        public ulong RSI;
        public ulong RBP;
        public ulong RSP;
        public ulong RBX;
        public ulong RDX;
        public ulong RCX;
        public ulong RAX;

        // Interrupt information
        public ulong InterruptNumber;
        public ulong ErrorCode;

        // Registers automatically saved by the CPU
        public ulong RIP;
        public ulong CS;
        public ulong RFLAGS;
        public ulong UserRSP;  // Only on privilege changes
        public ulong SS;       // Only on privilege changes
    }

    /// <summary>
    /// Basic system interrupt manager
    /// </summary>
    public static unsafe class InterruptManager
    {
        // Table of pointers to interrupt handlers
        private static IntPtr* _handlerTable;
        internal static bool _initialized;

        // Table de manejadores de IRQ para hardware
        private static InterruptDelegate[] _irqHandlers;

        // Puertos para comunicación con el PIC
        private const ushort PIC1_COMMAND = 0x20;
        private const ushort PIC1_DATA = 0x21;
        private const ushort PIC2_COMMAND = 0xA0;
        private const ushort PIC2_DATA = 0xA1;

        // Comandos del PIC
        private const byte PIC_EOI = 0x20;

        /// <summary>
        /// Inicializa el gestor de interrupciones
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            SerialDebug.Info("Initializing interrupt manager...");
            _irqHandlers = new InterruptDelegate[16];
            // Allocate memory for the handler table (256 possible interrupts)
            _handlerTable = (IntPtr*)Allocator.malloc((nuint)(sizeof(IntPtr) * 256));

            // Initialize all pointers to zero
            for (int i = 0; i < 256; i++)
            {
                _handlerTable[i] = IntPtr.Zero;
            }

            // Initialize IRQ handlers
            for (int i = 0; i < 16; i++)
            {
                _irqHandlers[i] = null;
            }

            // Register handlers for important exceptions
            RegisterHandler(0, &DivideByZeroHandler);      // Divide by zero
            RegisterHandler(6, &InvalidOpcodeHandler);     // Invalid opcode
            RegisterHandler(8, &DoubleFaultHandler);       // Double fault
            RegisterHandler(13, &GeneralProtectionHandler); // General protection
            RegisterHandler(14, &PageFaultHandler);        // Page fault

            // Registrar manejadores para las IRQs básicas (32-47 en Intel x86)
            for (int i = 0; i < 16; i++)
            {
                RegisterHandler(32 + i, &IRQHandler);
            }

            _initialized = true;
            // Report that the manager is initialized
            SerialDebug.Info("Interrupt manager initialized successfully");
        }

        public static void DiagnoseInterruptSystem()
        {
            SerialDebug.Info("Starting Interrupt System Diagnosis");

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
            for (int i = 0; i < 16; i++)
            {
                if (_irqHandlers[i] != null)
                {
                    activeIRQHandlers++;
                }
            }

            SerialDebug.Info($"Active IRQ handlers: {activeIRQHandlers}");

            // Prueba de habilitación/deshabilitación de interrupciones
            SerialDebug.Info("Testing interrupt enable/disable");

            DisableInterrupts();
            SerialDebug.Info("Interrupts disabled");

            EnableInterrupts();
            SerialDebug.Info("Interrupts enabled");
        }

        /// <summary>
        /// Función genérica para manejar las IRQs de hardware
        /// </summary>
        /// <param name="frame">Frame con información de la interrupción</param>
        public static void IRQHandler(InterruptFrame* frame)
        {
            // Calcular el número de IRQ (0-15) a partir del número de interrupción (32-47)
            int irqNumber = (int)(frame->InterruptNumber - 32);

            // Verificar que sea un IRQ válido
            if (irqNumber >= 0 && irqNumber < 16)
            {
                // Llamar al manejador específico si existe
                if (_irqHandlers[irqNumber] != null)
                {
                    _irqHandlers[irqNumber]();
                }

                // Enviar End-Of-Interrupt al PIC
                SendEndOfInterrupt((byte)irqNumber);
            }
        }

        /// <summary>
        /// Registra un manejador para una IRQ específica (0-15)
        /// </summary>
        /// <param name="irq">Número de IRQ (0-15)</param>
        /// <param name="handler">Delegado que maneja la IRQ</param>
        public static void RegisterIRQHandler(byte irq, InterruptDelegate handler)
        {
            if (irq < 16)
            {
                _irqHandlers[irq] = handler;
                SerialDebug.Info($"Registered handler for IRQ {irq}");
            }
        }

        /// <summary>
        /// Habilita una IRQ específica en el PIC
        /// </summary>
        /// <param name="irq">Número de IRQ (0-15)</param>
        public static void EnableIRQ(byte irq)
        {
            // Deshabilitar interrupciones globalmente
            DisableInterrupts();

            SerialDebug.Info($"Attempting to enable IRQ {irq}");

            if (irq < 16)
            {
                if (irq < 8)
                {
                    // IRQs 0-7 en PIC maestro
                    byte mask = IOPort.InByte(PIC1_DATA);
                    SerialDebug.Info($"Master PIC Initial Mask: 0x{((ulong)mask).ToStringHex()}");

                    // Limpiar el bit correspondiente para habilitar
                    mask &= (byte)~(1 << irq);

                    SerialDebug.Info($"Calculated Mask: 0x{((ulong)mask).ToStringHex()}");

                    // IMPORTANTE: Escribir la máscara explícitamente
                    IOPort.OutByte(PIC1_DATA, mask);

                    // Verificar la máscara después de escribirla
                    byte verifyMask = IOPort.InByte(PIC1_DATA);
                    SerialDebug.Info($"Verified Master PIC Mask: 0x{((ulong)verifyMask).ToStringHex()}");

                    // Verificación adicional
                    if (verifyMask != mask)
                    {
                        SerialDebug.Warning($"Mask write failed. Expected 0x{((ulong)mask).ToStringHex()}, got 0x{((ulong)verifyMask).ToStringHex()}");
                    }
                }
                else
                {
                    // Código para IRQs 8-15 (similar)
                    byte slaveMask = IOPort.InByte(PIC2_DATA);
                    slaveMask &= (byte)~(1 << (irq - 8));
                    IOPort.OutByte(PIC2_DATA, slaveMask);

                    // Habilitar línea de cascada en PIC maestro
                    byte masterMask = IOPort.InByte(PIC1_DATA);
                    masterMask &= unchecked((byte)~(1 << 2));
                    IOPort.OutByte(PIC1_DATA, masterMask);

                    SerialDebug.Info($"Slave PIC Mask: 0x{((ulong)slaveMask).ToStringHex()}");
                    SerialDebug.Info($"Master PIC Cascade Mask: 0x{((ulong)masterMask).ToStringHex()}");
                }
            }
            else
            {
                SerialDebug.Warning($"Invalid IRQ number: {irq}");
            }


            EnableInterrupts();

            DiagnoseInterruptInitialization();

            SerialDebug.Info($"IRQ {irq} enabled successfully");
        }

        public static void DiagnoseInterruptInitialization()
        {
            SerialDebug.Info("Comprehensive Interrupt System Diagnosis");

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

            // Verificar configuración de PIC
            byte masterMask = IOPort.InByte(PIC1_DATA);
            byte slaveMask = IOPort.InByte(PIC2_DATA);

            SerialDebug.Info($"Master PIC Mask: 0x{((ulong)masterMask).ToStringHex()}");
            SerialDebug.Info($"Slave PIC Mask: 0x{((ulong)slaveMask).ToStringHex()}");
        }

        /// <summary>
        /// Verifica si una IRQ específica está habilitada
        /// </summary>
        private static bool IsIRQEnabled(byte irq)
        {
            if (irq < 8)
            {
                byte mask = IOPort.InByte(PIC1_DATA);
                return (mask & (1 << irq)) == 0;
            }
            else if (irq < 16)
            {
                byte slaveMask = IOPort.InByte(PIC2_DATA);
                byte masterMask = IOPort.InByte(PIC1_DATA);

                // Verificar máscara del PIC esclavo y la línea de cascada del maestro
                return (slaveMask & (1 << (irq - 8))) == 0 &&
                       (masterMask & (1 << 2)) == 0;
            }

            return false;
        }

        /// <summary>
        /// Deshabilita una IRQ específica en el PIC
        /// </summary>
        /// <param name="irq">Número de IRQ (0-15)</param>
        public static void DisableIRQ(byte irq)
        {
            if (irq < 16)
            {
                if (irq < 8)
                {
                    // IRQs 0-7 están en el PIC maestro
                    byte mask = IOPort.InByte(PIC1_DATA);
                    mask |= (byte)(1 << irq);
                    IOPort.OutByte(PIC1_DATA, mask);
                }
                else
                {
                    // IRQs 8-15 están en el PIC esclavo
                    byte mask = IOPort.InByte(PIC2_DATA);
                    mask |= (byte)(1 << (irq - 8));
                    IOPort.OutByte(PIC2_DATA, mask);
                }

                SerialDebug.Info($"Disabled IRQ {irq}");
            }
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
        /// Sends an end of interrupt command to the interrupt controller
        /// </summary>
        /// <param name="irq">IRQ number</param>
        public static void SendEndOfInterrupt(byte irq)
        {
            // For IRQs 8-15, send EOI also to the slave PIC
            if (irq >= 8)
            {
                // Send EOI to slave PIC (port 0xA0)
                IOPort.OutByte(PIC2_COMMAND, PIC_EOI);
            }

            // Send EOI to master PIC (port 0x20)
            IOPort.OutByte(PIC1_COMMAND, PIC_EOI);
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