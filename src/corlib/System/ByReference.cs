using System.Runtime.CompilerServices;
namespace System
{
    // ByReference<T> is meant to be used to represent "ref T" fields. It is working
    // around lack of first class support for byref fields in C# and IL. The JIT and
    // type loader has special handling for it that turns it into a thin wrapper around ref T.
    //[NonVersionable]
    internal readonly ref struct ByReference<T>
    {
        // The actual reference value - the JIT will replace this field with a reference
        private readonly IntPtr _value;

        /// <summary>
        /// Creates a new instance of <see cref="ByReference{T}"/>.
        /// </summary>
        /// <param name="value">The value to reference.</param>
        [Intrinsic]
        public ByReference(ref T value)
        {
            // This constructor is implemented by the JIT as an intrinsic
            // This implementation exists as a fallback if the JIT intrinsic is missing

            // Since this is a JIT intrinsic, we implement a simple fallback
            // that will never be reached in normal execution
            _value = IntPtr.Zero; // Placeholder that will never be used
        }

        /// <summary>
        /// Gets the value of the reference.
        /// </summary>
        public ref T Value
        {
            [Intrinsic]
            get
            {
                // This property getter is implemented by the JIT as an intrinsic
                // This implementation exists as a fallback if the JIT intrinsic is missing

                // Since this is a JIT intrinsic, we implement a simple fallback
                // that will never be reached in normal execution
                unsafe
                {
                    T* dummy = null;
                    return ref *dummy;
                }
            }
        }
    }
}