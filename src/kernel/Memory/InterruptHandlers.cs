using Kernel.Diagnostics;
using System.Runtime;
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Kernel.Memory
{
    /// <summary>
    /// Estructura que representa el estado de la CPU durante una interrupcion
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InterruptFrame
    {
        // Registros empujados por la instruccion 'pusha'
        public uint EDI;
        public uint ESI;
        public uint EBP;
        public uint ESP;
        public uint EBX;
        public uint EDX;
        public uint ECX;
        public uint EAX;

        // Identificador de interrupcion y codigo de error
        public uint InterruptNumber;
        public uint ErrorCode;

        // Registros empujados automáticamente por el CPU
        public uint EIP;
        public uint CS;
        public uint EFLAGS;
        public uint UserESP;  // Solo en cambios de privilegio
        public uint SS;       // Solo en cambios de privilegio
    }

    /// <summary>
    /// Clase que maneja las interrupciones del sistema
    /// </summary>
    public static unsafe class InterruptHandlers
    {
        /// <summary>
        /// Define un manejador de interrupcion
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate void InterruptHandlerDelegate(InterruptFrame* frame);

        // Array para almacenar punteros a funciones nativas que implementan los manejadores
        private static IntPtr* _handlerPointers;

        /// <summary>
        /// Inicializa los manejadores de interrupcion
        /// </summary>
        public static void Initialize()
        {
            //SendString.Info("Inicializando manejadores de interrupcion...");

            // Alojar memoria para los punteros a manejadores
            _handlerPointers = (IntPtr*)NativeMemory.Alloc((nuint)(sizeof(IntPtr) * 256));

            // Inicializar todos los punteros a cero
            for (int i = 0; i < 256; i++)
            {
                _handlerPointers[i] = IntPtr.Zero;
            }

            // Registrar manejadores para excepciones específicas
            SetInterruptHandler(0, &DivideByZeroHandler);
            SetInterruptHandler(1, &DebugHandler);
            SetInterruptHandler(2, &NMIHandler);
            SetInterruptHandler(3, &BreakpointHandler);
            SetInterruptHandler(4, &OverflowHandler);
            SetInterruptHandler(5, &BoundRangeHandler);
            SetInterruptHandler(6, &InvalidOpcodeHandler);
            SetInterruptHandler(7, &DeviceNotAvailableHandler);
            SetInterruptHandler(8, &DoubleFaultHandler);
            SetInterruptHandler(10, &InvalidTSSHandler);
            SetInterruptHandler(11, &SegmentNotPresentHandler);
            SetInterruptHandler(12, &StackFaultHandler);
            SetInterruptHandler(13, &GeneralProtectionHandler);
            SetInterruptHandler(14, &PageFaultHandler);
            SetInterruptHandler(16, &FPUErrorHandler);
            SetInterruptHandler(17, &AlignmentCheckHandler);
            SetInterruptHandler(18, &MachineCheckHandler);
            SetInterruptHandler(19, &SIMDFPHandler);

            //SendString.Info("Manejadores de interrupcion inicializados");
        }

        /// <summary>
        /// Establece un manejador de interrupcion para un número específico
        /// </summary>
        /// <param name="interruptNumber">Número de interrupcion</param>
        /// <param name="handler">Puntero al manejador</param>
        private static void SetInterruptHandler(int interruptNumber, delegate*<InterruptFrame*, void> handler)
        {
            if (interruptNumber >= 0 && interruptNumber < 256)
            {
                _handlerPointers[interruptNumber] = (IntPtr)handler;
                WriteHandlerToTable(interruptNumber, (IntPtr)handler);
            }
        }

        /// <summary>
        /// Método principal que recibe todas las interrupciones desde assembly
        /// </summary>
        [RuntimeExport("HandleInterrupt")]
        public static void HandleInterrupt(InterruptFrame* frame)
        {
            int interruptNumber = (int)frame->InterruptNumber;

            // Aquí manejas la interrupcion según su número
            if (interruptNumber >= 0 && interruptNumber < 256)
            {
                switch (interruptNumber)
                {
                    case 0:
                        DivideByZeroHandler(frame);
                        break;
                    case 1:
                        DebugHandler(frame);
                        break;
                    // ... otros casos
                    default:
                        DefaultHandler(frame);
                        break;
                }
            }
        }

        /// <summary>
        /// Registra un manejador de interrupcion a nivel nativo
        /// </summary>
        // Esta funcion escribe en la tabla definida en ensamblador
        [DllImport("*", EntryPoint = "_WriteInterruptHandler")]
        private static extern void WriteHandlerToTable(int index, IntPtr handler);

        #region Manejadores de Interrupcion Específicos

        /// <summary>
        /// Manejador por defecto para interrupciones sin un manejador específico
        /// </summary>
        /// <param name="frame">Puntero a la estructura con la informacion de la interrupcion</param>
        public static void DefaultHandler(InterruptFrame* frame)
        {
            // Mostrar informacion básica sobre la interrupcion
            //SendString.Warning($"Interrupcion no manejada: {frame->InterruptNumber} en EIP=0x{frame->EIP.ToString()}");

            // Si hay un codigo de error, mostrarlo
            if (frame->InterruptNumber == 8 ||
                frame->InterruptNumber == 10 ||
                frame->InterruptNumber == 11 ||
                frame->InterruptNumber == 12 ||
                frame->InterruptNumber == 13 ||
                frame->InterruptNumber == 14 ||
                frame->InterruptNumber == 17 ||
                frame->InterruptNumber == 30)
            {
                //SendString.Warning($"Codigo de error: 0x{frame->ErrorCode.ToString()}");
            }

            // Solo mostrar en consola si es una interrupcion no esperada
            if (frame->InterruptNumber < 32)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Interrupcion no manejada: {frame->InterruptNumber} en direccion 0x{frame->EIP.ToString()}");
                Console.ForegroundColor = ConsoleColor.White;
            }

            // No necesitamos detener el sistema, solo registrar la informacion
            // Si fuera una interrupcion crítica, deberíamos llamar a HaltSystem()
        }
        /// <summary>
        /// Manejador para division por cero (INT 0)
        /// </summary>
        public static void DivideByZeroHandler(InterruptFrame* frame)
        {
            //SendString.Error($"EXCEPCIoN: Division por cero en EIP=0x{frame->EIP.ToString()}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: Division por cero en direccion 0x{frame->EIP.ToString()}");
            Console.ForegroundColor = ConsoleColor.White;
            HaltSystem();
        }

        /// <summary>
        /// Manejador para depuracion (INT 1)
        /// </summary>
        public static void DebugHandler(InterruptFrame* frame)
        {
            //SendString.Warning($"Excepcion de depuracion en EIP=0x{frame->EIP.ToString()}");
        }

        /// <summary>
        /// Manejador para NMI (INT 2)
        /// </summary>
        public static void NMIHandler(InterruptFrame* frame)
        {
            //SendString.Error("EXCEPCIoN: Interrupcion no enmascarable (NMI)");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: Interrupcion no enmascarable (NMI)");
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Manejador para breakpoint (INT 3)
        /// </summary>
        public static void BreakpointHandler(InterruptFrame* frame)
        {
            //SendString.Info($"Breakpoint en EIP=0x{frame->EIP.ToString()}");
            Console.WriteLine($"Breakpoint alcanzado en 0x{frame->EIP.ToString()}");
        }

        /// <summary>
        /// Manejador para desbordamiento (INT 4)
        /// </summary>
        public static void OverflowHandler(InterruptFrame* frame)
        {
            //SendString.Error($"EXCEPCIoN: Desbordamiento en EIP=0x{frame->EIP.ToString()}");
        }

        /// <summary>
        /// Manejador para salida de rango (INT 5)
        /// </summary>
        public static void BoundRangeHandler(InterruptFrame* frame)
        {
            //SendString.Error($"EXCEPCIoN: Límite excedido en EIP=0x{frame->EIP.ToString()}");
        }

        /// <summary>
        /// Manejador para codigo de operacion inválido (INT 6)
        /// </summary>
        public static void InvalidOpcodeHandler(InterruptFrame* frame)
        {
            //SendString.Error($"EXCEPCIoN: Codigo de operacion inválido en EIP=0x{frame->EIP.ToString()}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: Codigo de operacion inválido en direccion 0x{frame->EIP.ToString()}");
            Console.ForegroundColor = ConsoleColor.White;
            HaltSystem();
        }

        /// <summary>
        /// Manejador para dispositivo no disponible (INT 7)
        /// </summary>
        public static void DeviceNotAvailableHandler(InterruptFrame* frame)
        {
            //SendString.Error($"EXCEPCIoN: Dispositivo no disponible en EIP=0x{frame->EIP.ToString()}");
        }

        /// <summary>
        /// Manejador para doble falta (INT 8)
        /// </summary>
        public static void DoubleFaultHandler(InterruptFrame* frame)
        {
            //SendString.Error($"EXCEPCIoN CRÍTICA: Doble falta. Codigo de error: 0x{frame->ErrorCode.ToString()}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR CRÍTICO: Doble falta. El sistema se detendrá.");
            Console.ForegroundColor = ConsoleColor.White;
            HaltSystem();
        }

        /// <summary>
        /// Manejador para TSS inválido (INT 10)
        /// </summary>
        public static void InvalidTSSHandler(InterruptFrame* frame)
        {
            //SendString.Error($"EXCEPCIoN: TSS inválido. Codigo de error: 0x{frame->ErrorCode.ToString()}");
        }

        /// <summary>
        /// Manejador para segmento no presente (INT 11)
        /// </summary>
        public static void SegmentNotPresentHandler(InterruptFrame* frame)
        {
            //SendString.Error($"EXCEPCIoN: Segmento no presente. Codigo de error: 0x{frame->ErrorCode.ToString()}");
        }

        /// <summary>
        /// Manejador para falta de pila (INT 12)
        /// </summary>
        public static void StackFaultHandler(InterruptFrame* frame)
        {
            //SendString.Error($"EXCEPCIoN: Falta de pila. Codigo de error: 0x{frame->ErrorCode.ToString()}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: Falta de pila. Codigo de error: 0x{frame->ErrorCode.ToString()}");
            Console.ForegroundColor = ConsoleColor.White;
            HaltSystem();
        }

        /// <summary>
        /// Manejador para falta de proteccion general (INT 13)
        /// </summary>
        public static void GeneralProtectionHandler(InterruptFrame* frame)
        {
            //SendString.Error($"EXCEPCIoN: Falta de proteccion general en EIP=0x{frame->EIP.ToString()}. Codigo de error: 0x{frame->ErrorCode.ToString()}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: Falta de proteccion general en direccion 0x{frame->EIP.ToString()}");
            Console.WriteLine($"Codigo de error: 0x{frame->ErrorCode.ToString()}");
            Console.ForegroundColor = ConsoleColor.White;
            HaltSystem();
        }

        /// <summary>
        /// Manejador para falta de página (INT 14)
        /// </summary>
        public static void PageFaultHandler(InterruptFrame* frame)
        {
            // Leer el registro CR2 que contiene la direccion que causo la falta
            uint faultAddress = GetCR2();

            // Análisis del codigo de error
            bool present = (frame->ErrorCode & 1) != 0;         // Página presente
            bool write = (frame->ErrorCode & 2) != 0;           // Operacion de escritura
            bool user = (frame->ErrorCode & 4) != 0;            // Modo usuario
            bool reserved = (frame->ErrorCode & 8) != 0;        // Bits reservados
            bool instruction = (frame->ErrorCode & 16) != 0;    // Fetch de instruccion

            //SendString.Error($"EXCEPCIoN: Falta de página en direccion 0x{faultAddress.ToString()}");
            //SendString.Error($"  EIP=0x{frame->EIP.ToString()}, Codigo de error: 0x{frame->ErrorCode.ToString()}");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: Falta de página en direccion 0x{faultAddress.ToString()}");
            Console.WriteLine($"Tipo: {(present ? "Violacion de proteccion" : "Página no presente")}");
            Console.WriteLine($"Operacion: {(write ? "Escritura" : "Lectura")}");
            Console.WriteLine($"Contexto: {(user ? "Usuario" : "Kernel")}");
            if (reserved) Console.WriteLine("Error de bits reservados");
            if (instruction) Console.WriteLine("Causada por fetch de instruccion");
            Console.ForegroundColor = ConsoleColor.White;

            HaltSystem();
        }

        /// <summary>
        /// Manejador para error FPU (INT 16)
        /// </summary>
        public static void FPUErrorHandler(InterruptFrame* frame)
        {
            //SendString.Error($"EXCEPCIoN: Error de punto flotante en EIP=0x{frame->EIP.ToString()}");
        }

        /// <summary>
        /// Manejador para error de alineacion (INT 17)
        /// </summary>
        public static void AlignmentCheckHandler(InterruptFrame* frame)
        {
            //SendString.Error($"EXCEPCIoN: Error de alineacion en EIP=0x{frame->EIP.ToString()}. Codigo de error: 0x{frame->ErrorCode.ToString()}");
        }

        /// <summary>
        /// Manejador para error de máquina (INT 18)
        /// </summary>
        public static void MachineCheckHandler(InterruptFrame* frame)
        {
            //SendString.Error("EXCEPCIoN CRÍTICA: Error de verificacion de máquina. Posible fallo de hardware.");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR CRÍTICO: Error de verificacion de máquina. Posible fallo de hardware.");
            Console.ForegroundColor = ConsoleColor.White;
            HaltSystem();
        }

        /// <summary>
        /// Manejador para error SIMD (INT 19)
        /// </summary>
        public static void SIMDFPHandler(InterruptFrame* frame)
        {
            //SendString.Error($"EXCEPCIoN: Error SIMD de punto flotante en EIP=0x{frame->EIP.ToString()}");
        }

        #endregion

        /// <summary>
        /// Obtiene el valor del registro CR2 (direccion que causo la falta de página)
        /// </summary>
        [DllImport("*", EntryPoint = "_GetCR2")]
        private static extern uint GetCR2();

        /// <summary>
        /// Detiene el sistema tras una excepcion crítica
        /// </summary>
        private static void HaltSystem()
        {
            // Deshabilitar interrupciones
            DisableInterrupts();

            Console.WriteLine("Sistema detenido");

            // Bucle infinito
            while (true)
            {
                // Pausar el CPU para ahorrar energía
                Halt();
            }
        }

        /// <summary>
        /// Deshabilita las interrupciones
        /// </summary>
        [DllImport("*", EntryPoint = "_CLI")]
        private static extern void DisableInterrupts();

        /// <summary>
        /// Pausa el CPU (instruccion HLT)
        /// </summary>
        [DllImport("*", EntryPoint = "_Halt")]
        private static extern void Halt();
    }
}