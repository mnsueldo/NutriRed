using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NutriRed.Services.DTOs;

namespace NutriRed.Services.Interfaces;

public interface IReporteService
{
    Task<List<ReporteInventarioItemDto>> ObtenerReporteInventarioAsync(int? categoriaId, string? busquedaInventario, string? estadoStock, string? semaforoFefo);
    Task<List<ReporteDonacionItemDto>> ObtenerReporteDonacionesAsync(DateTime? desde, DateTime? hasta, int? donanteId);
    Task<List<ReporteEntregaItemDto>> ObtenerReporteEntregasAsync(DateTime? desde, DateTime? hasta, int? tipoPaqueteId, string? voluntarioId);
    Task<List<ReporteFamiliaItemDto>> ObtenerReporteFamiliasAsync(DateTime? desde, DateTime? hasta, string? busqueda);
    Task<List<ReporteTrazabilidadItemDto>> ObtenerReporteTrazabilidadAsync(DateTime? desde, DateTime? hasta, string? busqueda);
}