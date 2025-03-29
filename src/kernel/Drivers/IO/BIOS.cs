using System;
using System.Runtime.InteropServices;

namespace Kernel.Drivers.IO
{
    /// <summary>
    /// Estructura para pasar registros x86 a llamadas a la BIOS
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RegistersX86
    {
        public uint EAX;
        public uint EBX;
        public uint ECX;
        public uint EDX;
        public uint ESI;
        public uint EDI;
        public uint EBP;
        public ushort DS;
        public ushort ES;
        public ushort FS;
        public ushort GS;
        public uint EFLAGS;

        // Propiedades para acceso a registros de 16 bits
        public ushort AX
        {
            get { return (ushort)(EAX & 0xFFFF); }
            set { EAX = (EAX & 0xFFFF0000) | value; }
        }

        public ushort BX
        {
            get { return (ushort)(EBX & 0xFFFF); }
            set { EBX = (EBX & 0xFFFF0000) | value; }
        }

        public ushort CX
        {
            get { return (ushort)(ECX & 0xFFFF); }
            set { ECX = (ECX & 0xFFFF0000) | value; }
        }

        public ushort DX
        {
            get { return (ushort)(EDX & 0xFFFF); }
            set { EDX = (EDX & 0xFFFF0000) | value; }
        }

        public ushort SI
        {
            get { return (ushort)(ESI & 0xFFFF); }
            set { ESI = (ESI & 0xFFFF0000) | value; }
        }

        public ushort DI
        {
            get { return (ushort)(EDI & 0xFFFF); }
            set { EDI = (EDI & 0xFFFF0000) | value; }
        }

        public ushort BP
        {
            get { return (ushort)(EBP & 0xFFFF); }
            set { EBP = (EBP & 0xFFFF0000) | value; }
        }

        // Propiedades para acceso a registros de 8 bits
        public byte AL
        {
            get { return (byte)(EAX & 0xFF); }
            set { EAX = (EAX & 0xFFFFFF00) | value; }
        }

        public byte AH
        {
            get { return (byte)((EAX >> 8) & 0xFF); }
            set { EAX = (EAX & 0xFFFF00FF) | ((uint)value << 8); }
        }

        public byte BL
        {
            get { return (byte)(EBX & 0xFF); }
            set { EBX = (EBX & 0xFFFFFF00) | value; }
        }

        public byte BH
        {
            get { return (byte)((EBX >> 8) & 0xFF); }
            set { EBX = (EBX & 0xFFFF00FF) | ((uint)value << 8); }
        }

        public byte CL
        {
            get { return (byte)(ECX & 0xFF); }
            set { ECX = (ECX & 0xFFFFFF00) | value; }
        }

        public byte CH
        {
            get { return (byte)((ECX >> 8) & 0xFF); }
            set { ECX = (ECX & 0xFFFF00FF) | ((uint)value << 8); }
        }

        public byte DL
        {
            get { return (byte)(EDX & 0xFF); }
            set { EDX = (EDX & 0xFFFFFF00) | value; }
        }

        public byte DH
        {
            get { return (byte)((EDX >> 8) & 0xFF); }
            set { EDX = (EDX & 0xFFFF00FF) | ((uint)value << 8); }
        }
    }

    /// <summary>
    /// Proporciona acceso a las funciones de la BIOS a través de interrupciones software
    /// </summary>
    public static class BIOS
    {
        /// <summary>
        /// Ejecuta la interrupción 0x10 (Servicios de video) de la BIOS
        /// </summary>
        /// <param name="registers">Registros x86 con los parámetros de la función</param>
        [DllImport("*", EntryPoint = "_Int10h")]
        public static extern void Int10h(ref RegistersX86 registers);

        /// <summary>
        /// Ejecuta la interrupción 0x13 (Servicios de disco) de la BIOS
        /// </summary>
        /// <param name="registers">Registros x86 con los parámetros de la función</param>
        [DllImport("*", EntryPoint = "_Int13h")]
        public static extern void Int13h(ref RegistersX86 registers);

        /// <summary>
        /// Ejecuta la interrupción 0x16 (Servicios de teclado) de la BIOS
        /// </summary>
        /// <param name="registers">Registros x86 con los parámetros de la función</param>
        [DllImport("*", EntryPoint = "_Int16h")]
        public static extern void Int16h(ref RegistersX86 registers);

        /// <summary>
        /// Establece un modo de video mediante la BIOS
        /// </summary>
        /// <param name="mode">Modo de video (ej. 0x03 para modo texto 80x25, 0x13 para gráfico 320x200)</param>
        /// <returns>true si se estableció correctamente</returns>
        public static bool SetVideoMode(byte mode)
        {
            RegistersX86 regs = new RegistersX86();
            regs.AH = 0x00;  // Función 0x00: Establecer modo de video
            regs.AL = mode;  // Modo de video a establecer

            Int10h(ref regs);

            // Verificar resultado (el registro AH contiene el estado)
            // Un valor de 0 en AH indica éxito en muchas llamadas BIOS
            return regs.AH == 0;
        }

