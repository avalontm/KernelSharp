using System;
using System.Collections.Generic;
using Internal.Runtime.CompilerHelpers;
using Kernel.Diagnostics;

namespace Kernel.Threading
{
    /// <summary>
    /// Cooperative thread scheduler for the kernel
    /// </summary>
    public static class Scheduler
    {
        // List of available threads
        private static List<Thread> _threads;

        // Current thread
        private static Thread _currentThread;

        // Current thread index
        private static int _currentIndex;

        // Indicates if the scheduler is started
        private static bool _initialized;

        // Class for threads waiting with their remaining time
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

        // List of waiting threads
        private static List<WaitingThread> _waitingThreads;

        // Tick counter for timing
        private static int _tickCount;

        /// <summary>
        /// Initializes the thread scheduler
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
                return;

            SerialDebug.Info("Thread scheduler initialization started");

            lock (null)
            {
                SerialDebug.Info("PASO 1");
                _threads = new List<Thread>();
                SerialDebug.Info("PASO 2");
                _waitingThreads = new List<WaitingThread>();
                SerialDebug.Info("PASO 3");
                _currentIndex = -1;
                SerialDebug.Info("PASO 4");
                _tickCount = 0;
                SerialDebug.Info("PASO 5");
                _initialized = true;
                SerialDebug.Info("PASO 6");
                // Register the main thread
                Thread mainThread = Thread.CurrentThread;
                if (mainThread != null)
                {
                    _threads.Add(mainThread);
                    _currentThread = mainThread;
                    _currentIndex = 0;
                }
            }

            SerialDebug.Info("Thread scheduler initialization successful");
        }

        /// <summary>
        /// Adds a thread to the scheduler
        /// </summary>
        public static void AddThread(Thread thread)
        {
            if (!_initialized)
                Initialize();

            if (thread == null)
                ThrowHelpers.ArgumentNullException("thread");

            lock (null)
            {
                // Verify that the thread is not already in the list
                if (!_threads.Contains(thread))
                {
                    _threads.Add(thread);
                    SerialDebug.Info("Thread added to scheduler: " + thread.Name);
                }
            }
        }

        /// <summary>
        /// Removes a thread from the scheduler
        /// </summary>
        public static void RemoveThread(Thread thread)
        {
            if (!_initialized || thread == null)
                return;

            lock (null)
            {
                _threads.Remove(thread);

                // Also remove from the waiting list if present
                for (int i = _waitingThreads.Count - 1; i >= 0; i--)
                {
                    if (_waitingThreads[i].Thread == thread)
                    {
                        _waitingThreads.RemoveAt(i);
                        break;
                    }
                }

                // If it was the current thread, force a context switch
                if (_currentThread == thread)
                {
                    _currentThread = null;
                    _currentIndex = -1;
                    Yield();
                }
            }
        }

        /// <summary>
        /// Puts the current thread to sleep for the specified time
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

            // Get the current thread
            Thread currentThread = Thread.CurrentThread;
            if (currentThread == null)
                return;

            // Calculate wake time
            int wakeTime = _tickCount + milliseconds;

            lock (null)
            {
                // Add to the waiting list
                _waitingThreads.Add(new WaitingThread(currentThread, wakeTime));

                // Remove from the active threads list
                _threads.Remove(currentThread);

                if (_currentThread == currentThread)
                {
                    _currentThread = null;
                    _currentIndex = -1;

                    // Force context switch
                    Yield();
                }
            }
        }

        /// <summary>
        /// Yields the processor to another thread
        /// </summary>
        public static void Yield()
        {
            if (!_initialized)
                return;

            lock (null)
            {
                // Check waiting threads that should wake up
                CheckWaitingThreads();

                // If there are no threads, return immediately
                if (_threads.Count == 0)
                    return;

                // Select the next thread
                _currentIndex = (_currentIndex + 1) % _threads.Count;
                Thread nextThread = _threads[_currentIndex];

                // If it's the same thread, do nothing
                if (nextThread == Thread.CurrentThread)
                    return;

                // Switch to the next thread
                _currentThread = nextThread;
                nextThread.SwitchToThread();
            }
        }

        /// <summary>
        /// Checks waiting threads and wakes up those whose time has expired
        /// </summary>
        private static void CheckWaitingThreads()
        {
            if (_waitingThreads.Count == 0)
                return;

            // Increment the tick counter
            _tickCount++;

            // Check threads that should wake up
            for (int i = _waitingThreads.Count - 1; i >= 0; i--)
            {
                if (_tickCount >= _waitingThreads[i].WakeTime)
                {
                    // Wake up the thread
                    Thread thread = _waitingThreads[i].Thread;
                    _waitingThreads.RemoveAt(i);

                    // Add back to the active threads list
                    if (!_threads.Contains(thread))
                    {
                        _threads.Add(thread);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the currently executing thread
        /// </summary>
        public static Thread CurrentThread
        {
            get { return _currentThread ?? Thread.CurrentThread; }
        }

        /// <summary>
        /// Indicates if the scheduler has been initialized
        /// </summary>
        public static bool IsInitialized
        {
            get { return _initialized; }
        }

        /// <summary>
        /// Gets the number of active threads
        /// </summary>
        public static int ThreadCount
        {
            get
            {
                lock (null)
                {
                    return _threads.Count;
                }
            }
        }

        /// <summary>
        /// Gets the number of waiting threads
        /// </summary>
        public static int WaitingThreadCount
        {
            get
            {
                lock (null)
                {
                    return _waitingThreads.Count;
                }
            }
        }
    }
}