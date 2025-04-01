namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Provides additional runtime compatibility options for the assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class RuntimeCompatibilityAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether to wrap non-CLS compliant exceptions.
        /// </summary>
        public bool WrapNonExceptionThrows { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable runtime-specific compatibility modes.
        /// </summary>
        public bool EnableCompatibilityMode { get; set; }

        /// <summary>
        /// Gets or sets the runtime version compatibility level.
        /// </summary>
        public string CompatibilityVersion { get; set; }

        /// <summary>
        /// Initializes a new instance of the RuntimeCompatibilityAttribute class.
        /// </summary>
        public RuntimeCompatibilityAttribute()
        {
            WrapNonExceptionThrows = false;
            EnableCompatibilityMode = false;
            CompatibilityVersion = null;
        }
    }
}