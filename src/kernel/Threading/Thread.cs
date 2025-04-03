using System;
using System.Runtime;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerHelpers;
using Kernel.Diagnostics;
using Kernel.Memory;

namespace Kernel.Threading
{
    /*
    /// <summary>
    /// Estados posibles de un hilo
    /// </summary>
    public enum ThreadState
    {
        New,
        Running,
        Waiting,
        Suspended,
        Terminated
    }
    */
    /// <summary>
    /// Clase que representa un hilo de ejecución
    /// </summary>
    public unsafe class Thread
    {
        /*
        // Tamaño predeterminado de la pila del hilo (8 KB)
        private const int DEFAULT_STACK_SIZE = 8 * 1024;

        // ID único del hilo
        private int _id;

        // Estado actual del hilo
        private ThreadState _state;

        // Nombre del hilo (opcional)
        private string _name;

        // Indica si el hilo es un hilo de fondo
        private bool _isBackground;

        // Puntero a la pila del hilo
        private byte* _stackPointer;

        // Tamaño de la pila
        private int _stackSize;

        // Delegado que representa el método a ejecutar
        private Action _threadStart;

        // Pila de contexto para la ejecución
        private ThreadContext _context;

        // Contador global de IDs de hilo
        private static int _nextThreadId = 1;

        // Hilo actual en ejecución
        private static Thread _currentThread;

        // Mutex para operaciones relacionadas con hilos
        private static object _threadLock = new object();

        /// <summary>
        /// Estructura que almacena el contexto de ejecución de un hilo
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 16)]
        private struct ThreadContext
        {
            // Registros de propósito general
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
            public ulong RIP;      // Puntero de instrucción
            public ulong RFLAGS;   // Flags
        }
        
        /// <summary>
        /// Crea un nuevo hilo para ejecutar el método especificado
        /// </summary>
        /// <param name="start">Método a ejecutar</param>
        public Thread(Action start) : this(start, DEFAULT_STACK_SIZE)
        {
        }

        /// <summary>
        /// Crea un nuevo hilo con el tamaño de pila especificado
        /// </summary>
        /// <param name="start">Método a ejecutar</param>
        /// <param name="stackSize">Tamaño de la pila en bytes</param>
        public Thread(Action start, int stackSize)
        {
            lock (_threadLock)
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

            // Asignar memoria para la pila
            _stackPointer = (byte*)Allocator.malloc((nuint)_stackSize);
            if (_stackPointer == null)
            {
                ThrowHelpers.OutOfMemoryException("No se pudo asignar memoria para la pila del hilo.");
            }

            // Inicializar el contexto del hilo
            InitializeContext();
        }

        /// <summary>
        /// Inicializa el contexto de ejecución del hilo
        /// </summary>
        private void InitializeContext()
        {
            // Limpiar el contexto
            _context = new ThreadContext();

            // Configurar puntero de pila (apunta al final de la pila - crece hacia abajo)
            _context.RSP = (ulong)(_stackPointer + _stackSize - 8);

            // Alinear la pila a 16 bytes (requerido por la ABI)
            _context.RSP &= ~15UL;

            lock (_threadLock)
            {
                _context.RIP = (ulong)(delegate*<IntPtr, void>)&ThreadStartWrapper;
            }

            // Establecer flags iniciales (solo interrupciones habilitadas)
            _context.RFLAGS = 0x200; // IF=1 (interrupciones habilitadas)

            // Parámetros para ThreadStartWrapper (this en RDI según la convención de llamada)
            _context.RDI = (ulong)GCHandle.ToIntPtr(GCHandle.Alloc(this));
        }

        /// <summary>
        /// Función wrapper en ensamblador que prepara y ejecuta el método del hilo
        /// </summary>
        [RuntimeExport("ThreadStartWrapper")]
        private static void ThreadStartWrapper(IntPtr threadHandle)
        {
            // Recuperar el objeto Thread desde el handle
            GCHandle handle = GCHandle.FromIntPtr(threadHandle);
            Thread thread = (Thread)handle.Target;
            handle.Free();

            // Establecer como hilo actual
            _currentThread = thread;
            thread._state = ThreadState.Running;

            // Ejecutar el método del hilo
            thread._threadStart();

            // Marcar el hilo como terminado
            thread._state = ThreadState.Terminated;
            _currentThread = null;

            // Si hay un planificador, ceder el control
            if (Scheduler.IsInitialized)
            {
                Scheduler.Yield();
            }
            else
            {
                // Si no hay planificador, simplemente detener el hilo
                while (true)
                {
                    Native.Halt();
                }
            }
        }

        /// <summary>
        /// Inicia la ejecución del hilo
        /// </summary>
        public void Start()
        {
            if (_state != ThreadState.New)
            {
               ThrowHelpers.InvalidOperationException("El hilo ya ha sido iniciado.");
            }

            // Registrar el hilo con el planificador
            if (Scheduler.IsInitialized)
            {
                Scheduler.AddThread(this);
            }
            else
            {
                // Si no hay planificador, iniciar directamente el hilo
                _state = ThreadState.Running;
                SwitchToThread();
            }
        }

        /// <summary>
        /// Cambia el contexto al de este hilo
        /// </summary>
        internal void SwitchToThread()
        {
            // Guardar el contexto actual si hay un hilo en ejecución
            Thread current = _currentThread;
            if (current != null && current != this)
            {
                SaveContext(current);
            }

            // Establecer como hilo actual
            _currentThread = this;

            // Restaurar contexto y cambiar a este hilo
            RestoreContext(this);
        }

        /// <summary>
        /// Guarda el contexto de un hilo
        /// </summary>
        [DllImport("*", EntryPoint = "_SaveThreadContext")]
        private static extern void SaveContext(Thread thread);

        /// <summary>
        /// Restaura el contexto de un hilo
        /// </summary>
        [DllImport("*", EntryPoint = "_RestoreThreadContext")]
        private static extern void RestoreContext(Thread thread);

        /// <summary>
        /// Suspende la ejecución del hilo actual durante el tiempo especificado
        /// </summary>
        /// <param name="milliseconds">Tiempo de espera en milisegundos</param>
        public static void Sleep(int milliseconds)
        {
            if (Scheduler.IsInitialized)
            {
                // Usar el planificador para la espera
                Scheduler.Sleep(milliseconds);
            }
            else
            {
                // Espera simple (aproximada)
                for (int i = 0; i < milliseconds * 10000; i++)
                {
                    Native.Pause();
                }
            }
        }

        /// <summary>
        /// Cede la ejecución a otro hilo
        /// </summary>
        public static void Yield()
        {
            if (Scheduler.IsInitialized)
            {
                Scheduler.Yield();
            }
            else
            {
                // Si no hay planificador, una pequeña pausa
                Native.Pause();
            }
        }

        /// <summary>
        /// Espera a que el hilo termine
        /// </summary>
        public void Join()
        {
            // No puede unirse a sí mismo
            if (this == _currentThread)
            {
                ThrowHelpers.InvalidOperationException("Un hilo no puede unirse a sí mismo.");
            }

            // Esperar hasta que el hilo termine
            while (_state != ThreadState.Terminated)
            {
                Yield();
            }
        }

        /// <summary>
        /// Libera los recursos utilizados por el hilo
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
            
        }

        public void Resume()
        {
          
        }

        /// <summary>
        /// Obtiene o establece el nombre del hilo
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Obtiene el ID del hilo
        /// </summary>
        public int Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Obtiene el estado actual del hilo
        /// </summary>
        public ThreadState State
        {
            get { return _state; }
        }

        /// <summary>
        /// Obtiene o establece si el hilo es un hilo de fondo
        /// </summary>
        public bool IsBackground
        {
            get { return _isBackground; }
            set { _isBackground = value; }
        }

        /// <summary>
        /// Indica si el hilo está vivo (en ejecución o esperando)
        /// </summary>
        public bool IsAlive
        {
            get { return _state == ThreadState.Running || _state == ThreadState.Waiting; }
        }

        /// <summary>
        /// Obtiene el hilo actual
        /// </summary>
        public static Thread CurrentThread
        {
            get { return _currentThread; }
        }

        /// <summary>
        /// Obtiene dirección del contexto para el cambio de contexto
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
        }*/
    }
}