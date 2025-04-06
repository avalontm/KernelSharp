using Kernel.Diagnostics;
using Kernel.Drivers.IO;
using System.Runtime.InteropServices;

/// <summary>
/// 8259A Programmable Interrupt Controller (PIC) Controller
/// </summary>
public static class PICController
{
    // PIC ports
    private const byte PIC1_COMMAND = 0x20;    // Master PIC: command port
    private const byte PIC1_DATA = 0x21;       // Master PIC: data port
    private const byte PIC2_COMMAND = 0xA0;    // Slave PIC: command port
    private const byte PIC2_DATA = 0xA1;       // Slave PIC: data port

    // PIC commands
    private const byte ICW1_ICW4 = 0x01;      // ICW4 needed
    private const byte ICW1_SINGLE = 0x02;    // Single mode
    private const byte ICW1_INTERVAL4 = 0x04; // Call interval 4
    private const byte ICW1_LEVEL = 0x08;     // Level triggered mode
    private const byte ICW1_INIT = 0x10;      // Initialization

    private const byte ICW4_8086 = 0x01;      // 8086/88 mode
    private const byte ICW4_AUTO = 0x02;      // Auto EOI
    private const byte ICW4_BUF_SLAVE = 0x08; // Buffered mode for slave
    private const byte ICW4_BUF_MASTER = 0x0C; // Buffered mode for master
    private const byte ICW4_SFNM = 0x10;      // Special fully nested mode

    private const byte PIC_EOI = 0x20;        // End of Interrupt command

    // IRQ offset (to avoid collision with CPU exceptions)
    private const byte IRQ_OFFSET_MASTER = 0x20; // IRQ 0-7: INT 0x20-0x27
    private const byte IRQ_OFFSET_SLAVE = 0x28;  // IRQ 8-15: INT 0x28-0x2F

    /// <summary>
    /// Initializes the PIC with IRQ remapping
    /// </summary>
    public static void Initialize()
    {
        SerialDebug.Info("Initializing PIC controller...");

        // Save current masks (if important)
        byte mask1 = IOPort.In8(PIC1_DATA);
        byte mask2 = IOPort.In8(PIC2_DATA);

        // Start initialization sequence (ICW1)
        IOPort.Out8(PIC1_COMMAND, ICW1_INIT | ICW1_ICW4);
        IOWait();
        IOPort.Out8(PIC2_COMMAND, ICW1_INIT | ICW1_ICW4);
        IOWait();

        // ICW2: IRQ remapping
        IOPort.Out8(PIC1_DATA, IRQ_OFFSET_MASTER); // IRQ 0-7 -> INT 0x20-0x27
        IOWait();
        IOPort.Out8(PIC2_DATA, IRQ_OFFSET_SLAVE);  // IRQ 8-15 -> INT 0x28-0x2F
        IOWait();

        // ICW3: Master/slave configuration
        IOPort.Out8(PIC1_DATA, 0x04);  // Bit 2 indicates slave on IRQ2
        IOWait();
        IOPort.Out8(PIC2_DATA, 0x02);  // Value 2 indicates cascade identity
        IOWait();

        // ICW4: Mode configuration
        IOPort.Out8(PIC1_DATA, ICW4_8086);
        IOWait();
        IOPort.Out8(PIC2_DATA, ICW4_8086);
        IOWait();

        // Restore original masks or set new ones
        // Here we disable all IRQs except keyboard (IRQ1) and timer (IRQ0)
        IOPort.Out8(PIC1_DATA, 0xFC); // 1111 1100 - Only allow IRQ0 and IRQ1
        IOPort.Out8(PIC2_DATA, 0xFF); // 1111 1111 - Disable all IRQs on PIC2

        SerialDebug.Info("PIC controller initialized successfully");
    }

    /// <summary>
    /// Enables the PIC to process interrupts
    /// </summary>
    public static void Enable()
    {
        SerialDebug.Info("Enabling PIC...");

        // Apply masks that enable required IRQs
        // Default: enable timer (IRQ0) and keyboard (IRQ1) only
        IOPort.Out8(PIC1_DATA, 0xFC); // 1111 1100 - Only IRQ0 and IRQ1 enabled
        IOPort.Out8(PIC2_DATA, 0xFF); // All slave PIC IRQs disabled

        SerialDebug.Info("PIC enabled for timer and keyboard interrupts");
    }

    /// <summary>
    /// Completely disables the PIC by masking all interrupts
    /// This should be called when switching to APIC mode
    /// </summary>
    public static void Disable()
    {
        SerialDebug.Info("Disabling PIC (preparing for APIC mode)...");

        // Mask all interrupts in both PICs
        IOPort.Out8(PIC1_DATA, 0xFF); // Disable all IRQs on master PIC
        IOPort.Out8(PIC2_DATA, 0xFF); // Disable all IRQs on slave PIC

        SerialDebug.Info("PIC disabled - all interrupts masked");
    }

    /// <summary>
    /// Sends an End of Interrupt (EOI) command to the PIC
    /// </summary>
    /// <param name="irq">IRQ number (0-15)</param>
    public static void SendEOI(byte irq)
    {
        if (irq >= 8)
        {
            // If it's an IRQ from the slave PIC, send EOI to both PICs
            IOPort.Out8(PIC2_COMMAND, PIC_EOI);
        }

        // Always send EOI to the master PIC
        IOPort.Out8(PIC1_COMMAND, PIC_EOI);
    }

    /// <summary>
    /// Enables a specific IRQ
    /// </summary>
    /// <param name="irq">IRQ number (0-15)</param>
    public static void EnableIRQ(byte irq)
    {
        byte port;
        byte value;

        if (irq < 8)
        {
            port = PIC1_DATA;
            value = (byte)(IOPort.In8(port) & ~(1 << irq));
        }
        else
        {
            port = PIC2_DATA;
            value = (byte)(IOPort.In8(port) & ~(1 << (irq - 8)));
        }

        IOPort.Out8(port, value);
    }

    /// <summary>
    /// Disables a specific IRQ
    /// </summary>
    /// <param name="irq">IRQ number (0-15)</param>
    public static void DisableIRQ(byte irq)
    {
        byte port;
        byte value;

        if (irq < 8)
        {
            port = PIC1_DATA;
            value = (byte)(IOPort.In8(port) | (1 << irq));
        }
        else
        {
            port = PIC2_DATA;
            value = (byte)(IOPort.In8(port) | (1 << (irq - 8)));
        }

        IOPort.Out8(port, value);
    }

    /// <summary>
    /// Sets the interrupt mask for the master PIC
    /// </summary>
    /// <param name="mask">Mask (1 bit per IRQ, 1 = disabled)</param>
    public static void SetMasterMask(byte mask)
    {
        IOPort.Out8(PIC1_DATA, mask);
    }

    /// <summary>
    /// Sets the interrupt mask for the slave PIC
    /// </summary>
    /// <param name="mask">Mask (1 bit per IRQ, 1 = disabled)</param>
    public static void SetSlaveMask(byte mask)
    {
       IOPort.Out8(PIC2_DATA, mask);
    }

    /// <summary>
    /// Small delay to ensure the PIC processes commands
    /// </summary>
    private static void IOWait()
    {
        // Simple method: write to an unused port
        IOPort.Out8(0x80, 0);
    }
}