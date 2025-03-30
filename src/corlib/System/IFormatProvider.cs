namespace System
{
    /// <summary>
    /// Proporciona una interfaz que admite la formato-específico de objetos.
    /// </summary>
    /// <remarks>
    /// Esta interfaz se utiliza para acceder a un objeto que controla el formato.
    /// Los proveedores de formato pueden ser específicos de una cultura o pueden proporcionar
    /// formatos personalizados para diferentes tipos de datos.
    /// </remarks>
    public interface IFormatProvider
    {
        /// <summary>
        /// Devuelve un objeto que proporciona servicios de formato del tipo especificado por el parámetro formatType.
        /// </summary>
        /// <param name="formatType">Un objeto que especifica el tipo de objeto de formato a devolver.</param>
        /// <returns>
        /// Un objeto que proporciona servicios de formato para el tipo especificado, 
        /// o null si no puede proporcionar un objeto del tipo especificado.
        /// </returns>
        object GetFormat(Type formatType);
    }
}