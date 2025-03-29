using Kernel.Drivers.IO;
using System;
using System.Runtime.InteropServices;

namespace Kernel.Drivers.Video
{
    /// <summary>
    /// Driver para el modo texto VGA estándar (80x25)
    /// </summary>
    public unsafe class VGATextDriver
    {
        // Constantes para el modo texto VGA
        private const int WIDTH = 80;
        private const int HEIGHT = 25;
        private const int BUFFER_ADDRESS = 0xB8000;

        // Estructura para un carácter en el buffer de video
        [StructLayout(LayoutKind.Sequential)]
        public struct VGAChar
        {
            public byte Character;
            public byte Attribute;

            public VGAChar(char c, Color foreground, Color background)
            {
                Character = (byte)c;
                Attribute = (byte)(((byte)background << 4) | ((byte)foreground & 0x0F));
            }
        }

        // Buffer de video
        private VGAChar* _buffer;

        // Posición actual del cursor
        private int _cursorX;
        private int _cursorY;

        // Colores actuales
        private Color _foreground;
        private Color _background;

        /// <summary>
        /// Constructor del driver VGA
        /// </summary>
        public VGATextDriver() : this(Color.White, Color.Black)
        {
        }

        /// <summary>
        /// Constructor del driver VGA con colores personalizados
        /// </summary>
        /// <param name="foreground">Color de texto</param>
        /// <param name="background">Color de fondo</param>
        public VGATextDriver(Color foreground, Color background)
        {
            _buffer = (VGAChar*)BUFFER_ADDRESS;
            _cursorX = 0;
            _cursorY = 0;
            _foreground = foreground;
            _background = background;

            // Verificar si la memoria de video está accesible
            if (!IsMemoryAccessible())
            {
                // Si no podemos acceder a la memoria directamente, intentar cambiar el modo de video
                SetVideoMode(0x03); // Modo texto 80x25 estándar
            }

            // Limpiar la pantalla al iniciar
            Clear();

            // Actualizar el cursor de hardware
            UpdateCursor();
        }

        /// <summary>
        /// Verifica si la memoria de video es accesible
        /// </summary>
        /// <returns>true si la memoria es accesible</returns>
        private unsafe bool IsMemoryAccessible()
        {
            // Verificación básica: comprobar si el puntero a buffer no es nulo
            if (_buffer == null)
                return false;

            // Para sistemas sin manejo de excepciones, una forma de probar 
            // si la memoria es accesible es verificar que esté dentro de un rango válido
            // Por ejemplo, comprobando que la dirección esté dentro de la región de memoria de video

            byte* videoMemory = (byte*)0xB8000;                // Dirección estándar de memoria de video
            byte* videoMemoryEnd = videoMemory + (80 * 25 * 2);    // Fin de memoria de video (80x25 caracteres, 2 bytes por carácter)

            // Verificar si el buffer está dentro del rango válido de memoria de video
            if ((byte*)_buffer < videoMemory || (byte*)_buffer >= videoMemoryEnd)
                return false;

            // Otra opción es intentar hacer una operación "segura" con la memoria
            // Por ejemplo, leer un valor y verificar si está dentro del rango esperado para la memoria de video
            byte value = _buffer[0].Character;

            // Los valores en memoria de video suelen estar en el rango ASCII (0-127)
            // Si obtenemos valores muy extraños, podría indicar memoria no válida
            return value <= 127;
        }

        /// <summary>
        /// Establece un modo de video usando la BIOS
        /// </summary>
        /// <param name="mode">Modo de video (0x03 para 80x25 texto)</param>
        private void SetVideoMode(byte mode)
        {
            BIOS.SetVideoMode(mode);

            // Reiniciar el puntero al buffer de video
            _buffer = (VGAChar*)BUFFER_ADDRESS;
        }

        /// <summary>
        /// Limpia la pantalla con los colores actuales
        /// </summary>
        public void Clear()
        {
            VGAChar empty = new VGAChar(' ', _foreground, _background);

            for (int i = 0; i < WIDTH * HEIGHT; i++)
            {
                _buffer[i] = empty;
            }

            _cursorX = 0;
            _cursorY = 0;
            UpdateCursor();
        }

