using Microsoft.AspNetCore.Mvc;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.Interfaces;

namespace NutriRed.Web.Controllers.Api;

[ApiController]
[Route("api/productos")]
[Produces("application/json")]
public class ProductosApiController : ControllerBase
{
    private readonly IProductoService _productoService;
    private readonly ICategoriaService _categoriaService;
    private readonly ILogger<ProductosApiController> _logger;

    public ProductosApiController(
        IProductoService productoService,
        ICategoriaService categoriaService,
        ILogger<ProductosApiController> logger)
    {
        _productoService = productoService;
        _categoriaService = categoriaService;
        _logger = logger;
    }

    /// <summary>
    /// Consulta un producto por su código de barras comercial (EAN-13) para el escáner de la app móvil.
    /// Soporta rutas /api/productos/barcode/{barcode} y /api/scanner/producto/{barcode}.
    /// </summary>
    [HttpGet("barcode/{barcode}")]
    [HttpGet("/api/scanner/producto/{barcode}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BuscarPorCodigoBarras(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return BadRequest(new { success = false, message = "El código de barras no puede estar vacío." });
        }

        var result = await _productoService.BuscarPorEanAsync(barcode.Trim());
        if (!result.Success || result.Data == null)
        {
            return NotFound(new
            {
                success = false,
                message = result.ErrorMessage ?? $"No se encontró ningún producto con el código de barras '{barcode}'."
            });
        }

        var p = result.Data;
        var stockDisponible = p.Lotes?.Sum(l => l.CantidadDisponible) ?? 0;
        return Ok(new
        {
            id = p.Id.ToString(),
            productoId = p.Id,
            barcode = p.CodigoBarras,
            name = p.Nombre,
            unitOfMeasure = p.UnidadMedida.ToString().ToLower(),
            unit_of_measure = p.UnidadMedida.ToString().ToLower(),
            category = p.Categoria?.Nombre ?? "General",
            stockActual = stockDisponible,
            stockMinimo = p.StockMinimo,
            activo = p.Activo
        });
    }

    /// <summary>
    /// Obtiene el catálogo completo de alimentos activos para la app móvil.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerCatalogo()
    {
        var result = await _productoService.ObtenerTodosAsync(soloActivos: true);
        if (!result.Success || result.Data == null)
        {
            return StatusCode(500, new { success = false, message = result.ErrorMessage });
        }

        var lista = result.Data.Select(p => new
        {
            id = p.Id.ToString(),
            productoId = p.Id,
            barcode = p.CodigoBarras,
            name = p.Nombre,
            unitOfMeasure = p.UnidadMedida.ToString().ToLower(),
            unit_of_measure = p.UnidadMedida.ToString().ToLower(),
            category = p.Categoria?.Nombre ?? "General",
            stockActual = p.Lotes?.Sum(l => l.CantidadDisponible) ?? 0,
            stockMinimo = p.StockMinimo
        });

        return Ok(lista);
    }

    /// <summary>
    /// Búsqueda de productos por nombre o categoría.
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Buscar([FromQuery] string q)
    {
        var result = await _productoService.ObtenerTodosAsync(soloActivos: true);
        if (!result.Success || result.Data == null)
        {
            return StatusCode(500, new { success = false, message = result.ErrorMessage });
        }

        var query = (q ?? string.Empty).Trim().ToLower();
        var filtrados = result.Data
            .Where(p => p.Nombre.ToLower().Contains(query) ||
                        p.CodigoBarras.Contains(query) ||
                        (p.Categoria != null && p.Categoria.Nombre.ToLower().Contains(query)))
            .Select(p => new
            {
                id = p.Id.ToString(),
                productoId = p.Id,
                barcode = p.CodigoBarras,
                name = p.Nombre,
                unitOfMeasure = p.UnidadMedida.ToString().ToLower(),
                unit_of_measure = p.UnidadMedida.ToString().ToLower(),
                category = p.Categoria?.Nombre ?? "General"
            });

        return Ok(filtrados);
    }

