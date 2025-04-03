using Internal.Runtime.CompilerHelpers;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// A simple HashSet implementation suitable for kernel environments
    /// </summary>
    /// <typeparam name="T">The type of elements in the hash set</typeparam>
    public class HashSet<T>
    {
        // Default initial capacity and load factor
        private const int DefaultCapacity = 8;
        private const float DefaultLoadFactor = 0.75f;
        private const int MaxArraySize = 0x7FEFFFFF; // Max array size in .NET

        // The slots array - each entry is a linked list of nodes with the same hash code
        private Slot[] _slots;

        // Number of items in the set
        private int _count;

        // Size threshold for resizing
        private int _threshold;

        // Custom equality comparer
        private IEqualityComparer<T> _comparer;

        /// <summary>
        /// Represents a slot in the hash table
        /// </summary>
        private struct Slot
        {
            public int HashCode;   // Hash code of the item (or -1 if empty)
            public T Value;        // The item value
            public int Next;       // Index of the next slot in the same bucket (or -1 if end of list)
        }

        /// <summary>
        /// Initializes a new instance of the HashSet class with default capacity
        /// </summary>
        public HashSet() : this(DefaultCapacity, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the HashSet class with specified capacity
        /// </summary>
        /// <param name="capacity">Initial capacity</param>
        public HashSet(int capacity) : this(capacity, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the HashSet class with specified comparer
        /// </summary>
        /// <param name="comparer">Equality comparer</param>
        public HashSet(IEqualityComparer<T> comparer)
            : this(DefaultCapacity, comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the HashSet class with specified capacity and comparer
        /// </summary>
        /// <param name="capacity">Initial capacity</param>
        /// <param name="comparer">Equality comparer</param>
        public HashSet(int capacity, IEqualityComparer<T> comparer)
        {
            if (capacity < 0)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException("Capacity must be positive");
            }

            // Ensure minimum capacity
            capacity = Math.Max(capacity, DefaultCapacity);

            _slots = new Slot[capacity];
            InitializeSlots(0, capacity);

            _comparer = comparer ?? EqualityComparer<T>.Default;
            _threshold = (int)(capacity * DefaultLoadFactor);
        }

        /// <summary>
        /// Gets the number of elements in the HashSet
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Adds an item to the HashSet
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <returns>True if the item was added, false if it already exists</returns>
        public bool Add(T item)
        {
            // Resize if needed
            if (_count >= _threshold)
            {
                Resize();
            }

            // Calculate hash code
            int hashCode = GetHashCode(item);

            // Find the bucket
            int bucket = hashCode % _slots.Length;
            if (bucket < 0) bucket += _slots.Length;

            // Look for existing item
            for (int i = bucket; i >= 0; i = _slots[i].Next)
            {
                if (_slots[i].HashCode == hashCode && AreEqual(_slots[i].Value, item))
                {
                    // Item already exists
                    return false;
                }

                // End of chain
                if (_slots[i].Next < 0)
                {
                    // Find an empty slot
                    int emptyIndex = FindEmptySlot();

                    // Set up the new slot
                    _slots[emptyIndex].HashCode = hashCode;
                    _slots[emptyIndex].Value = item;
                    _slots[emptyIndex].Next = -1;

                    // Link into the chain
                    _slots[i].Next = emptyIndex;

                    _count++;
                    return true;
                }
            }

            // Bucket is empty
            _slots[bucket].HashCode = hashCode;
            _slots[bucket].Value = item;
            _slots[bucket].Next = -1;

            _count++;
            return true;
        }

        /// <summary>
        /// Determines whether the HashSet contains a specific item
        /// </summary>
        /// <param name="item">The item to locate</param>
        /// <returns>True if found, false otherwise</returns>
        public bool Contains(T item)
        {
            if (_count == 0)
            {
                return false;
            }

            // Calculate hash code
            int hashCode = GetHashCode(item);

            // Find the bucket
            int bucket = hashCode % _slots.Length;
            if (bucket < 0) bucket += _slots.Length;

            // Look for the item
            for (int i = bucket; i >= 0; i = _slots[i].Next)
            {
                if (_slots[i].HashCode == hashCode && AreEqual(_slots[i].Value, item))
                {
                    return true;
                }

                if (_slots[i].Next < 0)
                {
                    break;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes an item from the HashSet
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <returns>True if removed, false if not found</returns>
        public bool Remove(T item)
        {
            if (_count == 0)
            {
                return false;
            }

            // Calculate hash code
            int hashCode = GetHashCode(item);

            // Find the bucket
            int bucket = hashCode % _slots.Length;
            if (bucket < 0) bucket += _slots.Length;

            // Special case for first item in bucket
            if (_slots[bucket].HashCode == hashCode && AreEqual(_slots[bucket].Value, item))
            {
                // If there's a next item, copy it to this slot and remove that slot
                if (_slots[bucket].Next >= 0)
                {
                    int nextIndex = _slots[bucket].Next;
                    _slots[bucket] = _slots[nextIndex];
                    FreeSlot(nextIndex);
                }
                else
                {
                    // Otherwise just clear this slot
                    _slots[bucket].HashCode = -1;
                    _slots[bucket].Value = default(T);
                    _slots[bucket].Next = -1;
                }

                _count--;
                return true;
            }

            // Look for the item in the chain
            int previous = bucket;
            int current = _slots[previous].Next;

            while (current >= 0)
            {
                if (_slots[current].HashCode == hashCode && AreEqual(_slots[current].Value, item))
                {
                    // Unlink from chain
                    _slots[previous].Next = _slots[current].Next;

                    // Free the slot
                    FreeSlot(current);

                    _count--;
                    return true;
                }

                previous = current;
                current = _slots[current].Next;
            }

            return false;
        }

        /// <summary>
        /// Removes all items from the HashSet
        /// </summary>
        public void Clear()
        {
            if (_count > 0)
            {
                InitializeSlots(0, _slots.Length);
                _count = 0;
            }
        }

        /// <summary>
        /// Returns the hash code for the specified item
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetHashCode(T item)
        {
            if (item == null)
            {
                return 0;
            }

            int hashCode = _comparer.GetHashCode(item) & 0x7FFFFFFF;
            return hashCode == 0 ? 1 : hashCode;
        }

        /// <summary>
        /// Determines whether two items are equal
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool AreEqual(T x, T y)
        {
            if (x == null)
            {
                return y == null;
            }

            return _comparer.Equals(x, y);
        }

        /// <summary>
        /// Finds an empty slot
        /// </summary>
        /// <returns>Index of the empty slot</returns>
        private int FindEmptySlot()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].HashCode < 0)
                {
                    return i;
                }
            }

            // Should never get here if we resize properly
            ThrowHelpers.ThrowInvalidOperationException("No empty slots available");
            return -1;
        }

        /// <summary>
        /// Frees a slot
        /// </summary>
        private void FreeSlot(int index)
        {
            _slots[index].HashCode = -1;
            _slots[index].Value = default(T);
            _slots[index].Next = -1;
        }

        /// <summary>
        /// Initializes slots to empty
        /// </summary>
        private void InitializeSlots(int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                _slots[i].HashCode = -1;
                _slots[i].Value = default(T);
                _slots[i].Next = -1;
            }
        }

        /// <summary>
        /// Resizes the hash table
        /// </summary>
        private void Resize()
        {
            // Calculate new size (double the current size)
            int newSize = _slots.Length * 2;

            // Check for capacity limits
            if (newSize > MaxArraySize)
            {
                newSize = MaxArraySize;
                if (newSize <= _slots.Length)
                {
                    // Can't resize further
                    return;
                }
            }

            // Create a new slots array
            Slot[] newSlots = new Slot[newSize];
            for (int i = 0; i < newSize; i++)
            {
                newSlots[i].HashCode = -1;
                newSlots[i].Next = -1;
            }

            // Rehash all items into the new slots
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].HashCode >= 0)
                {
                    // Calculate new bucket
                    int bucket = _slots[i].HashCode % newSize;
                    if (bucket < 0) bucket += newSize;

                    // Find the end of the chain
                    int j = bucket;
                    while (newSlots[j].HashCode >= 0)
                    {
                        if (newSlots[j].Next < 0)
                        {
                            // End of chain
                            newSlots[j].Next = bucket + _slots.Length + (i - bucket);
                            break;
                        }

                        j = newSlots[j].Next;
                    }

                    // If this slot is empty, use it
                    if (newSlots[bucket].HashCode < 0)
                    {
                        newSlots[bucket] = _slots[i];
                        newSlots[bucket].Next = -1;
                    }
                    else
                    {
                        // Otherwise copy to the new slot at the end of the chain
                        newSlots[bucket + _slots.Length + (i - bucket)] = _slots[i];
                        newSlots[bucket + _slots.Length + (i - bucket)].Next = -1;
                    }
                }
            }

            _slots = newSlots;
            _threshold = (int)(newSize * DefaultLoadFactor);
        }
    }
}