        /// <summary>
        /// Escribe un carácter en la posición actual del cursor
        /// </summary>
        /// <param name="c">Carácter a escribir</param>
        public void Write(char c)
        {
            // Manejar caracteres especiales
            switch (c)
            {
                case '\n': // Nueva línea
                    _cursorX = 0;
                    _cursorY++;
                    break;

                case '\r': // Retorno de carro
                    _cursorX = 0;
                    break;

                case '\t': // Tabulador
                    const int TAB_SIZE = 4;
                    _cursorX = (_cursorX + TAB_SIZE) & ~(TAB_SIZE - 1);
                    break;

                case '\b': // Retroceso
                    if (_cursorX > 0)
                    {
                        _cursorX--;
                    }
                    else if (_cursorY > 0)
                    {
                        _cursorY--;
                        _cursorX = WIDTH - 1;
                    }

                    // Borrar el carácter en la posición actual
                    _buffer[_cursorY * WIDTH + _cursorX] = new VGAChar(' ', _foreground, _background);
                    break;

                default: // Carácter normal
                    // Escribir el carácter en la posición actual
                    _buffer[_cursorY * WIDTH + _cursorX] = new VGAChar(c, _foreground, _background);
                    _cursorX++;
                    break;
            }

            // Manejar desbordamiento horizontal
            if (_cursorX >= WIDTH)
            {
                _cursorX = 0;
                _cursorY++;
            }

            // Scroll si es necesario
            if (_cursorY >= HEIGHT)
            {
                ScrollUp();
            }

            // Actualizar el cursor de hardware
            UpdateCursor();
        }

        /// <summary>
        /// Escribe una cadena en la posición actual del cursor
        /// </summary>
        /// <param name="s">Cadena a escribir</param>
        public void Write(string s)
        {
            if (string.IsNullOrEmpty(s))
                return;

            for (int i = 0; i < s.Length; i++)
            {
                Write(s[i]);
            }
        }

        /// <summary>
        /// Escribe una cadena seguida de un salto de línea
        /// </summary>
        /// <param name="s">Cadena a escribir</param>
        public void WriteLine(string s)
        {
            Write(s);
            Write('\n');
        }

        /// <summary>
        /// Escribe una línea vacía
        /// </summary>
        public void WriteLine()
        {
            Write('\n');
        }

        /// <summary>
        /// Establece la posición del cursor
        /// </summary>
        /// <param name="x">Coordenada X (columna)</param>
        /// <param name="y">Coordenada Y (fila)</param>
        public void SetCursorPosition(int x, int y)
        {
            _cursorX = Math.Clamp(x, 0, WIDTH - 1);
            _cursorY = Math.Clamp(y, 0, HEIGHT - 1);
            UpdateCursor();
        }

        /// <summary>
        /// Obtiene la posición X actual del cursor
        /// </summary>
        public int CursorX => _cursorX;

        /// <summary>
        /// Obtiene la posición Y actual del cursor
        /// </summary>
        public int CursorY => _cursorY;

        /// <summary>
        /// Establece los colores para las nuevas escrituras
        /// </summary>
        /// <param name="foreground">Color de texto</param>
        /// <param name="background">Color de fondo</param>
        public void SetColors(Color foreground, Color background)
        {
            _foreground = foreground;
            _background = background;
        }

        /// <summary>
        /// Establece el color de texto para las nuevas escrituras
        /// </summary>
        /// <param name="foreground">Color de texto</param>
        public void SetForegroundColor(Color foreground)
        {
            _foreground = foreground;
        }

        /// <summary>
        /// Establece el color de fondo para las nuevas escrituras
        /// </summary>
        /// <param name="background">Color de fondo</param>
        public void SetBackgroundColor(Color background)
        {
            _background = background;
        }

        /// <summary>
        /// Escribe un carácter en una posición específica
        /// </summary>
        /// <param name="x">Coordenada X</param>
        /// <param name="y">Coordenada Y</param>
        /// <param name="c">Carácter a escribir</param>
        /// <param name="foreground">Color de texto</param>
        /// <param name="background">Color de fondo</param>
        public void WriteAt(int x, int y, char c, Color foreground, Color background)
        {
            if (x < 0 || x >= WIDTH || y < 0 || y >= HEIGHT)
                return;

            _buffer[y * WIDTH + x] = new VGAChar(c, foreground, background);
        }

        /// <summary>
        /// Escribe un carácter en una posición específica con los colores actuales
        /// </summary>
        /// <param name="x">Coordenada X</param>
        /// <param name="y">Coordenada Y</param>
        /// <param name="c">Carácter a escribir</param>
        public void WriteAt(int x, int y, char c)
        {
            WriteAt(x, y, c, _foreground, _background);
        }

