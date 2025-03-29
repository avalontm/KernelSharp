using System.Runtime.InteropServices;
using System;
using Kernel.Drivers.IO;

namespace Kernel.Drivers
{
    /// <summary>
    /// Driver para modo gráfico VGA (320x200 con 256 colores)
    /// </summary>
    public unsafe class VGAGraphicsDriver
    {
        // Constantes para el modo gráfico VGA
        private const int WIDTH = 320;
        private const int HEIGHT = 200;
        private const int BUFFER_ADDRESS = 0xA0000;

        // Buffer de video
        private byte* _buffer;

        // Paleta de colores VGA
        private byte[] _palette;

        /// <summary>
        /// Constructor del driver gráfico VGA
        /// </summary>
        public VGAGraphicsDriver()
        {
            _buffer = (byte*)BUFFER_ADDRESS;
            _palette = new byte[256 * 3]; // 256 colores, 3 componentes por color (RGB)

            // Inicializar el modo gráfico
            InitializeGraphicsMode();

            // Configurar la paleta por defecto
            SetDefaultPalette();

            // Limpiar la pantalla
            Clear(0); // Negro (índice 0)
        }

        /// <summary>
        /// Inicializa el modo gráfico VGA 13h (320x200, 256 colores)
        /// </summary>
        private void InitializeGraphicsMode()
        {
            // Configurar el modo 13h mediante la interrupción BIOS
            // Esto se debe hacer con código de ensamblador
            // En un kernel real, probablemente querrás configurar el VGA directamente sin BIOS

            // Aquí hay una implementación básica usando una función externa
            SetVGAMode(0x13); // Modo 13h
        }

        /// <summary>
        /// Configura la paleta por defecto
        /// </summary>
        private void SetDefaultPalette()
        {
            // Configurar los primeros 16 colores estándar EGA
            byte[,] egaColors = {
                {0, 0, 0},       // 0: Negro
                {0, 0, 170},     // 1: Azul
                {0, 170, 0},     // 2: Verde
                {0, 170, 170},   // 3: Cyan
                {170, 0, 0},     // 4: Rojo
                {170, 0, 170},   // 5: Magenta
                {170, 85, 0},    // 6: Marrón
                {170, 170, 170}, // 7: Gris claro
                {85, 85, 85},    // 8: Gris oscuro
                {85, 85, 255},   // 9: Azul claro
                {85, 255, 85},   // 10: Verde claro
                {85, 255, 255},  // 11: Cyan claro
                {255, 85, 85},   // 12: Rojo claro
                {255, 85, 255},  // 13: Magenta claro
                {255, 255, 85},  // 14: Amarillo
                {255, 255, 255}  // 15: Blanco
            };

            // Asignar los colores EGA
            for (int i = 0; i < 16; i++)
            {
                _palette[i * 3] = egaColors[i, 0];
                _palette[i * 3 + 1] = egaColors[i, 1];
                _palette[i * 3 + 2] = egaColors[i, 2];
            }

            // Asignar una gradiente para los colores restantes
            for (int i = 16; i < 256; i++)
            {
                // Crear una gradiente simple
                _palette[i * 3] = (byte)(i % 8 * 36);
                _palette[i * 3 + 1] = (byte)(i / 8 % 8 * 36);
                _palette[i * 3 + 2] = (byte)(i / 64 % 4 * 85);
            }

            // Aplicar la paleta al hardware
            UpdatePalette();
        }

        /// <summary>
        /// Actualiza la paleta en el hardware VGA
        /// </summary>
        private void UpdatePalette()
        {
            // DAC Address Write Mode Register - índice inicial
            IOPort.OutByte(0x3C8, 0);

            // Escribir los datos RGB de la paleta al Data Register
            for (int i = 0; i < 256 * 3; i++)
            {
                // Los valores RGB de VGA son de 6 bits (0-63)
                IOPort.OutByte(0x3C9, (byte)(_palette[i] >> 2));
            }
        }

        /// <summary>
        /// Establece un color específico en la paleta
        /// </summary>
        public void SetPaletteColor(byte index, byte r, byte g, byte b)
        {
            _palette[index * 3] = r;
            _palette[index * 3 + 1] = g;
            _palette[index * 3 + 2] = b;

            // Actualizar solo este color en el hardware
            IOPort.OutByte(0x3C8, index);
            IOPort.OutByte(0x3C9, (byte)(r >> 2));
            IOPort.OutByte(0x3C9, (byte)(g >> 2));
            IOPort.OutByte(0x3C9, (byte)(b >> 2));
        }

