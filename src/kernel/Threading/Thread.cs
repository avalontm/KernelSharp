using System;
using System.Runtime;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerHelpers;
using Kernel.Diagnostics;
using Kernel.Memory;

namespace Kernel.Threading
{
    /// <summary>
    /// Possible thread states
    /// </summary>
    public enum ThreadState
    {
        New,
        Running,
        Waiting,
        Suspended,
        Terminated
    }

    /// <summary>
    /// Class representing an execution thread
    /// </summary>
    public unsafe class Thread
    {
        // Default thread stack size (8 KB)
        private const int DEFAULT_STACK_SIZE = 8 * 1024;

        // Unique thread ID
        private int _id;

        // Current thread state
        private ThreadState _state;

        // Thread name (optional)
        private string _name;

        // Indicates if the thread is a background thread
        private bool _isBackground;

        // Pointer to the thread stack
        private byte* _stackPointer;

        // Stack size
        private int _stackSize;

        // Delegate representing the method to execute
        private Action _threadStart;

        // Execution context stack
        private ThreadContext _context;

        // Global thread ID counter
        private static int _nextThreadId = 1;

        // Currently executing thread
        private static Thread _currentThread;

        /// <summary>
        /// Structure that stores a thread's execution context
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 16)]
        private struct ThreadContext
        {
            // General purpose registers
            public ulong RAX;
            public ulong RBX;
            public ulong RCX;
            public ulong RDX;
            public ulong RSI;
            public ulong RDI;
            public ulong RBP;
            public ulong RSP;
            public ulong R8;
            public ulong R9;
            public ulong R10;
            public ulong R11;
            public ulong R12;
            public ulong R13;
            public ulong R14;
            public ulong R15;
            public ulong RIP;      // Instruction pointer
            public ulong RFLAGS;   // Flags
        }

        /// <summary>
        /// Creates a new thread to execute the specified method
        /// </summary>
        /// <param name="start">Method to execute</param>
        public Thread(Action start) : this(start, DEFAULT_STACK_SIZE)
        {
        }

        /// <summary>
        /// Creates a new thread with the specified stack size
        /// </summary>
        /// <param name="start">Method to execute</param>
        /// <param name="stackSize">Stack size in bytes</param>
        public Thread(Action start, int stackSize)
        {
            lock (null)
            {
                _id = _nextThreadId++;
                _context.RIP = (ulong)(delegate*<IntPtr, void>)&ThreadStartWrapper;
            }

            if (start == null)
            {
                ThrowHelpers.ArgumentNullException("start");
            }

            _threadStart = start;
            _stackSize = stackSize > 0 ? stackSize : DEFAULT_STACK_SIZE;
            _state = ThreadState.New;
            _name = "Thread-" + _id.ToString();
            _isBackground = false;

            // Allocate memory for the stack
            _stackPointer = (byte*)Allocator.malloc((nuint)_stackSize);
            if (_stackPointer == null)
            {
                ThrowHelpers.OutOfMemoryException("Could not allocate memory for thread stack.");
            }

            // Initialize thread context
            InitializeContext();

            SerialDebug.Info("Thread created: " + _name);
        }

        /// <summary>
        /// Initializes the thread's execution context
        /// </summary>
        private void InitializeContext()
        {
            // Clear the context
            _context = new ThreadContext();

            // Configure stack pointer (points to end of stack - grows downward)
            _context.RSP = (ulong)(_stackPointer + _stackSize - 8);

            // Align stack to 16 bytes (required by ABI)
            _context.RSP &= ~15UL;

            lock (null)
            {
                _context.RIP = (ulong)(delegate*<IntPtr, void>)&ThreadStartWrapper;
            }

            // Set initial flags (interrupts enabled only)
            _context.RFLAGS = 0x200; // IF=1 (interrupts enabled)

            // Parameters for ThreadStartWrapper (this in RDI as per calling convention)
            _context.RDI = (ulong)GCHandle.ToIntPtr(GCHandle.Alloc(this));
        }

        /// <summary>
        /// Assembly wrapper function that prepares and executes the thread method
        /// </summary>
        [RuntimeExport("ThreadStartWrapper")]
        private static void ThreadStartWrapper(IntPtr threadHandle)
        {
            // Recover Thread object from handle
            GCHandle handle = GCHandle.FromIntPtr(threadHandle);
            Thread thread = (Thread)handle.Target;
            handle.Free();

            // Set as current thread
            _currentThread = thread;
            thread._state = ThreadState.Running;

            SerialDebug.Info("Thread started: " + thread._name);

            // Execute the thread method
            thread._threadStart();

            // Mark thread as terminated
            thread._state = ThreadState.Terminated;
            _currentThread = null;

            SerialDebug.Info("Thread terminated: " + thread._name);

            // If there's a scheduler, yield control
            if (Scheduler.IsInitialized)
            {
                Scheduler.Yield();
            }
            else
            {
                // If there's no scheduler, simply stop the thread
                while (true)
                {
                    Native.Halt();
                }
            }
        }

        /// <summary>
        /// Starts thread execution
        /// </summary>
        public void Start()
        {
            if (_state != ThreadState.New)
            {
                ThrowHelpers.InvalidOperationException("Thread has already been started.");
            }

            SerialDebug.Info("Starting thread: " + _name);

            // Register the thread with the scheduler
            if (Scheduler.IsInitialized)
            {
                Scheduler.AddThread(this);
            }
            else
            {
                // If there's no scheduler, start the thread directly
                _state = ThreadState.Running;
                SwitchToThread();
            }
        }

        /// <summary>
        /// Switches context to this thread
        /// </summary>
        internal void SwitchToThread()
        {
            // Save current context if there's a thread running
            Thread current = _currentThread;
            if (current != null && current != this)
            {
                SaveContext(current);
            }

            // Set as current thread
            _currentThread = this;

            // Restore context and switch to this thread
            RestoreContext(this);
        }

        /// <summary>
        /// Saves a thread's context
        /// </summary>
        [DllImport("*", EntryPoint = "_SaveThreadContext")]
        private static extern void SaveContext(Thread thread);

        /// <summary>
        /// Restores a thread's context
        /// </summary>
        [DllImport("*", EntryPoint = "_RestoreThreadContext")]
        private static extern void RestoreContext(Thread thread);

        /// <summary>
        /// Suspends execution of the current thread for the specified time
        /// </summary>
        /// <param name="milliseconds">Wait time in milliseconds</param>
        public static void Sleep(int milliseconds)
        {
            if (Scheduler.IsInitialized)
            {
                // Use the scheduler for waiting
                Scheduler.Sleep(milliseconds);
            }
            else
            {
                // Simple wait (approximate)
                for (int i = 0; i < milliseconds * 10000; i++)
                {
                    Native.Pause();
                }
            }
        }

        /// <summary>
        /// Yields execution to another thread
        /// </summary>
        public static void Yield()
        {
            if (Scheduler.IsInitialized)
            {
                Scheduler.Yield();
            }
            else
            {
                // If there's no scheduler, a small pause
                Native.Pause();
            }
        }

        /// <summary>
        /// Waits for the thread to terminate
        /// </summary>
        public void Join()
        {
            // Cannot join itself
            if (this == _currentThread)
            {
                ThrowHelpers.InvalidOperationException("A thread cannot join itself.");
            }

            // Wait until the thread terminates
            while (_state != ThreadState.Terminated)
            {
                Yield();
            }
        }

        /// <summary>
        /// Releases resources used by the thread
        /// </summary>
        public override void Dispose()
        {
            if (_stackPointer != null)
            {
                //MemoryHelpers.Free((IntPtr)_stackPointer);
                _stackPointer = null;
            }
            base.Dispose();
        }

        public void Suspend()
        {
            // To be implemented
        }

        public void Resume()
        {
            // To be implemented
        }

        /// <summary>
        /// Gets or sets the thread name
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets the thread ID
        /// </summary>
        public int Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets the current thread state
        /// </summary>
        public ThreadState State
        {
            get { return _state; }
        }

        /// <summary>
        /// Gets or sets whether the thread is a background thread
        /// </summary>
        public bool IsBackground
        {
            get { return _isBackground; }
            set { _isBackground = value; }
        }

        /// <summary>
        /// Indicates if the thread is alive (running or waiting)
        /// </summary>
        public bool IsAlive
        {
            get { return _state == ThreadState.Running || _state == ThreadState.Waiting; }
        }

        /// <summary>
        /// Gets the current thread
        /// </summary>
        public static Thread CurrentThread
        {
            get { return _currentThread; }
        }

        /// <summary>
        /// Gets context address for context switching
        /// </summary>
        internal IntPtr ContextPointer
        {
            get
            {
                fixed (ThreadContext* contextPtr = &_context)
                {
                    return (IntPtr)contextPtr;
                }
            }
        }
    }
}