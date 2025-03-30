namespace System
{
    public abstract class FormattableString
    {
        public abstract string Format { get; }
        public abstract object[] GetArguments();
        public abstract int ArgumentCount { get; }
        public abstract object GetArgument(int index);
        public abstract string ToString(IFormatProvider formatProvider);


    }
}
