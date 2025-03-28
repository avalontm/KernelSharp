using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// Proporciona una implementación base para las comparaciones de igualdad.
    /// </summary>
    /// <typeparam name="T">El tipo de objetos a comparar.</typeparam>
    public abstract class EqualityComparer<T> : IEqualityComparer<T>
    {
        // Instancia predeterminada estática
        private static EqualityComparer<T> defaultComparer;

        /// <summary>
        /// Obtiene un comparer de igualdad predeterminado para el tipo especificado por el parámetro de tipo genérico.
        /// </summary>
        public static EqualityComparer<T> Default
        {
            get
            {
                if (defaultComparer == null)
                {
                    defaultComparer = CreateComparer();
                }
                return defaultComparer;
            }
        }

        /// <summary>
        /// Cuando se implementa en una clase derivada, determina si dos objetos son iguales.
        /// </summary>
        public abstract bool Equals(T x, T y);

        /// <summary>
        /// Cuando se implementa en una clase derivada, devuelve un código hash para el objeto especificado.
        /// </summary>
        public abstract int GetHashCode(T obj);

        // Método para crear el comparer apropiado basado en T
        private static EqualityComparer<T> CreateComparer()
        {
            // Para los tipos primitivos comunes, usamos implementaciones optimizadas
            if (typeof(T) == typeof(byte))
                return (EqualityComparer<T>)(object)new ByteEqualityComparer();
            if (typeof(T) == typeof(int))
                return (EqualityComparer<T>)(object)new IntEqualityComparer();
            if (typeof(T) == typeof(string))
                return (EqualityComparer<T>)(object)new StringEqualityComparer();
            // Agregar más tipos según necesidad

            // Para tipos de referencia y todos los demás tipos
            return new ObjectEqualityComparer<T>();
        }

        // Implementación interna para objetos genéricos
        private sealed class ObjectEqualityComparer<TObj> : EqualityComparer<TObj>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool Equals(TObj x, TObj y)
            {
                if (x == null)
                    return y == null;
                if (y == null)
                    return false;
                return x.Equals(y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode(TObj obj)
            {
                if (obj == null)
                    return 0;
                return obj.GetHashCode();
            }
        }

        // Implementaciones optimizadas para tipos primitivos comunes
        private sealed class ByteEqualityComparer : EqualityComparer<byte>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool Equals(byte x, byte y) => x == y;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode(byte obj) => (int)obj;
        }

        private sealed class IntEqualityComparer : EqualityComparer<int>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool Equals(int x, int y) => x == y;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode(int obj) => obj;
        }

        private sealed class StringEqualityComparer : EqualityComparer<string>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool Equals(string x, string y)
            {
                if (x == null)
                    return y == null;
                if (y == null)
                    return false;

                if (object.ReferenceEquals(x, y))
                    return true;

                // Comparar por contenido
                if (x.Length != y.Length)
                    return false;

                for (int i = 0; i < x.Length; i++)
                {
                    if (x[i] != y[i])
                        return false;
                }

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode(string obj)
            {
                if (obj == null)
                    return 0;

                int hash = 0;
                // Algoritmo de hash simple
                for (int i = 0; i < obj.Length; i++)
                {
                    hash = (hash * 31) + obj[i];
                }
                return hash;
            }
        }
    }
}