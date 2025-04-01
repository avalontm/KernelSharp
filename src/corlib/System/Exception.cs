namespace System
{
    public abstract class Exception
    {
        private string _exceptionString;

        public Exception() { }

        public Exception(String str)
        {
            _exceptionString = str;
        }
    }

}
