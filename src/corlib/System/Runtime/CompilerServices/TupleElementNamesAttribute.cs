﻿using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates that the use of <see cref="System.ValueTuple"/> on a member is meant to be treated as a tuple with element names.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Event)]
    public sealed class TupleElementNamesAttribute : Attribute
    {
#nullable enable
        private readonly string?[] _transformNames;
#nullable disable

        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="TupleElementNamesAttribute"/> class.
        /// </summary>
        /// <param name="transformNames">
        /// Specifies, in a pre-order depth-first traversal of a type's
        /// construction, which <see cref="System.ValueType"/> occurrences are
        /// meant to carry element names.
        /// </param>
        /// <remarks>
        /// This constructor is meant to be used on types that contain an
        /// instantiation of <see cref="System.ValueType"/> that contains
        /// element names.  For instance, if <c>C</c> is a generic type with
        /// two type parameters, then a use of the constructed type <c>C{<see
        /// cref="System.ValueTuple{T1, T2}"/>, <see
        /// cref="System.ValueTuple{T1, T2, T3}"/></c> might be intended to
        /// treat the first type argument as a tuple with element names and the
        /// second as a tuple without element names. In which case, the
        /// appropriate attribute specification should use a
        /// <c>transformNames</c> value of <c>{ "name1", "name2", null, null,
        /// null }</c>.
        /// </remarks>
#nullable enable
        public TupleElementNamesAttribute(string?[] transformNames)
#nullable disable
        {
            //ArgumentNullException.ThrowIfNull(transformNames);

            _transformNames = transformNames;
        }

        /// <summary>
        /// Specifies, in a pre-order depth-first traversal of a type's
        /// construction, which <see cref="System.ValueTuple"/> elements are
        /// meant to carry element names.
        /// </summary>
#nullable enable
        public List<string?> TransformNames => new(_transformNames);
#nullable disable
    }
}