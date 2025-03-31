using System.ComponentModel;

namespace System.Runtime.InteropServices
{
    //
    // Resumen:
    //     Indicates how to marshal the array elements when an array is marshaled from managed
    //     to unmanaged code as a System.Runtime.InteropServices.UnmanagedType.SafeArray.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum VarEnum
    {
        //
        // Resumen:
        //     Indicates that a value was not specified.
        VT_EMPTY = 0,
        //
        // Resumen:
        //     Indicates a null value, similar to a null value in SQL.
        VT_NULL = 1,
        //
        // Resumen:
        //     Indicates a short integer.
        VT_I2 = 2,
        //
        // Resumen:
        //     Indicates a long integer.
        VT_I4 = 3,
        //
        // Resumen:
        //     Indicates a float value.
        VT_R4 = 4,
        //
        // Resumen:
        //     Indicates a double value.
        VT_R8 = 5,
        //
        // Resumen:
        //     Indicates a currency value.
        VT_CY = 6,
        //
        // Resumen:
        //     Indicates a DATE value.
        VT_DATE = 7,
        //
        // Resumen:
        //     Indicates a BSTR string.
        VT_BSTR = 8,
        //
        // Resumen:
        //     Indicates an IDispatch pointer.
        VT_DISPATCH = 9,
        //
        // Resumen:
        //     Indicates an SCODE.
        VT_ERROR = 10,
        //
        // Resumen:
        //     Indicates a Boolean value.
        VT_BOOL = 11,
        //
        // Resumen:
        //     Indicates a VARIANT far pointer.
        VT_VARIANT = 12,
        //
        // Resumen:
        //     Indicates an IUnknown pointer.
        VT_UNKNOWN = 13,
        //
        // Resumen:
        //     Indicates a decimal value.
        VT_DECIMAL = 14,
        //
        // Resumen:
        //     Indicates a char value.
        VT_I1 = 16,
        //
        // Resumen:
        //     Indicates a byte.
        VT_UI1 = 17,
        //
        // Resumen:
        //     Indicates an unsignedshort.
        VT_UI2 = 18,
        //
        // Resumen:
        //     Indicates an unsignedlong.
        VT_UI4 = 19,
        //
        // Resumen:
        //     Indicates a 64-bit integer.
        VT_I8 = 20,
        //
        // Resumen:
        //     Indicates an 64-bit unsigned integer.
        VT_UI8 = 21,
        //
        // Resumen:
        //     Indicates an integer value.
        VT_INT = 22,
        //
        // Resumen:
        //     Indicates an unsigned integer value.
        VT_UINT = 23,
        //
        // Resumen:
        //     Indicates a C style void.
        VT_VOID = 24,
        //
        // Resumen:
        //     Indicates an HRESULT.
        VT_HRESULT = 25,
        //
        // Resumen:
        //     Indicates a pointer type.
        VT_PTR = 26,
        //
        // Resumen:
        //     Indicates a SAFEARRAY. Not valid in a VARIANT.
        VT_SAFEARRAY = 27,
        //
        // Resumen:
        //     Indicates a C style array.
        VT_CARRAY = 28,
        //
        // Resumen:
        //     Indicates a user defined type.
        VT_USERDEFINED = 29,
        //
        // Resumen:
        //     Indicates a null-terminated string.
        VT_LPSTR = 30,
        //
        // Resumen:
        //     Indicates a wide string terminated by null.
        VT_LPWSTR = 31,
        //
        // Resumen:
        //     Indicates a user defined type.
        VT_RECORD = 36,
        //
        // Resumen:
        //     Indicates a FILETIME value.
        VT_FILETIME = 64,
        //
        // Resumen:
        //     Indicates length prefixed bytes.
        VT_BLOB = 65,
        //
        // Resumen:
        //     Indicates that the name of a stream follows.
        VT_STREAM = 66,
        //
        // Resumen:
        //     Indicates that the name of a storage follows.
        VT_STORAGE = 67,
        //
        // Resumen:
        //     Indicates that a stream contains an object.
        VT_STREAMED_OBJECT = 68,
        //
        // Resumen:
        //     Indicates that a storage contains an object.
        VT_STORED_OBJECT = 69,
        //
        // Resumen:
        //     Indicates that a blob contains an object.
        VT_BLOB_OBJECT = 70,
        //
        // Resumen:
        //     Indicates the clipboard format.
        VT_CF = 71,
        //
        // Resumen:
        //     Indicates a class ID.
        VT_CLSID = 72,
        //
        // Resumen:
        //     Indicates a simple, counted array.
        VT_VECTOR = 4096,
        //
        // Resumen:
        //     Indicates a SAFEARRAY pointer.
        VT_ARRAY = 8192,
        //
        // Resumen:
        //     Indicates that a value is a reference.
        VT_BYREF = 16384
    }
}
