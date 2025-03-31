using System.Runtime.InteropServices;

namespace System.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class RuntimeExportAttribute : Attribute
    {
        public string ExportName { get; }
        public CallingConvention CallingConvention { get; set; } = CallingConvention.Cdecl;
        public RuntimeExportAttribute(string entry)
        {
            ExportName = entry;
        }
    }
}