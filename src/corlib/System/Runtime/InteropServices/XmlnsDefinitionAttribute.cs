using System;


namespace System.Runtime.InteropServices
{
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
    public class XmlnsDefinitionAttribute : Attribute
    {
        private string v1;
        private string v2;

        public XmlnsDefinitionAttribute(string v1, string v2)
        {
            this.v1 = v1;
            this.v2 = v2;
        }
    }
}