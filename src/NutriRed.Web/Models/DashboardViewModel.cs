namespace NutriRed.Web.Models;

public class DashboardViewModel
{
    public decimal AlimentosDistribuidosKg { get; set; }
    public int FamiliasBeneficiariasActivas { get; set; }
    public decimal TotalProductosStockKg { get; set; }
    public decimal ProductosMermaKg { get; set; }

    // Lista para alimentar el panel FEFO de prevención de desperdicios
    public List<LoteAlertaItemViewModel> LotesAlertas { get; set; } = new();
    // Conteos para la gráfica de Semáforo FEFO
    public int CantidadFefoOptimo { get; set; }   // > 15 días
    public int CantidadFefoAtencion { get; set; } // 6 a 15 días (o <= 15 d)
    public int CantidadFefoUrgente { get; set; }  // 1 a 5 días
    public int CantidadFefoVencido { get; set; }  // <= 0 días
    // Datos dinámicos para el gráfico de Distribución Mensual (Kgs)
    public List<string> DistribucionMensualEtiquetas { get; set; } = new();
    public List<decimal> DistribucionMensualValores { get; set; } = new();
}

public class LoteAlertaItemViewModel
{
    public string NombreProducto { get; set; } = string.Empty;
    public string CodigoLote { get; set; } = string.Empty;
    public decimal CantidadDisponible { get; set; }
    public int DiasParaVencer { get; set; }
}