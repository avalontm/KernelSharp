using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerHelpers;

namespace System.Collections.Generic
{
    // Optimized List<T> implementation for kernel environment
    public class List<T>
    {
        // Internal storage with dynamic resizing
        private T[] _items;
        private int _size;
        private const int _defaultCapacity = 4;  // Start with reasonable capacity

        // Constructors with explicit array creation
        public List()
        {
            _items = new T[_defaultCapacity];
            _size = 0;
        }

        public List(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelpers.ThrowArgumentException("Capacity cannot be negative");
            }

            capacity = capacity == 0 ? _defaultCapacity : capacity;
            _items = new T[capacity];
            _size = 0;
        }

        // Basic properties
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _size;
        }

        public int Capacity
        {
            get => _items.Length;
            set
            {
                if (value < _size)
                {
                    ThrowHelpers.ThrowArgumentException("Capacity cannot be less than Count");
                }

                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        T[] newItems = new T[value];
                        if (_size > 0)
                        {
                            for (int i = 0; i < _size; i++)
                            {
                                newItems[i] = _items[i];
                            }
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

        // Optimized Add method for handling many items
        public void Add(T item)
        {
            if (_size == _items.Length)
            {
                // Growth factor of 2 is efficient for most scenarios
                int newCapacity = _items.Length == 0 ? _defaultCapacity : _items.Length * 2;
                // Safety check for very large arrays
                if (newCapacity > 0x7FEFFFFF)
                {
                    // More conservative growth for very large arrays
                    newCapacity = _items.Length + Math.Min(_items.Length, 0x7FEFFFFF - _items.Length);
                }

                EnsureCapacity(newCapacity);
            }

            _items[_size] = item;
            _size++;
        }

        // Helper method to ensure sufficient capacity
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

        // Indexer with bounds checking
        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_size)
                {
                    ThrowHelpers.ThrowArgumentException("Index out of range");
                }
                return _items[index];
            }
            set
            {
                if ((uint)index >= (uint)_size)
                {
                    ThrowHelpers.ThrowArgumentException("Index out of range");
                }
                _items[index] = value;
            }
        }

        // Improved RemoveAt that doesn't reallocate unnecessarily
        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_size)
            {
                ThrowHelpers.ThrowArgumentException("Index out of range");
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

        // Clear the list without releasing memory unnecessarily
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

        // Find the index of an item (more efficient implementation)
        public int IndexOf(T item)
        {
            // Use EqualityComparer for value types or null references
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
                    // For reference types, use reference equality
                    // This could be improved with a proper EqualityComparer in a full implementation
                    if (Object.ReferenceEquals(_items[i], item) ||
                        (_items[i] != null && _items[i].Equals(item)))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        // Optimized Contains method
        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        // Convert to array (optimized)
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

        public bool Remove<T>(T value)
        {
            throw new NotImplementedException();
        }

        // Iterator support
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }


        // Nested enumerator (more efficient implementation)
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

            public T Current => _current;
        }
    }
}