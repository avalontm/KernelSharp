using System.Runtime.Serialization;
using System;
using Internal.Runtime.CompilerHelpers;

namespace System.Reflection
{
    public abstract partial class Assembly : ICustomAttributeProvider, ISerializable
    {
        public object[] GetCustomAttributes(bool inherit)
        {
            return null;
        }

        public object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return null;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            
        }

        public bool IsDefined(Type attributeType, bool inherit)
        {
           return false;
        }
    }
}