        /// <summary>
        /// Lee un carácter desde el teclado mediante la BIOS
        /// </summary>
        /// <returns>El carácter leído</returns>
        public static char ReadKeyboardCharacter()
        {
            RegistersX86 regs = new RegistersX86();
            regs.AH = 0x00;  // Función 0x00: Leer carácter del teclado

            Int16h(ref regs);

            // El carácter se devuelve en AL
            return (char)regs.AL;
        }

        /// <summary>
        /// Comprueba si hay una tecla disponible en el buffer del teclado
        /// </summary>
        /// <returns>true si hay una tecla disponible</returns>
        public static bool IsKeyAvailable()
        {
            RegistersX86 regs = new RegistersX86();
            regs.AH = 0x01;  // Función 0x01: Comprobar estado del teclado

            Int16h(ref regs);

            // Si la bandera Zero (bit 6 de EFLAGS) no está establecida, hay una tecla disponible
            return (regs.EFLAGS & 0x40) == 0;
        }

        /// <summary>
        /// Obtiene la posición actual del cursor
        /// </summary>
        /// <param name="page">Página de video (normalmente 0)</param>
        /// <param name="row">Variable donde se almacenará la fila</param>
        /// <param name="column">Variable donde se almacenará la columna</param>
        public static void GetCursorPosition(byte page, out byte row, out byte column)
        {
            RegistersX86 regs = new RegistersX86();
            regs.AH = 0x03;  // Función 0x03: Obtener posición y tamaño del cursor
            regs.BH = page;  // Página de video

            Int10h(ref regs);

            // Resultados en DH (fila) y DL (columna)
            row = regs.DH;
            column = regs.DL;
        }

        /// <summary>
        /// Establece la posición del cursor
        /// </summary>
        /// <param name="page">Página de video (normalmente 0)</param>
        /// <param name="row">Fila (0-24 en modo texto 80x25)</param>
        /// <param name="column">Columna (0-79 en modo texto 80x25)</param>
        public static void SetCursorPosition(byte page, byte row, byte column)
        {
            RegistersX86 regs = new RegistersX86();
            regs.AH = 0x02;  // Función 0x02: Establecer posición del cursor
            regs.BH = page;  // Página de video
            regs.DH = row;   // Fila
            regs.DL = column; // Columna

            Int10h(ref regs);
        }

        /// <summary>
        /// Escribe un carácter en la posición actual del cursor con un atributo específico
        /// </summary>
        /// <param name="character">Carácter a escribir</param>
        /// <param name="attribute">Atributo de color (4 bits para fondo, 4 bits para texto)</param>
        /// <param name="count">Número de veces a escribir el carácter</param>
        /// <param name="page">Página de video (normalmente 0)</param>
        public static void WriteCharacter(char character, byte attribute, ushort count, byte page)
        {
            RegistersX86 regs = new RegistersX86();
            regs.AH = 0x09;  // Función 0x09: Escribir carácter y atributo en la posición actual
            regs.AL = (byte)character; // Carácter a escribir
            regs.BH = page;  // Página de video
            regs.BL = attribute; // Atributo de color
            regs.CX = count; // Número de veces a escribir el carácter

            Int10h(ref regs);
        }

        /// <summary>
        /// Lee información sobre el modo de video actual
        /// </summary>
        /// <param name="columns">Variable donde se almacenará el número de columnas</param>
        /// <param name="mode">Variable donde se almacenará el modo de video actual</param>
        /// <param name="page">Variable donde se almacenará la página activa</param>
        public static void GetVideoMode(out byte columns, out byte mode, out byte page)
        {
            RegistersX86 regs = new RegistersX86();
            regs.AH = 0x0F;  // Función 0x0F: Obtener modo de video actual

            Int10h(ref regs);

            // Resultados: AL=modo, AH=número de columnas, BH=página activa
            mode = regs.AL;
            columns = regs.AH;
            page = regs.BH;
        }

        /// <summary>
        /// Obtiene información de memoria disponible mediante la BIOS
        /// </summary>
        /// <param name="baseLow">Variable donde se almacenará la memoria base disponible (en KB)</param>
        /// <param name="extendedLow">Variable donde se almacenará la memoria extendida disponible (en KB)</param>
        public static void GetMemorySize(out uint baseLow, out uint extendedLow)
        {
            // Interrupción 0x12 para memoria base
            RegistersX86 regs = new RegistersX86();
            regs.AH = 0x88;  // Función 0x88: Obtener memoria extendida

            Int15h(ref regs);

            // Resultado en AX (KB de memoria extendida)
            extendedLow = regs.AX;

            // Interrupción 0x12 para memoria base
            regs = new RegistersX86();

            Int12h(ref regs);

            // Resultado en AX (KB de memoria base)
            baseLow = regs.AX;
        }

