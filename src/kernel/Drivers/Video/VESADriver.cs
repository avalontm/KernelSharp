using System;
using System.Runtime.InteropServices;
using Kernel.Drivers.IO;

namespace Kernel.Drivers.Video
{
    /// <summary>
    /// Driver para gráficos VESA/VBE que permite acceder a modos gráficos avanzados
    /// </summary>
    public unsafe class VESADriver
    {
        #region Estructuras VESA

        /// <summary>
        /// Información sobre el modo VESA (VBE 3.0)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct VBEModeInfo
        {
            // VBE 1.0+
            public ushort ModeAttributes;       // Atributos del modo
            public byte WinAAttributes;         // Atributos de la ventana A
            public byte WinBAttributes;         // Atributos de la ventana B
            public ushort WinGranularity;       // Granularidad de la ventana en KB
            public ushort WinSize;              // Tamaño de la ventana en KB
            public ushort WinASegment;          // Segmento de la ventana A
            public ushort WinBSegment;          // Segmento de la ventana B
            public uint WinFuncPtr;             // Puntero a función de cambio de ventana
            public ushort BytesPerScanLine;     // Bytes por línea de escaneo

            // VBE 1.2+
            public ushort XResolution;          // Ancho horizontal en píxeles
            public ushort YResolution;          // Altura vertical en píxeles
            public byte XCharSize;              // Ancho de carácter en píxeles
            public byte YCharSize;              // Altura de carácter en píxeles
            public byte NumberOfPlanes;         // Número de planos de memoria
            public byte BitsPerPixel;           // Bits por píxel
            public byte NumberOfBanks;          // Número de bancos
            public byte MemoryModel;            // Modelo de memoria
            public byte BankSize;               // Tamaño del banco en KB
            public byte NumberOfImagePages;     // Número de páginas de imagen
            public byte Reserved0;              // Reservado para compatibilidad con VBE 1.0

            // Campos de color directo
            public byte RedMaskSize;            // Tamaño de la máscara roja en bits
            public byte RedFieldPosition;       // Posición del campo rojo en bits
            public byte GreenMaskSize;          // Tamaño de la máscara verde en bits
            public byte GreenFieldPosition;     // Posición del campo verde en bits
            public byte BlueMaskSize;           // Tamaño de la máscara azul en bits
            public byte BlueFieldPosition;      // Posición del campo azul en bits
            public byte RsvdMaskSize;           // Tamaño de la máscara reservada en bits
            public byte RsvdFieldPosition;      // Posición del campo reservado en bits
            public byte DirectColorModeInfo;    // Información de modo de color directo

            // VBE 2.0+
            public uint PhysBasePtr;            // Puntero al inicio del framebuffer
            public uint OffScreenMemOffset;     // Inicio de la memoria fuera de pantalla
            public ushort OffScreenMemSize;     // Tamaño de la memoria fuera de pantalla en KB

            // VBE 3.0+
            public ushort LinBytesPerScanLine;  // Bytes por línea en modo lineal
            public byte BnkNumberOfImagePages;  // Número de páginas en modo banco
            public byte LinNumberOfImagePages;  // Número de páginas en modo lineal
            public byte LinRedMaskSize;         // Tamaño de la máscara roja en modo lineal
            public byte LinRedFieldPosition;    // Posición del campo rojo en modo lineal
            public byte LinGreenMaskSize;       // Tamaño de la máscara verde en modo lineal
            public byte LinGreenFieldPosition;  // Posición del campo verde en modo lineal
            public byte LinBlueMaskSize;        // Tamaño de la máscara azul en modo lineal
            public byte LinBlueFieldPosition;   // Posición del campo azul en modo lineal
            public byte LinRsvdMaskSize;        // Tamaño de la máscara reservada en modo lineal
            public byte LinRsvdFieldPosition;   // Posición del campo reservado en modo lineal
            public uint MaxPixelClock;          // Frecuencia máxima de píxel en Hz

            // Relleno
            public fixed byte Reserved1[190];   // Reservado para futuras extensiones
        }

        /// <summary>
        /// Información sobre la tarjeta VESA (VBE 3.0)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct VBEInfo
        {
            public fixed byte Signature[4];     // Firma "VESA"
            public ushort Version;              // Número de versión VBE
            public uint OemStringPtr;           // Puntero a cadena OEM
            public fixed byte Capabilities[4];  // Capacidades del controlador
            public uint VideoModePtr;           // Puntero a lista de modos
            public ushort TotalMemory;          // Memoria total en bloques de 64KB

            // VBE 2.0+
            public ushort OemSoftwareRev;       // Revisión del software OEM
            public uint OemVendorNamePtr;       // Puntero al nombre del vendedor OEM
            public uint OemProductNamePtr;      // Puntero al nombre del producto OEM
            public uint OemProductRevPtr;       // Puntero a la revisión del producto OEM

            // Relleno
            public fixed byte Reserved[222];    // Reservado para VESA
            public fixed byte OemData[256];     // Área de uso OEM
        }

