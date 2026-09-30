using System.ComponentModel.DataAnnotations;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.DTOs;

namespace NutriRed.Web.Models;

public class PaqueteIndexViewModel
{
    public IEnumerable<PaqueteDto> Paquetes { get; set; } = Enumerable.Empty<PaqueteDto>();

    [Display(Name = "Estado")]
    public EstadoPaquete? EstadoFiltro { get; set; }

    [Display(Name = "Código QR / Seguimiento")]
    public string? CodigoSeguimiento { get; set; }

    public int TotalPaquetes => Paquetes.Count();
    public int Pendientes => Paquetes.Count(p => p.Estado == EstadoPaquete.Pendiente);
    public int Preparados => Paquetes.Count(p => p.Estado == EstadoPaquete.Preparado);
    public int Entregados => Paquetes.Count(p => p.Estado == EstadoPaquete.Entregado);
}

public class ItemArmadoViewModel
{
    [Required]
    public int ProductoId { get; set; }

    public string? NombreProducto { get; set; }
    public string? CodigoBarras { get; set; }

    [Range(0.01, 100, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public decimal Cantidad { get; set; }

    public bool EsSustituto { get; set; } = false;
    public int? ProductoSustituidoId { get; set; }
    public string? NombreProductoSustituido { get; set; }
}

public class ArmarPaqueteViewModel
{
    [Required(ErrorMessage = "Debe seleccionar una familia beneficiaria.")]
    [Display(Name = "Familia Beneficiaria")]
    public int? FamiliaId { get; set; }

    [Display(Name = "Plantilla de Kit Nutricional")]
    public int? TipoPaqueteId { get; set; }

    [Required(ErrorMessage = "El usuario armador es obligatorio.")]
    [Display(Name = "Operador / Armador")]
    public string UsuarioArmadorId { get; set; } = "operador_armado";

    [Display(Name = "Observaciones de Armado")]
    [StringLength(300, ErrorMessage = "No puede superar 300 caracteres.")]
    public string? Observaciones { get; set; }

    // Propuesta calculada por el motor FEFO
    public PropuestaPaqueteDto? Propuesta { get; set; }

    // Lista de ítems finales a confirmar
    public List<ItemArmadoViewModel> Items { get; set; } = new();
}
