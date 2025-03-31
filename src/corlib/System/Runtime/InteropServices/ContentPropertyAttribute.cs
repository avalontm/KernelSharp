using System;

namespace System.Runtime.InteropServices
{
    public class ContentPropertyAttribute : Attribute
    {
        public string Content { set; get; }

        public ContentPropertyAttribute(string content)
        {
            Content = content;
        }
    }
}