using Internal.Runtime;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    public abstract unsafe class Enum : ValueType
    {
        [Intrinsic]
        public bool HasFlag(Enum flag)
        {
            return false;
        }

        public override string ToString()
        {
            // Convert to string
            return GetUnderlyingValue().ToString();
        }

        // Helper method to get the underlying value as a specific type
        private unsafe object GetUnderlyingValue()
        {
            switch (m_pEEType->ElementType)
            {
                case EETypeElementType.Byte:
                    return (Byte)(object)this;
                case EETypeElementType.SByte:
                    return (SByte)(object)this;
                case EETypeElementType.Int16:
                    return (Int16)(object)this;
                case EETypeElementType.UInt16:
                    return (UInt16)(object)this;
                case EETypeElementType.Int32:
                    return (Int32)(object)this;
                case EETypeElementType.UInt32:
                    return (UInt32)(object)this;
                case EETypeElementType.Int64:
                    return (Int64)(object)this;
                case EETypeElementType.UInt64:
                    return (UInt64)(object)this;
                default:
                    return "Enum";
            }
        }
    }
}