        /// <summary>
        /// Ejecuta la interrupción 0x12 (Obtener memoria base) de la BIOS
        /// </summary>
        /// <param name="registers">Registros x86 con los parámetros de la función</param>
        [DllImport("*", EntryPoint = "_Int12h")]
        public static extern void Int12h(ref RegistersX86 registers);

        /// <summary>
        /// Ejecuta la interrupción 0x15 (Servicios de sistema) de la BIOS
        /// </summary>
        /// <param name="registers">Registros x86 con los parámetros de la función</param>
        [DllImport("*", EntryPoint = "_Int15h")]
        public static extern void Int15h(ref RegistersX86 registers);

        /// <summary>
        /// Realiza una operación de disco mediante la BIOS
        /// </summary>
        /// <param name="function">Función de disco (ej. 0x02 para leer sectores)</param>
        /// <param name="drive">Número de unidad (0x00 para A:, 0x80 para primer disco duro)</param>
        /// <param name="cylinder">Número de cilindro</param>
        /// <param name="head">Número de cabezal</param>
        /// <param name="sector">Número de sector (1-63)</param>
        /// <param name="count">Número de sectores a leer/escribir</param>
        /// <param name="buffer">Buffer de datos</param>
        /// <returns>true si la operación tuvo éxito</returns>
        public static unsafe bool DiskOperation(byte function, byte drive, ushort cylinder, byte head, byte sector, byte count, void* buffer)
        {
            RegistersX86 regs = new RegistersX86();
            regs.AH = function;  // Función (0x02=leer, 0x03=escribir)
            regs.AL = count;     // Número de sectores
            regs.CH = (byte)(cylinder & 0xFF);  // 8 bits bajos del cilindro
            regs.CL = (byte)(((cylinder >> 8) & 0x03) << 6 | (sector & 0x3F));  // 2 bits altos del cilindro + sector
            regs.DH = head;      // Cabezal
            regs.DL = drive;     // Unidad
            regs.ES = (ushort)((uint)buffer >> 4);  // Segmento del buffer
            regs.BX = (ushort)((uint)buffer & 0xF); // Offset del buffer

            Int13h(ref regs);

            // CF=0 indica éxito, AH=0 indica sin error
            return (regs.EFLAGS & 0x01) == 0 && regs.AH == 0;
        }

        /// <summary>
        /// Resetea el sistema de disco
        /// </summary>
        /// <param name="drive">Número de unidad (0x00 para disquete, 0x80 para disco duro)</param>
        /// <returns>true si la operación tuvo éxito</returns>
        public static bool ResetDiskSystem(byte drive)
        {
            RegistersX86 regs = new RegistersX86();
            regs.AH = 0x00;  // Función 0x00: Resetear sistema de disco
            regs.DL = drive; // Unidad

            Int13h(ref regs);

            // CF=0 indica éxito, AH=0 indica sin error
            return (regs.EFLAGS & 0x01) == 0 && regs.AH == 0;
        }

        /// <summary>
        /// Obtiene parámetros de disco
        /// </summary>
        /// <param name="drive">Número de unidad (0x80 para primer disco duro)</param>
        /// <param name="cylinders">Variable donde se almacenará el número de cilindros</param>
        /// <param name="heads">Variable donde se almacenará el número de cabezales</param>
        /// <param name="sectors">Variable donde se almacenará el número de sectores por pista</param>
        /// <returns>true si la operación tuvo éxito</returns>
        public static bool GetDiskParameters(byte drive, out ushort cylinders, out byte heads, out byte sectors)
        {
            RegistersX86 regs = new RegistersX86();
            regs.AH = 0x08;  // Función 0x08: Obtener parámetros de disco
            regs.DL = drive; // Unidad

            Int13h(ref regs);

            // CF=0 indica éxito
            if ((regs.EFLAGS & 0x01) != 0)
            {
                cylinders = 0;
                heads = 0;
                sectors = 0;
                return false;
            }

            // Resultados: CH=bits bajos de cilindros, CL[7:6]=bits altos de cilindros, CL[5:0]=sectores, DH=cabezales
            cylinders = (ushort)(((regs.CL & 0xC0) << 2) | regs.CH);
            heads = (byte)(regs.DH + 1);  // El valor devuelto es el máximo índice, por lo que hay que sumar 1
            sectors = (byte)(regs.CL & 0x3F);

            return true;
        }
    }
}