using Kernel.Diagnostics;
using Kernel.Drivers;
using Kernel.Drivers.IO;
using System;
using System.Runtime.InteropServices;

namespace Kernel.Drivers.Audio
{
    /// <summary>
    /// Controlador de audio para dispositivos AC97 emulados por QEMU y otros entornos virtuales
    /// </summary>
    public unsafe class AC97AudioDriver : BaseDriver
    {
        // Identificadores PCI para el controlador AC97
        private const ushort AC97_VENDOR_ID = 0x8086;   // Intel
        private const ushort AC97_DEVICE_ID = 0x2415;   // 82801AA AC97 Audio Controller

        // Registros de control de bus maestro
        private const ushort NABM_BASE = 0x10;          // Base del Bus Master
        private const ushort NABM_OFFSET = 0x20;        // Base + Offset = Dirección efectiva

        // Registros de Mixer
        private const ushort MIXER_BASE = 0x00;
        private const ushort RESET_REGISTER = 0x00;
        private const ushort MASTER_VOLUME = 0x02;
        private const ushort PCM_VOLUME = 0x18;
        private const ushort PCM_STATUS = 0x26;
        private const ushort PCM_RATE = 0x2C;
        private const ushort EXT_AUDIO_ID = 0x28;
        private const ushort EXT_AUDIO_STATUS = 0x2A;
        private const ushort PCM_FRONT_DAC_RATE = 0x2C;

        // Registros de Bus Maestro para salida PCM
        private const ushort PO_BDBAR = 0x10;           // Descriptor de buffer base
        private const ushort PO_CIV = 0x14;             // Índice actual
        private const ushort PO_LVI = 0x15;             // Último índice válido
        private const ushort PO_STATUS = 0x16;          // Status
        private const ushort PO_CONTROL = 0x1B;         // Control

        // Banderas de control
        private const byte CONTROL_RESET = 0x02;
        private const byte CONTROL_RUN = 0x01;

        // Dispositivo PCI y sus puertos de E/S base
        private PCIDevice _pciDevice;
        private ushort _nabmPort;   // Native Audio Bus Mastering Port
        private ushort _mixerPort;  // AC97 Mixer Port

        // Estado del controlador
        private bool _initialized;
        private ulong _descriptorListPhys;
        private BufferDescriptor* _descriptorList;
        private const int BUFFER_COUNT = 32;
        private const int BUFFER_SIZE = 4096;
        private ulong _audioBufferPhys;
        private byte* _audioBuffer;
        private int _currentBuffer;

        // Estructura para descriptores de buffer (Buffer Descriptor List Entry)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BufferDescriptor
        {
            public ulong BufferAddress;   // Dirección física del buffer
            public ushort BufferSize;     // Tamaño del buffer (0-65535 bytes)
            public ushort Control;        // Flags de control
        }

        /// <summary>
        /// Constructor del driver AC97
        /// </summary>
        public AC97AudioDriver(PCIDevice device) : base("AC97Audio", "Intel AC97 Audio Controller")
        {
            _pciDevice = device;
            _initialized = false;
            _currentBuffer = 0;
        }

        /// <summary>
        /// Inicialización del controlador AC97
        /// </summary>
        protected override bool OnInitialize()
        {
            SerialDebug.Info("Initializing Intel AC97 Audio Controller");

            if (_pciDevice == null)
            {
                SerialDebug.Error("AC97 Audio: No PCI device provided");
                return false;
            }

            // Activar controlador PCI
            EnablePCI();

            // Obtener puertos base
            _nabmPort = (ushort)(_pciDevice.BAR[1] & 0xFFFC);
            _mixerPort = (ushort)(_pciDevice.BAR[0] & 0xFFFC);

            SerialDebug.Info($"AC97 Audio: NABM Port = 0x{((ulong)_nabmPort).ToStringHex()}, Mixer Port = 0x{((ulong)_mixerPort).ToStringHex()}");

            // Realizar reset del controlador
            ResetController();

            // Inicializar descriptores de audio
            if (!InitializeBuffers())
            {
                SerialDebug.Error("AC97 Audio: Failed to initialize buffers");
                return false;
            }

            // Configurar volumen y formato de audio
            SetupAudio();

            _initialized = true;
            SerialDebug.Info("AC97 Audio: Controller initialized successfully");

            SetupInterrupts();
            return true;
        }

