using System;

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface, Inherited = false)]
    public sealed class UnmanagedAttribute : Attribute
    {
        public UnmanagedAttribute() { }
    }
}