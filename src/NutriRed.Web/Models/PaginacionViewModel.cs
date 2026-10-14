namespace NutriRed.Web.Models;

public class PaginacionViewModel
{
    public int PaginaActual { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
    public int TotalRegistros { get; set; } = 0;
    public int RegistrosPorPagina { get; set; } = 10;
    public string Accion { get; set; } = "Index";
    public string? Controlador { get; set; }
    public Dictionary<string, string> ValoresRuta { get; set; } = new();

    public bool TienePaginaAnterior => PaginaActual > 1;
    public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;

    public int RegistroInicio => TotalRegistros == 0 ? 0 : (PaginaActual - 1) * RegistrosPorPagina + 1;
    public int RegistroFin => Math.Min(PaginaActual * RegistrosPorPagina, TotalRegistros);

    /// <summary>
    /// Genera una copia de los parámetros de ruta incluyendo la página destino solicitada.
    /// </summary>
    public Dictionary<string, string> ObtenerRutaConPagina(int paginaDestino)
    {
        var ruta = new Dictionary<string, string>(ValoresRuta, StringComparer.OrdinalIgnoreCase);
        ruta["pagina"] = paginaDestino.ToString();
        return ruta;
    }
}

public static class PaginacionExtensions
{
    public static (List<T> Items, PaginacionViewModel Paginacion) Paginar<T>(
        this IEnumerable<T> source,
        int pagina,
        int tamanoPagina,
        string accion,
        string controlador,
        Dictionary<string, string>? parametrosRuta = null)
    {
        var total = source.Count();
        tamanoPagina = tamanoPagina <= 0 ? 10 : tamanoPagina;
        var totalPaginas = (int)Math.Ceiling((double)total / tamanoPagina);
        if (totalPaginas == 0) totalPaginas = 1;

        pagina = Math.Max(1, pagina);
        if (pagina > totalPaginas) pagina = totalPaginas;

        var items = source.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToList();

        var paginacion = new PaginacionViewModel
        {
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            TotalRegistros = total,
            RegistrosPorPagina = tamanoPagina,
            Accion = accion,
            Controlador = controlador,
            ValoresRuta = parametrosRuta ?? new Dictionary<string, string>()
        };

        return (items, paginacion);
    }
}
