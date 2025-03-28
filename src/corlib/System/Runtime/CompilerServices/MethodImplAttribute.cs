namespace System.Runtime.CompilerServices
{
    // Atributo que especifica detalles sobre cómo se debe implementar un método
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    public sealed class MethodImplAttribute : Attribute
    {
        // Valor que especifica la implementación del método
        private MethodImplOptions _val;
        
        // Constructor que acepta opciones como entero
        public MethodImplAttribute(int val)
        {
            _val = (MethodImplOptions)val;
        }
        
        // Constructor que acepta opciones directamente
        public MethodImplAttribute(MethodImplOptions methodImplOptions)
        {
            _val = methodImplOptions;
        }
        
        // Propiedad para obtener o establecer las opciones de implementación
        public MethodImplOptions Value
        {
            get { return _val; }
        }
    }
    
    // Opciones disponibles para la implementación de métodos
    [Flags]
    public enum MethodImplOptions
    {
        // No se especifica ninguna opción particular
        Unmanaged = 0x0004,
        
        // No verificar si se sobrescribe un método virtual
        NoInlining = 0x0008,
        
        // Método será compilado usando optimizaciones agresivas
        AggressiveInlining = 0x0100,
        
        // El método no generará transiciones entre código administrado y no administrado
        NoOptimization = 0x0040,
        
        // El JIT no debe generar métodos de ayuda que verifiquen accesos a arrays
        AggressiveOptimization = 0x0200,
        
        // El método contiene instrucciones en IL no verificables
        ForwardRef = 0x0020,
        
        // El método es interno o síncrono
        Synchronized = 0x0020,
        
        // El método está implementado en hardware
        InternalCall = 0x1000,
        
        // Método compilado por defecto
        Default = 0x0000
    }
}