using Internal.Runtime.CompilerHelpers;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// Optimized List implementation for kernel environment
    /// </summary>
    public class List<T>
    {
        // Internal storage with dynamic resizing
        private T[] _items;
        private int _size;
        private const int _defaultCapacity = 4;  // Start with reasonable capacity

        /// <summary>
        /// Creates a new list with default capacity
        /// </summary>
        public List()
        {
            _items = new T[_defaultCapacity];
            _size = 0;
        }

        /// <summary>
        /// Creates a new list with specified capacity
        /// </summary>
        public List(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelpers.ArgumentException("Capacity cannot be negative");
            }

            capacity = capacity == 0 ? _defaultCapacity : capacity;
            _items = new T[capacity];
            _size = 0;
        }

        /// <summary>
        /// Gets the number of elements in the list
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _size; }
        }

        /// <summary>
        /// Gets or sets the capacity of the list
        /// </summary>
        public int Capacity
        {
            get { return _items.Length; }
            set
            {
                if (value < _size)
                {
                    ThrowHelpers.ArgumentException("Capacity cannot be less than Count");
                }

                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        T[] newItems = new T[value];
                        if (_size > 0)
                        {
                            Array.Copy(_items, 0, newItems, 0, _size);
                        }
                        _items = newItems;
                    }
                    else
                    {
                        _items = new T[_defaultCapacity];
                    }
                }
            }
        }

        /// <summary>
        /// Adds an item to the list
        /// </summary>
        public void Add(T item)
        {
            if (_size == _items.Length)
            {
                // Growth factor of 2 is efficient for most scenarios
                int newCapacity = _items.Length == 0 ? _defaultCapacity : _items.Length * 2;

                // Safety check for very large arrays
                if (_items.Length > 0x3FFFFFFF)
                {
                    // More conservative growth for very large arrays
                    newCapacity = _items.Length + (_items.Length / 2);
                }

                EnsureCapacity(newCapacity);
            }

            _items[_size] = item;
            _size++;
        }

        /// <summary>
        /// Helper method to ensure sufficient capacity
        /// </summary>
        private void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                int newCapacity = min;
                T[] newItems = new T[newCapacity];

                // Copy existing elements
                if (_size > 0)
                {
                    for (int i = 0; i < _size; i++)
                    {
                        newItems[i] = _items[i];
                    }
                }

                _items = newItems;
            }
        }

        /// <summary>
        /// Indexer for accessing list elements
        /// </summary>
        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_size)
                {
                    ThrowHelpers.ArgumentException("Index out of range");
                }
                return _items[index];
            }
            set
            {
                if ((uint)index >= (uint)_size)
                {
                    ThrowHelpers.ArgumentException("Index out of range");
                }
                _items[index] = value;
            }
        }

        /// <summary>
        /// Removes the element at the specified index
        /// </summary>
        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_size)
            {
                ThrowHelpers.ArgumentException("Index out of range");
            }

            _size--;

            // Only move elements if needed
            if (index < _size)
            {
                // Shift elements down to fill the gap
                for (int i = index; i < _size; i++)
                {
                    _items[i] = _items[i + 1];
                }
            }

            // Clear the last element to allow GC to collect it if needed
            _items[_size] = default(T);
        }

        /// <summary>
        /// Removes the first occurrence of a specific item from the list
        /// </summary>
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears all elements from the list
        /// </summary>
        public void Clear()
        {
            if (_size > 0)
            {
                // Clear references to allow GC collection
                for (int i = 0; i < _size; i++)
                {
                    _items[i] = default(T);
                }
                _size = 0;
            }
        }

        /// <summary>
        /// Find the index of an item
        /// </summary>
        public int IndexOf(T item)
        {
            // Handle null items
            if (item == null)
            {
                for (int i = 0; i < _size; i++)
                {
                    if (_items[i] == null) return i;
                }
            }
            else
            {
                for (int i = 0; i < _size; i++)
                {
                    // For reference types, use reference equality or Equals
                    if (Object.ReferenceEquals(_items[i], item) ||
                        (_items[i] != null && _items[i].Equals(item)))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Checks if the list contains a specific item
        /// </summary>
        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        /// <summary>
        /// Converts the list to an array
        /// </summary>
        public T[] ToArray()
        {
            if (_size == 0)
            {
                return new T[0];
            }

            T[] array = new T[_size];
            for (int i = 0; i < _size; i++)
            {
                array[i] = _items[i];
            }
            return array;
        }

        /// <summary>
        /// Gets an enumerator for the list
        /// </summary>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Nested enumerator struct for efficient iteration
        /// </summary>
        public struct Enumerator
        {
            private readonly List<T> _list;
            private int _index;
            private T _current;

            internal Enumerator(List<T> list)
            {
                _list = list;
                _index = -1;
                _current = default(T);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (++_index < _list._size)
                {
                    _current = _list._items[_index];
                    return true;
                }
                return false;
            }

            public T Current { get { return _current; } }
        }
    }
}