        private void SetupInterrupts()
        {
            SerialDebug.Info($"AC97 Audio: Setting up interrupts, IRQ: {_pciDevice.InterruptLine}");

            // Leer y limpiar cualquier interrupción pendiente
            ushort status = IOPort.In16((ushort)(_nabmPort + PO_STATUS));
            IOPort.Out16((ushort)(_nabmPort + PO_STATUS), (ushort)0x1F); // Limpiar todos los bits de estado

            // Habilitar interrupciones para reproducción
            // Bit 1 (0x02): Interrupt on Completion Enable
            // Bit 4 (0x10): Last Valid Index Interrupt Enable
            IOPort.Out8((ushort)(_nabmPort + PO_CONTROL), (byte)0x02);

            // Registrar manejador de interrupciones
            InterruptDelegate handler = new InterruptDelegate(HandleInterrupt);
            InterruptManager.RegisterIRQHandler(_pciDevice.InterruptLine, handler);
            InterruptManager.EnableIRQ(_pciDevice.InterruptLine);
        }

        // Manejador de interrupciones que será llamado cuando ocurra la IRQ
        public void HandleInterrupt()
        {
            SerialDebug.Info("AC97 Audio: Interrupt received");
            // Leer causas de interrupción
            ushort status = IOPort.In16((ushort)(_nabmPort + PO_STATUS));

            // Procesar la interrupción según los bits de estado
            if ((status & 0x01) != 0) // Buffer Completion Interrupt
            {
                // Procesar buffer completado
                byte currentIndex = IOPort.In8((ushort)(_nabmPort + PO_CIV));
            }

            // Limpiar todos los bits de estado escribiendo los bits a 1
            IOPort.Out16((ushort)(_nabmPort + PO_STATUS), status);
        }

        /// <summary>
        /// Habilita el dispositivo PCI
        /// </summary>
        private void EnablePCI()
        {
            // Habilitar Bus Mastering y espacio de I/O
            ushort command = PCIMMIOManager.ReadConfig16(_pciDevice.Location.Bus, _pciDevice.Location.Device, _pciDevice.Location.Function, 0x04);
            command |= 0x5; // I/O Space + Bus Master
            PCIMMIOManager.WriteConfig16(_pciDevice.Location.Bus, _pciDevice.Location.Device, _pciDevice.Location.Function, 0x04, command);
        }

        /// <summary>
        /// Realiza un reset del controlador AC97
        /// </summary>
        private void ResetController()
        {
            SerialDebug.Info("AC97 Audio: Resetting controller");

            // Reset AC97 Mixer
            IOPort.Out16((ushort)(_mixerPort + RESET_REGISTER), 0x0000);

            // Esperar un poco
            for (int i = 0; i < 10000; i++)
                Native.Pause();

            // Reset Bus Master
            IOPort.Out8((ushort)(_nabmPort + PO_CONTROL), CONTROL_RESET);

            // Esperar a que el reset se complete
            for (int i = 0; i < 1000; i++)
                Native.Pause();

            // Limpiar bit de reset
            IOPort.Out8((ushort)(_nabmPort + PO_CONTROL), 0);

            SerialDebug.Info("AC97 Audio: Reset complete");
        }

        /// <summary>
        /// Inicializa los buffers y descriptores de audio
        /// </summary>
        private bool InitializeBuffers()
        {
            SerialDebug.Info("AC97 Audio: Initializing buffers");

            // Asignar memoria para los descriptores de buffer (Buffer Descriptor List)
            _descriptorListPhys = (ulong)Allocator.malloc((ulong)(BUFFER_COUNT * sizeof(BufferDescriptor)));
            if (_descriptorListPhys == 0)
            {
                SerialDebug.Error("AC97 Audio: Failed to allocate descriptor list");
                return false;
            }
            _descriptorList = (BufferDescriptor*)_descriptorListPhys;

            // Asignar buffer de audio
            _audioBufferPhys = (ulong)Allocator.malloc((ulong)(BUFFER_COUNT * BUFFER_SIZE));
            if (_audioBufferPhys == 0)
            {
                SerialDebug.Error("AC97 Audio: Failed to allocate audio buffer");
                return false;
            }
            _audioBuffer = (byte*)_audioBufferPhys;

            // Limpiar el buffer de audio (silencio)
            Native.Stosb(_audioBuffer, 0, (ulong)(BUFFER_COUNT * BUFFER_SIZE));

            // Configurar cada descriptor
            for (int i = 0; i < BUFFER_COUNT; i++)
            {
                _descriptorList[i].BufferAddress = _audioBufferPhys + (ulong)(i * BUFFER_SIZE);
                _descriptorList[i].BufferSize = BUFFER_SIZE;

                // Último descriptor: IOC (Interrupt on Completion)
                // Para los demás descriptores solo necesitamos BUP (Buffer Underrun Policy)
                _descriptorList[i].Control = (ushort)((i == BUFFER_COUNT - 1) ? 0x8000 : 0x4000);
            }

            // Establecer la dirección del descriptor base
            IOPort.Out32((ushort)(_nabmPort + PO_BDBAR), (uint)_descriptorListPhys);

            // Inicializar índices
            _currentBuffer = 0;
            IOPort.Out8((ushort)(_nabmPort + PO_CIV), 0);
            IOPort.Out8((ushort)(_nabmPort + PO_LVI), (byte)(BUFFER_COUNT - 1));

            SerialDebug.Info("AC97 Audio: Buffers initialized");
            return true;
        }

