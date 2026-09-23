using NutriRed.Domain.Enums;

namespace NutriRed.Services.DTOs;

public class DonacionItemRequest
{
    public string CodigoBarras { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string? NumeroLote { get; set; }
}

public class RegistrarDonacionRequest
{
    public TipoDonante TipoDonante { get; set; } = TipoDonante.Individuo;
    public string? NumeroDocumento { get; set; } // DNI o CUIT
    public string? NombreRazonSocial { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string VoluntarioReceptorId { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public List<DonacionItemRequest> Items { get; set; } = new();
}

public class DonacionDetalleDto
{
    public int ProductoId { get; set; }
    public string CodigoBarras { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public UnidadMedida UnidadMedida { get; set; }
    public decimal Cantidad { get; set; }
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime FechaVencimiento { get; set; }
}

public class DonacionComprobanteDto
{
    public int DonacionId { get; set; }
    public string CodigoComprobante { get; set; } = string.Empty; // Ej: DON-2026-00001
    public DateTime FechaHora { get; set; }
    public string VoluntarioReceptorId { get; set; } = string.Empty;
    public string DonanteNombre { get; set; } = string.Empty;
    public string? DonanteDocumento { get; set; }
    public TipoDonante TipoDonante { get; set; }
    public string? Observaciones { get; set; }
    public decimal TotalUnidadesRecibidas { get; set; }
    public List<DonacionDetalleDto> Detalles { get; set; } = new();
}
