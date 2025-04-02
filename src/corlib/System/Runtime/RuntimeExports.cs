using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;
using Internal.Runtime;
using System.Runtime.CompilerServices;

namespace System.Runtime
{
    internal class RuntimeExports
    { 
        /// <summary>
      /// Boxes a value type into an object.
      /// </summary>
      /// <param name="pEEType">EEType of the value type to box</param>
      /// <param name="pValue">Pointer to the value to be boxed</param>
      /// <returns>Boxed object representing the value type</returns>
        [RuntimeExport("RhBox")]
        public static unsafe object RhBox(EEType* pEEType, void* pValue)
        {
            // Validate inputs
            if (pEEType == null || pValue == null)
                return null;

            // Verify this is a value type
            if (!pEEType->IsValueType)
                return null;

            // Calculate the size needed for the boxed object
            // Base size of the type + any additional data for the value type
            uint size = pEEType->BaseSize + pEEType->ComponentSize;

            // Ensure size is aligned
            if (size % 8 > 0)
                size = (size / 8 + 1) * 8;

            // Allocate memory for the boxed object
            object boxedObj = StartupCodeHelpers.RhpNewFast(pEEType);

            // Copy the value type data into the boxed object
            if (boxedObj != null)
            {
                // Get a pointer to the raw data of the boxed object
                byte* destPtr = (byte*)Unsafe.AsPointer(ref boxedObj.GetRawData());

                // Copy the value type data
                for (uint i = 0; i < pEEType->BaseSize; i++)
                {
                    destPtr[i] = ((byte*)pValue)[i];
                }
            }

            return boxedObj;
        }

        [RuntimeExport("RhUnbox2")]
        public static unsafe ref byte RhUnbox2(EEType* pUnboxToEEType, object obj)
        {
            if ((obj == null) || !UnboxAnyTypeCompare(obj.m_pEEType, pUnboxToEEType))
            {
                ExceptionIDs exID = obj == null ? ExceptionIDs.NullReference : ExceptionIDs.InvalidCast;
                //throw pUnboxToEEType->GetClasslibException(exID);
            }
            return ref obj.GetRawData();

        }
        /// <summary>
         /// Compara tipos para determinar si se permite el unboxing de uno a otro.
         /// </summary>
         /// <param name="pEEType">EEType del objeto que se está desempaquetando</param>
         /// <param name="ptrUnboxToEEType">EEType al que se quiere convertir</param>
         /// <returns>true si los tipos son compatibles para unboxing</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool UnboxAnyTypeCompare(EEType* pEEType, EEType* ptrUnboxToEEType)
        {
            // Si los tipos son exactamente iguales, siempre es válido
            if (TypeCast.AreTypesEquivalent(pEEType, ptrUnboxToEEType))
                return true;

            // Si tienen el mismo tipo de elemento, podemos hacer comprobaciones adicionales
            if (pEEType->ElementType == ptrUnboxToEEType->ElementType)
            {
                // Enums y tipos primitivos deben pasar las verificaciones de UnboxAny 
                // si tienen exactamente el mismo tipo de elemento fundamental
                switch (ptrUnboxToEEType->ElementType)
                {
                    case EETypeElementType.Byte:
                    case EETypeElementType.SByte:
                    case EETypeElementType.Int16:
                    case EETypeElementType.UInt16:
                    case EETypeElementType.Int32:
                    case EETypeElementType.UInt32:
                    case EETypeElementType.Int64:
                    case EETypeElementType.UInt64:
                    case EETypeElementType.IntPtr:
                    case EETypeElementType.UIntPtr:
                    case EETypeElementType.Single:
                    case EETypeElementType.Double:
                    case EETypeElementType.Boolean:
                    case EETypeElementType.Char:
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Unboxes a Nullable type, extracting its underlying value.
        /// </summary>
        /// <param name="pEEType">EEType of the Nullable type</param>
        /// <param name="obj">Boxed Nullable object to unbox</param>
        /// <param name="pValue">Pointer to store the unboxed value</param>
        /// <returns>True if unboxing was successful, false otherwise</returns>
        [RuntimeExport("RhUnboxNullable")]
        public static unsafe bool RhUnboxNullable(EEType* pEEType, object obj, void* pValue)
        {
            // Validate inputs
            if (pEEType == null || pValue == null)
                return false;

            // Null check for the object
            if (obj == null)
                return false;

            // Verify this is a Nullable type
            if (!pEEType->IsNullable)
                return false;

            // Get the object's EEType
            EEType* objEEType = obj.m_pEEType;

            // Verify type compatibility
            if (objEEType != pEEType)
                return false;

            // Get a pointer to the boxed Nullable object's data
            byte* objDataPtr = (byte*)Unsafe.AsPointer(ref obj.GetRawData());

            // In a Nullable<T>, the first byte indicates whether the value is present
            // 0 means null, non-zero means a value is present
            if (objDataPtr[0] == 0)
                return false;

            // Copy the underlying value
            // Skip the first byte (hasValue flag) and copy the actual value
            EEType* underlyingType = pEEType->RelatedParameterType;
            uint valueSize = underlyingType->BaseSize;

            // Copy the value
            byte* valuePtr = objDataPtr + 1;
            for (uint i = 0; i < valueSize; i++)
            {
                ((byte*)pValue)[i] = valuePtr[i];
            }

            return true;
        }

        /// <summary>
        /// Stores a reference element in an array, performing type checking and compatibility validation.
        /// </summary>
        /// <param name="array">The array to store the element in</param>
        /// <param name="index">The index at which to store the element</param>
        /// <param name="obj">The object to store in the array</param>
        [RuntimeExport("RhTypeCast_StelemRef")]
        public static unsafe void StelemRef(Array array, int index, object obj)
        {
            // Validate array input
            if (array == null)
                ThrowHelpers.ThrowArgumentNullException("array");

            // Validate index
            if (index < 0 || index >= array.Length)
                ThrowHelpers.ThrowIndexOutOfRangeException();

            // If object is null, it can be stored in any reference type array
            if (obj == null)
            {
                // Set the array element to null
                SetArrayElement(array, index, null);
                return;
            }

            // Get array and object EETypes
            EEType* arrayEEType = array.m_pEEType;
            EEType* objEEType = obj.m_pEEType;

            // Verify array is indeed an array
            if (!arrayEEType->IsArray)
                ThrowHelpers.ThrowInvalidOperationException("Not an array type");

            // Get the array's element type
            EEType* arrayElementType = arrayEEType->RelatedParameterType;

            // Exact type match is always allowed
            if (objEEType == arrayElementType)
            {
                SetArrayElement(array, index, obj);
                return;
            }

            // Check if object can be assigned to the array's element type
            if (IsAssignableFrom(arrayElementType, objEEType))
            {
                SetArrayElement(array, index, obj);
                return;
            }

            // If we reach here, the assignment is not valid
            ThrowHelpers.ThrowArrayTypeMismatchException();
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
            // In a typical runtime, this would use a write barrier
            // For a basic kernel implementation, we'll do a simple assignment
            fixed (int* lengthPtr = &array._numComponents)
            {
                var ptr = (byte*)lengthPtr;
                ptr += sizeof(void*);  // Skip array length
                ptr += index * array.m_pEEType->ComponentSize;
                var pp = (IntPtr*)ptr;
                *pp = obj == null ? IntPtr.Zero : Unsafe.As<object, IntPtr>(ref obj);
            }
        }
    }
}
