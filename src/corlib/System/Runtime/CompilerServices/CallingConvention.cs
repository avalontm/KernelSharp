namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Specifies the calling convention for platform invoke method calls.
    /// </summary>
    public enum CallingConvention
    {

        /// <summary>
        /// Use the default calling convention (typically Winapi/StdCall on Windows).
        /// </summary>
        Winapi = 1,

        /// <summary>
        /// The C declaration calling convention (cdecl).
        /// </summary>
        Cdecl = 2,

        /// <summary>
        /// Standard calling convention (stdcall).
        /// </summary>
        StdCall = 3,

        /// <summary>
        /// This call convention (used by C++ member functions).
        /// </summary>
        ThisCall = 4,

        /// <summary>
        /// Fast calling convention (uses registers for first arguments).
        /// </summary>
        FastCall = 5
    }

}