using Internal.Runtime;
using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;

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

            object result = IsInstanceOfClass(pTargetEEType, obj);

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

        //[RuntimeExport("RhTypeCast_AreTypesEquivalent")]
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

        /// <summary>
        /// Checks if an object can be cast to a specific type.
        /// </summary>
        /// <param name="pTargetType">Target EEType to cast to</param>
        /// <param name="obj">Object to cast</param>
        /// <returns>The object if cast is valid, null otherwise</returns>
        [RuntimeExport("RhTypeCast_CheckCastAny")]
        public static unsafe object CheckCastAny(EEType* pTargetType, object obj)
        {
            // Null can be cast to any reference type
            if (obj == null)
                return null;

            // Get the object's EEType
            EEType* pObjType = obj.m_pEEType;

            // If types are exactly the same, return the object
            if (pObjType == pTargetType)
                return obj;

            // Check if target type is an interface
            if (pTargetType->IsInterface)
            {
                // TODO: Implement interface checking logic
                // This is a placeholder - you'll need to implement actual interface checking
                return null;
            }

            // Check if it's an array
            if (pTargetType->IsArray)
            {
                return IsInstanceOfArray(pTargetType, obj);
            }

            // Check class hierarchy
            EEType* currentType = pObjType;
            while (currentType != null)
            {
                if (currentType == pTargetType)
                    return obj;

                // Move up the inheritance chain
                currentType = currentType->RawBaseType;
            }

            // Cast is not valid
            return null;
        }

        /// <summary>
        /// Checks if an object can be cast to a specific interface type.
        /// </summary>
        /// <param name="pTargetType">Target interface EEType to cast to</param>
        /// <param name="obj">Object to cast</param>
        /// <returns>The object if cast is valid, null otherwise</returns>
        [RuntimeExport("RhTypeCast_CheckCastInterface")]
        public static unsafe object CheckCastInterface(EEType* pTargetType, object obj)
        {
            // Null can be cast to any interface
            if (obj == null)
                return null;

            // Get the object's EEType
            EEType* pObjType = obj.m_pEEType;

            // If object type is already an interface, do a direct comparison
            if (pObjType == pTargetType)
                return obj;

            // If target is not an interface, this is an invalid cast
            if (!pTargetType->IsInterface)
                return null;

            // If object type is an interface, it can't implement another interface directly
            if (pObjType->IsInterface)
                return null;

            // Perform basic interface checking
            EEType* currentType = pObjType;
            while (currentType != null)
            {
                // Check direct interface implementation
                if (HasInterface(currentType, pTargetType))
                    return obj;

                // Move up the inheritance hierarchy
                currentType = currentType->RawBaseType;
            }

            // No matching interface found
            return null;
        }

        /// <summary>
        /// Checks if a type implements a specific interface.
        /// </summary>
        /// <param name="pType">Type to check for interface implementation</param>
        /// <param name="pInterfaceType">Interface type to find</param>
        /// <returns>True if the type implements the interface, false otherwise</returns>
        private static unsafe bool HasInterface(EEType* pType, EEType* pInterfaceType)
        {
            // Basic validation
            if (pType == null || pInterfaceType == null)
                return false;

            // Check if the type has any interfaces
            if (pType->NumInterfaces == 0)
                return false;

            // TODO: Implement actual interface table walking
            // This is a placeholder implementation
            // In a real runtime, you would:
            // 1. Access the interface table of the type
            // 2. Iterate through implemented interfaces
            // 3. Check for exact match or compatible generic interfaces

            // Conservatively return false
            return false;
        }

        /// <summary>
        /// Performs a specialized class cast check with additional type compatibility logic.
        /// </summary>
        /// <param name="pTargetType">Target EEType to cast to</param>
        /// <param name="obj">Object to cast</param>
        /// <returns>The object if cast is valid, null otherwise</returns>
        [RuntimeExport("RhTypeCast_CheckCastClassSpecial")]
        public static unsafe object CheckCastClassSpecial(EEType* pTargetType, object obj)
        {
            // Null can be cast to any reference type
            if (obj == null)
                return null;

            // Get the object's EEType
            EEType* pObjType = obj.m_pEEType;

            // If types are exactly the same, return the object
            if (pObjType == pTargetType)
                return obj;

            // Handle special type conversions
            // Check for compatibility with Object, ValueType, Enum
            if (IsSpecialTypeCompatible(pTargetType, pObjType))
                return obj;

            // Check class hierarchy
            EEType* currentType = pObjType;
            while (currentType != null)
            {
                if (currentType == pTargetType)
                    return obj;

                // Move up the inheritance chain
                currentType = currentType->RawBaseType;
            }

            // Additional checks for boxed value types or enums
            if (IsBoxedTypeCompatible(pTargetType, pObjType))
                return obj;

            // Cast is not valid
            return null;
        }

        /// <summary>
        /// Checks compatibility with special base types like Object, ValueType, Enum
        /// </summary>
        private static unsafe bool IsSpecialTypeCompatible(EEType* pTargetType, EEType* pObjType)
        {
            // Check compatibility with Object
            if (IsObjectType(pTargetType))
                return true;

            // Check compatibility with ValueType
            if (IsValueTypeType(pTargetType) && pObjType->IsValueType)
                return true;

            // Check compatibility with Enum
            if (IsEnumType(pTargetType) && pObjType->IsEnum)
                return true;

            return false;
        }

        /// <summary>
        /// Checks compatibility for boxed value types or enums
        /// </summary>
        private static unsafe bool IsBoxedTypeCompatible(EEType* pTargetType, EEType* pObjType)
        {
            // Placeholder for more complex boxed type checking
            // In a real implementation, this would involve:
            // 1. Checking if the object is a boxed value type
            // 2. Checking type equivalence or convertibility
            return false;
        }

        /// <summary>
        /// Checks if the given type is the Object base type
        /// </summary>
        private static unsafe bool IsObjectType(EEType* pType)
        {
            // TODO: Implement proper Object type identification
            // This might involve checking against a well-known Object EEType
            return false;
        }

        /// <summary>
        /// Checks if the given type is the ValueType base type
        /// </summary>
        private static unsafe bool IsValueTypeType(EEType* pType)
        {
            // TODO: Implement proper ValueType type identification
            // This might involve checking against a well-known ValueType EEType
            return false;
        }

        /// <summary>
        /// Checks if the given type is the Enum base type
        /// </summary>
        private static unsafe bool IsEnumType(EEType* pType)
        {
            // TODO: Implement proper Enum type identification
            // This might involve checking against a well-known Enum EEType
            return false;
        }

        /// <summary>
        /// Determines if an object is an instance of a specific type.
        /// </summary>
        /// <param name="pTargetType">Target EEType to check against</param>
        /// <param name="obj">Object to check</param>
        /// <returns>The object if it is an instance of the target type, null otherwise</returns>
        [RuntimeExport("RhTypeCast_IsInstanceOfAny")]
        public static unsafe object IsInstanceOfAny(EEType* pTargetType, object obj)
        {
            // Null can be an instance of any reference type
            if (obj == null)
                return null;

            // Get the object's EEType
            EEType* pObjType = obj.m_pEEType;

            // If types are exactly the same, return the object
            if (pObjType == pTargetType)
                return obj;

            // Check if target type is an interface
            if (pTargetType->IsInterface)
            {
                return CheckCastInterface(pTargetType, obj);
            }

            // Check if target type is an array
            if (pTargetType->IsArray)
            {
                return IsInstanceOfArray(pTargetType, obj);
            }

            // Check class hierarchy
            EEType* currentType = pObjType;
            while (currentType != null)
            {
                if (currentType == pTargetType)
                    return obj;

                // Move up the inheritance chain
                currentType = currentType->RawBaseType;
            }

            // Check for special type compatibility
            if (IsSpecialTypeCompatible(pTargetType, pObjType))
                return obj;

            // Not an instance of the target type
            return null;
        }

        /// <summary>
        /// Determines if an object is an instance of a specific interface.
        /// </summary>
        /// <param name="pTargetType">Target interface EEType to check against</param>
        /// <param name="obj">Object to check</param>
        /// <returns>The object if it is an instance of the target interface, null otherwise</returns>
        [RuntimeExport("RhTypeCast_IsInstanceOfInterface")]
        public static unsafe object IsInstanceOfInterface(EEType* pTargetType, object obj)
        {
            // Null can be an instance of any interface
            if (obj == null)
                return null;

            // Get the object's EEType
            EEType* pObjType = obj.m_pEEType;

            // If object type is already the target interface, return the object
            if (pObjType == pTargetType)
                return obj;

            // Ensure the target type is actually an interface
            if (!pTargetType->IsInterface)
                return null;

            // If object type is an interface, it can't implement another interface
            if (pObjType->IsInterface)
                return null;

            // Walk the inheritance hierarchy
            EEType* currentType = pObjType;
            while (currentType != null)
            {
                // Check if current type or its base types implement the interface
                if (ImplementsInterface(currentType, pTargetType))
                    return obj;

                // Move up the inheritance chain
                currentType = currentType->RawBaseType;
            }

            // No matching interface found
            return null;
        }

        /// <summary>
        /// Checks if a type implements a specific interface.
        /// </summary>
        /// <param name="pType">Type to check for interface implementation</param>
        /// <param name="pInterfaceType">Interface type to find</param>
        /// <returns>True if the type implements the interface, false otherwise</returns>
        private static unsafe bool ImplementsInterface(EEType* pType, EEType* pInterfaceType)
        {
            // Basic validation
            if (pType == null || pInterfaceType == null)
                return false;

            // Check if the type has any interfaces
            if (pType->NumInterfaces == 0)
                return false;

            // TODO: Implement actual interface table walking
            // In a full runtime, this would involve:
            // 1. Accessing the interface table of the type
            // 2. Iterating through implemented interfaces
            // 3. Checking for exact match or compatible generic interfaces

            // Conservatively return false for now
            return false;
        }

        /// <summary>
        /// Determines if an object is an instance of a specific class type.
        /// </summary>
        /// <param name="pTargetType">Target class EEType to check against</param>
        /// <param name="obj">Object to check</param>
        /// <returns>The object if it is an instance of the target class, null otherwise</returns>
        [RuntimeExport("RhTypeCast_IsInstanceOfClass")]
        public static unsafe object IsInstanceOfClass(EEType* pTargetType, object obj)
        {
            // Null can be an instance of any reference type
            if (obj == null)
                return null;

            // Get the object's EEType
            EEType* pObjType = obj.m_pEEType;

            // If types are exactly the same, return the object
            if (pObjType == pTargetType)
                return obj;

            // Ensure target type is a class (not an interface or value type)
            if (pTargetType->IsInterface || pTargetType->IsValueType)
                return null;

            // Walk the inheritance hierarchy
            EEType* currentType = pObjType;
            while (currentType != null)
            {
                if (currentType == pTargetType)
                    return obj;

                // Move up the inheritance chain
                currentType = currentType->RawBaseType;

                // Stop if we reach the root of the type hierarchy
                if (currentType == null || IsObjectType(currentType))
                    break;
            }

            // Check for special type compatibility
            if (IsSpecialTypeCompatible(pTargetType, pObjType))
                return obj;

            // Not an instance of the target class
            return null;
        }

        /// <summary>
        /// Determines if an object is an instance of a specific exception type.
        /// </summary>
        /// <param name="pTargetType">Target exception EEType to check against</param>
        /// <param name="obj">Object to check</param>
        /// <returns>The object if it is an instance of the target exception type, null otherwise</returns>
        [RuntimeExport("RhTypeCast_IsInstanceOfException")]
        public static unsafe object IsInstanceOfException(EEType* pTargetType, object obj)
        {
            // Null can be an instance of any reference type
            if (obj == null)
                return null;

            // Get the object's EEType
            EEType* pObjType = obj.m_pEEType;

            // If types are exactly the same, return the object
            if (pObjType == pTargetType)
                return obj;

            // TODO: Add a way to check if a type is an exception type
            // This might involve a flag in EEType or a specific base type check

            // Walk the inheritance hierarchy
            EEType* currentType = pObjType;
            while (currentType != null)
            {
                if (currentType == pTargetType)
                    return obj;

                // Move up the inheritance chain
                currentType = currentType->RawBaseType;

                // Stop if we reach the root of the type hierarchy
                if (currentType == null || IsExceptionBaseType(currentType))
                    break;
            }

            // Not an instance of the target exception type
            return null;
        }

        /// <summary>
        /// Checks if the given type is the base Exception type
        /// </summary>
        private static unsafe bool IsExceptionBaseType(EEType* pType)
        {
            // TODO: Implement proper Exception base type identification
            // This might involve:
            // 1. Checking against a well-known Exception EEType
            // 2. Checking a specific flag or base type
            return false;
        }

        [RuntimeExport("RhpStelemRef")]
        static unsafe void RhpStelemRef(Array array, int index, object obj)
        {
            fixed (int* n = &array._numComponents)
            {
                var ptr = (byte*)n;
                ptr += sizeof(void*);   // Array length is padded to 8 bytes on 64-bit
                ptr += index * array.m_pEEType->ComponentSize;  // Component size should always be 8, seeing as it's a pointer...
                var pp = (IntPtr*)ptr;
                *pp = Unsafe.As<object, IntPtr>(ref obj);
            }
        }

        /// <summary>
        /// Checks if an object of type sourceType can be assigned to a variable of type targetType
        /// </summary>
        private static unsafe bool IsAssignableFrom(EEType* targetType, EEType* sourceType)
        {
            // Null can be assigned to any reference type
            if (sourceType == null)
                return true;

            // Exact type match
            if (targetType == sourceType)
                return true;

            // Walk the inheritance hierarchy
            EEType* currentType = sourceType;
            while (currentType != null)
            {
                if (currentType == targetType)
                    return true;

                // Move up the inheritance chain
                currentType = currentType->RawBaseType;
            }

            // Check for interface implementation
            // Note: This is a placeholder. In a full implementation, 
            // you would walk through the source type's interfaces
            return false;
        }

        /// <summary>
        /// Sets an array element at a specific index
        /// </summary>
        private static unsafe void SetArrayElement(Array array, int index, object obj)
        {
            var elementSize = array.m_pEEType->ComponentSize;
            var arrayData = (byte*)Unsafe.AsPointer(ref array);
            var elementPtr = arrayData + sizeof(int) + index * elementSize;
            var pp = (IntPtr*)elementPtr;
            *pp = obj == null ? IntPtr.Zero : Unsafe.As<object, IntPtr>(ref obj);
        }
    }
}
