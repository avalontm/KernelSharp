using Internal.Runtime.CompilerHelpers;
using System;
using System.Diagnostics;

namespace System.Collections.Generic
{
    public class Dictionary<TKey, TValue>
    {
        private struct Entry
        {
            public int HashCode;
            public TKey Key;
            public TValue Value;
            public int Next;
        }

        private Entry[] _entries;
        private int[] _buckets;
        private int _count;
        private int _freeList;
        private int _freeCount;
        private TKey[] _keys;

        public Dictionary(int capacity = 4)
        {
            _buckets = new int[capacity]; 
            _entries = new Entry[capacity];   
            _keys = new TKey[capacity];  

            Array.Fill(_buckets, -1);         

            _freeList = -1;  
        }

        public int Count => _count - _freeCount;

        public void Add(TKey key, TValue value)
        {
            int hashCode = key.GetHashCode() & 0x7FFFFFFF;
            int targetBucket = hashCode % _buckets.Length;

            // Comprobar si la clave ya existe en el diccionario
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
                // Recuperar una entrada libre
                index = _freeList;
                _freeList = _entries[index].Next;
                _freeCount--;
            }
            else
            {
                // Si el número de elementos alcanza la capacidad, redimensiona el diccionario
                if (_count == _entries.Length)
                {
                    Resize();
                    targetBucket = hashCode % _buckets.Length; // Recalcular el bucket después de redimensionar
                }

                index = _count;
                _count++;
            }

            // Asignar la entrada al diccionario
            _entries[index].HashCode = hashCode;
           // Thread.MemoryBarrier();
            _entries[index].Key = key;
            _entries[index].Value = value;
            _entries[index].Next = _buckets[targetBucket];
            _buckets[targetBucket] = index;
            // Asegúrate de redimensionar el arreglo de claves si es necesario
            if (_count > _keys.Length)
            {
                Array.Resize(ref _keys, _keys.Length * 2);
            }
           
            // Asignar la clave al arreglo de claves
            _keys[index] = key;
        }


        public TValue this[TKey key]
        {
            get
            {
                int index = IndexOfKey(key);
                if (index >= 0)
                {
                    return _entries[index].Value;
                }
                ThrowHelpers.KeyNotFoundException("Key");
                return default;
            }
            set
            {
                int index = IndexOfKey(key);
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

        public bool ContainsKey(TKey key) => IndexOfKey(key) >= 0;

        public TKey[] Keys => _keys;

        private int IndexOfKey(TKey key)
        {
            int hashCode = key.GetHashCode() & 0x7FFFFFFF;
            for (int i = _buckets[hashCode % _buckets.Length]; i >= 0; i = _entries[i].Next)
            {
                if (_entries[i].HashCode == hashCode && _entries[i].Key.Equals(key))
                {
                    return i;
                }
            }
            return -1;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int index = IndexOfKey(key);
            if (index != -1)
            {
                value = _entries[index].Value;
                return true;
            }
            value = default(TValue);
            return false;
        }


        private void Resize()
        {
            int newSize = _entries.Length * 2;
            Array.Resize(ref _entries, newSize);
            Array.Resize(ref _keys, newSize);
            _buckets = new int[newSize];
            Array.Fill(_buckets, -1);

            for (int i = 0; i < _count; i++)
            {
                int bucket = _entries[i].HashCode % newSize;
                _entries[i].Next = _buckets[bucket];
                _buckets[bucket] = i;
            }
        }
    }
}