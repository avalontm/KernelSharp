using Internal.Runtime.CompilerHelpers;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// Optimized Dictionary implementation for kernel environment
    /// </summary>
    public class Dictionary<TKey, TValue>
    {
        /// <summary>
        /// Entry struct for dictionary items
        /// </summary>
        private struct Entry
        {
            public int HashCode;    // Hash code for the key (masked to be non-negative)
            public TKey Key;        // The key
            public TValue Value;    // The value
            public int Next;        // Index of next entry in the same bucket (-1 if last)
        }

        private Entry[] _entries;   // Array of entries
        private int[] _buckets;     // Bucket array for hash table
        private int _count;         // Total number of entries in use (including free list)
        private int _freeList;      // First index of free slot in entries array (-1 if empty)
        private int _freeCount;     // Number of entries in free list
        private TKey[] _keys;       // Array of keys for enumeration
        private const int InitialCapacity = 4;  // Default initial capacity

        /// <summary>
        /// Creates a new dictionary with optional initial capacity
        /// </summary>
        public Dictionary(int capacity = InitialCapacity)
        {
            if (capacity < 0)
            {
                ThrowHelpers.ArgumentException("Capacity cannot be negative");
            }

            capacity = capacity == 0 ? InitialCapacity : capacity;

            _buckets = new int[capacity];
            _entries = new Entry[capacity];
            _keys = new TKey[capacity];

            // Initialize buckets to -1 (empty)
            for (int i = 0; i < capacity; i++)
            {
                _buckets[i] = -1;
            }

            _freeList = -1;
        }

        /// <summary>
        /// Gets the number of key/value pairs in the dictionary
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _count - _freeCount; }
        }

        /// <summary>
        /// Adds a key/value pair to the dictionary
        /// </summary>
        public void Add(TKey key, TValue value)
        {
            if (key == null)
            {
                ThrowHelpers.ArgumentException("Key cannot be null");
            }

            int hashCode = key.GetHashCode() & 0x7FFFFFFF;
            int targetBucket = hashCode % _buckets.Length;

            // Check if the key already exists in the dictionary
            for (int i = _buckets[targetBucket]; i >= 0; i = _entries[i].Next)
            {
                if (_entries[i].HashCode == hashCode && _entries[i].Key.Equals(key))
                {
                    ThrowHelpers.ArgumentException("Key already exists");
                }
            }

            int index;
            if (_freeCount > 0)
            {
                // Reuse a free entry
                index = _freeList;
                _freeList = _entries[index].Next;
                _freeCount--;
            }
            else
            {
                // If the number of elements reaches capacity, resize the dictionary
                if (_count == _entries.Length)
                {
                    Resize();
                    targetBucket = hashCode % _buckets.Length; // Recalculate bucket after resizing
                }

                index = _count;
                _count++;
            }

            // Assign the entry to the dictionary
            _entries[index].HashCode = hashCode;
            _entries[index].Key = key;
            _entries[index].Value = value;
            _entries[index].Next = _buckets[targetBucket];
            _buckets[targetBucket] = index;

            // Make sure the keys array is large enough
            if (index >= _keys.Length)
            {
                int newSize = _keys.Length * 2;
                TKey[] newKeys = new TKey[newSize];
                for (int i = 0; i < _keys.Length; i++)
                {
                    newKeys[i] = _keys[i];
                }
                _keys = newKeys;
            }

            // Assign the key to the keys array
            _keys[index] = key;
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                int index = FindEntry(key);
                if (index >= 0)
                {
                    return _entries[index].Value;
                }
                ThrowHelpers.KeyNotFoundException("Key");
                return default;
            }
            set
            {
                int index = FindEntry(key);
                if (index >= 0)
                {
                    _entries[index].Value = value;
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        /// <summary>
        /// Determines whether the dictionary contains the specified key
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(TKey key)
        {
            return FindEntry(key) >= 0;
        }

        /// <summary>
        /// Gets the array of keys in the dictionary
        /// </summary>
        public TKey[] Keys
        {
            get
            {
                TKey[] keys = new TKey[Count];
                int index = 0;

                for (int i = 0; i < _count; i++)
                {
                    if (_entries[i].HashCode >= 0) // Skip free entries
                    {
                        keys[index++] = _entries[i].Key;
                    }
                }

                return keys;
            }
        }

        /// <summary>
        /// Attempts to get the value associated with the specified key
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            int index = FindEntry(key);
            if (index >= 0)
            {
                value = _entries[index].Value;
                return true;
            }
            value = default(TValue);
            return false;
        }

        /// <summary>
        /// Removes the value with the specified key from the dictionary
        /// </summary>
        public bool Remove(TKey key)
        {
            if (key == null)
            {
                ThrowHelpers.ArgumentException("Key cannot be null");
            }

            if (_buckets == null)
            {
                return false;
            }

            int hashCode = key.GetHashCode() & 0x7FFFFFFF;
            int bucket = hashCode % _buckets.Length;
            int last = -1;

            for (int i = _buckets[bucket]; i >= 0; last = i, i = _entries[i].Next)
            {
                if (_entries[i].HashCode == hashCode && _entries[i].Key.Equals(key))
                {
                    if (last < 0)
                    {
                        _buckets[bucket] = _entries[i].Next;
                    }
                    else
                    {
                        _entries[last].Next = _entries[i].Next;
                    }

                    // Clear entry and add to free list
                    _entries[i].HashCode = -1;
                    _entries[i].Next = _freeList;
                    _entries[i].Key = default(TKey);
                    _entries[i].Value = default(TValue);
                    _keys[i] = default(TKey);
                    _freeList = i;
                    _freeCount++;

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Clears all keys and values from the dictionary
        /// </summary>
        public void Clear()
        {
            if (_count > 0)
            {
                for (int i = 0; i < _buckets.Length; i++)
                {
                    _buckets[i] = -1;
                }

                for (int i = 0; i < _count; i++)
                {
                    _entries[i].HashCode = -1;
                    _entries[i].Next = -1;
                    _entries[i].Key = default(TKey);
                    _entries[i].Value = default(TValue);
                    _keys[i] = default(TKey);
                }

                _freeList = -1;
                _count = 0;
                _freeCount = 0;
            }
        }

        /// <summary>
        /// Finds the index of the entry for the specified key
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindEntry(TKey key)
        {
            if (key == null)
            {
                ThrowHelpers.ArgumentException("Key cannot be null");
            }

            if (_buckets != null)
            {
                int hashCode = key.GetHashCode() & 0x7FFFFFFF;
                int bucket = hashCode % _buckets.Length;

                for (int i = _buckets[bucket]; i >= 0; i = _entries[i].Next)
                {
                    if (_entries[i].HashCode == hashCode && _entries[i].Key.Equals(key))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Resizes the dictionary to accommodate more entries
        /// </summary>
        private void Resize()
        {
            // Double the size with a minimum of 4
            int newSize = Math.Max(_entries.Length * 2, InitialCapacity);

            // Create new arrays
            int[] newBuckets = new int[newSize];
            Entry[] newEntries = new Entry[newSize];

            // Initialize buckets to -1 (empty)
            for (int i = 0; i < newSize; i++)
            {
                newBuckets[i] = -1;
            }

            // Copy the old entries to the new arrays
            for (int i = 0; i < _count; i++)
            {
                newEntries[i] = _entries[i];

                // Rehash entries to new bucket array
                if (_entries[i].HashCode >= 0) // Skip entries in free list
                {
                    int bucket = _entries[i].HashCode % newSize;
                    newEntries[i].Next = newBuckets[bucket];
                    newBuckets[bucket] = i;
                }
            }

            // Update the dictionary with the new arrays
            _buckets = newBuckets;
            _entries = newEntries;

            // Update the keys array
            TKey[] newKeys = new TKey[newSize];
            for (int i = 0; i < _count; i++)
            {
                newKeys[i] = _keys[i];
            }
            _keys = newKeys;
        }
    }
}