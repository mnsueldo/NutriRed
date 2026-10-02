using System.ComponentModel.DataAnnotations;
using NutriRed.Domain.Enums;

namespace NutriRed.Web.Models;

public class RegistrarMermaViewModel
{
    [Required(ErrorMessage = "Debe seleccionar un alimento/producto.")]
    [Display(Name = "Producto / Alimento")]
    public int? ProductoId { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un lote afectado.")]
    [Display(Name = "Lote Afectado")]
    public int LoteId { get; set; }

    [Required(ErrorMessage = "Debe ingresar la cantidad a dar de baja.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad a dar de baja debe ser un número entero mayor a cero.")]
    public int Cantidad { get; set; }

    [Required(ErrorMessage = "Debe seleccionar el motivo de la baja.")]
    [Display(Name = "Motivo de la Merma")]
    public MotivoMovimiento Motivo { get; set; }

    [Display(Name = "Observaciones / Inconsistencias")]
    public string? Observaciones { get; set; }

    // Propiedades de ayuda para visualización
    public string? CodigoLote { get; set; }
    public string? NombreProducto { get; set; }
    public decimal? ExistenciaActual { get; set; }
}