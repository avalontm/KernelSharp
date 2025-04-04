using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Kernel
{
    /// <summary>
    /// Proporciona acceso a funciones nativas de bajo nivel para interactuar con el hardware
    /// </summary>
    public static unsafe class Native
    {
        /// <summary>
        /// Lee el valor del registro CR0
        /// </summary>
        /// <returns>Valor actual del registro CR0</returns>
        [DllImport("*", EntryPoint = "_ReadCR0")]
        public static extern ulong ReadCR0();

        /// <summary>
        /// Escribe un valor en el registro CR0
        /// </summary>
        /// <param name="value">Valor a escribir en CR0</param>
        [DllImport("*", EntryPoint = "_WriteCR0")]
        public static extern void WriteCR0(ulong value);

        /// <summary>
        /// Lee el valor del registro CR2 (dirección que causó una falla de página)
        /// </summary>
        /// <returns>Valor actual del registro CR2</returns>
        [DllImport("*", EntryPoint = "_ReadCR2")]
        public static extern ulong ReadCR2();

        /// <summary>
        /// Lee el valor del registro CR3 (directorio de páginas)
        /// </summary>
        /// <returns>Valor actual del registro CR3</returns>
        [DllImport("*", EntryPoint = "_ReadCR3")]
        public static extern ulong ReadCR3();

        /// <summary>
        /// Escribe un valor en el registro CR3 (carga un nuevo directorio de páginas)
        /// </summary>
        /// <param name="value">Valor a escribir en CR3</param>
        [DllImport("*", EntryPoint = "_WriteCR3")]
        public static extern void WriteCR3(ulong value);

        /// <summary>
        /// Lee el valor del registro CR4
        /// </summary>
        /// <returns>Valor actual del registro CR4</returns>
        [DllImport("*", EntryPoint = "_ReadCR4")]
        public static extern ulong ReadCR4();

        /// <summary>
        /// Escribe un valor en el registro CR4
        /// </summary>
        /// <param name="value">Valor a escribir en CR4</param>
        [DllImport("*", EntryPoint = "_WriteCR4")]
        public static extern void WriteCR4(ulong value);

        /// <summary>
        /// Invalida una entrada en la TLB para una dirección específica
        /// </summary>
        /// <param name="address">Dirección virtual a invalidar</param>
        [DllImport("*", EntryPoint = "_Invlpg")]
        public static extern void Invlpg(ulong address);

        /// <summary>
        /// Deshabilita las interrupciones
        /// </summary>
        [DllImport("*", EntryPoint = "_STI")]
        public static extern void STI();

        /// <summary>
        /// Habilita las interrupciones
        /// </summary>
        [DllImport("*", EntryPoint = "_CLI")]
        public static extern void CLI();

        /// <summary>
        /// Lee un byte desde un puerto de E/S
        /// </summary>
        /// <param name="port">Puerto de E/S</param>
        /// <returns>Byte leído</returns>
        [DllImport("*", EntryPoint = "_InByte")]
        public static extern byte InByte(ushort port);

        /// <summary>
        /// Escribe un byte en un puerto de E/S
        /// </summary>
        /// <param name="port">Puerto de E/S</param>
        /// <param name="value">Valor a escribir</param>
        [DllImport("*", EntryPoint = "_OutByte")]
        public static extern void OutByte(ushort port, byte value);

        /// <summary>
        /// Lee una palabra (16 bits) desde un puerto de E/S
        /// </summary>
        /// <param name="port">Puerto de E/S</param>
        /// <returns>Palabra leída</returns>
        [DllImport("*", EntryPoint = "_InWord")]
        public static extern ushort InWord(ushort port);

        /// <summary>
        /// Escribe una palabra (16 bits) en un puerto de E/S
        /// </summary>
        /// <param name="port">Puerto de E/S</param>
        /// <param name="value">Valor a escribir</param>
        [DllImport("*", EntryPoint = "_OutWord")]
        public static extern void OutWord(ushort port, ushort value);

        /// <summary>
        /// Lee una doble palabra (32 bits) desde un puerto de E/S
        /// </summary>
        /// <param name="port">Puerto de E/S</param>
        /// <returns>Doble palabra leída</returns>
        [DllImport("*", EntryPoint = "_InDWord")]
        public static extern ulong InDWord(ushort port);

        /// <summary>
        /// Escribe una doble palabra (32 bits) en un puerto de E/S
        /// </summary>
        /// <param name="port">Puerto de E/S</param>
        /// <param name="value">Valor a escribir</param>
        [DllImport("*", EntryPoint = "_OutDWord")]
        public static extern void OutDWord(ushort port, ulong value);

        /// <summary>
        /// Rellena un bloque de memoria con un valor específico (similar a memset)
        /// </summary>
        /// <param name="dest">Puntero al destino</param>
        /// <param name="value">Valor de relleno (byte)</param>
        /// <param name="count">Número de bytes a rellenar</param>
        [DllImport("*", EntryPoint = "_Stosb")]
        public static extern void Stosb(void* dest, byte value, ulong count);

        /// <summary>
        /// Copia un bloque de memoria de una ubicación a otra (similar a memcpy)
        /// </summary>
        /// <param name="dest">Puntero al destino</param>
        /// <param name="src">Puntero al origen</param>
        /// <param name="count">Número de bytes a copiar</param>
        [DllImport("*", EntryPoint = "_Movsb")]
        public static extern void Movsb(void* dest, void* src, ulong count);

        /// <summary>
        /// Espera un ciclo de CPU (útil para sincronización)
        /// </summary>
        [DllImport("*", EntryPoint = "_Halt")]
        public static extern void Halt();

        /// <summary>
        /// Detiene la ejecución del CPU hasta que ocurra una interrupción
        /// </summary>
        [DllImport("*", EntryPoint = "_Hlt")]
        public static extern void Hlt();

        /// <summary>
        /// Reinicia el sistema usando la funcionalidad del teclado del controlador 8042
        /// </summary>
        [DllImport("*", EntryPoint = "_Reset")]
        public static extern void Reset();

        /// <summary>
        /// Obtiene el estado actual de las banderas (EFLAGS)
        /// </summary>
        /// <returns>Valor actual de EFLAGS</returns>
        [DllImport("*", EntryPoint = "_GetEFlags")]
        public static extern ulong GetEFlags();

        /// <summary>
        /// Establece el estado de las banderas (EFLAGS)
        /// </summary>
        /// <param name="flags">Valor a establecer en EFLAGS</param>
        [DllImport("*", EntryPoint = "_SetEFlags")]
        public static extern void SetEFlags(ulong flags);

        /// <summary>
        /// Ejecuta la instrucción PAUSE para reducir el consumo en esperas activas
        /// </summary>
        [DllImport("*", EntryPoint = "_Pause")]
        public static extern void Pause();

        /// <summary>
        /// Lee un registro específico del Model Specific Register (MSR)
        /// </summary>
        /// <param name="msr">Registro MSR a leer</param>
        /// <returns>Valor del registro</returns>
        [DllImport("*", EntryPoint = "_ReadMSR")]
        public static extern ulong ReadMSR(uint msr);

        /// <summary>
        /// Escribe un valor en un registro específico del Model Specific Register (MSR)
        /// </summary>
        /// <param name="msr">Registro MSR a escribir</param>
        /// <param name="value">Valor a escribir</param>
        [DllImport("*", EntryPoint = "_WriteMSR")]
        public static extern void WriteMSR(uint msr, ulong value);

        /// <summary>
        /// Obtiene el ID del APIC Local usando la instrucción CPUID
        /// </summary>
        /// <returns>ID del APIC Local</returns>
        [DllImport("*", EntryPoint = "_GetAPICID")]
        public static extern byte GetAPICID();

        /// <summary>
        /// Envía un comando al puerto de comando del controlador de teclado
        /// </summary>
        /// <param name="command">Comando a enviar</param>
        [DllImport("*", EntryPoint = "_KBControllerSendCommand")]
        public static extern void KBControllerSendCommand(byte command);

        /// <summary>
        /// Ejecuta la instrucción CPUID
        /// </summary>
        /// <param name="leaf">Función CPUID a ejecutar</param>
        /// <param name="eax">Registro EAX (entrada/salida)</param>
        /// <param name="ebx">Registro EBX (salida)</param>
        /// <param name="ecx">Registro ECX (entrada/salida)</param>
        /// <param name="edx">Registro EDX (salida)</param>
        [DllImport("*", EntryPoint = "_CPUID")]
        public static extern void Cpuid(uint leaf, ref uint eax, ref uint ebx, ref uint ecx, ref uint edx);

        /// <summary>
        /// Ejecuta la instrucción CPUID con el valor de función especificado
        /// </summary>
        /// <param name="function">Función CPUID a ejecutar</param>
        /// <returns>Valor de EDX después de ejecutar CPUID</returns>
        public static uint CPUID(uint function)
        {
            uint eax = function;
            uint ebx = 0;
            uint ecx = 0;
            uint edx = 0;

            Cpuid(function, ref eax, ref ebx, ref ecx, ref edx);

            return edx;
        }
    }
}