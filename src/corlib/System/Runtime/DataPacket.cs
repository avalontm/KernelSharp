namespace System.Runtime.InteropServices
{
    // Fundamental structure for native data transmission
    [StructLayout(LayoutKind.Sequential)]
    public struct DataPacket
    {
        // Pointer to the data buffer
        public IntPtr Pointer;

        // Length of the data
        public int Length;

        // Optional flags or metadata
        public int Flags;

        // Constructors
        public DataPacket(IntPtr pointer, int length)
        {
            Pointer = pointer;
            Length = length;
            Flags = 0;
        }

        public DataPacket(IntPtr pointer, int length, int flags)
        {
            Pointer = pointer;
            Length = length;
            Flags = flags;
        }

        // Method to validate the packet
        public bool IsValid()
        {
            return Pointer != IntPtr.Zero && Length > 0;
        }
    }
}