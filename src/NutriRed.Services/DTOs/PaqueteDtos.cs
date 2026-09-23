using NutriRed.Domain.Enums;

namespace NutriRed.Services.DTOs;

public class ItemArmadoRequest
{
    public int ProductoId { get; set; }
    public decimal Cantidad { get; set; }
    public bool EsSustituto { get; set; } = false;
    public int? ProductoSustituidoId { get; set; }
}

public class ConfirmarArmadoPaqueteRequest
{
    public int FamiliaId { get; set; }
    public int TipoPaqueteId { get; set; }
    public string UsuarioArmadorId { get; set; } = string.Empty;
    public List<ItemArmadoRequest> Items { get; set; } = new();
}

public class ItemPropuestaDto
{
    public int ProductoId { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public string CodigoBarras { get; set; } = string.Empty;
    public decimal CantidadRequerida { get; set; }
    public decimal CantidadDisponible { get; set; }
    public decimal Faltante => Math.Max(0, CantidadRequerida - CantidadDisponible);
    public bool CubiertoTotalmente => CantidadDisponible >= CantidadRequerida;
    public List<AsignacionLoteDto> LotesAsignados { get; set; } = new();
    public bool EsSustituto { get; set; } = false;
    public int? ProductoSustituidoId { get; set; }
    public string? NombreProductoSustituido { get; set; }
}

public class PropuestaPaqueteDto
{
    public int FamiliaId { get; set; }
    public string FamiliaTitular { get; set; } = string.Empty;
    public string DniTitular { get; set; } = string.Empty;
    public int CantidadIntegrantes { get; set; }
    public int TipoPaqueteId { get; set; }
    public string TipoPaqueteNombre { get; set; } = string.Empty;
    public bool HayFaltantes => Items.Any(i => !i.CubiertoTotalmente);
    public List<ItemPropuestaDto> Items { get; set; } = new();
}

public class PaqueteItemDto
{
    public int ProductoId { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public string CodigoBarras { get; set; } = string.Empty;
    public int LoteId { get; set; }
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime FechaVencimiento { get; set; }
    public decimal Cantidad { get; set; }
    public bool EsSustituto { get; set; }
    public string? NombreProductoSustituido { get; set; }
}

public class PaqueteDto
{
    public int Id { get; set; }
    public string CodigoSeguimiento { get; set; } = string.Empty; // QR rotulación ej: PKG-2026-00001
    public int FamiliaId { get; set; }
    public string FamiliaTitular { get; set; } = string.Empty;
    public string DniTitular { get; set; } = string.Empty;
    public int TipoPaqueteId { get; set; }
    public string TipoPaqueteNombre { get; set; } = string.Empty;
    public EstadoPaquete Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string UsuarioArmadorId { get; set; } = string.Empty;
    public List<PaqueteItemDto> Items { get; set; } = new();
}