        #endregion

        #region Constantes

        // Modos de memoria VBE
        private const byte VBE_MEMORYMODEL_TEXT = 0;
        private const byte VBE_MEMORYMODEL_CGA = 1;
        private const byte VBE_MEMORYMODEL_HERCULES = 2;
        private const byte VBE_MEMORYMODEL_PLANAR = 3;
        private const byte VBE_MEMORYMODEL_PACKED_PIXEL = 4;
        private const byte VBE_MEMORYMODEL_NON_CHAIN_4 = 5;
        private const byte VBE_MEMORYMODEL_DIRECT_COLOR = 6;
        private const byte VBE_MEMORYMODEL_YUV = 7;

        // Atributos de modo
        private const ushort VBE_MODE_ATTR_HARDWARE = 0x0001;
        private const ushort VBE_MODE_ATTR_TTY = 0x0004;
        private const ushort VBE_MODE_ATTR_COLOR = 0x0008;
        private const ushort VBE_MODE_ATTR_GRAPHICS = 0x0010;
        private const ushort VBE_MODE_ATTR_NOT_VGA = 0x0020;
        private const ushort VBE_MODE_ATTR_NOT_WINDOWED = 0x0040;
        private const ushort VBE_MODE_ATTR_LINEAR = 0x0080;

        // Flag para usar framebuffer lineal
        private const ushort VBE_USE_LINEAR_FRAMEBUFFER = 0x4000;

        #endregion

        private VBEModeInfo _modeInfo;
        private VBEInfo _vbeInfo;
        private byte* _frameBuffer;
        private bool _initialized;

        // Propiedades públicas para acceder a la información del modo
        public int Width => _modeInfo.XResolution;
        public int Height => _modeInfo.YResolution;
        public int BitsPerPixel => _modeInfo.BitsPerPixel;
        public int BytesPerPixel => _modeInfo.BitsPerPixel / 8;
        public int BytesPerScanLine => _modeInfo.LinBytesPerScanLine > 0 ? _modeInfo.LinBytesPerScanLine : _modeInfo.BytesPerScanLine;
        public uint FrameBufferAddress => _modeInfo.PhysBasePtr;
        public bool IsInitialized => _initialized;

        // Máscaras y desplazamientos de color
        public byte RedMaskSize => _modeInfo.LinRedMaskSize > 0 ? _modeInfo.LinRedMaskSize : _modeInfo.RedMaskSize;
        public byte RedFieldPosition => _modeInfo.LinRedFieldPosition > 0 ? _modeInfo.LinRedFieldPosition : _modeInfo.RedFieldPosition;
        public byte GreenMaskSize => _modeInfo.LinGreenMaskSize > 0 ? _modeInfo.LinGreenMaskSize : _modeInfo.GreenMaskSize;
        public byte GreenFieldPosition => _modeInfo.LinGreenFieldPosition > 0 ? _modeInfo.LinGreenFieldPosition : _modeInfo.GreenFieldPosition;
        public byte BlueMaskSize => _modeInfo.LinBlueMaskSize > 0 ? _modeInfo.LinBlueMaskSize : _modeInfo.BlueMaskSize;
        public byte BlueFieldPosition => _modeInfo.LinBlueFieldPosition > 0 ? _modeInfo.LinBlueFieldPosition : _modeInfo.BlueFieldPosition;

        /// <summary>
        /// Constructor del driver VESA. Intenta inicializar con una resolución predeterminada.
        /// </summary>
        public VESADriver() : this(800, 600, 32)
        {
        }

