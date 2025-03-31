using System;

namespace System.Runtime.InteropServices
{
    public class GuidAttribute : Attribute
    {
        private string v;

        public GuidAttribute(string v)
        {
            this.v = v;
        }
    }
}