using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Internal.Runtime.CompilerServices
{
    /// <summary>
    /// Provides low-level functionality for pointer manipulation.
    /// </summary>
    [CLSCompliant(false)]
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
            // Explicit handling for common types to avoid __Canon issues
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
                return 1;
            if (typeof(T) == typeof(char) || typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
                return 2;
            if (typeof(T) == typeof(int) || typeof(T) == typeof(uint) || typeof(T) == typeof(float))
                return 4;
            if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong) || typeof(T) == typeof(double))
                return 8;
            if (typeof(T) == typeof(decimal))
                return 16;
            if (typeof(T) == typeof(IntPtr) || typeof(T) == typeof(UIntPtr) || typeof(T).IsPointer)
                return IntPtr.Size;

            // For value types, use Marshal.SizeOf when available
            if (typeof(T).IsValueType)
            {
                // Create an instance to get the size
                T instance = default!;
                // Use direct pointer arithmetic for the size
                byte* nullPtr = null;
                byte* ptr = (byte*)&instance;
                return (int)(ptr - nullPtr);
            }

            // For reference types, return the size of a pointer
            return IntPtr.Size;
        }

        /// <summary>
        /// Converts the given object to the specified type, without performing dynamic type checking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T As<T>(object? value) where T : class?
        {
            // Direct conversion without verification
            return (T)value!;
        }

        /// <summary>
        /// Reinterprets the given reference as a reference to a value of type TTo.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TTo As<TFrom, TTo>(ref TFrom source)
        {
            // Implementation using pointers for primitive types
            return ref *(TTo*)AsPointer(ref source);
        }

        /// <summary>
        /// Adds an element offset to the given reference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Add<T>(ref T source, int elementOffset)
        {
            // Calculate bytes for specific types to avoid __Canon issues
            int byteOffset;

            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
                byteOffset = elementOffset * 1;
            else if (typeof(T) == typeof(char) || typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
                byteOffset = elementOffset * 2;
            else if (typeof(T) == typeof(int) || typeof(T) == typeof(uint) || typeof(T) == typeof(float))
                byteOffset = elementOffset * 4;
            else if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong) || typeof(T) == typeof(double))
                byteOffset = elementOffset * 8;
            else
                byteOffset = elementOffset * SizeOf<T>();

            return ref AddByteOffset(ref source, byteOffset);
        }

        /// <summary>
        /// Adds an element offset to the given reference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Add<T>(ref T source, IntPtr elementOffset)
        {
            // Calculate the offset in bytes (safe cast as we control both sides)
            return ref Add(ref source, (int)elementOffset);
        }

        /// <summary>
        /// Adds an element offset to the given pointer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* Add<T>(void* source, int elementOffset)
        {
            // Direct pointer arithmetic based on type
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
                return (byte*)source + elementOffset;
            if (typeof(T) == typeof(char) || typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
                return (byte*)source + (elementOffset * 2);
            if (typeof(T) == typeof(int) || typeof(T) == typeof(uint) || typeof(T) == typeof(float))
                return (byte*)source + (elementOffset * 4);
            if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong) || typeof(T) == typeof(double))
                return (byte*)source + (elementOffset * 8);

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
            return (nint)AsPointer(ref left) > (nint)AsPointer(ref right);
        }

        /// <summary>
        /// Determines whether the memory address referenced by left is less than
        /// the memory address referenced by right.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAddressLessThan<T>(ref T left, ref T right)
        {
            // Compare addresses
            return (nint)AsPointer(ref left) < (nint)AsPointer(ref right);
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
        /// Reads a value of type T from the given location without alignment requirements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadUnaligned<T>(void* source)
        {
            // Manually handle common types to avoid __Canon issues
            if (typeof(T) == typeof(byte))
                return (T)(object)(*(byte*)source);
            if (typeof(T) == typeof(sbyte))
                return (T)(object)(*(sbyte*)source);
            if (typeof(T) == typeof(short))
                return (T)(object)(*(short*)source);
            if (typeof(T) == typeof(ushort))
                return (T)(object)(*(ushort*)source);
            if (typeof(T) == typeof(int))
                return (T)(object)(*(int*)source);
            if (typeof(T) == typeof(uint))
                return (T)(object)(*(uint*)source);
            if (typeof(T) == typeof(long))
                return (T)(object)(*(long*)source);
            if (typeof(T) == typeof(ulong))
                return (T)(object)(*(ulong*)source);
            if (typeof(T) == typeof(float))
                return (T)(object)(*(float*)source);
            if (typeof(T) == typeof(double))
                return (T)(object)(*(double*)source);

            // Default case - may not work for all types in AOT
            return *(T*)source;
        }

        /// <summary>
        /// Reads a value of type T from the given location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadUnaligned<T>(ref byte source)
        {
            return ReadUnaligned<T>(AsPointer(ref source));
        }

        /// <summary>
        /// Writes a value of type T to the given location without alignment requirements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUnaligned<T>(void* destination, T value)
        {
            // Manually handle common types to avoid __Canon issues
            if (typeof(T) == typeof(byte))
                *(byte*)destination = (byte)(object)value;
            else if (typeof(T) == typeof(sbyte))
                *(sbyte*)destination = (sbyte)(object)value;
            else if (typeof(T) == typeof(short))
                *(short*)destination = (short)(object)value;
            else if (typeof(T) == typeof(ushort))
                *(ushort*)destination = (ushort)(object)value;
            else if (typeof(T) == typeof(int))
                *(int*)destination = (int)(object)value;
            else if (typeof(T) == typeof(uint))
                *(uint*)destination = (uint)(object)value;
            else if (typeof(T) == typeof(long))
                *(long*)destination = (long)(object)value;
            else if (typeof(T) == typeof(ulong))
                *(ulong*)destination = (ulong)(object)value;
            else if (typeof(T) == typeof(float))
                *(float*)destination = (float)(object)value;
            else if (typeof(T) == typeof(double))
                *(double*)destination = (double)(object)value;
            else
                *(T*)destination = value; // May not work for all types in AOT
        }

        /// <summary>
        /// Writes a value of type T to the given location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUnaligned<T>(ref byte destination, T value)
        {
            WriteUnaligned(AsPointer(ref destination), value);
        }

        /// <summary>
        /// Adds a byte offset to the given reference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddByteOffset<T>(ref T source, int byteOffset)
        {
            // Direct implementation using pointers
            byte* ptr = (byte*)AsPointer(ref source) + byteOffset;
            return ref *(T*)ptr;
        }

        /// <summary>
        /// Adds a byte offset to the given reference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddByteOffset<T>(ref T source, IntPtr byteOffset)
        {
            // Cast to int for simplicity, we control both sides
            return ref AddByteOffset(ref source, (int)byteOffset);
        }

        /// <summary>
        /// Adds a byte offset to the given reference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ref T AddByteOffset<T>(ref T source, nuint byteOffset)
        {
            // Convert to IntPtr for the public method
            return ref AddByteOffset(ref source, (int)byteOffset);
        }

        /// <summary>
        /// Reads a value of type T from the given location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(void* source)
        {
            // Like ReadUnaligned, but assumes aligned memory
            return ReadUnaligned<T>(source);
        }

        /// <summary>
        /// Reads a value of type T from the given location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(ref byte source)
        {
            return Read<T>(AsPointer(ref source));
        }

        /// <summary>
        /// Writes a value of type T to the given location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(void* destination, T value)
        {
            // Like WriteUnaligned, but assumes aligned memory
            WriteUnaligned<T>(destination, value);
        }

        /// <summary>
        /// Writes a value of type T to the given location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(ref byte destination, T value)
        {
            Write<T>(AsPointer(ref destination), value);
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
            // Direct implementation
            fixed (T* p = &source)
            {
                return ref *p;
            }
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
            // IMPORTANT: This is unsafe and should only be used in very specific scenarios
            // Use a local pinned null pointer
            void* p = null;
            return ref *(T*)p;
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
            // We create a dummy buffer and use it to initialize value without really initializing it
            byte* dummy = stackalloc byte[SizeOf<T>()];
            value = Read<T>(dummy);
        }

        // Métodos adicionales para tipos específicos (evita problemas con __Canon)

        /// <summary>
        /// Returns the size of an int.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOfInt() => sizeof(int);

        /// <summary>
        /// Returns the size of a long.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOfLong() => sizeof(long);

        /// <summary>
        /// Returns the size of a byte.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOfByte() => sizeof(byte);

        /// <summary>
        /// Returns the size of a pointer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOfPointer() => IntPtr.Size;

        /// <summary>
        /// Manually ensures that the commonly used generic methods are compiled for specific types.
        /// This helps avoid __Canon issues in AOT compilation.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void EnsureGenericInstantiations()
        {
            // This method should never be called, it's just to force the compiler
            // to generate code for these specific instantiations

            // SizeOf
            int s1 = SizeOf<int>();
            int s2 = SizeOf<byte>();
            int s3 = SizeOf<long>();
            int s4 = SizeOf<IntPtr>();

            // As<TFrom, TTo>
            byte b = 0;
            ref int ri = ref As<byte, int>(ref b);

            // Add
            int i = 0;
            ref int ri2 = ref Add<int>(ref i, 1);

            // AddByteOffset
            ref int ri3 = ref AddByteOffset<int>(ref i, 4);

            // Read/Write
            byte b2 = 0;
            int i2 = Read<int>(ref b2);
            Write<int>(ref b2, 0);
        }
    }
}