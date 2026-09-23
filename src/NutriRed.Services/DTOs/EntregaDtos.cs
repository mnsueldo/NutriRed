using NutriRed.Domain.Enums;

namespace NutriRed.Services.DTOs;

public class RegistrarEntregaRequest
{
    public string CodigoSeguimiento { get; set; } = string.Empty; // QR escaneado (ej: PKG-2026-00001)
    public TipoReceptor TipoReceptor { get; set; } = TipoReceptor.Titular;
    public string DniReceptor { get; set; } = string.Empty;
    public string NombreReceptor { get; set; } = string.Empty;
    public string? VinculoConTitular { get; set; } // Obligatorio si es TerceroAutorizado
    public string FirmaDigital { get; set; } = string.Empty; // Trazo en Base64 desde el celular
    public string VoluntarioDespachoId { get; set; } = string.Empty;
}

public class RegistrarEntregaFallidaRequest
{
    public string CodigoSeguimiento { get; set; } = string.Empty;
    public string MotivoNoEntrega { get; set; } = string.Empty; // Ej: Ausente, Rechazado
    public string VoluntarioDespachoId { get; set; } = string.Empty;
}

public class PaquetePendienteDespachoDto
{
    public int PaqueteId { get; set; }
    public string CodigoSeguimiento { get; set; } = string.Empty;
    public int FamiliaId { get; set; }
    public string FamiliaTitular { get; set; } = string.Empty;
    public string DniTitular { get; set; } = string.Empty;
    public int CantidadIntegrantes { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string TipoPaqueteNombre { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public List<PaqueteItemDto> Alimentos { get; set; } = new();
}

public class ComprobanteEntregaDto
{
    public int EntregaId { get; set; }
    public int PaqueteId { get; set; }
    public string CodigoSeguimiento { get; set; } = string.Empty;
    public DateTime FechaHoraEntrega { get; set; }
    public string VoluntarioDespachoId { get; set; } = string.Empty;
    public string FamiliaTitular { get; set; } = string.Empty;
    public string DniFamilia { get; set; } = string.Empty;
    public TipoReceptor TipoReceptor { get; set; }
    public string DniReceptor { get; set; } = string.Empty;
    public string NombreReceptor { get; set; } = string.Empty;
    public string? VinculoConTitular { get; set; }
    public string FirmaDigital { get; set; } = string.Empty;
    public bool Concretada { get; set; }
    public string? MotivoNoEntrega { get; set; }
    public List<PaqueteItemDto> AlimentosEntregados { get; set; } = new();
}
