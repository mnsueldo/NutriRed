using NutriRed.Domain.Enums;

namespace NutriRed.Services.DTOs;

public class AsignacionLoteDto
{
    public int LoteId { get; set; }
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime FechaVencimiento { get; set; }
    public decimal CantidadAsignada { get; set; }
}

public class ResultadoFefoDto
{
    public int ProductoId { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public decimal CantidadRequerida { get; set; }
    public decimal CantidadAsignada { get; set; }
    public bool CubiertoTotalmente => CantidadAsignada >= CantidadRequerida;
    public decimal Faltante => Math.Max(0, CantidadRequerida - CantidadAsignada);
    public List<AsignacionLoteDto> Asignaciones { get; set; } = new();
}

public class StockProductoDto
{
    public int ProductoId { get; set; }
    public string CodigoBarras { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public UnidadMedida UnidadMedida { get; set; }
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
    public bool RequiereReposicion => StockActual <= StockMinimo;
    public int CantidadLotesActivos { get; set; }
    public DateTime? ProximoVencimiento { get; set; }
}

public class AjusteMermaRequest
{
    public int LoteId { get; set; }
    public decimal Cantidad { get; set; }
    public TipoMovimiento TipoMovimiento { get; set; } = TipoMovimiento.BajaPorMerma;
    public MotivoMovimiento Motivo { get; set; } = MotivoMovimiento.Deteriorado;
    public string UsuarioId { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}
