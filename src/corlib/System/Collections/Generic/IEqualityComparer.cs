namespace System.Collections.Generic
{
    /// <summary>
    /// Define métodos para admitir la comparación de objetos para determinar la igualdad.
    /// </summary>
    /// <typeparam name="T">El tipo de objetos a comparar.</typeparam>
    public interface IEqualityComparer<T>
    {
        /// <summary>
        /// Determina si los objetos especificados son iguales.
        /// </summary>
        /// <param name="x">El primer objeto a comparar.</param>
        /// <param name="y">El segundo objeto a comparar.</param>
        /// <returns>true si los objetos especificados son iguales; de lo contrario, false.</returns>
        bool Equals(T x, T y);

        /// <summary>
        /// Devuelve un código hash para el objeto especificado.
        /// </summary>
        /// <param name="obj">El objeto para el cual se obtendrá un código hash.</param>
        /// <returns>Un código hash para el objeto especificado.</returns>
        int GetHashCode(T obj);
    }
}