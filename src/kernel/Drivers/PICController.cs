using Kernel.Diagnostics;
using System.Runtime.InteropServices;

namespace Kernel.Hardware
{
    /// <summary>
    /// Controlador del PIC (Programmable Interrupt Controller) 8259A
    /// </summary>
    public static class PICController
    {
        // Puertos de los PICs
        private const byte PIC1_COMMAND = 0x20;    // PIC maestro: puerto de comandos
        private const byte PIC1_DATA = 0x21;       // PIC maestro: puerto de datos
        private const byte PIC2_COMMAND = 0xA0;    // PIC esclavo: puerto de comandos
        private const byte PIC2_DATA = 0xA1;       // PIC esclavo: puerto de datos

        // Comandos PIC
        private const byte ICW1_ICW4 = 0x01;      // ICW4 necesario
        private const byte ICW1_SINGLE = 0x02;    // Modo simple
        private const byte ICW1_INTERVAL4 = 0x04; // Intervalo de llamada 4
        private const byte ICW1_LEVEL = 0x08;     // Modo disparado por nivel
        private const byte ICW1_INIT = 0x10;      // Inicialización

        private const byte ICW4_8086 = 0x01;      // Modo 8086/88
        private const byte ICW4_AUTO = 0x02;      // EOI automático
        private const byte ICW4_BUF_SLAVE = 0x08; // Modo buffer para esclavo
        private const byte ICW4_BUF_MASTER = 0x0C; // Modo buffer para maestro
        private const byte ICW4_SFNM = 0x10;      // Modo anidado completo especial

        private const byte PIC_EOI = 0x20;        // Comando de fin de interrupción

        // Offset de IRQ (para evitar colisión con excepciones de CPU)
        private const byte IRQ_OFFSET_MASTER = 0x20; // IRQ 0-7: INT 0x20-0x27
        private const byte IRQ_OFFSET_SLAVE = 0x28;  // IRQ 8-15: INT 0x28-0x2F

        /// <summary>
        /// Inicializa el PIC con remapeo de IRQs
        /// </summary>
        public static void Initialize()
        {
            SerialDebug.Info("Inicializando controlador PIC...");

            // Guardar máscaras actuales (si son importantes)
            byte mask1 = InByte(PIC1_DATA);
            byte mask2 = InByte(PIC2_DATA);

            // Iniciar secuencia de inicialización (ICW1)
            OutByte(PIC1_COMMAND, ICW1_INIT | ICW1_ICW4);
            IOWait();
            OutByte(PIC2_COMMAND, ICW1_INIT | ICW1_ICW4);
            IOWait();

            // ICW2: Remapeo de IRQs
            OutByte(PIC1_DATA, IRQ_OFFSET_MASTER); // IRQ 0-7 -> INT 0x20-0x27
            IOWait();
            OutByte(PIC2_DATA, IRQ_OFFSET_SLAVE);  // IRQ 8-15 -> INT 0x28-0x2F
            IOWait();

            // ICW3: Configuración maestro/esclavo
            OutByte(PIC1_DATA, 0x04);  // El bit 2 indica que hay un esclavo en IRQ2
            IOWait();
            OutByte(PIC2_DATA, 0x02);  // El valor 2 indica identidad de cascada
            IOWait();

            // ICW4: Configuración del modo
            OutByte(PIC1_DATA, ICW4_8086);
            IOWait();
            OutByte(PIC2_DATA, ICW4_8086);
            IOWait();

            // Restaurar máscaras originales o configurar nuevas
            // Aquí deshabilitamos todas las IRQs excepto teclado (IRQ1) y temporizador (IRQ0)
            OutByte(PIC1_DATA, 0xFC); // 1111 1100 - Solo permitir IRQ0 e IRQ1
            OutByte(PIC2_DATA, 0xFF); // 1111 1111 - Deshabilitar todas las IRQs del PIC2

            SerialDebug.Info("Controlador PIC inicializado correctamente");
        }

        /// <summary>
        /// Envía un comando de fin de interrupción (EOI) al PIC
        /// </summary>
        /// <param name="irq">Número de IRQ (0-15)</param>
        public static void SendEOI(byte irq)
        {
            if (irq >= 8)
            {
                // Si es una IRQ del PIC esclavo, enviar EOI a ambos PICs
                OutByte(PIC2_COMMAND, PIC_EOI);
            }

            // Siempre enviar EOI al PIC maestro
            OutByte(PIC1_COMMAND, PIC_EOI);
        }

        /// <summary>
        /// Habilita una IRQ específica
        /// </summary>
        /// <param name="irq">Número de IRQ (0-15)</param>
        public static void EnableIRQ(byte irq)
        {
            byte port;
            byte value;

            if (irq < 8)
            {
                port = PIC1_DATA;
                value = (byte)(InByte(port) & ~(1 << irq));
            }
            else
            {
                port = PIC2_DATA;
                value = (byte)(InByte(port) & ~(1 << (irq - 8)));
            }

            OutByte(port, value);
        }

        /// <summary>
        /// Deshabilita una IRQ específica
        /// </summary>
        /// <param name="irq">Número de IRQ (0-15)</param>
        public static void DisableIRQ(byte irq)
        {
            byte port;
            byte value;

            if (irq < 8)
            {
                port = PIC1_DATA;
                value = (byte)(InByte(port) | (1 << irq));
            }
            else
            {
                port = PIC2_DATA;
                value = (byte)(InByte(port) | (1 << (irq - 8)));
            }

            OutByte(port, value);
        }

        /// <summary>
        /// Establece la máscara de interrupciones para el PIC maestro
        /// </summary>
        /// <param name="mask">Máscara (1 bit por IRQ, 1 = deshabilitada)</param>
        public static void SetMasterMask(byte mask)
        {
            OutByte(PIC1_DATA, mask);
        }

        /// <summary>
        /// Establece la máscara de interrupciones para el PIC esclavo
        /// </summary>
        /// <param name="mask">Máscara (1 bit por IRQ, 1 = deshabilitada)</param>
        public static void SetSlaveMask(byte mask)
        {
            OutByte(PIC2_DATA, mask);
        }

        // Funciones de acceso a puertos de E/S
        [DllImport("*", EntryPoint = "_OutByte")]
        private static extern void OutByte(byte port, byte value);

        [DllImport("*", EntryPoint = "_InByte")]
        private static extern byte InByte(byte port);

        /// <summary>
        /// Pequeña espera para asegurar que el PIC procese los comandos
        /// </summary>
        private static void IOWait()
        {
            // Método simple: escribir en un puerto no utilizado
            OutByte(0x80, 0);
        }
    }
}