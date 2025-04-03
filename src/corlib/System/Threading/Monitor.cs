using System.Runtime.InteropServices;

namespace System.Threading
{
    public static unsafe class Monitor
    {
        public static void Enter(object obj)
        {
           //Lock();
        }

        public static void Exit(object obj)
        {
             Unlock();
        }
        
        [DllImport("*", EntryPoint = "_Lock")]
        static extern void Lock();

        [DllImport("*", EntryPoint = "_Unlock")]
        static extern void Unlock();
    }
}
