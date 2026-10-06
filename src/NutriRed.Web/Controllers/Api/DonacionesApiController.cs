using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using NutriRed.Domain.Enums;
using NutriRed.Services.DTOs;
using NutriRed.Services.Interfaces;

namespace NutriRed.Web.Controllers.Api;

[ApiController]
[Route("api/donaciones")]
[Produces("application/json")]
public class DonacionesApiController : ControllerBase
{
    private readonly IDonacionService _donacionService;
    private readonly ILogger<DonacionesApiController> _logger;

    public DonacionesApiController(
        IDonacionService donacionService,
        ILogger<DonacionesApiController> logger)
    {
        _donacionService = donacionService;
        _logger = logger;
    }

    /// <summary>
    /// Registra una donación recibida desde la app móvil Android.
    /// Valida los alimentos, crea los lotes, audita movimientos de stock e incrementa inventario.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegistrarDonacion([FromBody] RegistrarDonacionApiRequest request)
    {
        if (request == null || request.Items == null || !request.Items.Any())
        {
            return BadRequest(new { success = false, message = "La donación debe contener al menos un alimento." });
        }

        // Mapear TipoDonante
        TipoDonante tipo = TipoDonante.Individuo;
        var rawType = request.DonorType ?? request.Donor?.Type ?? string.Empty;
        if (rawType.Equals("INSTITUTION", StringComparison.OrdinalIgnoreCase) ||
            rawType.Equals("Institucion", StringComparison.OrdinalIgnoreCase))
        {
            tipo = TipoDonante.Institucion;
        }
        else if (rawType.Equals("ANONYMOUS", StringComparison.OrdinalIgnoreCase) ||
                 rawType.Equals("Anonimo", StringComparison.OrdinalIgnoreCase))
        {
            tipo = TipoDonante.Anonimo;
        }

        var docNumber = request.DocumentNumber ?? request.Donor?.DocumentNumber;
        var donorName = request.DonorName ?? request.Donor?.Name;

        if (tipo != TipoDonante.Anonimo && (string.IsNullOrWhiteSpace(docNumber) || string.IsNullOrWhiteSpace(donorName)))
        {
            return BadRequest(new { success = false, message = "Para donantes individuales o instituciones, el documento y nombre son obligatorios." });
        }

        // Mapear Items
        var itemsRequest = new List<DonacionItemRequest>();
        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ProductBarcode))
            {
                return BadRequest(new { success = false, message = "Cada alimento debe contar con su código de barras escaneado." });
            }

            if (item.Quantity <= 0)
            {
                return BadRequest(new { success = false, message = $"La cantidad para el producto '{item.ProductBarcode}' debe ser mayor a 0." });
            }

            var fechaVenc = ParsearFecha(item.ExpirationDate);
            if (fechaVenc.Date <= DateTime.Today)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"El producto con código '{item.ProductBarcode}' está vencido o vence hoy ({fechaVenc:dd/MM/yyyy}). No se puede recibir según política FEFO."
                });
            }

            itemsRequest.Add(new DonacionItemRequest
            {
                CodigoBarras = item.ProductBarcode.Trim(),
                Cantidad = (decimal)item.Quantity,
                FechaVencimiento = fechaVenc,
                NumeroLote = string.IsNullOrWhiteSpace(item.BatchNumber) ? null : item.BatchNumber.Trim()
            });
        }

        var serviceRequest = new RegistrarDonacionRequest
        {
            TipoDonante = tipo,
            NumeroDocumento = docNumber?.Trim(),
            NombreRazonSocial = donorName?.Trim(),
            Telefono = (request.Phone ?? request.Donor?.Phone)?.Trim(),
            Email = (request.Email ?? request.Donor?.Email)?.Trim(),
            VoluntarioReceptorId = string.IsNullOrWhiteSpace(request.VolunteerId) ? "voluntario_movil" : request.VolunteerId.Trim(),
            Observaciones = request.Observaciones,
            Items = itemsRequest
        };

        var resultado = await _donacionService.RegistrarDonacionAsync(serviceRequest);
        if (!resultado.Success || resultado.Data == null)
        {
            return BadRequest(new { success = false, message = resultado.ErrorMessage });
        }

        var data = resultado.Data;
        return CreatedAtAction(nameof(ObtenerPorId), new { id = data.DonacionId }, new
        {
            success = true,
            id = data.DonacionId,
            donation_code = data.CodigoComprobante,
            donationCode = data.CodigoComprobante,
            timestamp = data.FechaHora.ToString("dd/MM/yyyy HH:mm"),
            volunteer_id = data.VoluntarioReceptorId,
            volunteer_name = "Voluntario Recepción",
            donor_name = data.DonanteNombre,
            total_items = data.TotalUnidadesRecibidas,
            items = data.Detalles.Select(d => new
            {
                product_barcode = d.CodigoBarras,
                product_name = d.NombreProducto,
                quantity = d.Cantidad,
                unit_of_measure = d.UnidadMedida.ToString().ToLower(),
                batch_number = d.NumeroLote,
                expiration_date = d.FechaVencimiento.ToString("MM/yyyy")
            }),
            message = $"Donación registrada con éxito. Comprobante: {data.CodigoComprobante}"
        });
    }

    /// <summary>
    /// Consulta el detalle de una donación por su ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var result = await _donacionService.ObtenerPorIdAsync(id);
        if (!result.Success || result.Data == null)
        {
            return NotFound(new { success = false, message = result.ErrorMessage ?? "Donación no encontrada." });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Consulta el listado histórico de donaciones.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerTodas()
    {
        var result = await _donacionService.ObtenerTodasAsync();
        if (!result.Success || result.Data == null)
        {
            return StatusCode(500, new { success = false, message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    private static DateTime ParsearFecha(string? fechaStr)
    {
        if (string.IsNullOrWhiteSpace(fechaStr))
        {
            return DateTime.UtcNow.AddMonths(6);
        }

        var clean = fechaStr.Trim();

        // Formato MM/yyyy (ej: "11/2026") -> último día del mes
        if (DateTime.TryParseExact(clean, "MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var my))
        {
            int ultimoDia = DateTime.DaysInMonth(my.Year, my.Month);
            return new DateTime(my.Year, my.Month, ultimoDia, 23, 59, 59, DateTimeKind.Utc);
        }

        // Formato dd/MM/yyyy (ej: "15/11/2026")
        if (DateTime.TryParseExact(clean, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dmy))
        {
            return DateTime.SpecifyKind(dmy, DateTimeKind.Utc);
        }

        // Formato ISO estándar yyyy-MM-dd
        if (DateTime.TryParse(clean, CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso))
        {
            return DateTime.SpecifyKind(iso, DateTimeKind.Utc);
        }

        return DateTime.UtcNow.AddMonths(6);
    }
}

public class RegistrarDonacionApiRequest
{
    public string? DonorId { get; set; }
    public string? DonorType { get; set; }
    public string? DocumentNumber { get; set; }
    public string? DonorName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? VolunteerId { get; set; } = "VOL-01";
    public string? VolunteerName { get; set; }
    public string? Observaciones { get; set; }
    public DonanteDto? Donor { get; set; }
    public List<DonacionItemApiRequest> Items { get; set; } = new();
}

public class DonacionItemApiRequest
{
    public string? ProductId { get; set; }
    public string ProductBarcode { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public double Quantity { get; set; }
    public string? UnitOfMeasure { get; set; }
    public string? ExpirationDate { get; set; }
    public string? BatchNumber { get; set; }
}
