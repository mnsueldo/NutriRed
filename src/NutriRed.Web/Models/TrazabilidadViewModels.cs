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
        public string? VoluntarioId { get; set; } // Nuevo filtro de voluntario

        // Listas para los desplegables (Selects)
        public List<SelectListItem> Donantes { get; set; } = new();
        public List<SelectListItem> TiposPaquete { get; set; } = new();
        public List<SelectListItem> Voluntarios { get; set; } = new(); // Lista desplegable

        public List<TrazabilidadEntregaGroupDto> EntregasGrouped { get; set; } = new();

        // KPIs para Trazabilidad
        public int TotalEntregas => EntregasGrouped.Count;

        // Suma total de unidades/kilos/litros de todos los ítems entregados
        public decimal TotalAlimentosEntregados => EntregasGrouped
            .SelectMany(g => g.Items)
            .Sum(i => i.Cantidad);

        // Cantidad total de ítems/productos físicos en los paquetes
        public int TotalItemsEntregados => EntregasGrouped
            .SelectMany(g => g.Items)
            .Count();

        public int TotalFamiliasImpactadas => EntregasGrouped
            .Select(g => g.FamiliaId)
            .Distinct()
            .Count();

        public int TotalDonantesInvolucrados => EntregasGrouped
            .SelectMany(g => g.Items)
            .Select(i => i.DonanteId)
            .Where(id => id > 0)
            .Distinct()
            .Count();
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
        public string CodigoEan { get; set; } = string.Empty;
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