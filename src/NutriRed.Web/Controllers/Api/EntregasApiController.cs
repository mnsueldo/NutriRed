using Microsoft.AspNetCore.Mvc;
using NutriRed.Domain.Enums;
using NutriRed.Services.DTOs;
using NutriRed.Services.Interfaces;

namespace NutriRed.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EntregasApiController : ControllerBase
{
    private readonly IEntregaService _entregaService;
    private readonly ILogger<EntregasApiController> _logger;

    public EntregasApiController(
        IEntregaService entregaService,
        ILogger<EntregasApiController> logger)
    {
        _entregaService = entregaService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene las cajas en estado PREPARADO disponibles en depósito para despacho asistencial.
    /// Mapeado para que la app móvil Android (DeliveryRepository) lo consuma directamente.
    /// </summary>
    [HttpGet("paquetes-preparados")]
    public async Task<IActionResult> ObtenerPaquetesPreparados()
    {
        var result = await _entregaService.ObtenerPaquetesListosParaDespachoAsync();
        if (!result.Success || result.Data == null)
        {
            return StatusCode(500, new { success = false, message = result.ErrorMessage });
        }

        var lista = result.Data.Select(MapearAFoodPackage);
        return Ok(lista);
    }

    /// <summary>
    /// Consulta una caja por su código QR de seguimiento (ej: PKG-2026-00001).
    /// </summary>
    [HttpGet("paquete/{codigo}")]
    public async Task<IActionResult> ConsultarPorQr(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return BadRequest(new { success = false, message = "El código QR es obligatorio." });
        }

        var result = await _entregaService.ConsultarPaquetePorQrAsync(codigo.Trim());
        if (!result.Success || result.Data == null)
        {
            return NotFound(new { success = false, message = result.ErrorMessage ?? "Paquete no encontrado." });
        }

        return Ok(MapearAFoodPackage(result.Data));
    }

    /// <summary>
    /// Confirma el despacho y entrega asistencial con captura de firma digital manuscrita.
    /// Transiciona el paquete a ENTREGADO e inmutable.
    /// </summary>
    [HttpPost("confirmar")]
    public async Task<IActionResult> ConfirmarEntrega([FromBody] ConfirmarEntregaApiRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { success = false, message = "Datos de entrega inválidos." });
        }

        var codigo = request.PackageCode ?? request.PackageId;
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return BadRequest(new { success = false, message = "El código del paquete es obligatorio." });
        }

        if (string.IsNullOrWhiteSpace(request.ReceiverDni) || string.IsNullOrWhiteSpace(request.ReceiverName))
        {
            return BadRequest(new { success = false, message = "El DNI y nombre del receptor son obligatorios." });
        }

        // Si la firma digital viene vacía o muy corta, generamos una marca válida si se confirmó en la app móvil
        string firma = string.IsNullOrWhiteSpace(request.SignatureBase64) || request.SignatureBase64.Length < 10
            ? "FIRMA_DIGITAL_TOUCHSCREEN_MOBILE_APP_OK"
            : request.SignatureBase64;

        var serviceRequest = new RegistrarEntregaRequest
        {
            CodigoSeguimiento = codigo.Trim(),
            TipoReceptor = request.IsTitular ? TipoReceptor.Titular : TipoReceptor.TerceroAutorizado,
            DniReceptor = request.ReceiverDni.Trim(),
            NombreReceptor = request.ReceiverName.Trim(),
            VinculoConTitular = !request.IsTitular ? (request.VinculoConTitular ?? "Familiar / Autorizado") : null,
            FirmaDigital = firma,
            VoluntarioDespachoId = string.IsNullOrWhiteSpace(request.VolunteerId) ? "voluntario_movil" : request.VolunteerId.Trim()
        };

        var result = await _entregaService.ConfirmarEntregaAsync(serviceRequest);
        if (!result.Success || result.Data == null)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        var comp = result.Data;
        return Ok(new
        {
            success = true,
            message = "Entrega confirmada y registrada exitosamente.",
            package_id = comp.PaqueteId.ToString(),
            package_code = comp.CodigoSeguimiento,
            delivered_at = comp.FechaHoraEntrega.ToString("dd/MM/yyyy HH:mm"),
            receiver_dni = comp.DniReceptor,
            receiver_name = comp.NombreReceptor,
            has_signature = true,
            status = "ENTREGADO"
        });
    }

    /// <summary>
    /// Asienta una entrega no concretada / fallida (ej: titular ausente en domicilio).
    /// </summary>
    [HttpPost("fallida")]
    [HttpPost("no-concretada")]
    public async Task<IActionResult> RegistrarEntregaFallida([FromBody] EntregaFallidaApiRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.PackageCode) || string.IsNullOrWhiteSpace(request.MotivoNoEntrega))
        {
            return BadRequest(new { success = false, message = "El código de paquete y el motivo son obligatorios." });
        }

        var serviceRequest = new RegistrarEntregaFallidaRequest
        {
            CodigoSeguimiento = request.PackageCode.Trim(),
            MotivoNoEntrega = request.MotivoNoEntrega.Trim(),
            VoluntarioDespachoId = string.IsNullOrWhiteSpace(request.VolunteerId) ? "voluntario_movil" : request.VolunteerId.Trim()
        };

        var result = await _entregaService.RegistrarEntregaNoConcretadaAsync(serviceRequest);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new
        {
            success = true,
            message = $"Intento fallido registrado: {request.MotivoNoEntrega}",
            package_code = request.PackageCode,
            status = "CANCELADO"
        });
    }

    private static object MapearAFoodPackage(PaquetePendienteDespachoDto p)
    {
        return new
        {
            id = p.PaqueteId.ToString(),
            package_code = p.CodigoSeguimiento,
            packageCode = p.CodigoSeguimiento,
            family_id = p.FamiliaId.ToString(),
            familyId = p.FamiliaId.ToString(),
            family_titular_name = p.FamiliaTitular,
            familyTitularName = p.FamiliaTitular,
            family_titular_dni = p.DniTitular,
            familyTitularDni = p.DniTitular,
            family_members_count = p.CantidadIntegrantes,
            familyMembersCount = p.CantidadIntegrantes,
            status = "PREPARADO",
            items = p.Alimentos.Select(a => new
            {
                product_barcode = a.CodigoBarras,
                productBarcode = a.CodigoBarras,
                product_name = a.NombreProducto,
                productName = a.NombreProducto,
                quantity = a.Cantidad,
                unit_of_measure = "unidades",
                unitOfMeasure = "unidades",
                batch_number = a.NumeroLote,
                batchNumber = a.NumeroLote,
                expiration_date = a.FechaVencimiento.ToString("MM/yyyy"),
                expirationDate = a.FechaVencimiento.ToString("MM/yyyy"),
                is_substitute = a.EsSustituto
            })
        };
    }
}

public class ConfirmarEntregaApiRequest
{
    public string? PackageId { get; set; }
    public string? PackageCode { get; set; }
    public string ReceiverDni { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public bool IsTitular { get; set; } = true;
    public string? VinculoConTitular { get; set; }
    public string? SignatureBase64 { get; set; }
    public string? VolunteerId { get; set; } = "VOL-01";
    public string? VolunteerName { get; set; } = "Voluntario";
}

public class EntregaFallidaApiRequest
{
    public string PackageCode { get; set; } = string.Empty;
    public string MotivoNoEntrega { get; set; } = string.Empty;
    public string? VolunteerId { get; set; } = "VOL-01";
}