        /// <summary>
        /// Escribe una cadena en una posición específica
        /// </summary>
        /// <param name="x">Coordenada X</param>
        /// <param name="y">Coordenada Y</param>
        /// <param name="s">Cadena a escribir</param>
        /// <param name="foreground">Color de texto</param>
        /// <param name="background">Color de fondo</param>
        public void WriteAt(int x, int y, string s, Color foreground, Color background)
        {
            if (string.IsNullOrEmpty(s))
                return;

            for (int i = 0; i < s.Length; i++)
            {
                if (x + i >= WIDTH)
                    break;

                WriteAt(x + i, y, s[i], foreground, background);
            }
        }

        /// <summary>
        /// Escribe una cadena en una posición específica con los colores actuales
        /// </summary>
        /// <param name="x">Coordenada X</param>
        /// <param name="y">Coordenada Y</param>
        /// <param name="s">Cadena a escribir</param>
        public void WriteAt(int x, int y, string s)
        {
            WriteAt(x, y, s, _foreground, _background);
        }

        /// <summary>
        /// Desplaza la pantalla hacia arriba una línea
        /// </summary>
        private void ScrollUp()
        {
            // Mover todas las líneas hacia arriba una posición
            for (int y = 0; y < HEIGHT - 1; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    _buffer[y * WIDTH + x] = _buffer[(y + 1) * WIDTH + x];
                }
            }

            // Limpiar la última línea
            for (int x = 0; x < WIDTH; x++)
            {
                _buffer[(HEIGHT - 1) * WIDTH + x] = new VGAChar(' ', _foreground, _background);
            }

            // Ajustar el cursor
            _cursorY = HEIGHT - 1;
        }

        /// <summary>
        /// Actualiza la posición del cursor de hardware
        /// </summary>
        private void UpdateCursor()
        {
            // Calcular la posición lineal
            ushort position = (ushort)(_cursorY * WIDTH + _cursorX);

            // Los puertos 0x3D4 y 0x3D5 son para controlar el cursor VGA
            // Enviar el byte bajo
            IOPort.OutByte(0x3D4, 0x0F);
            IOPort.OutByte(0x3D5, (byte)(position & 0xFF));

            // Enviar el byte alto
            IOPort.OutByte(0x3D4, 0x0E);
            IOPort.OutByte(0x3D5, (byte)((position >> 8) & 0xFF));
        }

        /// <summary>
        /// Habilita o deshabilita la visibilidad del cursor
        /// </summary>
        /// <param name="visible">true para mostrar el cursor, false para ocultarlo</param>
        public void SetCursorVisible(bool visible)
        {
            if (visible)
            {
                // Establecer tamaño normal del cursor (líneas 13-14)
                IOPort.OutByte(0x3D4, 0x0A);
                IOPort.OutByte(0x3D5, (byte)((IOPort.InByte(0x3D5) & 0xC0) | 13));

                IOPort.OutByte(0x3D4, 0x0B);
                IOPort.OutByte(0x3D5, (byte)((IOPort.InByte(0x3D5) & 0xE0) | 14));
            }
            else
            {
                // Deshabilitar cursor (bit 5 del registro 0x0A)
                IOPort.OutByte(0x3D4, 0x0A);
                IOPort.OutByte(0x3D5, (byte)(IOPort.InByte(0x3D5) | 0x20));
            }
        }

