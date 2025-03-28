namespace Kernel
{
    /// <summary>
    /// A virtual terminal for the kernel to interact with the user through
    /// </summary>
    unsafe public struct Console
    {
        private int width;
        private int height;

        private FrameBuffer frameBuffer;

        private int column;
        private int row;

        private Color foregroundColor;
        private Color backgroundColor;

        // Constructor predeterminado con valores por defecto
        public Console(FrameBuffer frameBuffer)
            : this(80, 25, Color.White, frameBuffer)
        { }

        public Console(int width, int height, FrameBuffer frameBuffer)
            : this(width, height, Color.White, frameBuffer)
        { }

        public Console(int width, int height, Color foregroundColor, FrameBuffer frameBuffer)
        {
            this.width = width;
            this.height = height;
            this.foregroundColor = foregroundColor;
            this.backgroundColor = Color.Black; // Color de fondo predeterminado
            this.frameBuffer = frameBuffer;
            this.column = 0;
            this.row = 0;
        }

        // Método para cambiar el color de primer plano
        public void SetForegroundColor(Color color)
        {
            this.foregroundColor = color;
        }

        // Método para cambiar el color de fondo
        public void SetBackgroundColor(Color color)
        {
            this.backgroundColor = color;
        }

        /// <summary>
        /// Clear the screen
        /// </summary>
        public void Clear()
        {
            // Resetear posición del cursor
            column = 0;
            row = 0;

            // Limpiar pantalla con espacios
            for (int i = 0; i < width * height; i++)
            {
                Print(' ');
            }

            // Resetear posición del cursor
            column = 0;
            row = 0;
        }


        public void PrintLine(string str)
        {
            Print(str);
            Print('\n');
        }

        public void Print(string str)
        {
            // Imprimir carácter por carácter
            for (int i = 0; i < str.Length; i++)
            {
                Print(str[i]);
            }
        }

        /// <summary>
        /// Print a character to the current cursor position
        /// </summary>
        void Print(char c)
        {
            // Desplazar si el cursor ha llegado al final de la pantalla
            if (row >= height)
            {
                // Desplazar contenido hacia arriba
                frameBuffer.Copy(width * 2, 0, (height - 1) * width * 2);

                row = height - 1;
                column = 0;

                // Limpiar la última línea
                for (int i = 0; i < width; i++)
                {
                    WriteVGATextCharacter(' ');
                    column++;
                }

                // Volver al inicio de la última línea
                column = 0;
            }

            // Manejo de salto de línea
            if (c == '\n')
            {
                column = 0;
                row++;
                return;
            }

            WriteVGATextCharacter(c);

            // Mover cursor
            column++;

            // Saltar a nueva línea si se alcanza el final
            if (column >= width)
            {
                column = 0;
                row++;
            }
        }

        /// <summary>
        /// Escribir un carácter directamente en memoria de video
        /// </summary>
        private void WriteVGATextCharacter(char c)
        {
            // Calcular posición en memoria de video
            int position = (row * width + column) * 2;

            // Convertir correctamente el carácter a un byte para VGA text mode
            byte charByte;

            // Expandir la lógica de conversión de caracteres
            switch (c)
            {
                // Manejar caracteres de control
                case '\t':   // Tabulación
                    charByte = (byte)' ';
                    break;
                case '\r':   // Retorno de carro
                    column = 0;
                    return;
                case '\0':   // Carácter nulo
                    return;

                // Caracteres ASCII estándar (0-127)
                case char ch when ch <= 127:
                    charByte = (byte)ch;
                    break;

                // Caracteres ASCII extendidos (128-255)
                case char ch when ch >= 128 && ch <= 255:
                    charByte = (byte)ch;
                    break;

                // Caracteres Unicode fuera del rango de VGA text mode
                default:
                    // Mapeo de algunos caracteres Unicode comunes
                    switch (c)
                    {
                        case 'á': charByte = (byte)'a'; break;
                        case 'é': charByte = (byte)'e'; break;
                        case 'í': charByte = (byte)'i'; break;
                        case 'ó': charByte = (byte)'o'; break;
                        case 'ú': charByte = (byte)'u'; break;
                        case 'ñ': charByte = (byte)'n'; break;
                        case 'Á': charByte = (byte)'A'; break;
                        case 'É': charByte = (byte)'E'; break;
                        case 'Í': charByte = (byte)'I'; break;
                        case 'Ó': charByte = (byte)'O'; break;
                        case 'Ú': charByte = (byte)'U'; break;
                        case 'Ñ': charByte = (byte)'N'; break;

                        // Caracteres Unicode no representables
                        default:
                            charByte = (byte)'?';
                            break;
                    }
                    break;
            }

            // Escribir carácter
            frameBuffer.Write(position, charByte);

            // Calcular atributo de color (primer plano y fondo)
            byte colorAttribute = (byte)(
                ((byte)backgroundColor << 4) |
                ((byte)foregroundColor & 0x0F)
            );

            // Escribir atributo de color
            frameBuffer.Write(position + 1, colorAttribute);
        }
    }
}