        /// <summary>
        /// Configura parámetros de audio como volumen y frecuencia de muestreo
        /// </summary>
        private void SetupAudio()
        {
            SerialDebug.Info("AC97 Audio: Setting up audio parameters");

            // Establecer Master Volume (0x0000 = max, 0x8000 = mute)
            // 0x0F0F = 75% del volumen (los 8 bits más bajos, para ambos canales)
            IOPort.Out16((ushort)(_mixerPort + MASTER_VOLUME), 0x0F0F);

            // Establecer PCM Volume (0x0000 = max, 0x8000 = mute)
            IOPort.Out16((ushort)(_mixerPort + PCM_VOLUME), 0x0F0F);

            // Establecer frecuencia de muestreo para PCM (44100 Hz)
            IOPort.Out16((ushort)(_mixerPort + PCM_FRONT_DAC_RATE), 44100);

            // Leer y mostrar el ID del codec
            ushort extAudioId = IOPort.In16((ushort)(_mixerPort + EXT_AUDIO_ID));
            SerialDebug.Info($"AC97 Audio: Extended Audio ID: 0x{((ulong)extAudioId).ToStringHex()}");

            // Leer y mostrar estado del codec
            ushort extAudioStatus = IOPort.In16((ushort)(_mixerPort + EXT_AUDIO_STATUS));
            SerialDebug.Info($"AC97 Audio: Extended Audio Status: 0x{((ulong)extAudioStatus).ToStringHex()}");
        }

        /// <summary>
        /// Escribe datos de audio en un buffer para reproducción
        /// </summary>
        /// <param name="data">Datos de audio (PCM, 16 bits, estéreo)</param>
        /// <param name="offset">Desplazamiento en el array de datos</param>
        /// <param name="length">Longitud de los datos a escribir</param>
        /// <returns>True si los datos se escribieron correctamente</returns>
        public bool WriteAudio(byte[] data, int offset, int length)
        {
            if (!_initialized)
            {
                SerialDebug.Warning("AC97 Audio: Controller not initialized");
                return false;
            }

            if (length <= 0 || offset < 0 || offset + length > data.Length)
            {
                SerialDebug.Warning("AC97 Audio: Invalid data parameters");
                return false;
            }

            // Determinar buffer actual
            byte currentIndex = IOPort.In8((ushort)(_nabmPort + PO_CIV));
            int bufferIndex = (_currentBuffer + 1) % BUFFER_COUNT;

            // Verificar si el buffer está disponible
            if (bufferIndex == currentIndex)
            {
                SerialDebug.Warning("AC97 Audio: No buffer available");
                return false;
            }

            // Copiar datos al buffer
            byte* targetBuffer = (byte*)(_descriptorList[bufferIndex].BufferAddress);
            int copyLength = Math.Min(length, BUFFER_SIZE);

            for (int i = 0; i < copyLength; i++)
            {
                targetBuffer[i] = data[offset + i];
            }

            // Si no hemos escrito el tamaño completo, rellenar con silencio
            if (copyLength < BUFFER_SIZE)
            {
                for (int i = copyLength; i < BUFFER_SIZE; i++)
                {
                    targetBuffer[i] = 0;
                }
            }

            // Actualizar el último índice válido
            IOPort.Out8((ushort)(_nabmPort + PO_LVI), (byte)bufferIndex);

            // Iniciar reproducción si está detenida
            if ((IOPort.In8((ushort)(_nabmPort + PO_CONTROL)) & CONTROL_RUN) == 0)
            {
                StartPlayback();
            }

            _currentBuffer = bufferIndex;
            return true;
        }

        /// <summary>
        /// Inicia la reproducción de audio
        /// </summary>
        public void StartPlayback()
        {
            if (!_initialized)
            {
                SerialDebug.Warning("AC97 Audio: Controller not initialized");
                return;
            }

            SerialDebug.Info("AC97 Audio: Starting playback");

            // Limpiar registros de estado
            IOPort.Out16((ushort)(_nabmPort + PO_STATUS), 0x1F);

            // Iniciar reproducción
            IOPort.Out8((ushort)(_nabmPort + PO_CONTROL), CONTROL_RUN);
        }

