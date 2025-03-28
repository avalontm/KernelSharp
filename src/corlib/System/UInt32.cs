namespace System
{
    public struct UInt32
    {
        public const uint MaxValue = 4294967295u;
        public const uint MinValue = 0u;

        // Internal value of the unsigned integer
        internal uint m_value;

        public unsafe override string ToString()
        {
            // Buffer to store string representation
            char* buffer = stackalloc char[11]; // 10 digits + null terminator for uint
            int position = 10;
            buffer[10] = '\0';

            // Handle zero as a special case
            if (m_value == 0)
                return "0";

            uint remainingValue = m_value;

            // Convert digit by digit
            do
            {
                buffer[--position] = (char)('0' + (remainingValue % 10));
                remainingValue /= 10;
            } while (remainingValue > 0);

            // Create string from the buffer
            return new string(buffer + position, 0, 10 - position);
        }
    }
}