        /// <summary>
        /// Dibuja un borde alrededor de una región de la pantalla
        /// </summary>
        /// <param name="x">Coordenada X superior izquierda</param>
        /// <param name="y">Coordenada Y superior izquierda</param>
        /// <param name="width">Ancho del borde</param>
        /// <param name="height">Altura del borde</param>
        /// <param name="singleLine">true para línea simple, false para línea doble</param>
        public void DrawBorder(int x, int y, int width, int height, bool singleLine = true)
        {
            if (x < 0 || y < 0 || width < 2 || height < 2 ||
                x + width > WIDTH || y + height > HEIGHT)
                return;

            // Caracteres para el borde (pueden cambiar según la página de códigos)
            char topLeft, topRight, bottomLeft, bottomRight, horizontal, vertical;

            if (singleLine)
            {
                // Caracteres de línea simple
                topLeft = '┌';     // 0xDA en CP437
                topRight = '┐';    // 0xBF en CP437
                bottomLeft = '└';  // 0xC0 en CP437
                bottomRight = '┘'; // 0xD9 en CP437
                horizontal = '─';  // 0xC4 en CP437
                vertical = '│';    // 0xB3 en CP437
            }
            else
            {
                // Caracteres de línea doble
                topLeft = '╔';     // 0xC9 en CP437
                topRight = '╗';    // 0xBB en CP437
                bottomLeft = '╚';  // 0xC8 en CP437
                bottomRight = '╝'; // 0xBC en CP437
                horizontal = '═';  // 0xCD en CP437
                vertical = '║';    // 0xBA en CP437
            }

            // Dibujar esquinas
            WriteAt(x, y, topLeft);
            WriteAt(x + width - 1, y, topRight);
            WriteAt(x, y + height - 1, bottomLeft);
            WriteAt(x + width - 1, y + height - 1, bottomRight);

            // Dibujar bordes horizontales
            for (int i = 1; i < width - 1; i++)
            {
                WriteAt(x + i, y, horizontal);
                WriteAt(x + i, y + height - 1, horizontal);
            }

            // Dibujar bordes verticales
            for (int i = 1; i < height - 1; i++)
            {
                WriteAt(x, y + i, vertical);
                WriteAt(x + width - 1, y + i, vertical);
            }
        }

        /// <summary>
        /// Dibuja un cuadro con un título
        /// </summary>
        /// <param name="x">Coordenada X superior izquierda</param>
        /// <param name="y">Coordenada Y superior izquierda</param>
        /// <param name="width">Ancho del cuadro</param>
        /// <param name="height">Altura del cuadro</param>
        /// <param name="title">Título del cuadro</param>
        /// <param name="titleColor">Color del título</param>
        public void DrawBox(int x, int y, int width, int height, string title, Color titleColor)
        {
            // Dibujar el borde
            DrawBorder(x, y, width, height);

            // Dibujar el título centrado
            if (!string.IsNullOrEmpty(title))
            {
                int titleLength = Math.Min(title.Length, width - 4);
                int titleX = x + (width - titleLength) / 2;

                // Guardar colores actuales
                Color savedForeground = _foreground;

                // Establecer color de título
                _foreground = titleColor;

                // Escribir título
                WriteAt(titleX, y, title.Substring(0, titleLength));

                // Restaurar colores
                _foreground = savedForeground;
            }
        }

        /// <summary>
        /// Crea un marco para una ventana con título y contenido
        /// </summary>
        /// <param name="x">Coordenada X superior izquierda</param>
        /// <param name="y">Coordenada Y superior izquierda</param>
        /// <param name="width">Ancho de la ventana</param>
        /// <param name="height">Altura de la ventana</param>
        /// <param name="title">Título de la ventana</param>
        /// <param name="content">Contenido de la ventana (puede tener múltiples líneas)</param>
        public void DrawWindow(int x, int y, int width, int height, string title, string content)
        {
            // Dibujar el cuadro con título
            DrawBox(x, y, width, height, title, Color.Yellow);

            // Verificar si hay contenido
            if (string.IsNullOrEmpty(content))
                return;

            // Dividir el contenido en líneas
            string[] lines = content.Split('\n');

            // Calcular el área de contenido disponible
            int contentWidth = width - 2;
            int contentHeight = height - 2;

            // Escribir el contenido línea por línea
            for (int i = 0; i < lines.Length && i < contentHeight; i++)
            {
                string line = lines[i];
                if (line.Length > contentWidth)
                    line = line.Substring(0, contentWidth);

                WriteAt(x + 1, y + 1 + i, line);
            }
        }

        /// <summary>
        /// Dibuja una barra de progreso
        /// </summary>
        /// <param name="x">Coordenada X</param>
        /// <param name="y">Coordenada Y</param>
        /// <param name="width">Ancho total de la barra</param>
        /// <param name="progress">Valor de progreso (0-100)</param>
        /// <param name="barColor">Color de la barra</param>
        public void DrawProgressBar(int x, int y, int width, int progress, Color barColor)
        {
            // Asegurarse de que el progreso esté en el rango correcto
            progress = Math.Clamp(progress, 0, 100);

            // Calcular la longitud de la barra llena
            int filledWidth = (progress * (width - 2)) / 100;

            // Guardar colores actuales
            Color savedForeground = _foreground;
            Color savedBackground = _background;

            // Dibujar el borde de la barra
            WriteAt(x, y, '[');
            WriteAt(x + width - 1, y, ']');

            // Dibujar la parte vacía
            _foreground = Color.DarkGrey;
            for (int i = 0; i < width - 2 - filledWidth; i++)
            {
                WriteAt(x + 1 + filledWidth + i, y, '·');
            }

            // Dibujar la parte llena
            _foreground = barColor;
            for (int i = 0; i < filledWidth; i++)
            {
                WriteAt(x + 1 + i, y, '█');
            }

            // Restaurar colores
            _foreground = savedForeground;
            _background = savedBackground;
        }

