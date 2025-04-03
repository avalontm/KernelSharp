using System;
using System.Collections.Generic;
using Internal.Runtime.CompilerHelpers;
using Kernel.Diagnostics;

namespace Kernel.Threading
{
    /// <summary>
    /// Planificador cooperativo de hilos para el kernel
    /// </summary>
    public static class Scheduler
    {
        /*
        // Lista de hilos disponibles
        private static List<Thread> _threads;

        // Hilo actual
        private static Thread _currentThread;

        // Índice del hilo actual
        private static int _currentIndex;

        // Indica si el planificador está iniciado
        private static bool _initialized;

        // Mutex para operaciones del planificador
        private static object _schedulerLock = new object();

        // Lista de hilos en espera con su tiempo restante
        private class WaitingThread
        {
            public Thread Thread;
            public int WakeTime;

            public WaitingThread(Thread thread, int wakeTime)
            {
                Thread = thread;
                WakeTime = wakeTime;
            }
        }

        // Lista de hilos en espera
        private static List<WaitingThread> _waitingThreads;

        // Contador de tics para temporización
        private static int _tickCount;

        /// <summary>
        /// Inicializa el planificador de hilos
        /// </summary>
        public static void Initialize()
        {
            lock (_schedulerLock)
            {
                if (_initialized)
                    return;

                _threads = new List<Thread>();
                _waitingThreads = new List<WaitingThread>();
                _currentIndex = -1;
                _tickCount = 0;
                _initialized = true;

                // Registrar el hilo principal
                Thread mainThread = Thread.CurrentThread;
                if (mainThread != null)
                {
                    _threads.Add(mainThread);
                    _currentThread = mainThread;
                    _currentIndex = 0;
                }

                SerialDebug.Info("Planificador de hilos inicializado");
            }
        }

        /// <summary>
        /// Añade un hilo al planificador
        /// </summary>
        public static void AddThread(Thread thread)
        {
            if (!_initialized)
                Initialize();

            if (thread == null)
                ThrowHelpers.ArgumentNullException("thread");

            lock (_schedulerLock)
            {
                // Verificar que el hilo no esté ya en la lista
                if (!_threads.Contains(thread))
                {
                    _threads.Add(thread);
                    SerialDebug.Info("Hilo añadido al planificador: " + thread.Name);
                }
            }
        }

        /// <summary>
        /// Elimina un hilo del planificador
        /// </summary>
        public static void RemoveThread(Thread thread)
        {
            if (!_initialized || thread == null)
                return;

            lock (_schedulerLock)
            {
                _threads.Remove(thread);

                // Eliminar también de la lista de espera si estuviera
                for (int i = _waitingThreads.Count - 1; i >= 0; i--)
                {
                    if (_waitingThreads[i].Thread == thread)
                    {
                        _waitingThreads.RemoveAt(i);
                        break;
                    }
                }

                // Si era el hilo actual, forzar un cambio de contexto
                if (_currentThread == thread)
                {
                    _currentThread = null;
                    _currentIndex = -1;
                    Yield();
                }
            }
        }

        /// <summary>
        /// Pone el hilo actual en espera durante el tiempo especificado
        /// </summary>
        public static void Sleep(int milliseconds)
        {
            if (!_initialized)
                return;

            if (milliseconds <= 0)
            {
                Yield();
                return;
            }

            // Obtener el hilo actual
            Thread currentThread = Thread.CurrentThread;
            if (currentThread == null)
                return;

            // Calcular el tiempo de despertar
            int wakeTime = _tickCount + milliseconds;

            lock (_schedulerLock)
            {
                // Añadir a la lista de espera
                _waitingThreads.Add(new WaitingThread(currentThread, wakeTime));

                // Remover de la lista de hilos activos
                _threads.Remove(currentThread);

                if (_currentThread == currentThread)
                {
                    _currentThread = null;
                    _currentIndex = -1;

                    // Forzar cambio de contexto
                    Yield();
                }
            }
        }

        /// <summary>
        /// Cede el procesador a otro hilo
        /// </summary>
        public static void Yield()
        {
            if (!_initialized)
                return;

            lock (_schedulerLock)
            {
                // Verificar hilos en espera que deben despertar
                CheckWaitingThreads();

                // Si no hay hilos, retornar inmediatamente
                if (_threads.Count == 0)
                    return;

                // Seleccionar el siguiente hilo
                _currentIndex = (_currentIndex + 1) % _threads.Count;
                Thread nextThread = _threads[_currentIndex];

                // Si es el mismo hilo, no hacer nada
                if (nextThread == Thread.CurrentThread)
                    return;

                // Cambiar al siguiente hilo
                _currentThread = nextThread;
                nextThread.SwitchToThread();
            }
        }

        /// <summary>
        /// Verifica los hilos en espera y despierta aquellos cuyo tiempo ha expirado
        /// </summary>
        private static void CheckWaitingThreads()
        {
            if (_waitingThreads.Count == 0)
                return;

            // Incrementar el contador de tics
            _tickCount++;

            // Verificar los hilos que deben despertar
            for (int i = _waitingThreads.Count - 1; i >= 0; i--)
            {
                if (_tickCount >= _waitingThreads[i].WakeTime)
                {
                    // Despertar el hilo
                    Thread thread = _waitingThreads[i].Thread;
                    _waitingThreads.RemoveAt(i);

                    // Añadir de nuevo a la lista de hilos activos
                    if (!_threads.Contains(thread))
                    {
                        _threads.Add(thread);
                    }
                }
            }
        }

        /// <summary>
        /// Obtiene el hilo que se está ejecutando actualmente
        /// </summary>
        public static Thread CurrentThread
        {
            get { return _currentThread ?? Thread.CurrentThread; }
        }

        /// <summary>
        /// Indica si el planificador ha sido inicializado
        /// </summary>
        public static bool IsInitialized
        {
            get { return _initialized; }
        }

        /// <summary>
        /// Obtiene el número de hilos activos
        /// </summary>
        public static int ThreadCount
        {
            get
            {
                lock (_schedulerLock)
                {
                    return _threads.Count;
                }
            }
        }

        /// <summary>
        /// Obtiene el número de hilos en espera
        /// </summary>
        public static int WaitingThreadCount
        {
            get
            {
                lock (_schedulerLock)
                {
                    return _waitingThreads.Count;
                }
            }
        }*/
    }
}