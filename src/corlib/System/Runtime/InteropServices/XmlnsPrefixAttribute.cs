using System;

namespace System.Runtime.InteropServices
{
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
    public class XmlnsPrefixAttribute : Attribute
    {
        private string v1;
        private string v2;

        public XmlnsPrefixAttribute(string v1, string v2)
        {
            this.v1 = v1;
            this.v2 = v2;
        }
    }
}