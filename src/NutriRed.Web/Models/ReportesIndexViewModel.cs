using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using NutriRed.Services.DTOs;

namespace NutriRed.Web.Models
{
    public class ReportesIndexViewModel
    {
        public string TabActiva { get; set; } = "inventario";

        // Filtros
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int? CategoriaId { get; set; }
        public string? BusquedaInventario { get; set; }
        public string? EstadoStock { get; set; }
        public string? SemaforoFefo { get; set; }
        public int? DonanteId { get; set; }
        public int? TipoPaqueteId { get; set; }
        public string? VoluntarioId { get; set; }
        public string? Busqueda { get; set; }

        // Paginación
        public int PaginaActual { get; set; } = 1;
        public int TotalPaginas { get; set; } = 1;
        public int TotalRegistros { get; set; }

        // Listas y datos...
        public List<SelectListItem> Categorias { get; set; } = new();
        public List<SelectListItem> Donantes { get; set; } = new();
        public List<SelectListItem> TiposPaquete { get; set; } = new();
        public List<SelectListItem> Voluntarios { get; set; } = new();

        public List<ReporteInventarioItemDto> ReporteInventario { get; set; } = new();
        public List<ReporteDonacionItemDto> ReporteDonaciones { get; set; } = new();
        public List<ReporteEntregaItemDto> ReporteEntregas { get; set; } = new();
        public List<ReporteFamiliaItemDto> ReporteFamilias { get; set; } = new();
        public List<ReporteTrazabilidadItemDto> ReporteTrazabilidad { get; set; } = new();
    }
}