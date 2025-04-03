using Internal.Runtime.CompilerHelpers;
using Kernel.Diagnostics;
using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Kernel.Memory
{
    /// <summary>
    /// Estructura que representa el estado de la CPU durante una interrupción
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InterruptFrame
    {
        // Registros guardados por la rutina de interrupción
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

        // Registros guardados automáticamente por la CPU
        public ulong RIP;
        public ulong CS;
        public ulong RFLAGS;
        public ulong UserRSP;  // Solo en cambios de privilegio
        public ulong SS;       // Solo en cambios de privilegio
    }

    /// <summary>
    /// Gestor básico de interrupciones del sistema
    /// </summary>
    public static unsafe class InterruptManager
    {
        // Tabla de punteros a manejadores de interrupciones
        public static IntPtr* _handlerTable;


        /// <summary>
        /// Inicializa el gestor de interrupciones
        /// </summary>
        public static void Initialize()
        {
            Console.WriteLine("Inicializando gestor de interrupciones...");

            // Asignar memoria para la tabla de manejadores (256 posibles interrupciones)
            _handlerTable = (IntPtr*)Allocator.malloc((nuint)(sizeof(IntPtr) * 256));

            // Inicializar todos los punteros a cero
            for (int i = 0; i < 256; i++)
            {
                _handlerTable[i] = IntPtr.Zero;
            }

            // Registrar manejadores para excepciones importantes
            RegisterHandler(0, &DivideByZeroHandler);      // División por cero
            RegisterHandler(6, &InvalidOpcodeHandler);     // Código de operación inválido
            RegisterHandler(8, &DoubleFaultHandler);       // Doble falta
            RegisterHandler(13, &GeneralProtectionHandler); // Protección general
            RegisterHandler(14, &PageFaultHandler);        // Falta de página

            // Informar que el gestor está inicializado
            Console.WriteLine("Gestor de interrupciones inicializado correctamente");
        }

        /// <summary>
        /// Registra un manejador para un número de interrupción específico
        /// </summary>
        private static void RegisterHandler(int interruptNumber, delegate*<InterruptFrame*, void> handler)
        {
            if (interruptNumber >= 0 && interruptNumber < 256)
            {
                _handlerTable[interruptNumber] = (IntPtr)handler;

                // Llamar a la función externa que establece el manejador en el IDT
                WriteInterruptHandler(interruptNumber, (IntPtr)handler);
            }
        }

        /// <summary>
        /// Punto de entrada principal para todas las interrupciones
        /// Esta función es llamada desde el código de ensamblador
        /// </summary>
        [RuntimeExport("HandleInterrupt")]
        public static void HandleInterrupt(InterruptFrame* frame)
        {
            int interruptNumber = (int)frame->InterruptNumber;

            // Manejar la interrupción según su número
            if (interruptNumber >= 0 && interruptNumber < 256)
            {
                // Verificar si hay un manejador registrado
                IntPtr handlerPtr = _handlerTable[interruptNumber];

                if (handlerPtr != IntPtr.Zero)
                {
                    // Llamar al manejador específico
                    ((delegate*<InterruptFrame*, void>)handlerPtr.ToPointer())(frame);
                }
                else
                {
                    // Usar el manejador por defecto
                    DefaultHandler(frame);
                }
            }
        }

        /// <summary>
        /// Función externa para registrar un manejador en la tabla de descriptores de interrupciones
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
        /// Manejador por defecto para interrupciones sin manejador específico
        /// </summary>
        public static void DefaultHandler(InterruptFrame* frame)
        {
            // Solo mostrar en consola si es una interrupción inesperada (no de hardware normal)
            if (frame->InterruptNumber < 32)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Interrupción no manejada: 0x{frame->InterruptNumber.ToStringHex()} en dirección 0x{frame->RIP.ToStringHex()}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        /// <summary>
        /// Manejador para división por cero (INT 0)
        /// </summary>
        public static void DivideByZeroHandler(InterruptFrame* frame)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: División por cero en dirección 0x{frame->RIP.ToStringHex()}");
            Console.ForegroundColor = ConsoleColor.White;
            HaltSystem();
        }

        /// <summary>
        /// Manejador para código de operación inválido (INT 6)
        /// </summary>
        public static void InvalidOpcodeHandler(InterruptFrame* frame)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: Código de operación inválido en dirección 0x{frame->RIP.ToStringHex()}");
            Console.ForegroundColor = ConsoleColor.White;
            HaltSystem();
        }

        /// <summary>
        /// Manejador para doble falta (INT 8)
        /// </summary>
        public static void DoubleFaultHandler(InterruptFrame* frame)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR CRÍTICO: Doble falta. El sistema se detendrá.");
            Console.ForegroundColor = ConsoleColor.White;
            HaltSystem();
        }

        /// <summary>
        /// Manejador para falta de protección general (INT 13)
        /// </summary>
        public static void GeneralProtectionHandler(InterruptFrame* frame)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: Falta de protección general en dirección 0x{frame->RIP.ToStringHex()}");
            Console.WriteLine($"Código de error: 0x{frame->ErrorCode.ToStringHex()}");
            Console.ForegroundColor = ConsoleColor.White;
            HaltSystem();
        }

        /// <summary>
        /// Manejador para falta de página (INT 14)
        /// </summary>
        public static void PageFaultHandler(InterruptFrame* frame)
        {
            // Leer el registro CR2 que contiene la dirección que causó la falta
            ulong faultAddress = ReadCR2();

            // Análisis del código de error
            bool present = (frame->ErrorCode & 1) != 0;      // Página presente
            bool write = (frame->ErrorCode & 2) != 0;        // Operación de escritura
            bool user = (frame->ErrorCode & 4) != 0;         // Modo usuario
            bool reserved = (frame->ErrorCode & 8) != 0;     // Bits reservados
            bool instruction = (frame->ErrorCode & 16) != 0; // Búsqueda de instrucción

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: Falta de página en dirección 0x{faultAddress.ToStringHex()}");
            //Console.WriteLine($"Tipo: {(present ? "Violación de protección" : "Página no presente")}");
            //Console.WriteLine($"Operación: {(write ? "Escritura" : "Lectura")}");
            //Console.WriteLine($"Contexto: {(user ? "Usuario" : "Kernel")}");
            if (reserved) Console.WriteLine("Error de bits reservados");
            if (instruction) Console.WriteLine("Causado por búsqueda de instrucción");
            Console.ForegroundColor = ConsoleColor.White;

            HaltSystem();
        }

        /// <summary>
        /// Lee el valor del registro CR2 (dirección que causó la falta de página)
        /// </summary>
        [DllImport("*", EntryPoint = "_ReadCR2")]
        private static extern ulong ReadCR2();

        /// <summary>
        /// Detiene el sistema después de una excepción crítica
        /// </summary>
        private static void HaltSystem()
        {
            // Deshabilitar interrupciones
            DisableInterrupts();

            Console.WriteLine("Sistema detenido");

            // Bucle infinito
            while (true)
            {
                // Pausar la CPU para ahorrar energía
                Halt();
            }
        }

        /// <summary>
        /// Deshabilita las interrupciones
        /// </summary>
        [DllImport("*", EntryPoint = "_DisableInterrupts")]
        private static extern void DisableInterrupts();

        /// <summary>
        /// Pausa la CPU (instrucción HLT)
        /// </summary>
        [DllImport("*", EntryPoint = "_Hlt")]
        private static extern void Halt();

        /// <summary>
        /// Envía un comando de fin de interrupción al controlador de interrupciones
        /// </summary>
        /// <param name="irq">Número de IRQ</param>
        public static void SendEndOfInterrupt(byte irq)
        {
            // Para IRQs 8-15, enviar EOI también al PIC esclavo
            if (irq >= 8)
            {
                // Enviar EOI al PIC esclavo (puerto 0xA0)
                Native.OutByte(0xA0, 0x20);
            }

            // Enviar EOI al PIC maestro (puerto 0x20)
            Native.OutByte(0x20, 0x20);
        }
    }
}