        /// <summary>
        /// Constructor del driver VESA que intenta establecer un modo con la resolución y profundidad de color especificadas.
        /// </summary>
        /// <param name="width">Ancho deseado en píxeles</param>
        /// <param name="height">Altura deseada en píxeles</param>
        /// <param name="bpp">Bits por píxel deseados (8, 16, 24, 32)</param>
        public VESADriver(int width, int height, int bpp)
        {
            _initialized = false;

            // Inicializar memoria para VBEInfo
            IntPtr vbeInfoPtr = MarshalHelper.AllocateTemporaryMemory(sizeof(VBEInfo));

            try
            {
                // Obtener información VBE
                if (!GetVBEInfo((VBEInfo*)vbeInfoPtr))
                {
                    Console.WriteLine("Error: No se pudo obtener información VESA/VBE");
                    return;
                }

                // Copiar la información VBE
                _vbeInfo = *(VBEInfo*)vbeInfoPtr;

                // Buscar el mejor modo disponible
                ushort bestMode = FindBestMode(width, height, bpp);
                if (bestMode == 0xFFFF)
                {
                    Console.WriteLine("Error: No se encontró un modo VESA compatible");
                    return;
                }

                // Inicializar memoria para ModeInfo
                IntPtr modeInfoPtr = MarshalHelper.AllocateTemporaryMemory(sizeof(VBEModeInfo));

                try
                {
                    // Obtener información del modo
                    if (!GetModeInfo(bestMode, (VBEModeInfo*)modeInfoPtr))
                    {
                        Console.WriteLine("Error: No se pudo obtener información del modo VESA");
                        return;
                    }

                    // Copiar la información del modo
                    _modeInfo = *(VBEModeInfo*)modeInfoPtr;

                    // Establecer el modo
                    if (!SetVBEMode(bestMode))
                    {
                        Console.WriteLine("Error: No se pudo establecer el modo VESA");
                        return;
                    }

                    // Mapear el framebuffer
                    _frameBuffer = (byte*)_modeInfo.PhysBasePtr;

                    // Verificar que se haya establecido correctamente
                    if (_frameBuffer == null || _modeInfo.PhysBasePtr == 0)
                    {
                        Console.WriteLine("Error: No se pudo mapear el framebuffer VESA");
                        return;
                    }

                    _initialized = true;

                    // Limpiar la pantalla
                    Clear(0);

                    Console.WriteLine($"Modo VESA establecido: {Width}x{Height}x{BitsPerPixel}");
                }
                finally
                {
                    MarshalHelper.FreeTemporaryMemory(modeInfoPtr);
                }
            }
            finally
            {
                MarshalHelper.FreeTemporaryMemory(vbeInfoPtr);
            }
        }

        /// <summary>
        /// Obtiene información de la tarjeta VESA/VBE
        /// </summary>
        /// <param name="info">Puntero a la estructura VBEInfo a rellenar</param>
        /// <returns>true si se obtuvo la información correctamente</returns>
        private bool GetVBEInfo(VBEInfo* info)
        {

            // Preparar la estructura de información VBE
            byte* sig = info->Signature;
            sig[0] = (byte)'V';
            sig[1] = (byte)'B';
            sig[2] = (byte)'E';
            sig[3] = (byte)'2';

            // Llamar a la interrupción BIOS
            RegistersX86 regs = new RegistersX86();
            regs.AX = 0x4F00;  // Función 00h: Obtener información del controlador VBE
            regs.ES = (ushort)((uint)info >> 4);
            regs.DI = (ushort)((uint)info & 0xF);

            // Ejecutar interrupción 0x10
            BIOS.Int10h(ref regs);

            // Verificar resultado
            if ((regs.AX & 0xFF) != 0x4F || ((regs.AX >> 8) & 0xFF) != 0)
                return false;

            // Verificar firma "VESA"
            if (info->Signature[0] != 'V' || info->Signature[1] != 'E' ||
                info->Signature[2] != 'S' || info->Signature[3] != 'A')
                return false;

            return true;

        }

