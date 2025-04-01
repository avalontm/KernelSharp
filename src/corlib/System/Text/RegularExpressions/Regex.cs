namespace System.Text.RegularExpressions
{
    public class Regex
    {
        public static string EncontrarCoincidencia(string texto, string patron)
        {
            int indice = 0;

            // Buscar la primera ocurrencia del patrón en el texto
            while ((indice = texto.IndexOf(patron, indice)) != -1)
            {
                // Obtener la coincidencia encontrada
                string coincidencia = ObtenerCoincidencia(texto, indice, patron.Length);

                if (coincidencia != null)
                {
                    // Devolver la coincidencia encontrada
                    return coincidencia;
                }

                indice++;
            }

            // No se encontró ninguna coincidencia
            return null;
        }

        public static string ObtenerCoincidencia(string texto, int indice, int longitud)
        {
            // Verificar que la coincidencia empieza en el índice correcto
            if (indice < 0 || indice + longitud > texto.Length)
            {
                return null;
            }

            // Obtener la coincidencia
            string coincidencia = texto.Substring(indice, longitud);

            // Verificar que la coincidencia es un número
            for (int i = 0; i < coincidencia.Length; i++)
            {
                char c = coincidencia[i];

                if (!char.IsDigit(c))
                {
                    return null;
                }
            }

            return coincidencia;
        }

        public static string Replace(string contenido, string texto, string patron)
        {
            // Buscar la coincidencia en el texto
            string coincidencia = EncontrarCoincidencia(texto, patron);

            if (coincidencia != null)
            {
                return coincidencia;
            }
            else
            {
                return contenido;
            }
        }
    }
}
