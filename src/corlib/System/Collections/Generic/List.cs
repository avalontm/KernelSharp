using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// Optimized List implementation for kernel environment
    /// </summary>
    public unsafe class List<T>
    {
        // Static field for empty array to prevent null references
        private static readonly T[] s_emptyArray = InitializeEmptyArray();

        private static T[] InitializeEmptyArray()
        {
            EETypePtr et = EETypePtr.EETypePtrOf<T[]>();
            object arrayObj = RuntimeImports.RhpNewArray(et._value, 0);
            return Unsafe.As<object, T[]>(ref arrayObj);
        }

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

            if (capacity == 0)
            {
                _items = s_emptyArray;
            }
            else
            {
                _items = new T[capacity];
            }
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
                        _items = s_emptyArray;
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
                if ((uint)_items.Length > 0x3FFFFFFF)
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
                    Array.Copy(_items, 0, newItems, 0, _size);
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
                Array.Copy(_items, index + 1, _items, index, _size - index);
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
                Array.Clear(_items, 0, _size);
                _size = 0;
            }
        }

        /// <summary>
        /// Find the index of an item
        /// </summary>
        public int IndexOf(T item)
        {
            return Array.IndexOf(_items, item, 0, _size);
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
                return s_emptyArray;
            }

            T[] array = new T[_size];
            Array.Copy(_items, 0, array, 0, _size);
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
        /// Adds a range of items from an array to the list
        /// </summary>
        public void AddRange(T[] collection)
        {
            if (collection == null)
            {
                ThrowHelpers.ArgumentNullException("collection");
            }

            int count = collection.Length;
            if (count > 0)
            {
                EnsureCapacity(_size + count);
                Array.Copy(collection, 0, _items, _size, count);
                _size += count;
            }
        }

        /// <summary>
        /// Inserts an item at the specified index
        /// </summary>
        public void Insert(int index, T item)
        {
            // Check bounds (allow insert at _size)
            if ((uint)index > (uint)_size)
            {
                ThrowHelpers.ArgumentException("Index out of range");
            }

            if (_size == _items.Length)
            {
                EnsureCapacity(_size + 1);
            }

            // Move items up to make space
            if (index < _size)
            {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }

            _items[index] = item;
            _size++;
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
                _current = default(T);
                return false;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return _current; }
            }

            public void Reset()
            {
                _index = -1;
                _current = default(T);
            }
        }
    }
}