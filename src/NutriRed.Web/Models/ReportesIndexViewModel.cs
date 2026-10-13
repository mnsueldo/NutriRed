using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using NutriRed.Services.DTOs;

namespace NutriRed.Web.Models
{
    public class ReportesIndexViewModel
    {
        public string TabActiva { get; set; } = "inventario";

        // Filtros Generales
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int? CategoriaId { get; set; }
        public string? EstadoLote { get; set; }
        public int? DonanteId { get; set; }
        public int? TipoPaqueteId { get; set; }
        public string? VoluntarioId { get; set; }
        public string? Busqueda { get; set; }

        // Selects
        public List<SelectListItem> Categorias { get; set; } = new();
        public List<SelectListItem> Donantes { get; set; } = new();
        public List<SelectListItem> TiposPaquete { get; set; } = new();
        public List<SelectListItem> Voluntarios { get; set; } = new();

        // Datos
        public List<ReporteInventarioItemDto> ReporteInventario { get; set; } = new();
        public List<ReporteDonacionItemDto> ReporteDonaciones { get; set; } = new();
        public List<ReporteEntregaItemDto> ReporteEntregas { get; set; } = new();
        public List<ReporteFamiliaItemDto> ReporteFamilias { get; set; } = new();
        public List<ReporteTrazabilidadItemDto> ReporteTrazabilidad { get; set; } = new();
    }
}