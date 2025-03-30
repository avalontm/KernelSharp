using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public static unsafe class Marshal
    {
        // Field offset calculation
        public static int OffsetOf<T>(string fieldName) where T : struct
        {
            // In a real implementation, this would use reflection
            // This is a simplified placeholder
            ThrowHelpers.NotImplementedException("Actual field offset calculation requires runtime support");

            return 0;
        }

        // Size of a type
        public static int SizeOf<T>() where T : struct
        {
            // In a real implementation, this would introspect the type's size
            return Unsafe.SizeOf<T>();
        }

        // Allocate unmanaged memory
        public static IntPtr AllocHGlobal(int cb)
        {
            return (IntPtr)MemoryHelpers.Malloc((uint)cb);
        }

        // Allocate unmanaged memory with a specific size
        public static IntPtr AllocHGlobal(IntPtr cb)
        {
            return (IntPtr)MemoryHelpers.Malloc((uint)cb);
        }

        // Free unmanaged memory
        public static void FreeHGlobal(IntPtr hglobal)
        {
            // In a minimal implementation, we might just set the pointer to null
            // A full implementation would actually free the memory
            hglobal = IntPtr.Zero;
        }

        // Read a value from unmanaged memory
        public static T PtrToStructure<T>(IntPtr ptr) where T : struct
        {
            if (ptr == IntPtr.Zero)
                ThrowHelpers.ArgumentNullException(nameof(ptr));

            return Unsafe.Read<T>((void*)ptr);
        }

        // Write a value to unmanaged memory
        public static void StructureToPtr<T>(T structure, IntPtr ptr, bool fDeleteOld) where T : struct
        {
            if (ptr == IntPtr.Zero)
                ThrowHelpers.ArgumentNullException(nameof(ptr));

            Unsafe.Write<T>((void*)ptr, structure);
        }

        // Copy memory block
        public static void Copy(IntPtr source, byte[] destination, int startIndex, int length)
        {
            if (source == IntPtr.Zero)
                ThrowHelpers.ArgumentNullException(nameof(source));
            if (destination == null)
                ThrowHelpers.ArgumentNullException(nameof(destination));
            if (startIndex < 0 || length < 0 || startIndex + length > destination.Length)
                ThrowHelpers.ArgumentOutOfRangeException();

            byte* src = (byte*)source;
            for (int i = 0; i < length; i++)
            {
                destination[startIndex + i] = src[i];
            }
        }

        public static void Copy(IntPtr source, IntPtr destination, int startIndex, int length)
        {
            if (source == IntPtr.Zero)
                ThrowHelpers.ArgumentNullException(nameof(source));
            if (destination == IntPtr.Zero)
                ThrowHelpers.ArgumentNullException(nameof(destination));
            if (startIndex < 0 || length < 0)
                ThrowHelpers.ArgumentOutOfRangeException();

            byte* src = (byte*)source;
            byte* dest = (byte*)destination;

            for (int i = 0; i < length; i++)
            {
                dest[startIndex + i] = src[i];
            }
        }

        // Get raw pointer to an object's data
        public static IntPtr GetRawDataAddress(object obj)
        {
            if (obj == null)
                ThrowHelpers.ArgumentNullException(nameof(obj));

            return Unsafe.As<object, IntPtr>(ref obj);
        }

        // Basic memory zeroing
        public static void ZeroMemory(IntPtr ptr, int size)
        {
            if (ptr == IntPtr.Zero)
                ThrowHelpers.ArgumentNullException(nameof(ptr));

            byte* p = (byte*)ptr;
            for (int i = 0; i < size; i++)
            {
                p[i] = 0;
            }
        }

        // Pointer to string conversion (ASCII)
        public static string PtrToStringAnsi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return null;

            int length = 0;
            byte* bytePtr = (byte*)ptr;
            while (bytePtr[length] != 0) length++;

            return String.FromASCII(ptr, length);
        }

        // String to pointer conversion (ASCII)
        public static IntPtr StringToHGlobalAnsi(string s)
        {
            if (s == null)
                return IntPtr.Zero;

            int length = s.Length;
            IntPtr ptr = AllocHGlobal(length + 1);
            byte* bytePtr = (byte*)ptr;

            for (int i = 0; i < length; i++)
            {
                bytePtr[i] = (byte)s[i];
            }
            bytePtr[length] = 0; // Null terminator

            return ptr;
        }

        // Free memory allocated by CoTaskMemAlloc
        [RuntimeExport("FreeCoTaskMem")]
        public static void FreeCoTaskMem(IntPtr ptr)
        {
            // In a typical CoreLib, this would free unmanaged memory
            // For a minimal implementation, we'll just do a no-op
            // In a real system, this would call the appropriate memory free method
            if (ptr == IntPtr.Zero)
                return;

            // Placeholder for actual memory freeing
            // In a full implementation, this would use system-specific memory management
        }

        // Allocate memory using CoTaskMemAlloc
        public static IntPtr CoTaskMemAlloc(int cb)
        {
            if (cb < 0)
               ThrowHelpers.ArgumentException("Allocation size must be non-negative");

            // In a real implementation, this would allocate unmanaged memory
            // For now, just return a placeholder
            return new IntPtr(cb);
        }

    }
}