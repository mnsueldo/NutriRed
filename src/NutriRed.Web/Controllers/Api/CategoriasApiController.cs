using Microsoft.AspNetCore.Mvc;
using NutriRed.Domain.Entities;
using NutriRed.Services.Interfaces;

namespace NutriRed.Web.Controllers.Api;

[ApiController]
[Route("api/categorias")]
[Route("api/productos/categorias")]
[Produces("application/json")]
public class CategoriasApiController : ControllerBase
{
    private readonly ICategoriaService _categoriaService;
    private readonly ILogger<CategoriasApiController> _logger;

    public CategoriasApiController(
        ICategoriaService categoriaService,
        ILogger<CategoriasApiController> logger)
    {
        _categoriaService = categoriaService;
        _logger = logger;
    }

    /// <summary>
    /// Devuelve el listado de categorías activas para el clasificador de alimentos de la app móvil.
    /// Garantiza la existencia de la categoría comodín "Otros Alimentos / Varios" para evitar productos huérfanos.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerCategorias()
    {
        var result = await _categoriaService.ObtenerTodasAsync(soloActivas: true);
        if (!result.Success || result.Data == null)
        {
            return StatusCode(500, new { success = false, message = result.ErrorMessage });
        }

        var lista = result.Data.ToList();

        // Asegurar que exista la categoría comodín intermedia (reconociendo cualquier variante creada)
        bool yaExisteComodin = lista.Any(c => c.Nombre.Contains("otro", StringComparison.OrdinalIgnoreCase) || c.Nombre.Contains("vario", StringComparison.OrdinalIgnoreCase));
        if (!yaExisteComodin)
        {
            var resCrear = await _categoriaService.CrearAsync(new Categoria
            {
                Nombre = "Otros Alimentos / Varios",
                Descripcion = "Alimentos varios no clasificados en categorías específicas",
                Activo = true
            });

            if (resCrear.Success && resCrear.Data != null)
            {
                lista.Add(resCrear.Data);
                _logger.LogInformation("Categoría comodín auto-creada en Supabase: {Id} - {Nombre}", resCrear.Data.Id, resCrear.Data.Nombre);
            }
        }

        var response = lista.OrderBy(c => c.Id).Select(c => new
        {
            id = c.Id,
            name = c.Nombre,
            description = c.Descripcion,
            activo = c.Activo
        });

        return Ok(response);
    }
}
