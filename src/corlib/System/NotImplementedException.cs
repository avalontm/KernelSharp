using Internal.Runtime.CompilerHelpers;

namespace System
{
    public class NotImplementedException : Exception
    {
        public NotImplementedException()
        {
            ThrowHelpers.NotImplementedException();
        }

        public NotImplementedException(string str) : base(str)
        {
            ThrowHelpers.NotImplementedException(str);
        }
    }
}
