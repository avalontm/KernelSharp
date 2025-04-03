using System;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// Provides helper methods for compiler-generated reference assignment operations.
    /// These helpers are used specifically for 32-bit x86 environments.
    /// </summary>
    public static unsafe partial class RuntimeHelpers
    {
        /// <summary>
        /// Assigns a reference type value to a reference type field or variable.
        /// This method is specifically designed for x86 calling convention where ECX register is used.
        /// </summary>
        /// <remarks>
        /// This method is called by the compiler for reference assignments in certain scenarios.
        /// The ECX suffix indicates this version is optimized for the x86 calling convention
        /// where the 'this' pointer or first parameter is passed in the ECX register.
        /// </remarks>
        [RuntimeExport("RhpAssignRefECX")]
        public static void RhpAssignRefECX(ref object destination, object value)
        {
            // Simple implementation: just assign the reference
            destination = value;

            // NOTE: In a full runtime with garbage collection, this would include:
            // 1. Write barriers for generational GC
            // 2. Potential memory fences for concurrency
            // 3. Handling of interior pointers

            // For a basic kernel implementation, the simple assignment is sufficient
        }

        /// <summary>
        /// Assigns a reference type value to a reference type field or variable.
        /// Standard version without register-specific optimizations.
        /// </summary>
        [RuntimeExport("RhpAssignRef")]
        static unsafe void RhpAssignRef(void** address, void* obj)
        {
            *address = obj;
        }

        /// <summary>
        /// Assigns a reference type value to a reference type field or variable.
        /// Variant used for assignment within arithmetic operations.
        /// </summary>
        [RuntimeExport("RhpCheckedAssignRefArithmetic")]
        public static void RhpCheckedAssignRefArithmetic(object destination, object value)
        {
            // Simple implementation: just assign the reference
            destination = value;
        }

        /// <summary>
        /// Assigns a reference from one variable to another.
        /// Used specifically for ByRef assignments.
        /// </summary>
        [RuntimeExport("RhpByRefAssignRef")]
        static unsafe void RhpByRefAssignRef(void** address, void* obj)
        {
            *address = obj;
        }
    }
}