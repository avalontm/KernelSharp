namespace System
{
    public interface FormattableString
    {
        string Format { get; }
        object[] GetArguments();
    }

    public class DefaultFormattableString : FormattableString
    {
        private readonly string _format;
        private readonly object[] _arguments;

        public DefaultFormattableString(string format, object[] arguments)
        {
            _format = format;
            _arguments = arguments;
        }

        public string Format => _format;

        public object[] GetArguments() => _arguments;

        public override string ToString()
        {
            return String.Format(_format, _arguments);
        }
    }
}
