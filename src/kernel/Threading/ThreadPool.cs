using Kernel.Diagnostics;
using System;
using System.Collections.Generic;

namespace Kernel.Threading
{
    /// <summary>
    /// Basic implementation of a thread pool for the kernel
    /// </summary>
    public class ThreadPool
    {
        // Maximum number of threads in the pool
        private readonly int _maxThreads;

        // Worker threads
        private Thread[] _threads;

        // Queue of pending tasks
        private Queue<WorkItem> _workItems;

        // Pool state
        private bool _isRunning;
        private readonly object _syncRoot;

        // Structure representing a task to execute
        private struct WorkItem
        {
            // Delegate for the action to execute
            public Action TaskAction;

            // Optional parameter for the task
            public object State;

            // Constructor
            public WorkItem(Action taskAction, object state)
            {
                TaskAction = taskAction;
                State = state;
            }
        }

        /// <summary>
        /// Constructor that initializes the pool with a specific number of threads
        /// </summary>
        /// <param name="maxThreads">Maximum number of threads to create</param>
        public ThreadPool(int maxThreads)
        {
            // Validate parameters
            if (maxThreads <= 0)
                maxThreads = Environment.ProcessorCount > 0 ? Environment.ProcessorCount : 1;

            _maxThreads = maxThreads;
            _workItems = new Queue<WorkItem>();
            _syncRoot = new object();
            _isRunning = false;
            _threads = new Thread[_maxThreads];
        }

        public static void Initialize()
        {
            SerialDebug.Info("ThreadPool initialization started");
            Monitor.Initialize();
            Scheduler.Initialize();
            SerialDebug.Info("ThreadPool initialization successful");
        }

        /// <summary>
        /// Starts the thread pool
        /// </summary>
        public void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;

            // Create and start worker threads
            for (int i = 0; i < _maxThreads; i++)
            {
                Thread thread = new Thread(WorkerThreadFunc);
                thread.Name = "ThreadPool-" + i.ToString();
                thread.IsBackground = true;
                _threads[i] = thread;
                thread.Start();

                SerialDebug.Info("Started ThreadPool thread: " + thread.Name);
            }

            SerialDebug.Info("ThreadPool started with " + _maxThreads.ToString() + " threads");
        }

        /// <summary>
        /// Stops the thread pool
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;

            // Signal all threads to stop
            lock (_syncRoot)
            {
                Monitor.PulseAll(_syncRoot);
            }

            // Wait for all threads to finish
            for (int i = 0; i < _maxThreads; i++)
            {
                if (_threads[i] != null && _threads[i].IsAlive)
                {
                    _threads[i].Join();
                }
            }

            // Clear the task queue
            lock (_syncRoot)
            {
                _workItems.Clear();
            }

            SerialDebug.Info("ThreadPool stopped");
        }

        /// <summary>
        /// Queues a task for execution
        /// </summary>
        /// <param name="taskAction">Action to execute</param>
        /// <returns>true if the task is successfully queued, false otherwise</returns>
        public bool QueueUserWorkItem(Action taskAction)
        {
            return QueueUserWorkItem(taskAction, null);
        }

        /// <summary>
        /// Queues a task with state for execution
        /// </summary>
        /// <param name="taskAction">Action to execute</param>
        /// <param name="state">State associated with the task</param>
        /// <returns>true if the task is successfully queued, false otherwise</returns>
        public bool QueueUserWorkItem(Action taskAction, object state)
        {
            if (!_isRunning)
                return false;

            if (taskAction == null)
                return false;

            // Create the work item
            WorkItem workItem = new WorkItem(taskAction, state);

            // Queue in a thread-safe manner
            lock (_syncRoot)
            {
                _workItems.Enqueue(workItem);

                // Wake up a thread to process the new task
                Monitor.Pulse(_syncRoot);
            }

            return true;
        }

        /// <summary>
        /// Main function for worker threads
        /// </summary>
        private void WorkerThreadFunc()
        {
            while (_isRunning)
            {
                WorkItem workItem = default(WorkItem);
                bool hasWork = false;

                // Try to get a task from the queue
                lock (_syncRoot)
                {
                    while (_isRunning && _workItems.Count == 0)
                    {
                        // Wait until work is available
                        Monitor.Wait(_syncRoot);
                    }

                    // Exit if the pool is stopping
                    if (!_isRunning)
                        break;

                    // Get the next work item
                    if (_workItems.Count > 0)
                    {
                        workItem = _workItems.Dequeue();
                        hasWork = true;
                    }
                }

                // Execute the obtained task
                if (hasWork)
                {
                    // Execute the action
                    workItem.TaskAction();
                }
            }

            SerialDebug.Info("ThreadPool thread finished: " + Thread.CurrentThread.Name);
        }

        /// <summary>
        /// Gets the number of threads in the pool
        /// </summary>
        public int ThreadCount
        {
            get { return _maxThreads; }
        }

        /// <summary>
        /// Gets the number of pending tasks
        /// </summary>
        public int PendingWorkItemCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _workItems.Count;
                }
            }
        }

        /// <summary>
        /// Indicates if the pool is running
        /// </summary>
        public bool IsRunning
        {
            get { return _isRunning; }
        }
    }
}