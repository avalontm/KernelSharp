using System;

namespace System.Runtime.InteropServices
{
    [StructLayout(LayoutKind.Sequential)]
    internal class RawArrayData
    {
        public uint Length; // Array._numComponents padded to IntPtr
        public uint Padding;
        public byte Data;
    }

}