    /// <summary>
    /// Registra un nuevo producto en el catálogo desde el escáner móvil si el código de barras no existía.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CrearProducto([FromBody] CrearProductoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Barcode) || string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { success = false, message = "El código de barras y el nombre son obligatorios." });
        }

        // Buscar o asignar categoría activa del sistema con normalización flexible
        var categoriasRes = await _categoriaService.ObtenerTodasAsync();
        var listaCategorias = categoriasRes.Data?.ToList() ?? new List<Categoria>();

        // Función auxiliar para comparar nombres ignorando espacios, barras y mayúsculas
        static string Normalizar(string? s) => string.IsNullOrWhiteSpace(s) ? "" : s.Replace(" ", "").Replace("/", "").ToLowerInvariant();

        var nombreBuscado = !string.IsNullOrWhiteSpace(dto.CategoryName) ? dto.CategoryName : dto.Category;

        // 1. Intentar por ID exacto enviado desde el cliente
        Categoria? catValida = listaCategorias.FirstOrDefault(c => dto.CategoriaId.HasValue && c.Id == dto.CategoriaId.Value && c.Activo);

        // 2. Si el ID no coincidió pero vino nombre de categoría, buscar por nombre exacto o normalizado
        if (catValida == null && !string.IsNullOrWhiteSpace(nombreBuscado))
        {
            var norm = Normalizar(nombreBuscado);
            catValida = listaCategorias.FirstOrDefault(c => c.Activo && Normalizar(c.Nombre) == norm)
                        ?? listaCategorias.FirstOrDefault(c => c.Activo && c.Nombre.Contains(nombreBuscado.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        // 3. Si sigue sin coincidir y el nombre o intención era el comodín 'Otros' / 'Varios'
        if (catValida == null && (!string.IsNullOrWhiteSpace(nombreBuscado) && (nombreBuscado.Contains("otro", StringComparison.OrdinalIgnoreCase) || nombreBuscado.Contains("vario", StringComparison.OrdinalIgnoreCase))))
        {
            catValida = listaCategorias.FirstOrDefault(c => c.Activo && (c.Nombre.Contains("otro", StringComparison.OrdinalIgnoreCase) || c.Nombre.Contains("vario", StringComparison.OrdinalIgnoreCase)));
        }

        // 4. Si no se especificó categoría o no se encontró, buscar siempre la categoría comodín 'Otros' / 'Varios'
        if (catValida == null)
        {
            catValida = listaCategorias.FirstOrDefault(c => c.Activo && (c.Nombre.Contains("otro", StringComparison.OrdinalIgnoreCase) || c.Nombre.Contains("vario", StringComparison.OrdinalIgnoreCase)));
        }

        // 5. Si la categoría comodín no existe en la base, crearla dinámicamente en Supabase
        if (catValida == null)
        {
            var resCrearCat = await _categoriaService.CrearAsync(new Categoria
            {
                Nombre = "Otros Alimentos / Varios",
                Descripcion = "Alimentos varios no clasificados en categorías específicas",
                Activo = true
            });
            if (resCrearCat.Success && resCrearCat.Data != null)
            {
                catValida = resCrearCat.Data;
                _logger.LogInformation("Categoría comodín auto-creada en Supabase durante alta de producto: {Id} - {Nombre}", catValida.Id, catValida.Nombre);
            }
        }

        // 6. Último recurso absoluto
        int categoriaId = catValida?.Id ?? listaCategorias.FirstOrDefault(c => c.Activo)?.Id ?? 1;

        // Parsear UnidadMedida flexible (unidades, kilos/kg, litros)
        var uStr = (dto.UnitOfMeasure ?? "unidades").Trim().ToLowerInvariant();
        UnidadMedida unidad = uStr switch
        {
            "kilos" or "kg" or "kilo" => UnidadMedida.Kilos,
            "litros" or "l" or "litro" => UnidadMedida.Litros,
            _ => UnidadMedida.Unidades
        };

        var nuevo = new Producto
        {
            CodigoBarras = dto.Barcode.Trim(),
            Nombre = dto.Name.Trim(),
            CategoriaId = categoriaId,
            UnidadMedida = unidad,
            StockMinimo = dto.StockMinimo.GetValueOrDefault(10),
            Activo = true
        };

        var result = await _productoService.CrearAsync(nuevo);
        if (!result.Success || result.Data == null)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(BuscarPorCodigoBarras), new { barcode = result.Data.CodigoBarras }, new
        {
            id = result.Data.Id.ToString(),
            productoId = result.Data.Id,
            barcode = result.Data.CodigoBarras,
            name = result.Data.Nombre,
            unitOfMeasure = result.Data.UnidadMedida.ToString().ToLower(),
            unit_of_measure = result.Data.UnidadMedida.ToString().ToLower(),
            category = result.Data.Categoria?.Nombre ?? catValida?.Nombre ?? "General"
        });
    }
}

public class CrearProductoDto
{
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? UnitOfMeasure { get; set; } = "unidades";
    public int? CategoriaId { get; set; }
    public string? CategoryName { get; set; }
    public string? Category { get; set; }
    public decimal? StockMinimo { get; set; } = 10;
}
