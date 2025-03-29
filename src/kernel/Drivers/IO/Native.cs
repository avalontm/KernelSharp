using System;
using System.Runtime.InteropServices;

namespace Kernel.Drivers.IO
{
    public static unsafe class Native
    {
        [DllImport("*", EntryPoint = "_Memset")]
        public static extern void* Memset(void* destination, byte value, uint count);

        [DllImport("*", EntryPoint = "_Memcpy")]
        public static extern void* Memcpy(void* destination, void* source, uint count);

        [DllImport("*", EntryPoint = "_Stosb")]
        public static extern void Stosb(void* data, int value, ulong size);

        [DllImport("*", EntryPoint = "_Movsb")]
        public static extern void Movsb(void* dst, void* src, ulong size);

        public static void Write8(IntPtr address, byte value)
        {
            *(byte*)address = value;
        }

        public static void Write16(IntPtr address, ushort value)
        {
            *(ushort*)address = value;
        }

        public static void Write32(IntPtr address, uint value)
        {
            *(uint*)address = value;
        }

        public static void Write64(IntPtr address, ulong value)
        {
            *(ulong*)address = value;
        }

        public static byte Read8(IntPtr address)
        {
            return *(byte*)address;
        }

        public static ushort Read16(IntPtr address)
        {
            return *(ushort*)address;
        }

        public static uint Read32(IntPtr address)
        {
            return *(uint*)address;
        }

        public static ulong Read64(IntPtr address)
        {
            return *(ulong*)address;
        }

        [DllImport("*", EntryPoint = "_EnableInterrupts")]
        public static extern void EnableInterrupts();

        [DllImport("*", EntryPoint = "_DisableInterrupts")]
        public static extern void DisableInterrupts();

        [DllImport("*", EntryPoint = "_Halt")]
        public static extern void Halt();

        [DllImport("*", EntryPoint = "_CPU_ReadCR0")]
        public static extern uint ReadCR0();

        [DllImport("*", EntryPoint = "_CPU_WriteCR0")]
        public static extern void WriteCR0(uint value);

        [DllImport("*", EntryPoint = "_CPU_ReadCR3")]
        public static extern uint ReadCR3();

        [DllImport("*", EntryPoint = "_CPU_WriteCR3")]
        public static extern void WriteCR3(uint value);

        [DllImport("*", EntryPoint = "_Invlpg")]
        public static extern void Invlpg(void* address);

        [DllImport("*", EntryPoint = "_RDMSR")]
        public static extern void ReadMSR(uint msr, out ulong value);

        [DllImport("*", EntryPoint = "_WRMSR")]
        public static extern void WriteMSR(uint msr, ulong value);

        [DllImport("*", EntryPoint = "_RDTSC")]
        public static extern ulong ReadTSC();

        [DllImport("*", EntryPoint = "_CPUID")]
        public static extern void CPUID(uint function, uint subfunction, out uint eax, out uint ebx, out uint ecx, out uint edx);

        [DllImport("*", EntryPoint = "_PAUSE")]
        public static extern void Pause();

        [DllImport("*", EntryPoint = "_LoadIDT")]
        public static extern void LoadIDT(IntPtr @base, ushort limit);

        [DllImport("*", EntryPoint = "_LoadGDT")]
        public static extern void LoadGDT(IntPtr @base, ushort limit);

        [DllImport("*", EntryPoint = "_Interrupt")]
        public static extern void Interrupt(byte interrupt);

        public static void Wait(uint cycles)
        {
            // Leer el TSC antes y después para medir ciclos transcurridos
            ulong start = ReadTSC();
            ulong end;

            do
            {
                // Usar instrucción PAUSE para mejorar rendimiento en bucles de espera
                Pause();
                end = ReadTSC();
            } while (end - start < cycles);
        }

        public static void Reboot()
        {
            // Método 1: Mediante el controlador de teclado (8042)
            // Esperar a que el buffer de comandos esté vacío
            while ((IOPort.InByte(0x64) & 0x02) != 0)
                ;

            // Enviar comando de reset al controlador
            IOPort.OutByte(0x64, 0xFE);

            // Si el método anterior falla, intentar mediante Triple Fault
            DisableInterrupts();

            // Cargar una IDT inválida para forzar un triple fault
            IntPtr nullIDT = IntPtr.Zero;
            LoadIDT(nullIDT, 0);

            // Forzar una interrupción para provocar el triple fault
            Interrupt(0);

            // Si todo falla, simplemente detener la CPU
            while (true)
                Halt();
        }

        public static bool GetBit(uint value, int bit)
        {
            return (value & (1u << bit)) != 0;
        }

