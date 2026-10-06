using Microsoft.AspNetCore.Mvc;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.Interfaces;

namespace NutriRed.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
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

        // Buscar o asignar categoría
        int categoriaId = dto.CategoriaId.GetValueOrDefault(1);
        if (categoriaId <= 0)
        {
            var categorias = await _categoriaService.ObtenerTodasAsync();
            categoriaId = categorias.Data?.FirstOrDefault()?.Id ?? 1;
        }

        // Parsear UnidadMedida
        if (!Enum.TryParse<UnidadMedida>(dto.UnitOfMeasure, true, out var unidad))
        {
            unidad = UnidadMedida.Unidades;
        }

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
            category = result.Data.Categoria?.Nombre ?? "General"
        });
    }
}

public class CrearProductoDto
{
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? UnitOfMeasure { get; set; } = "unidades";
    public int? CategoriaId { get; set; }
    public decimal? StockMinimo { get; set; } = 10;
}