        /// <summary>
        /// Crea una tabla simple con bordes
        /// </summary>
        /// <param name="x">Coordenada X</param>
        /// <param name="y">Coordenada Y</param>
        /// <param name="columns">Anchos de las columnas</param>
        /// <param name="rows">Contenido de las filas (array de arrays)</param>
        /// <param name="headerColor">Color del encabezado</param>
        public void DrawTable(int x, int y, int[] columns, string[][] rows, Color headerColor)
        {
            if (columns == null || columns.Length == 0 || rows == null || rows.Length == 0)
                return;

            // Calcular el ancho total de la tabla
            int totalWidth = columns.Sum() + columns.Length + 1;

            // Verificar que la tabla cabe en la pantalla
            if (x < 0 || y < 0 || x + totalWidth > WIDTH || y + rows.Length * 2 > HEIGHT)
                return;

            // Guardar colores actuales
            Color savedForeground = _foreground;

            // Dibujar la línea superior
            WriteAt(x, y, '┌');
            int currentX = x + 1;
            for (int i = 0; i < columns.Length; i++)
            {
                for (int j = 0; j < columns[i]; j++)
                {
                    WriteAt(currentX++, y, '─');
                }

                if (i < columns.Length - 1)
                    WriteAt(currentX++, y, '┬');
                else
                    WriteAt(currentX++, y, '┐');
            }

            // Dibujar las filas
            for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                // Dibujar contenido de la fila
                currentX = x;
                WriteAt(currentX, y + rowIndex * 2 + 1, '│');
                currentX++;

                // Si es la primera fila, usar color de encabezado
                if (rowIndex == 0)
                    _foreground = headerColor;

                // Dibujar cada celda
                for (int colIndex = 0; colIndex < columns.Length && colIndex < rows[rowIndex].Length; colIndex++)
                {
                    string cell = rows[rowIndex][colIndex] ?? "";
                    if (cell.Length > columns[colIndex])
                        cell = cell.Substring(0, columns[colIndex]);

                    WriteAt(currentX, y + rowIndex * 2 + 1, cell);
                    currentX += columns[colIndex];
                    WriteAt(currentX, y + rowIndex * 2 + 1, '│');
                    currentX++;
                }

                // Restaurar color normal después del encabezado
                if (rowIndex == 0)
                    _foreground = savedForeground;

                // Dibujar línea separadora (excepto después de la última fila)
                if (rowIndex < rows.Length - 1)
                {
                    currentX = x;
                    WriteAt(currentX, y + rowIndex * 2 + 2, '├');
                    currentX++;

                    for (int colIndex = 0; colIndex < columns.Length; colIndex++)
                    {
                        for (int j = 0; j < columns[colIndex]; j++)
                        {
                            WriteAt(currentX++, y + rowIndex * 2 + 2, '─');
                        }

                        if (colIndex < columns.Length - 1)
                            WriteAt(currentX++, y + rowIndex * 2 + 2, '┼');
                        else
                            WriteAt(currentX++, y + rowIndex * 2 + 2, '┤');
                    }
                }
            }

            // Dibujar línea inferior
            currentX = x;
            WriteAt(currentX, y + rows.Length * 2, '└');
            currentX++;

            for (int colIndex = 0; colIndex < columns.Length; colIndex++)
            {
                for (int j = 0; j < columns[colIndex]; j++)
                {
                    WriteAt(currentX++, y + rows.Length * 2, '─');
                }

                if (colIndex < columns.Length - 1)
                    WriteAt(currentX++, y + rows.Length * 2, '┴');
                else
                    WriteAt(currentX++, y + rows.Length * 2, '┘');
            }

            // Restaurar colores
            _foreground = savedForeground;
        }
    }
}