using System;
using System.Collections.Generic;

namespace System.Runtime.Serialization
{
    public sealed partial class SerializationInfo
    {
        private const int DefaultSize = 4;

        // Even though we have a dictionary, we're still keeping all the arrays around for back-compat.
        // Otherwise we may run into potentially breaking behaviors like GetEnumerator() not returning entries in the same order they were added.
        private string[] _names;
        private object?[] _values;
        private Type[] _types;
        private int _count;
        private readonly Dictionary<string, int> _nameToIndex;
        private readonly IFormatterConverter _converter;
        private string _rootTypeName;
        private string _rootTypeAssemblyName;
        private Type _rootType;
    }
}
