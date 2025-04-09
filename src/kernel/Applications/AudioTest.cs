using Kernel.Diagnostics;
using Kernel.Drivers.Audio;
using System;

namespace Kernel.Applications
{
    /// <summary>
    /// Aplicación de prueba para el controlador de audio AC97
    /// </summary>
    public static class AudioTest
    {
        // Frecuencia de muestreo (debe coincidir con la configurada en el controlador AC97)
        private const int SAMPLE_RATE = 44100;

        // Canales: 2 para estéreo
        private const int CHANNELS = 2;

        // Bits por muestra: 16 bits (PCM)
        private const int BITS_PER_SAMPLE = 16;

        // Tamaño del buffer para un segundo de audio
        private const int BUFFER_SIZE = SAMPLE_RATE * CHANNELS * (BITS_PER_SAMPLE / 8);

        /// <summary>
        /// Main method to test audio
        /// </summary>
        public static void Test()
        {
            SerialDebug.Info("Starting AC97 audio test...");

            // Find the AC97 audio controller
            AC97AudioDriver audioDriver = AC97AudioDriver.Detect();

            if (audioDriver == null)
            {
                SerialDebug.Error("No AC97 controller available.");
                return;
            }

            // Set volume to 75%
            audioDriver.SetVolume(75);

            // Play a 440 Hz tone (A note) for 2 seconds
            PlayTone(audioDriver, 440, 2);

            // Display controller status
            SerialDebug.Info("AC97 controller status: " + audioDriver.GetStatus());

            // Wait a moment for audio playback to complete
            WaitSeconds(3);

            // Stop playback
            audioDriver.StopPlayback();

            SerialDebug.Info("Audio test completed.");
        }

        /// <summary>
        /// Generates and plays a tone of a specific frequency
        /// </summary>
        /// <param name="driver">Audio controller</param>
        /// <param name="frequency">Tone frequency in Hz</param>
        /// <param name="durationSeconds">Duration in seconds</param>
        private static void PlayTone(AC97AudioDriver driver, double frequency, double durationSeconds)
        {
            SerialDebug.Info($"Generating {frequency} Hz tone for {durationSeconds} seconds...");

            // Calculate total number of samples
            int totalSamples = (int)(SAMPLE_RATE * durationSeconds);

            // Divide into one-second fragments to avoid using too much memory
            int bufferSizeInSamples = SAMPLE_RATE;
            int bufferSizeInBytes = bufferSizeInSamples * CHANNELS * (BITS_PER_SAMPLE / 8);

            // Create buffer for one second of audio
            byte[] buffer = new byte[bufferSizeInBytes];

            // Generate and play fragments
            int samplesGenerated = 0;
            while (samplesGenerated < totalSamples)
            {
                // Determine how many samples to generate in this fragment
                int samplesToGenerate = Math.Min(bufferSizeInSamples, totalSamples - samplesGenerated);

                // Generate tone (sine wave)
                GenerateSineWave(buffer, samplesToGenerate, frequency);

                // Play fragment
                driver.WriteAudio(buffer, 0, samplesToGenerate * CHANNELS * (BITS_PER_SAMPLE / 8));

                // Update counter
                samplesGenerated += samplesToGenerate;

                // Show progress
                SerialDebug.Info($"Playing audio: {samplesGenerated * 100 / totalSamples}% completed");

                // Wait a bit for the driver to process the buffer
                WaitMilliseconds(100);
            }

            SerialDebug.Info("Tone generated and sent to the audio controller.");
        }

        /// <summary>
        /// Genera una onda senoidal en un buffer de audio PCM de 16 bits estéreo
        /// </summary>
        /// <param name="buffer">Buffer de bytes para almacenar la onda</param>
        /// <param name="samples">Número de muestras a generar</param>
        /// <param name="frequency">Frecuencia de la onda en Hz</param>
        private static void GenerateSineWave(byte[] buffer, int samples, double frequency)
        {
            // Factor de frecuencia
            double factor = 2.0 * Math.PI * frequency / SAMPLE_RATE;

            // Generar muestras
            for (int i = 0; i < samples; i++)
            {
                // Calcular valor senoidal (-1.0 a 1.0)
                double value = Math.Sin(factor * i);

                // Convertir a rango de int16 (-32768 a 32767) y aplicar amplitud del 50%
                short sampleValue = (short)(value * 16384);

                // Índice en el buffer (cada muestra estéreo ocupa 4 bytes)
                int bufferIndex = i * 4;

                // Escribir valor en formato little-endian para canal izquierdo
                buffer[bufferIndex] = (byte)(sampleValue & 0xFF);
                buffer[bufferIndex + 1] = (byte)((sampleValue >> 8) & 0xFF);

                // Duplicar para canal derecho
                buffer[bufferIndex + 2] = buffer[bufferIndex];
                buffer[bufferIndex + 3] = buffer[bufferIndex + 1];
            }
        }

        /// <summary>
        /// Espera un número específico de segundos
        /// </summary>
        /// <param name="seconds">Segundos a esperar</param>
        private static void WaitSeconds(int seconds)
        {
            WaitMilliseconds(seconds * 1000);
        }

        /// <summary>
        /// Espera un número específico de milisegundos
        /// </summary>
        /// <param name="milliseconds">Milisegundos a esperar</param>
        private static void WaitMilliseconds(int milliseconds)
        {
            // Un delay simple utilizando un contador
            long targetCount = milliseconds * 1000000; // Convertir a nanosegundos aproximados
            for (long i = 0; i < targetCount; i++)
            {
                // Insertar una instrucción de pausa para mejorar la eficiencia
                Native.Pause();
            }
        }
    }
}