        /// <summary>
        /// Detiene la reproducción de audio
        /// </summary>
        public void StopPlayback()
        {
            if (!_initialized)
            {
                SerialDebug.Warning("AC97 Audio: Controller not initialized");
                return;
            }

            SerialDebug.Info("AC97 Audio: Stopping playback");

            // Detener reproducción
            IOPort.Out8((ushort)(_nabmPort + PO_CONTROL), 0);

            // Limpiar registros de estado
            IOPort.Out32((ushort)(_nabmPort + PO_STATUS), 0x1F);
        }

        /// <summary>
        /// Establece el volumen maestro (0-100)
        /// </summary>
        /// <param name="volume">Nivel de volumen (0-100)</param>
        public void SetVolume(int volume)
        {
            if (!_initialized)
            {
                SerialDebug.Warning("AC97 Audio: Controller not initialized");
                return;
            }

            // Limitar volumen al rango 0-100
            volume = Math.Max(0, Math.Min(100, volume));

            // Convertir a formato AC97 (0 = max, 0x3F = mute)
            // Invertimos el volumen y lo escalamos a 6 bits
            int ac97Volume = (100 - volume) * 0x3F / 100;

            // Combinar para ambos canales (izquierdo y derecho)
            ushort volumeReg = (ushort)((ac97Volume << 8) | ac97Volume);

            // Si el volumen es 0, activar bit de mute
            if (volume == 0)
            {
                volumeReg |= 0x8000;
            }

            // Establecer volumen
            IOPort.Out16((ushort)(_mixerPort + MASTER_VOLUME), volumeReg);
        }

        /// <summary>
        /// Obtiene una representación del estado del controlador
        /// </summary>
        public string GetStatus()
        {
            if (!_initialized)
            {
                return "Not initialized";
            }

            ushort status = IOPort.In16((ushort)(_nabmPort + PO_STATUS));
            byte control = IOPort.In8((ushort)(_nabmPort + PO_CONTROL));
            byte currentIndex = IOPort.In8((ushort)(_nabmPort + PO_CIV));
            byte lastIndex = IOPort.In8((ushort)(_nabmPort + PO_LVI));

            return $"Status: 0x{((ulong)status).ToStringHex()}, Control: 0x{((ulong)control).ToStringHex()}, " +
                   $"Current: {currentIndex}, Last: {lastIndex}, Running: " + ((control & CONTROL_RUN) != 0);
        }

        /// <summary>
        /// Método de apagado
        /// </summary>
        protected override void OnShutdown()
        {
            if (!_initialized)
                return;

            SerialDebug.Info("AC97 Audio: Shutting down");

            // Detener reproducción
            StopPlayback();

            // Reset del controlador
            ResetController();

            // Liberar recursos
            if (_descriptorListPhys != 0)
            {
                Allocator.Free((IntPtr)_descriptorListPhys);
                _descriptorListPhys = 0;
                _descriptorList = null;
            }

            if (_audioBufferPhys != 0)
            {
                Allocator.Free((IntPtr)_audioBufferPhys);
                _audioBufferPhys = 0;
                _audioBuffer = null;
            }

            _initialized = false;
        }

        /// <summary>
        /// Busca un controlador AC97 en la lista de dispositivos PCI
        /// </summary>
        /// <returns>El driver inicializado o null si no se encuentra</returns>
        public static AC97AudioDriver Detect()
        {
            SerialDebug.Info("AC97 Audio: Detecting controller");

            // Buscar el dispositivo PCI
            for (int i = 0; i < PCIMMIOManager.GetDevices().Count; i++)
            {
                PCIDevice device = PCIMMIOManager.GetDevices()[i];

                // Verificar si es un controlador AC97
                if (device.ID.VendorID == AC97_VENDOR_ID && device.ID.DeviceID == AC97_DEVICE_ID)
                {
                    SerialDebug.Info($"AC97 Audio: Found controller at {device.Location.ToString()}");

                    // Crear e inicializar el driver
                    AC97AudioDriver driver = new AC97AudioDriver(device);
                    if (driver.Initialize())
                    {
                        return driver;
                    }

                    SerialDebug.Warning("AC97 Audio: Failed to initialize detected controller");
                    return null;
                }
            }

            SerialDebug.Info("AC97 Audio: No controller detected");
            return null;
        }
    }
}