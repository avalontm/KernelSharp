﻿namespace Internal.Runtime
{
    internal static class IndirectionConstants
    {
        /// <summary>
        /// Flag set on pointers to indirection cells to distinguish them
        /// from pointers to the object directly
        /// </summary>
        public const int IndirectionCellPointer = 0x1;

        /// <summary>
        /// Flag set on RVAs to indirection cells to distinguish them
        /// from RVAs to the object directly
        /// </summary>
        public const uint RVAPointsToIndirection = 0x80000000u;
    }

    internal struct GCStaticRegionConstants
    {
        /// <summary>
        /// Flag set if the corresponding GCStatic entry has not yet been initialized and
        /// the corresponding EEType pointer has been changed into a instance pointer of
        /// that EEType.
        /// </summary>
        public const int Uninitialized = 0x1;

        /// <summary>
        /// Flag set if the next pointer loc points to GCStaticsPreInitDataNode.
        /// Otherise it is the next GCStatic entry.
        /// </summary>
        public const int HasPreInitializedData = 0x2;

        public const int Mask = Uninitialized | HasPreInitializedData;
    }

    internal static class ArrayTypesConstants
    {
        /// <summary>
        /// Maximum allowable size for array element types.
        /// </summary>
        public const int MaxSizeForValueClassInArray = 0xFFFF;
    }
}