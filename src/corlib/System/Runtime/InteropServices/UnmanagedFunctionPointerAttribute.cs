﻿using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public sealed class UnmanagedFunctionPointerAttribute : Attribute
    {
        public UnmanagedFunctionPointerAttribute()
        {
            CallingConvention = CallingConvention.StdCall;
        }

        public UnmanagedFunctionPointerAttribute(CallingConvention callingConvention)
        {
            CallingConvention = callingConvention;
        }

        public CallingConvention CallingConvention { get; }

        public bool BestFitMapping;
        public bool SetLastError;
        public bool ThrowOnUnmappableChar;
        public CharSet CharSet;
    }
}