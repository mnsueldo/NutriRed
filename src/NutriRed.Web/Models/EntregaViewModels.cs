using System.ComponentModel.DataAnnotations;
using NutriRed.Domain.Enums;
using NutriRed.Services.DTOs;

namespace NutriRed.Web.Models;

public class EntregaIndexViewModel
{
    public IEnumerable<PaquetePendienteDespachoDto> PaquetesListos { get; set; } = Enumerable.Empty<PaquetePendienteDespachoDto>();
    public IEnumerable<PaqueteDespachoHistorialDto> HistorialPaquetes { get; set; } = Enumerable.Empty<PaqueteDespachoHistorialDto>();

    [Display(Name = "Código QR de Caja")]
    public string? CodigoQrBusqueda { get; set; }

    public string TabActiva { get; set; } = "pendientes"; // "pendientes" o "todos"
    public string? FiltroEstado { get; set; }

    public int TotalListos => PaquetesListos.Count();
    public int TotalEntregados { get; set; }
    public int TotalCancelados { get; set; }
    public int TotalTodos { get; set; }
}

public class DespacharPaqueteViewModel
{
    [Required(ErrorMessage = "El código de seguimiento es obligatorio.")]
    public string CodigoSeguimiento { get; set; } = string.Empty;

    public PaquetePendienteDespachoDto? Paquete { get; set; }

    [Required(ErrorMessage = "Debe indicar quién recibe el paquete.")]
    [Display(Name = "Tipo de Receptor")]
    public TipoReceptor TipoReceptor { get; set; } = TipoReceptor.Titular;

    private string _dniReceptor = string.Empty;

    [Required(ErrorMessage = "El DNI del receptor es obligatorio.")]
    [RegularExpression(@"^\d{7,8}$", ErrorMessage = "El DNI debe contener entre 7 y 8 dígitos numéricos.")]
    [Display(Name = "DNI del Receptor")]
    public string DniReceptor
    {
        get => _dniReceptor;
        set => _dniReceptor = string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : System.Text.RegularExpressions.Regex.Replace(value, @"\D", "");
    }

    [Required(ErrorMessage = "El nombre del receptor es obligatorio.")]
    [StringLength(100, ErrorMessage = "No puede superar los 100 caracteres.")]
    [Display(Name = "Nombre y Apellido del Receptor")]
    public string NombreReceptor { get; set; } = string.Empty;

    [Display(Name = "Parentesco / Vínculo con el Titular")]
    [StringLength(100, ErrorMessage = "No puede superar los 100 caracteres.")]
    public string? VinculoConTitular { get; set; }

    [Required(ErrorMessage = "La firma digital manuscrita es obligatoria para confirmar el despacho.")]
    [Display(Name = "Firma Digital")]
    public string FirmaDigital { get; set; } = string.Empty;

    [Required(ErrorMessage = "El voluntario u operador de despacho es obligatorio.")]
    [Display(Name = "Operador de Despacho")]
    [StringLength(100)]
    public string VoluntarioDespachoId { get; set; } = "operador_despacho";
}

public class RegistrarEntregaFallidaViewModel
{
    [Required]
    public string CodigoSeguimiento { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe especificar el motivo por el cual no se concretó la entrega.")]
    [StringLength(300, ErrorMessage = "El motivo no puede superar 300 caracteres.")]
    [Display(Name = "Motivo de Entrega no Concretada")]
    public string MotivoNoEntrega { get; set; } = string.Empty;

    [Required]
    public string VoluntarioDespachoId { get; set; } = "operador_despacho";
}
