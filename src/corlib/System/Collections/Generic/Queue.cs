using System.Diagnostics;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerHelpers;

namespace System.Collections.Generic
{
    /// <summary>
    /// Implementación de cola optimizada para entorno de kernel
    /// </summary>
    public class Queue<T>
    {
        // Almacenamiento interno con redimensionamiento dinámico
        private T[] _items;
        private int _head;       // Índice del primer elemento
        private int _tail;       // Índice donde se insertará el próximo elemento
        private int _size;       // Número de elementos en la cola
        private const int _defaultCapacity = 4;

        // Constructor por defecto
        public Queue()
        {
            _items = new T[_defaultCapacity];
            _head = 0;
            _tail = 0;
            _size = 0;
        }

        // Constructor con capacidad inicial
        public Queue(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelpers.ThrowArgumentException("Capacidad no puede ser negativa");
            }

            capacity = capacity == 0 ? _defaultCapacity : capacity;
            _items = new T[capacity];
            _head = 0;
            _tail = 0;
            _size = 0;
        }

        // Propiedades básicas
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _size;
        }

        // Encolar un elemento
        public void Enqueue(T item)
        {
            // Verificar y expandir si es necesario
            if (_size == _items.Length)
            {
                int newCapacity = _items.Length == 0 ? _defaultCapacity : _items.Length * 2;

                // Seguridad para arrays muy grandes
                if (newCapacity > 0x7FEFFFFF)
                {
                    newCapacity = _items.Length + Math.Min(_items.Length, 0x7FEFFFFF - _items.Length);
                }

                EnsureCapacity(newCapacity);
            }

            // Insertar elemento
            _items[_tail] = item;
            _tail = (_tail + 1) % _items.Length;
            _size++;
        }

        // Desencolar un elemento
        public T Dequeue()
        {
            if (_size == 0)
            {
                ThrowHelpers.ThrowInvalidOperationException("La cola está vacía");
            }

            T item = _items[_head];
            _items[_head] = default(T);  // Permitir recolección de basura
            _head = (_head + 1) % _items.Length;
            _size--;

            return item;
        }

        // Observar el primer elemento sin eliminarlo
        public T Peek()
        {
            if (_size == 0)
            {
                ThrowHelpers.ThrowInvalidOperationException("La cola está vacía");
            }

            return _items[_head];
        }

        // Asegurar capacidad
        private void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                int newCapacity = min;
                T[] newItems = new T[newCapacity];

                // Copiar elementos manteniendo el orden circular
                if (_size > 0)
                {
                    if (_head < _tail)
                    {
                        // Elementos contiguos
                        Array.Copy(_items, _head, newItems, 0, _size);
                    }
                    else
                    {
                        // Elementos dispersos por reinicio de cola
                        Array.Copy(_items, _head, newItems, 0, _items.Length - _head);
                        Array.Copy(_items, 0, newItems, _items.Length - _head, _tail);
                    }
                }

                _items = newItems;
                _head = 0;
                _tail = _size;
            }
        }

        // Limpiar la cola
        public void Clear()
        {
            if (_size > 0)
            {
                // Limpiar referencias para permitir recolección
                if (_head < _tail)
                {
                    Array.Clear(_items, _head, _size);
                }
                else
                {
                    Array.Clear(_items, _head, _items.Length - _head);
                    Array.Clear(_items, 0, _tail);
                }

                _head = 0;
                _tail = 0;
                _size = 0;
            }
        }

        // Verificar si la cola contiene un elemento
        public bool Contains(T item)
        {
            if (_size == 0)
                return false;

            int index = _head;
            for (int i = 0; i < _size; i++)
            {
                if (item == null)
                {
                    if (_items[index] == null)
                        return true;
                }
                else if (_items[index] != null && _items[index].Equals(item))
                {
                    return true;
                }

                index = (index + 1) % _items.Length;
            }

            return false;
        }

        // Convertir a array
        public T[] ToArray()
        {
            if (_size == 0)
                return new T[0];

            T[] array = new T[_size];

            if (_head < _tail)
            {
                Array.Copy(_items, _head, array, 0, _size);
            }
            else
            {
                Array.Copy(_items, _head, array, 0, _items.Length - _head);
                Array.Copy(_items, 0, array, _items.Length - _head, _tail);
            }

            return array;
        }

        // Enumerador
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        // Estructura de enumerador
        public struct Enumerator
        {
            private readonly Queue<T> _queue;
            private int _index;
            private T _current;

            internal Enumerator(Queue<T> queue)
            {
                _queue = queue;
                _index = -1;
                _current = default(T);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (++_index < _queue._size)
                {
                    _current = _queue._items[(_queue._head + _index) % _queue._items.Length];
                    return true;
                }
                return false;
            }

            public T Current => _current;
        }
    }
}