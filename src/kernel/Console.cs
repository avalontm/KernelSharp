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

        /// <summary>
        /// Método auxiliar para convertir objetos a cadena imprimible
        /// </summary>
        private string ObjectToString(object obj)
        {
            if (obj == null)
                return "null";

            // Manejar tipos primitivos directamente
            switch (obj)
            {
                case int intValue:
                    return intValue.ToString();
                case bool boolValue:
                    return boolValue.ToString();
                case double doubleValue:
                    return doubleValue.ToString();
                case float floatValue:
                    return floatValue.ToString();
                case long longValue:
                    return longValue.ToString();
                case short shortValue:
                    return shortValue.ToString();
                case byte byteValue:
                    return byteValue.ToString();
                case char charValue:
                    return charValue.ToString();
                case string stringValue:
                    return stringValue;
                default:
                    // Usar ToString() para otros tipos
                    return obj.ToString();
            }
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

            // Convertir correctamente el carácter Unicode a ASCII
            byte charByte;

            if (c <= 127) // ASCII estándar
            {
                charByte = (byte)c;
            }
            else if (c >= 0x80 && c <= 0xFF) // ASCII extendido
            {
                charByte = (byte)c;
            }
            else // Caracteres Unicode fuera del rango ASCII
            {
                // Caracteres Unicode no representables, usar un carácter sustituto como '?'
                charByte = (byte)'?';
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

        // Método de conversión de entero a cadena
        private unsafe static void IntToString(int value, char* buffer)
        {
            int index = 0;
            int originalValue = value;

            // Manejar caso de cero
            if (value == 0)
            {
                buffer[0] = '0';
                buffer[1] = '\0';
                return;
            }

            // Manejar números negativos
            int isNegative = 0;
            if (value < 0)
            {
                isNegative = 1;
                value = -value;
            }

            // Convertir dígitos
            while (value > 0)
            {
                buffer[index++] = (char)((value % 10) + '0');
                value /= 10;
            }

            // Añadir signo negativo si es necesario
            if (isNegative)
            {
                buffer[index++] = '-';
            }

            // Invertir la cadena
            int start = 0;
            int end = index - 1;
            while (start < end)
            {
                char temp = buffer[start];
                buffer[start] = buffer[end];
                buffer[end] = temp;
                start++;
                end--;
            }

            // Terminar con null
            buffer[index] = '\0';
        }
    }
}