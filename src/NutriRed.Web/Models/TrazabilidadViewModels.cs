using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace NutriRed.Web.Models
{
    public class TrazabilidadFiltroViewModel
    {
        public string? Busqueda { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int? DonanteId { get; set; }
        public int? TipoPaqueteId { get; set; }

        // Listas para los desplegables (Selects)
        public List<SelectListItem> Donantes { get; set; } = new();
        public List<SelectListItem> TiposPaquete { get; set; } = new();

        public List<TrazabilidadEntregaGroupDto> EntregasGrouped { get; set; } = new();
    }

    public class TrazabilidadEntregaGroupDto
    {
        public int PaqueteId { get; set; }
        public string CodigoPaquete { get; set; } = string.Empty;
        public string TipoPaquete { get; set; } = string.Empty;
        public DateTime FechaArmado { get; set; }

        public int FamiliaId { get; set; }
        public string NombreFamilia { get; set; } = string.Empty;
        public string DniRepresentante { get; set; } = string.Empty;
        public string DireccionFamilia { get; set; } = string.Empty;

        public string VoluntarioNombre { get; set; } = string.Empty;
        public string VoluntarioUserName { get; set; } = string.Empty;
        public DateTime FechaEntrega { get; set; }

        public List<TrazabilidadItemDto> Items { get; set; } = new();
    }

    public class TrazabilidadItemDto
    {
        public int LoteId { get; set; }
        public string NumeroLote { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public DateTime? FechaVencimiento { get; set; }
        public decimal Cantidad { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;

        public int DonacionId { get; set; }
        public int DonanteId { get; set; }
        public string NombreDonante { get; set; } = string.Empty;
        public string TipoDonante { get; set; } = string.Empty;
        public string DocumentoDonante { get; set; } = string.Empty;
        public DateTime FechaDonacion { get; set; }
    }
}