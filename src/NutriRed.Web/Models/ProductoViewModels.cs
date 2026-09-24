using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;

namespace NutriRed.Web.Models;

public class ProductoDetalleViewModel
{
    public Producto Producto { get; set; } = null!;
    public List<LoteItemViewModel> Lotes { get; set; } = new();

    public decimal StockTotalDisponible => Lotes
        .Where(l => l.Estado == EstadoLote.Disponible)
        .Sum(l => l.CantidadDisponible);

    public bool RequiereReposicion => StockTotalDisponible <= Producto.StockMinimo;
}

public class LoteItemViewModel
{
    public int Id { get; set; }
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime FechaVencimiento { get; set; }
    public decimal CantidadDisponible { get; set; }
    public decimal CantidadInicial { get; set; }
    public EstadoLote Estado { get; set; }

    public int DiasParaVencer => (int)(FechaVencimiento.Date - DateTime.Today).TotalDays;

    public string ClaseFefo => DiasParaVencer switch
    {
        <= 0 => "badge-fefo-critico",
        <= 5 => "badge-fefo-urgente",
        <= 15 => "badge-fefo-atencion",
        _ => "badge-fefo-optimo"
    };

    public string TextoFefo => DiasParaVencer switch
    {
        < 0 => $"Vencido hace {Math.Abs(DiasParaVencer)} días",
        0 => "Vence hoy",
        1 => "Vence mañana",
        _ => $"Vence en {DiasParaVencer} días"
    };
}