        /// <summary>
        /// Limpia la pantalla con un color específico
        /// </summary>
        public void Clear(byte colorIndex)
        {
            for (int i = 0; i < WIDTH * HEIGHT; i++)
            {
                _buffer[i] = colorIndex;
            }
        }

        /// <summary>
        /// Establece un pixel en una coordenada específica
        /// </summary>
        public void SetPixel(int x, int y, byte colorIndex)
        {
            if (x >= 0 && x < WIDTH && y >= 0 && y < HEIGHT)
            {
                _buffer[y * WIDTH + x] = colorIndex;
            }
        }

        /// <summary>
        /// Dibuja una línea horizontal
        /// </summary>
        public void DrawHLine(int x, int y, int width, byte colorIndex)
        {
            for (int i = 0; i < width; i++)
            {
                SetPixel(x + i, y, colorIndex);
            }
        }

        /// <summary>
        /// Dibuja una línea vertical
        /// </summary>
        public void DrawVLine(int x, int y, int height, byte colorIndex)
        {
            for (int i = 0; i < height; i++)
            {
                SetPixel(x, y + i, colorIndex);
            }
        }

        /// <summary>
        /// Dibuja un rectángulo
        /// </summary>
        public void DrawRect(int x, int y, int width, int height, byte colorIndex)
        {
            DrawHLine(x, y, width, colorIndex);
            DrawHLine(x, y + height - 1, width, colorIndex);
            DrawVLine(x, y, height, colorIndex);
            DrawVLine(x + width - 1, y, height, colorIndex);
        }

        /// <summary>
        /// Dibuja un rectángulo relleno
        /// </summary>
        public void FillRect(int x, int y, int width, int height, byte colorIndex)
        {
            for (int j = 0; j < height; j++)
            {
                DrawHLine(x, y + j, width, colorIndex);
            }
        }

        /// <summary>
        /// Implementación básica del algoritmo de Bresenham para líneas
        /// </summary>
        public void DrawLine(int x0, int y0, int x1, int y1, byte colorIndex)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                SetPixel(x0, y0, colorIndex);

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
        public void DrawCircle(int x0, int y0, int radius, byte colorIndex)
        {
            int x = radius;
            int y = 0;
            int decisionOver2 = 1 - x;   // Decision criterion divided by 2 evaluated at x=r, y=0

            while (y <= x)
            {
                SetPixel(x0 + x, y0 + y, colorIndex); // Octant 1
                SetPixel(x0 + y, y0 + x, colorIndex); // Octant 2
                SetPixel(x0 - y, y0 + x, colorIndex); // Octant 3
                SetPixel(x0 - x, y0 + y, colorIndex); // Octant 4
                SetPixel(x0 - x, y0 - y, colorIndex); // Octant 5
                SetPixel(x0 - y, y0 - x, colorIndex); // Octant 6
                SetPixel(x0 + y, y0 - x, colorIndex); // Octant 7
                SetPixel(x0 + x, y0 - y, colorIndex); // Octant 8

                y++;
                if (decisionOver2 <= 0)
                {
                    decisionOver2 += 2 * y + 1;   // Change in decision criterion for y -> y+1
                }
                else
                {
                    x--;
                    decisionOver2 += 2 * (y - x) + 1;   // Change for y -> y+1, x -> x-1
                }
            }
        }

        /// <summary>
        /// Dibuja un carácter usando una fuente básica
        /// </summary>
        public void DrawChar(int x, int y, char c, byte colorIndex)
        {
            // Implementar una fuente básica 8x8
            // Esto es solo un ejemplo, deberías definir una fuente completa
            byte[][] font8x8_basic = {
                // Cada carácter es un array de 8 bytes, un byte por línea
                // Ejemplo para la letra 'A'
                new byte[] { 0x0C, 0x1E, 0x33, 0x33, 0x3F, 0x33, 0x33, 0x00 }
                // Define el resto de caracteres...
            };

            // Asegurarse de que el carácter esté dentro del rango
            if (c < 32 || c > 126)
                c = '?';

            byte[] glyph = font8x8_basic[c - 32]; // Ajuste para el índice de la fuente

            for (int cy = 0; cy < 8; cy++)
            {
                for (int cx = 0; cx < 8; cx++)
                {
                    if ((glyph[cy] & (1 << cx)) != 0)
                    {
                        SetPixel(x + cx, y + cy, colorIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Dibuja un texto usando una fuente básica
        /// </summary>
        public void DrawText(int x, int y, string text, byte colorIndex)
        {
            for (int i = 0; i < text.Length; i++)
            {
                DrawChar(x + i * 8, y, text[i], colorIndex);
            }
        }

        [DllImport("*", EntryPoint = "_SetVGAMode")]
        private static extern void SetVGAMode(byte mode);
    }
}