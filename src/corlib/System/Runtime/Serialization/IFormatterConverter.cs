using System;

namespace System.Runtime.Serialization
{
    //
    // Resumen:
    //     Provides the connection between an instance of System.Runtime.Serialization.SerializationInfo
    //     and the formatter-provided class best suited to parse the data inside the System.Runtime.Serialization.SerializationInfo.
    //[CLSCompliant(false)]
    public interface IFormatterConverter
    {
        //
        // Resumen:
        //     Converts a value to the given System.Type.
        //
        // Parámetros:
        //   value:
        //     The object to be converted.
        //
        //   type:
        //     The System.Type into which value is to be converted.
        //
        // Devuelve:
        //     The converted value.
        object Convert(object value, Type type);
        //
        // Resumen:
        //     Converts a value to the given System.TypeCode.
        //
        // Parámetros:
        //   value:
        //     The object to be converted.
        //
        //   typeCode:
        //     The System.TypeCode into which value is to be converted.
        //
        // Devuelve:
        //     The converted value.
        object Convert(object value, TypeCode typeCode);
        //
        // Resumen:
        //     Converts a value to a System.Boolean.
        //
        // Parámetros:
        //   value:
        //     The object to be converted.
        //
        // Devuelve:
        //     The converted value.
        bool ToBoolean(object value);
        //
        // Resumen:
        //     Converts a value to an 8-bit unsigned integer.
        //
        // Parámetros:
        //   value:
        //     The object to be converted.
        //
        // Devuelve:
        //     The converted value.
        byte ToByte(object value);
        //
        // Resumen:
        //     Converts a value to a Unicode character.
        //
        // Parámetros:
        //   value:
        //     The object to be converted.
        //
        // Devuelve:
        //     The converted value.
        char ToChar(object value);
        //
        // Resumen:
        //     Converts a value to a System.DateTime.
        //
        // Parámetros:
        //   value:
        //     The object to be converted.
        //
        // Devuelve:
        //     The converted value.
        DateTime ToDateTime(object value);
        //
        // Resumen:
        //     Converts a value to a System.Decimal.
        //
        // Parámetros:
        //   value:
        //     The object to be converted.
        //
        // Devuelve:
        //     The converted value.
        //decimal ToDecimal(object value);
        //
        // Resumen:
        //     Converts a value to a double-precision floating-point number.
        //
        // Parámetros:
        //   value:
        //     The object to be converted.
        //
        // Devuelve:
        //     The converted value.
        double ToDouble(object value);
        //
        // Resumen:
        //     Converts a value to a 16-bit signed integer.
        //
        // Parámetros:
        //   value:
        //     The object to be converted.
        //
        // Devuelve:
        //     The converted value.
        short ToInt16(object value);
        //
        // Resumen:
        //     Converts a value to a 32-bit signed integer.
        //
        // Parámetros:
        //   value:
        //     The object to be converted.
        //
        // Devuelve:
        //     The converted value.
        int ToInt32(object value);
        //
        // Resumen:
        //     Converts a value to a 64-bit signed integer.
        //
        // Parámetros:
        //   value:
        //     The object to be converted.
        //
        // Devuelve:
        //     The converted value.
        long ToInt64(object value);
        //
        // Resumen:
        //     Converts a value to a System.SByte.
        //
        // Parámetros:
        //   value:
        //     The object to be converted.
        //
        // Devuelve:
        //     The converted value.
        sbyte ToSByte(object value);
        //
        // Resumen:
        //     Converts a value to a single-precision floating-point number.
        //
        // Parámetros:
        //   value:
        //     The object to be converted.
        //
        // Devuelve:
        //     The converted value.
        float ToSingle(object value);
        //
        // Resumen:
        //     Converts a value to a System.String.
        //
        // Parámetros:
        //   value:
        //     The object to be converted.
        //
        // Devuelve:
        //     The converted value.
        string? ToString(object value);
        //
        // Resumen:
        //     Converts a value to a 16-bit unsigned integer.
        //
        // Parámetros:
        //   value:
        //     The object to be converted.
        //
        // Devuelve:
        //     The converted value.
        ushort ToUInt16(object value);
        //
        // Resumen:
        //     Converts a value to a 32-bit unsigned integer.
        //
        // Parámetros:
        //   value:
        //     The object to be converted.
        //
        // Devuelve:
        //     The converted value.
        uint ToUInt32(object value);
        //
        // Resumen:
        //     Converts a value to a 64-bit unsigned integer.
        //
        // Parámetros:
        //   value:
        //     The object to be converted.
        //
        // Devuelve:
        //     The converted value.
        ulong ToUInt64(object value);
    }
}
