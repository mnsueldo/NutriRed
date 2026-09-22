using System.ComponentModel.DataAnnotations;
using NutriRed.Domain.Enums;

namespace NutriRed.Domain.Entities;

public class Donante
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El tipo de donante es obligatorio.")]
    [Display(Name = "Tipo de Donante")]
    public TipoDonante Tipo { get; set; } = TipoDonante.Individuo;

    [StringLength(20, ErrorMessage = "El documento no puede superar 20 caracteres.")]
    [Display(Name = "DNI o CUIT")]
    public string? NumeroDocumento { get; set; }

    [Required(ErrorMessage = "El nombre o razón social es obligatorio.")]
    [StringLength(150, ErrorMessage = "No puede superar los 150 caracteres.")]
    [Display(Name = "Nombre o Razón Social")]
    public string NombreRazonSocial { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
    [Display(Name = "Teléfono de Contacto")]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido.")]
    [StringLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
    [Display(Name = "Correo Electrónico")]
    public string? Email { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    // Navegación
    public ICollection<Donacion> Donaciones { get; set; } = new List<Donacion>();
}

public class Donacion
{
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "Código de Donación")]
    public string CodigoComprobante { get; set; } = string.Empty; // Ej: DON-2026-00045

    [Required]
    [Display(Name = "Fecha y Hora")]
    public DateTime FechaHora { get; set; } = DateTime.UtcNow;

    [Display(Name = "Donante")]
    public int? DonanteId { get; set; }
    public Donante? Donante { get; set; }

    [Required(ErrorMessage = "El voluntario receptor es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Voluntario Receptor")]
    public string VoluntarioReceptorId { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }

    // Navegación
    public ICollection<DonacionDetalle> Detalles { get; set; } = new List<DonacionDetalle>();
}

public class DonacionDetalle
{
    public int Id { get; set; }

    [Required]
    public int DonacionId { get; set; }
    public Donacion? Donacion { get; set; }

    [Required]
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    [Required]
    public int LoteId { get; set; }
    public Lote? Lote { get; set; }

    [Range(0.01, 100000, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    [Display(Name = "Cantidad Recibida")]
    public decimal Cantidad { get; set; }
}

public class MovimientoStock
{
    public int Id { get; set; }

    [Required]
    public int LoteId { get; set; }
    public Lote? Lote { get; set; }

    [Required]
    [Display(Name = "Tipo de Movimiento")]
    public TipoMovimiento TipoMovimiento { get; set; }

    [Required]
    [Display(Name = "Motivo")]
    public MotivoMovimiento Motivo { get; set; }

    [Range(0.01, 100000, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    [Display(Name = "Cantidad")]
    public decimal Cantidad { get; set; }

    [Display(Name = "Fecha")]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(100)]
    [Display(Name = "Usuario Responsable")]
    public string UsuarioId { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }
}
