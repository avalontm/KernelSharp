using Internal.Runtime.CompilerHelpers;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Proporciona un mecanismo simple para manejar referencias a objetos en un contexto de kernel
    /// </summary>
    public struct GCHandle
    {
        // Referencia al objeto
        private object _target;

        // Indica si el handle ha sido liberado
        private bool _freed;

        // Almacena el puntero nativo
        private IntPtr _handle;

        // Crea un nuevo GCHandle
        private GCHandle(object target)
        {
            _target = target;
            _freed = false;
            _handle = (IntPtr)MemoryHelpers.Malloc((nuint)IntPtr.Size);

            // Guardar la referencia en memoria
            unsafe
            {
                IntPtr* handlePtr = (IntPtr*)_handle;
                *handlePtr = (IntPtr)GCHandle.ToIntPtr(GCHandle.Alloc(_target));
            }
        }

        /// <summary>
        /// Libera los recursos asociados con el handle
        /// </summary>
        public void Free()
        {
            if (!_freed && _handle != IntPtr.Zero)
            {
                MemoryHelpers.Free(_handle);
                _handle = IntPtr.Zero;
                _target = null;
                _freed = true;
            }
        }

        /// <summary>
        /// Obtiene el objeto objetivo
        /// </summary>
        public object Target
        {
            get
            {
                if (_freed)
                    return null;

                unsafe
                {
                    IntPtr* handlePtr = (IntPtr*)_handle;
                    GCHandle gch = GCHandle.FromIntPtr(*handlePtr);
                    return gch.Target;
                }
            }
        }

        /// <summary>
        /// Convierte el handle a un IntPtr
        /// </summary>
        public IntPtr AddrOfPinnedObject()
        {
            if (_freed)
                return IntPtr.Zero;

            return _handle;
        }

        /// <summary>
        /// Crea un nuevo handle para el objeto especificado
        /// </summary>
        public static GCHandle Alloc(object value)
        {
            return new GCHandle(value);
        }

        /// <summary>
        /// Convierte un IntPtr a un GCHandle
        /// </summary>
        public static GCHandle FromIntPtr(IntPtr ptr)
        {
            GCHandle handle = new GCHandle();
            handle._handle = ptr;
            handle._freed = false;

            unsafe
            {
                IntPtr* handlePtr = (IntPtr*)ptr;
                GCHandle gch = GCHandle.FromIntPtr(*handlePtr);
                handle._target = gch.Target;
            }

            return handle;
        }

        /// <summary>
        /// Convierte un GCHandle a IntPtr
        /// </summary>
        public static IntPtr ToIntPtr(GCHandle handle)
        {
            return handle._handle;
        }
    }
}