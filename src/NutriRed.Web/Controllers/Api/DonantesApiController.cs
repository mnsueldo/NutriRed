using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutriRed.Data;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;

namespace NutriRed.Web.Controllers.Api;

[ApiController]
[Route("api/donantes")]
[Produces("application/json")]
public class DonantesApiController : ControllerBase
{
    private readonly NutriRedDbContext _context;
    private readonly ILogger<DonantesApiController> _logger;

    public DonantesApiController(
        NutriRedDbContext context,
        ILogger<DonantesApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Búsqueda de donante por DNI o CUIT (utilizado en el Paso 1 del flujo de donación móvil).
    /// </summary>
    [HttpGet("buscar")]
    public async Task<IActionResult> BuscarPorDocumento([FromQuery] string documento)
    {
        if (string.IsNullOrWhiteSpace(documento))
        {
            return BadRequest(new { success = false, message = "El número de documento es requerido." });
        }

        var docNormalizado = System.Text.RegularExpressions.Regex.Replace(documento.Trim(), @"\D", "");

        var donante = await _context.Donantes
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.NumeroDocumento != null &&
                                      d.NumeroDocumento.Replace(".", "").Replace("-", "") == docNormalizado);

        if (donante == null)
        {
            return NotFound(new { success = false, message = "Donante no encontrado." });
        }

        return Ok(MapearDonante(donante));
    }

    /// <summary>
    /// Obtiene la lista de donantes registrados.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        var donantes = await _context.Donantes
            .AsNoTracking()
            .OrderBy(d => d.NombreRazonSocial)
            .ToListAsync();

        return Ok(donantes.Select(MapearDonante));
    }

    /// <summary>
    /// Registra un nuevo donante desde la app móvil.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> GuardarDonante([FromBody] DonanteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { success = false, message = "El nombre o razón social es obligatorio." });
        }

        TipoDonante tipo = TipoDonante.Individuo;
        if (!string.IsNullOrWhiteSpace(dto.Type))
        {
            if (dto.Type.Equals("INSTITUTION", StringComparison.OrdinalIgnoreCase) ||
                dto.Type.Equals("Institucion", StringComparison.OrdinalIgnoreCase))
            {
                tipo = TipoDonante.Institucion;
            }
            else if (dto.Type.Equals("ANONYMOUS", StringComparison.OrdinalIgnoreCase) ||
                     dto.Type.Equals("Anonimo", StringComparison.OrdinalIgnoreCase))
            {
                tipo = TipoDonante.Anonimo;
            }
        }

        var nuevo = new Donante
        {
            Tipo = tipo,
            NumeroDocumento = dto.DocumentNumber?.Trim(),
            NombreRazonSocial = dto.Name.Trim(),
            Telefono = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            Activo = true
        };

        _context.Donantes.Add(nuevo);
        await _context.SaveChangesAsync();

        return Ok(MapearDonante(nuevo));
    }

    private static object MapearDonante(Donante d)
    {
        string typeStr = d.Tipo switch
        {
            TipoDonante.Institucion => "INSTITUTION",
            TipoDonante.Anonimo => "ANONYMOUS",
            _ => "INDIVIDUAL"
        };

        return new
        {
            id = $"DONOR-{d.Id:D3}",
            donanteId = d.Id,
            type = typeStr,
            document_number = d.NumeroDocumento,
            documentNumber = d.NumeroDocumento,
            name = d.NombreRazonSocial,
            phone = d.Telefono,
            email = d.Email
        };
    }
}

public class DonanteDto
{
    public string? Id { get; set; }
    public string? Type { get; set; } // INDIVIDUAL, INSTITUTION, ANONYMOUS
    public string? DocumentNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
