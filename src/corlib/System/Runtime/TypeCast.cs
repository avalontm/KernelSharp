using Internal.Runtime;
using Internal.Runtime.CompilerHelpers;

namespace System.Runtime
{
    public static class TypeCast
    {
        [Flags]
        internal enum AssignmentVariation
        {
            Normal = 0,

            /// <summary>
            /// Assume the source type is boxed so that value types and enums are compatible with Object, ValueType 
            /// and Enum (if applicable)
            /// </summary>
            BoxedSource = 1,

            /// <summary>
            /// Allow identically sized integral types and enums to be considered equivalent (currently used only for 
            /// array element types)
            /// </summary>
            AllowSizeEquivalence = 2,
        }

        [RuntimeExport("RhTypeCast_CheckCastClass")]
        public static unsafe object CheckCastClass(EEType* pTargetEEType, object obj)
        {
            // a null value can be cast to anything
            if (obj == null)
                return null;

            object result = StartupCodeHelpers.RhTypeCast_IsInstanceOfClass(pTargetEEType, obj);

            if (result == null)
            {
                // Throw the invalid cast exception defined by the classlib, using the input EEType* 
                // to find the correct classlib.

                //throw pTargetEEType->GetClasslibException(ExceptionIDs.InvalidCast);
                return null;
            }

            return result;
        }

       // [RuntimeExport("RhTypeCast_CheckCastArray")]
        public static unsafe object CheckCastArray(EEType* pTargetEEType, object obj)
        {
            // a null value can be cast to anything
            if (obj == null)
                return null;

            object result = IsInstanceOfArray(pTargetEEType, obj);

            if (result == null)
            {
                // Throw the invalid cast exception defined by the classlib, using the input EEType* 
                // to find the correct classlib.

                //throw pTargetEEType->GetClasslibException(ExceptionIDs.InvalidCast);
                return null;
            }

            return result;
        }

        [RuntimeExport("RhTypeCast_IsInstanceOfArray")]
        public static unsafe object IsInstanceOfArray(EEType* pTargetType, object obj)
        {
            if (obj == null)
            {
                return null;
            }

            EEType* pObjType = obj.m_pEEType;

            // if the types match, we are done
            if (pObjType == pTargetType)
            {
                return obj;
            }

            // if the object is not an array, we're done
            if (!pObjType->IsArray)
            {
                return null;
            }

            // compare the array types structurally

            if (pObjType->ParameterizedTypeShape != pTargetType->ParameterizedTypeShape)
            {
                // If the shapes are different, there's one more case to check for: Casting SzArray to MdArray rank 1.
                if (!pObjType->IsSzArray || pTargetType->ArrayRank != 1)
                {
                    return null;
                }
            }

            /*
            if (CastCache.AreTypesAssignableInternal(pObjType->RelatedParameterType, pTargetType->RelatedParameterType,
                AssignmentVariation.AllowSizeEquivalence, null))
            {
                return obj;
            }
            */

            return null;
        }

        [RuntimeExport("RhTypeCast_AreTypesEquivalent")]
        public static unsafe bool AreTypesEquivalent(EEType* pType1, EEType* pType2)
        {
            if (pType1 == pType2)
                return true;

            if (pType1->IsCloned)
                pType1 = pType1->CanonicalEEType;

            if (pType2->IsCloned)
                pType2 = pType2->CanonicalEEType;

            if (pType1 == pType2)
                return true;

            if (pType1->IsParameterizedType && pType2->IsParameterizedType)
                return AreTypesEquivalent(pType1->RelatedParameterType, pType2->RelatedParameterType) && pType1->ParameterizedTypeShape == pType2->ParameterizedTypeShape;

            return false;
        }

        private unsafe struct Key
        {
            private IntPtr _sourceTypeAndVariation;
            private IntPtr _targetType;


            public Key(EEType* pSourceType, EEType* pTargetType, AssignmentVariation variation)
            {
                //Debug.Assert((((long)pSourceType) & 3) == 0, "misaligned EEType!");
                //Debug.Assert(((uint)variation) <= 3, "variation enum has an unexpectedly large value!");

                _sourceTypeAndVariation = (IntPtr)(((byte*)pSourceType) + ((int)variation));
                _targetType = (IntPtr)pTargetType;
            }

            private static int GetHashCode(IntPtr intptr)
            {
                return unchecked((int)((long)intptr));
            }

            public int CalculateHashCode()
            {
                return ((GetHashCode(_targetType) >> 4) ^ GetHashCode(_sourceTypeAndVariation));
            }

            public bool Equals(ref Key other)
            {
                return (_sourceTypeAndVariation == other._sourceTypeAndVariation) && (_targetType == other._targetType);
            }

            public AssignmentVariation Variation
            {
                get { return (AssignmentVariation)(unchecked((int)(long)_sourceTypeAndVariation) & 3); }
            }

            public EEType* SourceType { get { return (EEType*)(((long)_sourceTypeAndVariation) & ~3L); } }
            public EEType* TargetType { get { return (EEType*)_targetType; } }
        }
    }
}
