using Kernel.Diagnostics;
using System.Runtime.InteropServices;

namespace Kernel.Drivers
{
    /// <summary>
    /// Controlador para el Programmable Interrupt Controller (PIC 8259)
    /// </summary>
    public static class PICController
    {
        // Puertos del PIC maestro y esclavo
        private const byte PIC1_COMMAND = 0x20;
        private const byte PIC1_DATA = 0x21;
        private const byte PIC2_COMMAND = 0xA0;
        private const byte PIC2_DATA = 0xA1;

        // Comandos PIC
        private const byte ICW1_ICW4 = 0x01;      // Se requiere ICW4
        private const byte ICW1_SINGLE = 0x02;    // Operación en modo único
        private const byte ICW1_INTERVAL4 = 0x04; // Intervalo de llamada 4
        private const byte ICW1_LEVEL = 0x08;     // Modo nivel
        private const byte ICW1_INIT = 0x10;      // Inicialización

        private const byte ICW4_8086 = 0x01;      // Modo 8086/88
        private const byte ICW4_AUTO = 0x02;      // Fin de interrupción automático
        private const byte ICW4_BUF_SLAVE = 0x08; // Esclavo en modo buffer
        private const byte ICW4_BUF_MASTER = 0x0C; // Maestro en modo buffer
        private const byte ICW4_SFNM = 0x10;      // Modo fully nested especial

        // Comandos EOI (End of Interrupt)
        private const byte PIC_EOI = 0x20;        // Comando de fin de interrupción

        /// <summary>
        /// Inicializa el PIC, remapeando las IRQs para que no colisionen 
        /// con las excepciones de la CPU
        /// </summary>
        public static void Initialize()
        {
            SerialDebug.Info("Inicializando PIC...");

            // Guardar máscaras actuales
            byte mask1 = InByte(PIC1_DATA);
            byte mask2 = InByte(PIC2_DATA);

            // Iniciar secuencia de inicialización (ICW1)
            OutByte(PIC1_COMMAND, ICW1_INIT | ICW1_ICW4);
            IOWait();
            OutByte(PIC2_COMMAND, ICW1_INIT | ICW1_ICW4);
            IOWait();

            // ICW2: Vector offset - Remapear IRQs
            OutByte(PIC1_DATA, 0x20);  // IRQ 0-7 -> INT 0x20-0x27
            IOWait();
            OutByte(PIC2_DATA, 0x28);  // IRQ 8-15 -> INT 0x28-0x2F
            IOWait();

            // ICW3: Configuración maestro/esclavo
            OutByte(PIC1_DATA, 0x04);  // PIC1 tiene un esclavo en la línea 2 (bit 2 = 1)
            IOWait();
            OutByte(PIC2_DATA, 0x02);  // PIC2 tiene ID de cascada 2
            IOWait();

            // ICW4: Configuración de modo de operación
            OutByte(PIC1_DATA, ICW4_8086);
            IOWait();
            OutByte(PIC2_DATA, ICW4_8086);
            IOWait();

            // Restaurar máscaras guardadas o establecer nuevas
            // Por defecto, deshabilitamos todas las IRQs excepto el teclado (IRQ1) y temporizador (IRQ0)
            OutByte(PIC1_DATA, 0xFC); // 1111 1100 - Solo permitimos IRQ0 (temporizador) y IRQ1 (teclado)
            IOWait();
            OutByte(PIC2_DATA, 0xFF); // 1111 1111 - Deshabilitamos todas las IRQs del PIC2

            SerialDebug.Info("PIC inicializado correctamente");
        }

        /// <summary>
        /// Envía un comando de fin de interrupción (EOI) al PIC apropiado
        /// </summary>
        /// <param name="irq">Número de IRQ</param>
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
        /// Establece la máscara de interrupciones para el PIC maestro
        /// </summary>
        /// <param name="mask">Máscara a establecer (1 = deshabilitado, 0 = habilitado)</param>
        public static void SetMasterMask(byte mask)
        {
            OutByte(PIC1_DATA, mask);
        }

        /// <summary>
        /// Establece la máscara de interrupciones para el PIC esclavo
        /// </summary>
        /// <param name="mask">Máscara a establecer (1 = deshabilitado, 0 = habilitado)</param>
        public static void SetSlaveMask(byte mask)
        {
            OutByte(PIC2_DATA, mask);
        }

        /// <summary>
        /// Establece la máscara de interrupciones para ambos PICs
        /// </summary>
        /// <param name="masterMask">Máscara para el PIC maestro</param>
        /// <param name="slaveMask">Máscara para el PIC esclavo</param>
        public static void SetMask(byte masterMask, byte slaveMask)
        {
            SetMasterMask(masterMask);
            SetSlaveMask(slaveMask);
        }

        /// <summary>
        /// Habilita una IRQ específica
        /// </summary>
        /// <param name="irq">Número de IRQ a habilitar</param>
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
        /// <param name="irq">Número de IRQ a deshabilitar</param>
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

        // Métodos de acceso a puertos de E/S
        [DllImport("*", EntryPoint = "_OutByte")]
        private static extern void OutByte(byte port, byte value);

        [DllImport("*", EntryPoint = "_InByte")]
        private static extern byte InByte(byte port);

        /// <summary>
        /// Pequeño retraso para asegurar que el PIC procese los comandos
        /// </summary>
        private static void IOWait()
        {
            // Un método simple es escribir en un puerto no utilizado
            OutByte(0x80, 0);
        }
    }
}