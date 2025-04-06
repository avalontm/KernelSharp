using System;
using System.Runtime.InteropServices;

namespace Kernel.Drivers.IO
{
    /// <summary>
    /// Proporciona métodos para acceder a los puertos de entrada/salida del hardware
    /// </summary>
    public static class IOPort
    {
        /// <summary>
        /// Escribe un byte en un puerto de salida
        /// </summary>
        /// <param name="port">Número de puerto (0-65535)</param>
        /// <param name="value">Valor a escribir (0-255)</param>
        [DllImport("*", EntryPoint = "_Out8")]
        public static extern void Out8(ushort port, byte value);

        /// <summary>
        /// Lee un byte desde un puerto de entrada
        /// </summary>
        /// <param name="port">Número de puerto (0-65535)</param>
        /// <returns>Valor leído (0-255)</returns>
        [DllImport("*", EntryPoint = "_In8")]
        public static extern byte In8(uint port);

        /// <summary>
        /// Escribe una palabra (2 bytes) en un puerto de salida
        /// </summary>
        /// <param name="port">Número de puerto (0-65535)</param>
        /// <param name="value">Valor a escribir (0-65535)</param>
        [DllImport("*", EntryPoint = "_Out16")]
        public static extern void Out16(ushort port, ushort value);

        /// <summary>
        /// Lee una palabra (2 bytes) desde un puerto de entrada
        /// </summary>
        /// <param name="port">Número de puerto (0-65535)</param>
        /// <returns>Valor leído (0-65535)</returns>
        [DllImport("*", EntryPoint = "_In16")]
        public static extern ushort In16(ushort port);

        /// <summary>
        /// Escribe una doble palabra (4 bytes) en un puerto de salida
        /// </summary>
        /// <param name="port">Número de puerto (0-65535)</param>
        /// <param name="value">Valor a escribir (32 bits)</param>
        [DllImport("*", EntryPoint = "_Out32")]
        public static extern void Out32(ushort port, uint value);

        /// <summary>
        /// Lee una doble palabra (4 bytes) desde un puerto de entrada
        /// </summary>
        /// <param name="port">Número de puerto (0-65535)</param>
        /// <returns>Valor leído (32 bits)</returns>
        [DllImport("*", EntryPoint = "_In32")]
        public static extern uint In32(ushort port);

        /// <summary>
        /// Espera un breve periodo (útil entre operaciones de E/S)
        /// </summary>
        public static void Wait()
        {
            // 0x80 es un puerto comúnmente usado para pequeños retrasos
            Out8(0x80, 0);
        }

        /// <summary>
        /// Escribe una secuencia de bytes en un puerto de E/S
        /// </summary>
        /// <param name="port">Número de puerto (0-65535)</param>
        /// <param name="data">Datos a escribir</param>
        public static void WriteBytes(ushort port, byte[] data)
        {
            if (data == null)
                return;

            for (int i = 0; i < data.Length; i++)
            {
                Out8(port, data[i]);
            }
        }

        /// <summary>
        /// Lee una secuencia de bytes desde un puerto de E/S
        /// </summary>
        /// <param name="port">Número de puerto (0-65535)</param>
        /// <param name="count">Número de bytes a leer</param>
        /// <returns>Matriz con los bytes leídos</returns>
        public static byte[] ReadBytes(ushort port, int count)
        {
            if (count <= 0)
                return new byte[0];

            byte[] data = new byte[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = In8(port);
            }
            return data;
        }

        /// <summary>
        /// Escribe un byte en un puerto de salida (alias para Out8)
        /// </summary>
        /// <param name="port">Número de puerto (0-65535)</param>
        /// <param name="value">Valor a escribir (0-255)</param>
        public static void Write8(ushort port, byte value)
        {
            Out8(port, value);
        }

        /// <summary>
        /// Lee un byte desde un puerto de entrada (alias para InByte)
        /// </summary>
        /// <param name="port">Número de puerto (0-65535)</param>
        /// <returns>Valor leído (0-255)</returns>
        public static byte Read8(ushort port)
        {
            return In8(port);
        }
    }
}