        public static uint SetBit(uint value, int bit, bool state)
        {
            if (state)
                return value | (1u << bit);
            else
                return value & ~(1u << bit);
        }

        public static uint AlignUp(uint value, uint alignment)
        {
            return (value + alignment - 1) & ~(alignment - 1);
        }

        public static bool IsAligned(uint address, uint alignment)
        {
            return (address & (alignment - 1)) == 0;
        }

        public static uint GetPhysicalAddress(void* virtualAddress)
        {
            // Este método depende de la paginación actual
            // En un sistema de memoria virtual completo, necesitaríamos consultar las tablas de páginas
            // Por ahora, asumimos identidad de mapeo (virtual = física)
            return (uint)virtualAddress;
        }

        public static byte GetCpuId()
        {
            uint eax, ebx, ecx, edx;

            // CPUID función 1: Información de características
            CPUID(1, 0, out eax, out ebx, out ecx, out edx);

            // El APIC ID está en los bits 24-31 de EBX
            return (byte)(ebx >> 24);
        }

        public static bool IsCpuFeatureAvailable(uint function, int register, int bit)
        {
            uint eax, ebx, ecx, edx;

            // CPUID con la función especificada
            CPUID(function, 0, out eax, out ebx, out ecx, out edx);

            // Comprobar el bit específico en el registro indicado
            switch (register)
            {
                case 0: return GetBit(eax, bit);
                case 1: return GetBit(ebx, bit);
                case 2: return GetBit(ecx, bit);
                case 3: return GetBit(edx, bit);
                default: return false;
            }
        }

        public static bool IsSSESupported()
        {
            // SSE se indica en el bit 25 de EDX con CPUID función 1
            return IsCpuFeatureAvailable(1, 3, 25);
        }

        public static bool IsSSE2Supported()
        {
            // SSE2 se indica en el bit 26 de EDX con CPUID función 1
            return IsCpuFeatureAvailable(1, 3, 26);
        }

        public static bool IsFPUSupported()
        {
            // FPU se indica en el bit 0 de EDX con CPUID función 1
            return IsCpuFeatureAvailable(1, 3, 0);
        }

        public static bool EnableFloatingPoint()
        {
            // Comprobar disponibilidad
            bool fpu = IsFPUSupported();
            bool sse = IsSSESupported();

            if (!fpu && !sse)
                return false;  // No hay soporte para punto flotante

            // Leer CR0 y CR4
            uint cr0 = ReadCR0();
            uint cr4 = ReadCR4();

            // Habilitar FPU
            if (fpu)
            {
                // Deshabilitar emulación de FPU (CR0.EM = 0)
                cr0 &= ~(1u << 2);
                // Habilitar monitoreo de coprocesador (CR0.MP = 1)
                cr0 |= (1u << 1);
                // Habilitar excepciones numéricas (CR0.NE = 1)
                cr0 |= (1u << 5);
            }

            // Habilitar SSE
            if (sse)
            {
                // Habilitar OSFXSR (CR4.OSFXSR = 1)
                cr4 |= (1u << 9);
                // Habilitar OSXMMEXCPT (CR4.OSXMMEXCPT = 1)
                cr4 |= (1u << 10);
            }

            // Escribir registros de control
            WriteCR0(cr0);
            WriteCR4(cr4);

            return true;
        }

        [DllImport("*", EntryPoint = "_CPU_ReadCR4")]
        public static extern uint ReadCR4();

        [DllImport("*", EntryPoint = "_CPU_WriteCR4")]
        public static extern void WriteCR4(uint value);

        public static ulong GetUptime()
        {
            // Esto depende de una implementación de temporizador del sistema
            // Por ahora, utilizamos TSC como aproximación
            ulong tsc = ReadTSC();

            // Asumimos una frecuencia de procesador de aproximadamente 1 GHz
            // Esto dará una aproximación muy burda, pero es mejor que nada
            return tsc / 1000000;
        }

        public static void Sleep(uint milliseconds)
        {
            // En un sistema kernel, esto podría ceder la CPU a otros procesos
            // Para nuestro caso simple, solo usamos un retardo activo
            ulong startTime = GetUptime();

            while (GetUptime() - startTime < milliseconds)
            {
                // Usar PAUSE para evitar consumir demasiados recursos
                Pause();
            }
        }

        
        public static void DebugWrite(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            // Utilizar el puerto 0xE9 para depuración (funciona en QEMU y Bochs)
            for (int i = 0; i < message.Length; i++)
            {
                IOPort.OutByte(0xE9, (byte)message[i]);
            }
        }
        


    }
}
