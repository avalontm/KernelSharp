using System;
using System.Collections.Generic;

namespace System.Text
{
    public static class ASCII
    {
        public static string GetString(byte[] aData, int index, int length)
        {
            char[] xResult = new char[length];
            for (int i = 0; i < length; i++)
            {
                xResult[i] = (char)(aData[index + i] & 0xFF);
            }
            return new String(xResult);
        }
    }
}