        /// <summary>
        /// Obtiene información de un modo VESA específico
        /// </summary>
        /// <param name="mode">Número de modo VESA</param>
        /// <param name="info">Puntero a la estructura VBEModeInfo a rellenar</param>
        /// <returns>true si se obtuvo la información correctamente</returns>
        private bool GetModeInfo(ushort mode, VBEModeInfo* info)
        {
            try
            {
                // Llamar a la interrupción BIOS
                RegistersX86 regs = new RegistersX86();
                regs.AX = 0x4F01;      // Función 01h: Obtener información de modo
                regs.CX = mode;        // Modo a consultar
                regs.ES = (ushort)((uint)info >> 4);
                regs.DI = (ushort)((uint)info & 0xF);

                // Ejecutar interrupción 0x10
                BIOS.Int10h(ref regs);

                // Verificar resultado
                if ((regs.AX & 0xFF) != 0x4F || ((regs.AX >> 8) & 0xFF) != 0)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Establece un modo VESA específico
        /// </summary>
        /// <param name="mode">Número de modo VESA</param>
        /// <returns>true si se estableció el modo correctamente</returns>
        private bool SetVBEMode(ushort mode)
        {
            try
            {
                // Solicitar modo con framebuffer lineal
                mode |= VBE_USE_LINEAR_FRAMEBUFFER;

                // Llamar a la interrupción BIOS
                RegistersX86 regs = new RegistersX86();
                regs.AX = 0x4F02;      // Función 02h: Establecer modo
                regs.BX = mode;        // Modo a establecer con framebuffer lineal

                // Ejecutar interrupción 0x10
                BIOS.Int10h(ref regs);

                // Verificar resultado
                if ((regs.AX & 0xFF) != 0x4F || ((regs.AX >> 8) & 0xFF) != 0)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Busca el mejor modo VESA que coincida con los parámetros especificados
        /// </summary>
        /// <param name="width">Ancho deseado en píxeles</param>
        /// <param name="height">Altura deseada en píxeles</param>
        /// <param name="bpp">Bits por píxel deseados</param>
        /// <returns>Número de modo VESA o 0xFFFF si no se encontró ninguno compatible</returns>
        private ushort FindBestMode(int width, int height, int bpp)
        {
            ushort bestMode = 0xFFFF;
            int bestScore = -1;

            try
            {
                IntPtr modeInfoPtr = MarshalHelper.AllocateTemporaryMemory(sizeof(VBEModeInfo));

                try
                {
                    // Obtener el puntero a la lista de modos
                    ushort* modes = (ushort*)ConvertSegmentedToLinear(_vbeInfo.VideoModePtr);

                    // Si no podemos acceder a la lista de modos, fallamos
                    if (modes == null)
                        return 0xFFFF;

                    // Recorrer la lista de modos (termina con 0xFFFF)
                    for (int i = 0; modes[i] != 0xFFFF; i++)
                    {
                        ushort currentMode = modes[i];

                        // Obtener información del modo
                        if (!GetModeInfo(currentMode, (VBEModeInfo*)modeInfoPtr))
                            continue;

                        VBEModeInfo modeInfo = *(VBEModeInfo*)modeInfoPtr;

                        // Verificar si el modo es compatible: debe ser gráfico y tener framebuffer lineal
                        if ((modeInfo.ModeAttributes & (VBE_MODE_ATTR_GRAPHICS | VBE_MODE_ATTR_LINEAR)) !=
                            (VBE_MODE_ATTR_GRAPHICS | VBE_MODE_ATTR_LINEAR))
                            continue;

                        // El modo debe ser de color directo para 16bpp, 24bpp o 32bpp
                        if (modeInfo.BitsPerPixel >= 16 && modeInfo.MemoryModel != VBE_MEMORYMODEL_DIRECT_COLOR)
                            continue;

                        // El framebuffer debe ser accesible
                        if (modeInfo.PhysBasePtr == 0)
                            continue;

                        // Calcular la puntuación del modo según la cercanía a las dimensiones deseadas
                        int score = 0;

                        // Coincidencia exacta de dimensiones
                        if (modeInfo.XResolution == width && modeInfo.YResolution == height)
                            score += 1000;
                        else
                        {
                            // Penalizar por diferencia de dimensiones
                            score -= Math.Abs(modeInfo.XResolution - width) / 10;
                            score -= Math.Abs(modeInfo.YResolution - height) / 10;

                            // Preferir resoluciones más grandes que las solicitadas
                            if (modeInfo.XResolution >= width && modeInfo.YResolution >= height)
                                score += 500;
                        }

                        // Coincidencia exacta de BPP
                        if (modeInfo.BitsPerPixel == bpp)
                            score += 500;
                        else
                        {
                            // Penalizar por diferencia de BPP
                            score -= Math.Abs(modeInfo.BitsPerPixel - bpp) * 10;
                        }

                        // Preferir 32bpp para mayor calidad de color
                        if (modeInfo.BitsPerPixel == 32)
                            score += 100;
                        else if (modeInfo.BitsPerPixel == 24)
                            score += 50;
                        else if (modeInfo.BitsPerPixel == 16)
                            score += 25;

                        // Actualizar el mejor modo si encontramos uno con mayor puntuación
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestMode = currentMode;

                            // Si es una coincidencia exacta, terminamos la búsqueda
                            if (modeInfo.XResolution == width && modeInfo.YResolution == height &&
                                modeInfo.BitsPerPixel == bpp)
                                break;
                        }
                    }
                }
                finally
                {
                    MarshalHelper.FreeTemporaryMemory(modeInfoPtr);
                }
            }
            catch
            {
                return 0xFFFF;
            }

            return bestMode;
        }

        /// <summary>
        /// Convierte una dirección de memoria segmentada (segmento:offset) a dirección lineal
        /// </summary>
        private void* ConvertSegmentedToLinear(uint segOffset)
        {
            // Una dirección segmentada tiene formato SEGMENT:OFFSET
            // La dirección lineal se calcula como: SEGMENT * 16 + OFFSET
            ushort segment = (ushort)(segOffset >> 16);
            ushort offset = (ushort)(segOffset & 0xFFFF);
            return (void*)((uint)segment * 16 + offset);
        }

        #region Operaciones Gráficas

        /// <summary>
        /// Establece un píxel en coordenadas específicas
        /// </summary>
        /// <param name="x">Coordenada X</param>
        /// <param name="y">Coordenada Y</param>
        /// <param name="color">Color en formato ARGB (el canal A se ignora)</param>
        public void SetPixel(int x, int y, uint color)
        {
            if (!_initialized || x < 0 || x >= Width || y < 0 || y >= Height)
                return;

            int offset = y * BytesPerScanLine + x * BytesPerPixel;

            // Escribir color según profundidad
            switch (BytesPerPixel)
            {
                case 1: // 8bpp (paleta de colores)
                    _frameBuffer[offset] = (byte)color;
                    break;

                case 2: // 16bpp
                    ushort color16 = 0;
                    // Extraer componentes RGB (cada uno de 8 bits)
                    byte r = (byte)((color >> 16) & 0xFF);
                    byte g = (byte)((color >> 8) & 0xFF);
                    byte b = (byte)(color & 0xFF);

                    // Convertir a formato de 16 bits según las máscaras
                    color16 |= (ushort)((r >> (8 - RedMaskSize)) << RedFieldPosition);
                    color16 |= (ushort)((g >> (8 - GreenMaskSize)) << GreenFieldPosition);
                    color16 |= (ushort)((b >> (8 - BlueMaskSize)) << BlueFieldPosition);

                    *(ushort*)(_frameBuffer + offset) = color16;
                    break;

                case 3: // 24bpp
                    _frameBuffer[offset] = (byte)(color & 0xFF);           // Azul
                    _frameBuffer[offset + 1] = (byte)((color >> 8) & 0xFF); // Verde
                    _frameBuffer[offset + 2] = (byte)((color >> 16) & 0xFF); // Rojo
                    break;

                case 4: // 32bpp
                    *(uint*)(_frameBuffer + offset) = color;
                    break;
            }
        }

        /// <summary>
        /// Obtiene el color de un píxel en coordenadas específicas
        /// </summary>
        /// <param name="x">Coordenada X</param>
        /// <param name="y">Coordenada Y</param>
        /// <returns>Color en formato ARGB</returns>
        public uint GetPixel(int x, int y)
        {
            if (!_initialized || x < 0 || x >= Width || y < 0 || y >= Height)
                return 0;

            int offset = y * BytesPerScanLine + x * BytesPerPixel;
            uint color = 0;

            // Leer color según profundidad
            switch (BytesPerPixel)
            {
                case 1: // 8bpp (paleta de colores)
                    color = _frameBuffer[offset];
                    break;

                case 2: // 16bpp
                    ushort color16 = *(ushort*)(_frameBuffer + offset);

                    // Extraer componentes según máscaras
                    byte r = (byte)(((color16 >> RedFieldPosition) & ((1 << RedMaskSize) - 1)) << (8 - RedMaskSize));
                    byte g = (byte)(((color16 >> GreenFieldPosition) & ((1 << GreenMaskSize) - 1)) << (8 - GreenMaskSize));
                    byte b = (byte)(((color16 >> BlueFieldPosition) & ((1 << BlueMaskSize) - 1)) << (8 - BlueMaskSize));

                    // Formar color ARGB
                    color = (uint)((r << 16) | (g << 8) | b);
                    break;

                case 3: // 24bpp
                    byte b3 = _frameBuffer[offset];
                    byte g3 = _frameBuffer[offset + 1];
                    byte r3 = _frameBuffer[offset + 2];
                    color = (uint)((r3 << 16) | (g3 << 8) | b3);
                    break;

                case 4: // 32bpp
                    color = *(uint*)(_frameBuffer + offset);
                    break;
            }

            return color;
        }

        /// <summary>
        /// Limpia la pantalla con un color específico
        /// </summary>
        /// <param name="color">Color en formato ARGB (el canal A se ignora)</param>
        public void Clear(uint color)
        {
            if (!_initialized)
                return;

            // Para eficiencia, usamos diferentes métodos según la profundidad de color
            int totalPixels = Width * Height;

            switch (BytesPerPixel)
            {
                case 1: // 8bpp
                    Native.Memset(_frameBuffer, (byte)color, (uint)(totalPixels * BytesPerPixel));
                    break;

                case 2: // 16bpp
                    ushort color16 = 0;
                    // Convertir ARGB a formato 16bpp
                    byte r = (byte)((color >> 16) & 0xFF);
                    byte g = (byte)((color >> 8) & 0xFF);
                    byte b = (byte)(color & 0xFF);

                    color16 |= (ushort)((r >> (8 - RedMaskSize)) << RedFieldPosition);
                    color16 |= (ushort)((g >> (8 - GreenMaskSize)) << GreenFieldPosition);
                    color16 |= (ushort)((b >> (8 - BlueMaskSize)) << BlueFieldPosition);

                    for (int i = 0; i < totalPixels; i++)
                    {
                        ((ushort*)_frameBuffer)[i] = color16;
                    }
                    break;

                case 3: // 24bpp
                    byte r24 = (byte)((color >> 16) & 0xFF);
                    byte g24 = (byte)((color >> 8) & 0xFF);
                    byte b24 = (byte)(color & 0xFF);

                    for (int i = 0; i < totalPixels; i++)
                    {
                        int offset = i * 3;
                        _frameBuffer[offset] = b24;
                        _frameBuffer[offset + 1] = g24;
                        _frameBuffer[offset + 2] = r24;
                    }
                    break;

                case 4: // 32bpp
                    for (int i = 0; i < totalPixels; i++)
                    {
                        ((uint*)_frameBuffer)[i] = color;
                    }
                    break;
            }
        }

        /// <summary>
        /// Dibuja una línea horizontal
        /// </summary>
        /// <param name="x">Coordenada X inicial</param>
        /// <param name="y">Coordenada Y</param>
        /// <param name="length">Longitud en píxeles</param>
        /// <param name="color">Color en formato ARGB</param>
        public void DrawHorizontalLine(int x, int y, int length, uint color)
        {
            if (!_initialized || y < 0 || y >= Height)
                return;

            // Recortar al área visible
            if (x < 0)
            {
                length += x;
                x = 0;
            }

            if (x + length > Width)
            {
                length = Width - x;
            }

            if (length <= 0)
                return;

            // Calcular offset inicial
            int offset = y * BytesPerScanLine + x * BytesPerPixel;

            // Dibujar la línea según profundidad
            switch (BytesPerPixel)
            {
                case 1: // 8bpp
                    Native.Memset(_frameBuffer + offset, (byte)color, (uint)length);
                    break;

                case 2: // 16bpp
                    ushort color16 = 0;
                    // Convertir ARGB a formato 16bpp
                    byte r = (byte)((color >> 16) & 0xFF);
                    byte g = (byte)((color >> 8) & 0xFF);
                    byte b = (byte)(color & 0xFF);

                    color16 |= (ushort)((r >> (8 - RedMaskSize)) << RedFieldPosition);
                    color16 |= (ushort)((g >> (8 - GreenMaskSize)) << GreenFieldPosition);
                    color16 |= (ushort)((b >> (8 - BlueMaskSize)) << BlueFieldPosition);

                    for (int i = 0; i < length; i++)
                    {
                        *(ushort*)(_frameBuffer + offset + i * 2) = color16;
                    }
                    break;

                case 3: // 24bpp
                    byte r24 = (byte)((color >> 16) & 0xFF);
                    byte g24 = (byte)((color >> 8) & 0xFF);
                    byte b24 = (byte)(color & 0xFF);

                    for (int i = 0; i < length; i++)
                    {
                        int pixelOffset = offset + i * 3;
                        _frameBuffer[pixelOffset] = b24;
                        _frameBuffer[pixelOffset + 1] = g24;
                        _frameBuffer[pixelOffset + 2] = r24;
                    }
                    break;

                case 4: // 32bpp
                    for (int i = 0; i < length; i++)
                    {
                        *(uint*)(_frameBuffer + offset + i * 4) = color;
                    }
                    break;
            }
        }

        /// <summary>
        /// Dibuja una línea vertical
        /// </summary>
        /// <param name="x">Coordenada X</param>
        /// <param name="y">Coordenada Y inicial</param>
        /// <param name="height">Altura en píxeles</param>
        /// <param name="color">Color en formato ARGB</param>
        public void DrawVerticalLine(int x, int y, int height, uint color)
        {
            if (!_initialized || x < 0 || x >= Width)
                return;

            // Recortar al área visible
            if (y < 0)
            {
                height += y;
                y = 0;
            }

            if (y + height > Height)
            {
                height = Height - y;
            }

            if (height <= 0)
                return;

            // Dibujar la línea píxel por píxel
            for (int i = 0; i < height; i++)
            {
                SetPixel(x, y + i, color);
            }
        }

        /// <summary>
        /// Dibuja un rectángulo
        /// </summary>
        /// <param name="x">Coordenada X</param>
        /// <param name="y">Coordenada Y</param>
        /// <param name="width">Ancho en píxeles</param>
        /// <param name="height">Altura en píxeles</param>
        /// <param name="color">Color en formato ARGB</param>
        public void DrawRectangle(int x, int y, int width, int height, uint color)
        {
            DrawHorizontalLine(x, y, width, color);
            DrawHorizontalLine(x, y + height - 1, width, color);
            DrawVerticalLine(x, y, height, color);
            DrawVerticalLine(x + width - 1, y, height, color);
        }

        /// <summary>
        /// Dibuja un rectángulo relleno
        /// </summary>
        /// <param name="x">Coordenada X</param>
        /// <param name="y">Coordenada Y</param>
        /// <param name="width">Ancho en píxeles</param>
        /// <param name="height">Altura en píxeles</param>
        /// <param name="color">Color en formato ARGB</param>
        public void FillRectangle(int x, int y, int width, int height, uint color)
        {
            if (!_initialized)
                return;

            // Recortar al área visible
            if (x < 0)
            {
                width += x;
                x = 0;
            }

            if (y < 0)
            {
                height += y;
                y = 0;
            }

            if (x + width > Width)
            {
                width = Width - x;
            }

            if (y + height > Height)
            {
                height = Height - y;
            }

            if (width <= 0 || height <= 0)
                return;

            // Dibujar líneas horizontales para cada fila
            for (int j = 0; j < height; j++)
            {
                DrawHorizontalLine(x, y + j, width, color);
            }
        }

        /// <summary>
        /// Implementación del algoritmo de Bresenham para líneas
        /// </summary>
        /// <param name="x0">Coordenada X inicial</param>
        /// <param name="y0">Coordenada Y inicial</param>
        /// <param name="x1">Coordenada X final</param>
        /// <param name="y1">Coordenada Y final</param>
        /// <param name="color">Color en formato ARGB</param>
        public void DrawLine(int x0, int y0, int x1, int y1, uint color)
        {
            if (!_initialized)
                return;

            // Optimización para líneas horizontales y verticales
            if (y0 == y1)
            {
                DrawHorizontalLine(Math.Min(x0, x1), y0, Math.Abs(x1 - x0) + 1, color);
                return;
            }

            if (x0 == x1)
            {
                DrawVerticalLine(x0, Math.Min(y0, y1), Math.Abs(y1 - y0) + 1, color);
                return;
            }

            // Algoritmo de Bresenham para líneas
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                SetPixel(x0, y0, color);

                if (x0 == x1 && y0 == y1)
                    break;

                int e2 = 2 * err;

                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        /// <summary>
        /// Dibuja un círculo utilizando el algoritmo de Bresenham
        /// </summary>
        /// <param name="xc">Coordenada X del centro</param>
        /// <param name="yc">Coordenada Y del centro</param>
        /// <param name="radius">Radio en píxeles</param>
        /// <param name="color">Color en formato ARGB</param>
        public void DrawCircle(int xc, int yc, int radius, uint color)
        {
            if (!_initialized || radius <= 0)
                return;

            int x = 0;
            int y = radius;
            int d = 3 - 2 * radius;

            while (x <= y)
            {
                // Dibujar los 8 octantes
                SetPixel(xc + x, yc + y, color);
                SetPixel(xc - x, yc + y, color);
                SetPixel(xc + x, yc - y, color);
                SetPixel(xc - x, yc - y, color);
                SetPixel(xc + y, yc + x, color);
                SetPixel(xc - y, yc + x, color);
                SetPixel(xc + y, yc - x, color);
                SetPixel(xc - y, yc - x, color);

                if (d < 0)
                {
                    d += 4 * x + 6;
                }
                else
                {
                    d += 4 * (x - y) + 10;
                    y--;
                }
                x++;
            }
        }

        /// <summary>
        /// Dibuja un círculo relleno
        /// </summary>
        /// <param name="xc">Coordenada X del centro</param>
        /// <param name="yc">Coordenada Y del centro</param>
        /// <param name="radius">Radio en píxeles</param>
        /// <param name="color">Color en formato ARGB</param>
        public void FillCircle(int xc, int yc, int radius, uint color)
        {
            if (!_initialized || radius <= 0)
                return;

            int x = 0;
            int y = radius;
            int d = 3 - 2 * radius;

            while (x <= y)
            {
                // Dibujar líneas horizontales entre los puntos
                DrawHorizontalLine(xc - x, yc + y, 2 * x + 1, color);
                DrawHorizontalLine(xc - x, yc - y, 2 * x + 1, color);
                DrawHorizontalLine(xc - y, yc + x, 2 * y + 1, color);
                DrawHorizontalLine(xc - y, yc - x, 2 * y + 1, color);

                if (d < 0)
                {
                    d += 4 * x + 6;
                }
                else
                {
                    d += 4 * (x - y) + 10;
                    y--;
                }
                x++;
            }
        }

        /// <summary>
        /// Copia un bloque de píxeles a otra ubicación
        /// </summary>
        /// <param name="srcX">Coordenada X origen</param>
        /// <param name="srcY">Coordenada Y origen</param>
        /// <param name="dstX">Coordenada X destino</param>
        /// <param name="dstY">Coordenada Y destino</param>
        /// <param name="width">Ancho del bloque</param>
        /// <param name="height">Altura del bloque</param>
        public void CopyBlock(int srcX, int srcY, int dstX, int dstY, int width, int height)
        {
            if (!_initialized)
                return;

            // Recortar a los límites de la pantalla
            if (srcX < 0)
            {
                width += srcX;
                dstX -= srcX;
                srcX = 0;
            }

            if (srcY < 0)
            {
                height += srcY;
                dstY -= srcY;
                srcY = 0;
            }

            if (dstX < 0)
            {
                width += dstX;
                srcX -= dstX;
                dstX = 0;
            }

            if (dstY < 0)
            {
                height += dstY;
                srcY -= dstY;
                dstY = 0;
            }

            if (srcX + width > Width)
                width = Width - srcX;

            if (srcY + height > Height)
                height = Height - srcY;

            if (dstX + width > Width)
                width = Width - dstX;

            if (dstY + height > Height)
                height = Height - dstY;

            if (width <= 0 || height <= 0)
                return;

            // Determinar dirección de copia (importante para evitar solapamiento)
            int srcOffset, dstOffset, yStep, yEnd;

            if (dstY > srcY)
            {
                // Copiar de abajo hacia arriba
                yStep = -1;
                yEnd = -1;
                srcY = srcY + height - 1;
                dstY = dstY + height - 1;
            }
            else
            {
                // Copiar de arriba hacia abajo
                yStep = 1;
                yEnd = height;
            }

            for (int y = 0; y != yEnd; y += yStep)
            {
                srcOffset = (srcY + y) * BytesPerScanLine + srcX * BytesPerPixel;
                dstOffset = (dstY + y) * BytesPerScanLine + dstX * BytesPerPixel;
                Native.Memcpy(_frameBuffer + dstOffset, _frameBuffer + srcOffset, (uint)(width * BytesPerPixel));
            }
        }

        /// <summary>
        /// Dibuja un carácter en la pantalla utilizando una fuente simple
        /// </summary>
        /// <param name="x">Coordenada X</param>
        /// <param name="y">Coordenada Y</param>
        /// <param name="c">Carácter a dibujar</param>
        /// <param name="color">Color del texto</param>
        /// <param name="backgroundColor">Color de fondo (transparente si es 0)</param>
        public void DrawChar(int x, int y, char c, uint color, uint backgroundColor = 0)
        {
            if (!_initialized || x < 0 || y < 0 || x >= Width - 8 || y >= Height - 16)
                return;

            // Aquí se podría implementar un sistema de fuentes personalizado
            // Por ahora, usaremos una fuente básica 8x16 incrustada

            byte[] fontData = GetFontData(c);
            if (fontData == null)
                return;

            for (int row = 0; row < 16; row++)
            {
                byte rowData = fontData[row];

                for (int col = 0; col < 8; col++)
                {
                    bool pixelSet = (rowData & (0x80 >> col)) != 0;

                    if (pixelSet)
                    {
                        SetPixel(x + col, y + row, color);
                    }
                    else if (backgroundColor != 0)
                    {
                        SetPixel(x + col, y + row, backgroundColor);
                    }
                }
            }
        }

        /// <summary>
        /// Dibuja una cadena de texto en la pantalla
        /// </summary>
        /// <param name="x">Coordenada X</param>
        /// <param name="y">Coordenada Y</param>
        /// <param name="text">Texto a dibujar</param>
        /// <param name="color">Color del texto</param>
        /// <param name="backgroundColor">Color de fondo (transparente si es 0)</param>
        public void DrawString(int x, int y, string text, uint color, uint backgroundColor = 0)
        {
            if (!_initialized || text == null)
                return;

            int currentX = x;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '\n')
                {
                    // Salto de línea
                    currentX = x;
                    y += 16; // Altura de la fuente
                    continue;
                }

                if (c == '\r')
                {
                    // Retorno de carro
                    currentX = x;
                    continue;
                }

                if (c == '\t')
                {
                    // Tabulador (4 espacios)
                    currentX += 8 * 4;
                    continue;
                }

                DrawChar(currentX, y, c, color, backgroundColor);
                currentX += 8; // Ancho de la fuente

                // Comprobar si llegamos al borde de la pantalla
                if (currentX >= Width - 8)
                {
                    currentX = x;
                    y += 16; // Altura de la fuente
                }
            }
        }

        /// <summary>
        /// Obtiene los datos de fuente para un carácter
        /// </summary>
        /// <param name="c">Carácter</param>
        /// <returns>Array de bytes con los datos de fuente (null si no está disponible)</returns>
        private byte[] GetFontData(char c)
        {
            // Esta es una implementación básica con una fuente limitada
            // En un sistema real, se cargaría una fuente completa desde un archivo

            // Solo implementamos los caracteres ASCII básicos (32-127)
            if (c < 32 || c > 127)
                c = '?';

            // Podríamos tener una fuente embebida aquí o cargarla desde un recurso
            // Por ahora, usamos un placeholder para algunos caracteres comunes

            // Ejemplo para algunos caracteres básicos (incompleto)
            switch (c)
            {
                case 'A':
                    return new byte[] {
                        0x00, 0x18, 0x24, 0x42, 0x42, 0x7E, 0x42, 0x42,
                        0x42, 0x42, 0x42, 0x00, 0x00, 0x00, 0x00, 0x00
                    };
                case 'B':
                    return new byte[] {
                        0x00, 0x7C, 0x42, 0x42, 0x42, 0x7C, 0x42, 0x42,
                        0x42, 0x42, 0x7C, 0x00, 0x00, 0x00, 0x00, 0x00
                    };
                // Añadir más caracteres según sea necesario

                default:
                    // Carácter genérico para el resto (un bloque)
                    return new byte[] {
                        0x00, 0x00, 0x7E, 0x7E, 0x7E, 0x7E, 0x7E, 0x7E,
                        0x7E, 0x7E, 0x7E, 0x00, 0x00, 0x00, 0x00, 0x00
                    };
            }
        }

        #endregion
    }
}