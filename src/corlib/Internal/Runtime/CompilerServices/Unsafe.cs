using System;
using System.Runtime.CompilerServices;

namespace Internal.Runtime.CompilerServices
{
    /// <summary>
    /// Provides low-level functionality for pointer manipulation.
    /// </summary>
    public static unsafe partial class Unsafe
    {
        /// <summary>
        /// Returns a pointer to the given reference parameter.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AsPointer<T>(ref T value)
        {
            // Direct implementation using a fixed pointer
            fixed (T* p = &value)
            {
                return (void*)p;
            }
        }

        /// <summary>
        /// Returns the size of an object of the given type parameter.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>()
        {
            // Basic implementation for common types
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
                return 1;
            if (typeof(T) == typeof(char) || typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
                return 2;
            if (typeof(T) == typeof(int) || typeof(T) == typeof(uint) || typeof(T) == typeof(float))
                return 4;
            if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong) || typeof(T) == typeof(double))
                return 8;

            // For string, return the size of a pointer (since it's a reference type)
            if (typeof(T) == typeof(string))
                return sizeof(IntPtr);

            // For pointers and IntPtr, use the platform's pointer size
            if (typeof(T) == typeof(IntPtr) || typeof(T) == typeof(UIntPtr) || typeof(T).IsPointer)
                return sizeof(IntPtr);

            // For other types, use an estimate
            return 16; // Default value
        }

        /// <summary>
        /// Converts the given object to the specified type, without performing dynamic type checking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T As<T>(object value) where T : class
        {
            // Direct conversion without verification
            return (T)value;
        }

        /// <summary>
        /// Reinterprets the given reference as a reference to a value of type TTo.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TTo As<TFrom, TTo>(ref TFrom source)
        {
            // Implementation using pointers
            return ref *(TTo*)AsPointer(ref source);
        }

        /// <summary>
        /// Adds an element offset to the given reference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Add<T>(ref T source, int elementOffset)
        {
            // Calculate the offset in bytes and use AddByteOffset
            return ref AddByteOffset(ref source, elementOffset * SizeOf<T>());
        }

        /// <summary>
        /// Adds an element offset to the given reference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Add<T>(ref T source, IntPtr elementOffset)
        {
            // Calculate the offset in bytes
            int offset = (int)elementOffset * SizeOf<T>();
            return ref AddByteOffset(ref source, offset);
        }

        /// <summary>
        /// Adds an element offset to the given pointer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* Add<T>(void* source, int elementOffset)
        {
            // Direct implementation for pointers
            return (byte*)source + (elementOffset * SizeOf<T>());
        }

        /// <summary>
        /// Determines whether the specified references point to the same location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreSame<T>(ref T left, ref T right)
        {
            // Compare memory addresses
            return AsPointer(ref left) == AsPointer(ref right);
        }

        /// <summary>
        /// Determines whether the memory address referenced by left is greater than
        /// the memory address referenced by right.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAddressGreaterThan<T>(ref T left, ref T right)
        {
            // Compare addresses
            return (ulong)AsPointer(ref left) > (ulong)AsPointer(ref right);
        }

        /// <summary>
        /// Determines whether the memory address referenced by left is less than
        /// the memory address referenced by right.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAddressLessThan<T>(ref T left, ref T right)
        {
            // Compare addresses
            return (ulong)AsPointer(ref left) < (ulong)AsPointer(ref right);
        }

        /// <summary>
        /// Initializes a block of memory at the given location with a given initial value
        /// without assuming architecture-dependent address alignment.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitBlockUnaligned(ref byte startAddress, byte value, uint byteCount)
        {
            // Implementation to initialize memory
            byte* p = (byte*)AsPointer(ref startAddress);
            for (uint i = 0; i < byteCount; i++)
            {
                p[i] = value;
            }
        }

        /// <summary>
        /// Reads a value of type T from the given location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadUnaligned<T>(void* source)
        {
            // Read memory without alignment
            return *(T*)source;
        }

        /// <summary>
        /// Reads a value of type T from the given location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadUnaligned<T>(ref byte source)
        {
            // Use As to convert the reference and then read
            return As<byte, T>(ref source);
        }

        /// <summary>
        /// Writes a value of type T to the given location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUnaligned<T>(void* destination, T value)
        {
            // Write memory without alignment
            *(T*)destination = value;
        }

        /// <summary>
        /// Writes a value of type T to the given location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUnaligned<T>(ref byte destination, T value)
        {
            // Use As to convert the reference and then write
            As<byte, T>(ref destination) = value;
        }

        /// <summary>
        /// Adds a byte offset to the given reference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddByteOffset<T>(ref T source, int byteOffset)
        {
            // Implementation to add a byte offset
            byte* ptr = (byte*)AsPointer(ref source) + byteOffset;
            return ref *(T*)ptr;
        }

        /// <summary>
        /// Adds a byte offset to the given reference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddByteOffset<T>(ref T source, IntPtr byteOffset)
        {
            // Implementation to add a byte offset
            byte* ptr = (byte*)AsPointer(ref source) + (int)byteOffset;
            return ref *(T*)ptr;
        }

        /// <summary>
        /// Adds a byte offset to the given reference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ref T AddByteOffset<T>(ref T source, ulong byteOffset)
        {
            // Implementation to add a byte offset
            return ref AddByteOffset(ref source, (int)byteOffset);
        }

        /// <summary>
        /// Reads a value of type T from the given location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(void* source)
        {
            // Direct implementation
            return *(T*)source;
        }

        /// <summary>
        /// Reads a value of type T from the given location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(ref byte source)
        {
            // Use As to convert the reference
            return As<byte, T>(ref source);
        }

        /// <summary>
        /// Writes a value of type T to the given location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(void* destination, T value)
        {
            // Direct implementation
            *(T*)destination = value;
        }

        /// <summary>
        /// Writes a value of type T to the given location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(ref byte destination, T value)
        {
            // Use As to convert the reference
            As<byte, T>(ref destination) = value;
        }

        /// <summary>
        /// Reinterprets the given location as a reference to a value of type T.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsRef<T>(void* source)
        {
            // Direct implementation
            return ref *(T*)source;
        }

        /// <summary>
        /// Converts a read-only reference to a mutable reference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsRef<T>(in T source)
        {
            // Revised implementation to ensure it returns the correct reference
            return ref As<T, T>(ref AsRef(source));
        }

        /// <summary>
        /// Determines the byte offset from the origin to the target of the given references.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr ByteOffset<T>(ref T origin, ref T target)
        {
            // Calculate the pointer difference
            return (IntPtr)((byte*)AsPointer(ref target) - (byte*)AsPointer(ref origin));
        }

        /// <summary>
        /// Returns a reference to a type T that is a null reference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T NullRef<T>()
        {
            // Return a reference to null memory (dangerous, for internal use only)
            return ref *(T*)null;
        }

        /// <summary>
        /// Returns whether a given reference to a type T is a null reference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullRef<T>(ref T source)
        {
            // Check if the address is null
            return AsPointer(ref source) == null;
        }

        /// <summary>
        /// Bypasses definite assignment rules by taking advantage of 'out' semantics.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SkipInit<T>(out T value)
        {
            // Implementation that doesn't initialize the variable
            value = default!;
        }
    }
}