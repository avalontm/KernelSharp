using Kernel.Diagnostics;
using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Kernel.Memory
{
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
        public static IntPtr* _handlerTable;
        internal static bool _initialized;

        /// <summary>
        /// Initializes the interrupt manager
        /// </summary>
        public static void Initialize()
        {
            if(_initialized)
            {
                return;
            }

            SerialDebug.Info("Initializing interrupt manager...");

            // Allocate memory for the handler table (256 possible interrupts)
            _handlerTable = (IntPtr*)Allocator.malloc((nuint)(sizeof(IntPtr) * 256));

            // Initialize all pointers to zero
            for (int i = 0; i < 256; i++)
            {
                _handlerTable[i] = IntPtr.Zero;

            }

            // Register handlers for important exceptions
            RegisterHandler(0, &DivideByZeroHandler);      // Divide by zero
            RegisterHandler(6, &InvalidOpcodeHandler);     // Invalid opcode
            RegisterHandler(8, &DoubleFaultHandler);       // Double fault
            RegisterHandler(13, &GeneralProtectionHandler); // General protection
            RegisterHandler(14, &PageFaultHandler);        // Page fault

            _initialized = true;
            // Report that the manager is initialized
            SerialDebug.Info("Interrupt manager initialized successfully");
        }

        /// <summary>
        /// Registers a handler for a specific interrupt number
        /// </summary>
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
                Native.OutByte(0xA0, 0x20);
            }

            // Send EOI to master PIC (port 0x20)
            Native.OutByte(0x20, 0x20);
        }
    }
}