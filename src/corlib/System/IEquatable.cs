﻿namespace System
{
    public interface IEquatable<T> //where T : allows ref struct // invariant due to questionable semantics around equality and inheritance
    {
        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><c>true</c> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <c>false</c>.</returns>
        bool Equals(T? other);
    }
}