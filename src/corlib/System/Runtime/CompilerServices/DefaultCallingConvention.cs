using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Specifies the default calling convention for the entire assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class DefaultCallingConvention : Attribute
    {
        /// <summary>
        /// Gets the default calling convention.
        /// </summary>
        public CallingConvention Convention { get; }


        public DefaultCallingConvention()
        {
        }

        /// <summary>
        /// Initializes a new instance of the DefaultCallingConventionAttribute.
        /// </summary>
        /// <param name="convention">The default calling convention to use.</param>
        public DefaultCallingConvention(CallingConvention convention)
        {
            Convention = convention;    
        }


    }
}
