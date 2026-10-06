using Microsoft.AspNetCore.Mvc;
using NutriRed.Services.DTOs;
using NutriRed.Services.Interfaces;
using NutriRed.Web.Models;

namespace NutriRed.Web.Controllers;

public class DashboardController : Controller
{
    private readonly IFamiliaService _familiaService;
    private readonly IInventarioService _inventarioService;

    public DashboardController(
        IFamiliaService familiaService,
        IInventarioService inventarioService)
    {
        _familiaService = familiaService;
        _inventarioService = inventarioService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // 1. Obtener familias en estado Activo
        var familiasResult = await _familiaService.ObtenerTodasAsync(incluirInactivos: false);
        int familiasActivas = (familiasResult != null && familiasResult.Success && familiasResult.Data != null)
            ? familiasResult.Data.Count()
            : 0;

        // 2. Obtener el stock total consolidado de productos y procesar conteos FEFO
        var stockResult = await _inventarioService.ObtenerStockConsolidadoAsync();
        var stockList = (stockResult != null && stockResult.Success && stockResult.Data != null)
            ? stockResult.Data.ToList()
            : new List<StockProductoDto>();

        decimal stockTotalKg = stockList.Sum(p => p.StockActual);

        // Conteos FEFO para la gráfica de torta
        int totalOptimo = stockList.Count(s => s.StockActual > 0 && s.EstadoFEFO == "Optimo");
        int totalAtencion = stockList.Count(s => s.StockActual > 0 && s.EstadoFEFO == "Alerta");
        int totalUrgente = stockList.Count(s => s.StockActual > 0 && s.EstadoFEFO == "Urgente");
        int totalVencido = stockList.Count(s => s.StockActual > 0 && s.EstadoFEFO == "Vencido");

        // 3. Obtener el total acumulado de mermas/bajas
        var mermaResult = await _inventarioService.ObtenerTotalMermasKgAsync();
        decimal mermaTotalKg = (mermaResult != null && mermaResult.Success)
            ? mermaResult.Data
            : 0m;

        // 4. Obtener el total acumulado de alimentos distribuidos
        var distribuidosResult = await _inventarioService.ObtenerTotalAlimentosDistribuidosAsync();
        decimal distribuidosTotal = (distribuidosResult != null && distribuidosResult.Success)
            ? distribuidosResult.Data
            : 0m;

        // 5. Obtener alertas de proximidad a vencer (próximos 30 días)
        var alertasViewModel = new List<LoteAlertaItemViewModel>();
        var lotesAlertasResult = await _inventarioService.ObtenerLotesProximosAVencerAsync(30);

        if (lotesAlertasResult != null && lotesAlertasResult.Success && lotesAlertasResult.Data != null)
        {
            foreach (var lote in lotesAlertasResult.Data)
            {
                int diasRestantes = (lote.FechaVencimiento.Date - DateTime.Today).Days;

                alertasViewModel.Add(new LoteAlertaItemViewModel
                {
                    NombreProducto = lote.Producto?.Nombre ?? "Producto sin nombre",
                    CodigoLote = !string.IsNullOrEmpty(lote.NumeroLote) ? lote.NumeroLote : $"Lote #{lote.Id}",
                    CantidadDisponible = lote.CantidadDisponible,
                    DiasParaVencer = diasRestantes
                });
            }
        }

        // 6. Construir un ÚNICO ViewModel con todos los datos integrados
        var viewModel = new DashboardViewModel
        {
            // KPIs Principales
            FamiliasBeneficiariasActivas = familiasActivas,
            TotalProductosStockKg = stockTotalKg,
            AlimentosDistribuidosKg = distribuidosTotal,
            ProductosMermaKg = mermaTotalKg,
            LotesAlertas = alertasViewModel,

            // Métricas para el Semáforo FEFO (Gráfica de Torta)
            CantidadFefoOptimo = totalOptimo,
            CantidadFefoAtencion = totalAtencion,
            CantidadFefoUrgente = totalUrgente,
            CantidadFefoVencido = totalVencido
        };

        return View(viewModel);
    }
}