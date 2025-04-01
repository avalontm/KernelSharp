using Internal.Runtime.CompilerHelpers;
using System;

namespace Internal.Runtime
{
    internal class InternalException : Exception
    {
        public InternalException()
        {
        }

        public InternalException(string str) : base(str)
        {
            ThrowHelpers.InternalException(str);
        }
    }
}