using System.Globalization;

namespace NutriRed.Web.Helpers
{
    public static class FormatExtensions
    {
        private static readonly CultureInfo CultureAr = new CultureInfo("es-AR");

        /// <summary>
        /// Convierte un DNI en formato con puntos cada 3 cifras (Ej: "25865478" -> "25.865.478").
        /// </summary>
        public static string FormatearDni(this string? dni)
        {
            if (string.IsNullOrWhiteSpace(dni))
                return "-";

            // Eliminar caracteres no numéricos por si viene formateado de la BD
            var digitos = new string(dni.Where(char.IsDigit).ToArray());

            if (long.TryParse(digitos, out long dniNumero))
            {
                // "N0" con cultura es-AR aplica puntos como separadores de miles
                return dniNumero.ToString("N0", CultureAr);
            }

            return dni;
        }

        /// <summary>
        /// Convierte un teléfono en formato estándar con guiones (Ej: "1165952678" -> "11-6595-2678").
        /// </summary>
        public static string FormatearTelefono(this string? telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
                return "-";

            // Eliminar caracteres no numéricos
            var digitos = new string(telefono.Where(char.IsDigit).ToArray());

            // Formato estándar de 10 dígitos (Ej: AMBA / Argentina 11-XXXX-XXXX)
            if (digitos.Length == 10)
            {
                return $"{digitos.Substring(0, 2)}-{digitos.Substring(2, 4)}-{digitos.Substring(6, 4)}";
            }

            // Formato de 11 dígitos si viene con '0' inicial (Ej: 011-XXXX-XXXX -> 11-XXXX-XXXX)
            if (digitos.Length == 11 && digitos.StartsWith("0"))
            {
                return $"{digitos.Substring(1, 2)}-{digitos.Substring(3, 4)}-{digitos.Substring(7, 4)}";
            }

            return telefono;
        }
    }
}
