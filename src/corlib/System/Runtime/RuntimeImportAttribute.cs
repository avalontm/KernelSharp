namespace System.Runtime
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    internal sealed class RuntimeImportAttribute : Attribute
    {
        public string DllName { get; }
        public string EntryPoint { get; }

        public RuntimeImportAttribute(string entry)
        {
            EntryPoint = entry;
        }

        public RuntimeImportAttribute(string dllName, string entry)
        {
            EntryPoint = entry;
            DllName = dllName;
        }
    }
}