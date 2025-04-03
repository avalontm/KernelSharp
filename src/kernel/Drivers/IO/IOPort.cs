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
        [DllImport("*", EntryPoint = "_OutByte")]
        public static extern void OutByte(ushort port, byte value);

        /// <summary>
        /// Lee un byte desde un puerto de entrada
        /// </summary>
        /// <param name="port">Número de puerto (0-65535)</param>
        /// <returns>Valor leído (0-255)</returns>
        [DllImport("*", EntryPoint = "_InByte")]
        public static extern byte InByte(ushort port);

        /// <summary>
        /// Escribe una palabra (2 bytes) en un puerto de salida
        /// </summary>
        /// <param name="port">Número de puerto (0-65535)</param>
        /// <param name="value">Valor a escribir (0-65535)</param>
        [DllImport("*", EntryPoint = "_OutWord")]
        public static extern void OutWord(ushort port, ushort value);

        /// <summary>
        /// Lee una palabra (2 bytes) desde un puerto de entrada
        /// </summary>
        /// <param name="port">Número de puerto (0-65535)</param>
        /// <returns>Valor leído (0-65535)</returns>
        [DllImport("*", EntryPoint = "_InWord")]
        public static extern ushort InWord(ushort port);

        /// <summary>
        /// Escribe una doble palabra (4 bytes) en un puerto de salida
        /// </summary>
        /// <param name="port">Número de puerto (0-65535)</param>
        /// <param name="value">Valor a escribir (32 bits)</param>
        [DllImport("*", EntryPoint = "_OutDword")]
        public static extern void OutDword(ushort port, uint value);

        /// <summary>
        /// Lee una doble palabra (4 bytes) desde un puerto de entrada
        /// </summary>
        /// <param name="port">Número de puerto (0-65535)</param>
        /// <returns>Valor leído (32 bits)</returns>
        [DllImport("*", EntryPoint = "_InDword")]
        public static extern uint InDword(ushort port);

        /// <summary>
        /// Espera un breve periodo (útil entre operaciones de E/S)
        /// </summary>
        public static void Wait()
        {
            // 0x80 es un puerto comúnmente usado para pequeños retrasos
            OutByte(0x80, 0);
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
                OutByte(port, data[i]);
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
                data[i] = InByte(port);
            }
            return data;
        }

        /// <summary>
        /// Escribe un byte en un puerto de salida (alias para OutByte)
        /// </summary>
        /// <param name="port">Número de puerto (0-65535)</param>
        /// <param name="value">Valor a escribir (0-255)</param>
        public static void Write8(ushort port, byte value)
        {
            OutByte(port, value);
        }

        /// <summary>
        /// Lee un byte desde un puerto de entrada (alias para InByte)
        /// </summary>
        /// <param name="port">Número de puerto (0-65535)</param>
        /// <returns>Valor leído (0-255)</returns>
        public static byte Read8(ushort port)
        {
            return InByte(port);
        }
    }
}