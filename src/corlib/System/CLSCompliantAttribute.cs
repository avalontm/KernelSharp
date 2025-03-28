namespace System
{
    /// <summary>
    /// Indica si un elemento de código es compatible con el Common Language Specification (CLS).
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | 
        AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | 
        AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | 
        AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.Delegate | 
        AttributeTargets.ReturnValue, Inherited = false)]
    public sealed class CLSCompliantAttribute : Attribute
    {
        private bool _isCompliant;

        /// <summary>
        /// Inicializa una nueva instancia de CLSCompliantAttribute con un valor que indica si 
        /// el elemento de código marcado es compatible con CLS.
        /// </summary>
        /// <param name="isCompliant">
        /// true si el elemento de código marcado cumple con CLS; de lo contrario, false.
        /// </param>
        public CLSCompliantAttribute(bool isCompliant)
        {
            _isCompliant = isCompliant;
        }

        /// <summary>
        /// Obtiene el valor que indica si el elemento de código marcado es compatible con CLS.
        /// </summary>
        public bool IsCompliant
        {
            get { return _isCompliant; }
        }
    }
}