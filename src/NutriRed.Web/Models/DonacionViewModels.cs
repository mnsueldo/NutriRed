using System.ComponentModel.DataAnnotations;
using NutriRed.Domain.Enums;
using NutriRed.Services.DTOs;

namespace NutriRed.Web.Models;

public class RegistrarDonacionViewModel
{
    [Required(ErrorMessage = "El tipo de donante es obligatorio.")]
    [Display(Name = "Tipo de Donante")]
    public TipoDonante TipoDonante { get; set; } = TipoDonante.Individuo;

    [Display(Name = "DNI o CUIT")]
    [StringLength(20, ErrorMessage = "El documento no puede superar los 20 caracteres.")]
    public string? NumeroDocumento { get; set; }

    [Display(Name = "Nombre / Razón Social")]
    [StringLength(150, ErrorMessage = "El nombre o razón social no puede superar los 150 caracteres.")]
    public string? NombreRazonSocial { get; set; }

    [Phone(ErrorMessage = "El formato del teléfono no es válido.")]
    [Display(Name = "Teléfono")]
    [StringLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [Display(Name = "Correo Electrónico")]
    [StringLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "El voluntario u operador receptor es obligatorio.")]
    [Display(Name = "Operador / Voluntario Receptor")]
    [StringLength(100)]
    public string VoluntarioReceptorId { get; set; } = "voluntario_deposito";

    [Display(Name = "Observaciones Generales")]
    [StringLength(500, ErrorMessage = "Las observaciones no pueden superar los 500 caracteres.")]
    public string? Observaciones { get; set; }

    public List<DonacionItemViewModel> Items { get; set; } = new();
}

public class DonacionItemViewModel
{
    [Required(ErrorMessage = "El código de barras es requerido.")]
    [Display(Name = "Código EAN")]
    public string CodigoBarras { get; set; } = string.Empty;

    [Display(Name = "Alimento")]
    public string? NombreProducto { get; set; }

    [Display(Name = "Categoría")]
    public string? Categoria { get; set; }

    [Display(Name = "Unidad")]
    public string? UnidadMedida { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, 100000, ErrorMessage = "La cantidad debe ser un número entero mayor a cero.")]
    [Display(Name = "Cantidad")]
    public int Cantidad { get; set; }

    [Required(ErrorMessage = "La fecha de vencimiento es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Vencimiento")]
    public DateTime FechaVencimiento { get; set; } = DateTime.Today.AddMonths(1);

    [Display(Name = "Lote de Origen (Opcional)")]
    [StringLength(50, ErrorMessage = "El número de lote no puede superar 50 caracteres.")]
    public string? NumeroLote { get; set; }

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
        < 0 => $"Vencido ({Math.Abs(DiasParaVencer)} d)",
        0 => "Vence hoy",
        1 => "Vence mañana",
        _ => $"{DiasParaVencer} días"
    };
}

public class DonacionIndexViewModel
{
    public IEnumerable<DonacionComprobanteDto> Donaciones { get; set; } = Enumerable.Empty<DonacionComprobanteDto>();

    [DataType(DataType.Date)]
    [Display(Name = "Desde")]
    public DateTime? FechaDesde { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Hasta")]
    public DateTime? FechaHasta { get; set; }

    [Display(Name = "Código")]
    public string? CodigoComprobante { get; set; }

    public int TotalRegistros => Donaciones.Count();
    public decimal TotalUnidadesKilos => Donaciones.Sum(d => d.TotalUnidadesRecibidas);
    public int DonacionesInstitucionales => Donaciones.Count(d => d.TipoDonante == TipoDonante.Institucion);
}
