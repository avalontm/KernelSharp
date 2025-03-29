using System;

namespace System.Runtime.Serialization
{
    //
    // Resumen:
    //     Allows an object to control its own serialization and deserialization.
    public interface ISerializable
    {
        //
        // Resumen:
        //     Populates a System.Runtime.Serialization.SerializationInfo with the data needed
        //     to serialize the target object.
        //
        // Parámetros:
        //   info:
        //     The System.Runtime.Serialization.SerializationInfo to populate with data.
        //
        //   context:
        //     The destination (see System.Runtime.Serialization.StreamingContext) for this
        //     serialization.
        //
        // Excepciones:
        //   T:System.Security.SecurityException:
        //     The caller does not have the required permission.
        void GetObjectData(SerializationInfo info, StreamingContext context);
    }
}
