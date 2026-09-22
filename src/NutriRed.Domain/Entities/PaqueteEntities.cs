using System.ComponentModel.DataAnnotations;
using NutriRed.Domain.Enums;

namespace NutriRed.Domain.Entities;

public class FamiliaBeneficiaria
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El DNI del titular es obligatorio.")]
    [StringLength(20, ErrorMessage = "El documento no puede superar 20 caracteres.")]
    [Display(Name = "DNI del Titular")]
    public string DniTitular { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del titular es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    [Display(Name = "Nombre")]
    public string NombreTitular { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido del titular es obligatorio.")]
    [StringLength(100, ErrorMessage = "El apellido no puede superar 100 caracteres.")]
    [Display(Name = "Apellido")]
    public string ApellidoTitular { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "El teléfono no puede superar 30 caracteres.")]
    [Display(Name = "Teléfono de Contacto")]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(250, ErrorMessage = "La dirección no puede superar 250 caracteres.")]
    [Display(Name = "Dirección Física")]
    public string Direccion { get; set; } = string.Empty;

    [Range(1, 30, ErrorMessage = "La cantidad de integrantes debe ser al menos 1.")]
    [Display(Name = "Cantidad de Integrantes")]
    public int CantidadIntegrantes { get; set; } = 1;

    [Required]
    [Display(Name = "Estado")]
    public EstadoFamilia Estado { get; set; } = EstadoFamilia.Activo;

    [Display(Name = "Fecha de Alta")]
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;

    // Navegación
    public ICollection<Paquete> HistorialPaquetes { get; set; } = new List<Paquete>();
}

public class TipoPaquete
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del tipo de paquete es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Nombre del Paquete (Kit)")]
    public string Nombre { get; set; } = string.Empty; // Ej: "Kit Básico Familiar 1-3"

    [StringLength(250)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Range(1, 30)]
    [Display(Name = "Mínimo Integrantes")]
    public int MinIntegrantes { get; set; } = 1;

    [Range(1, 30)]
    [Display(Name = "Máximo Integrantes")]
    public int MaxIntegrantes { get; set; } = 3;

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    // Productos estándar que componen esta plantilla de paquete
    public ICollection<PlantillaPaqueteDetalle> ItemsPlantilla { get; set; } = new List<PlantillaPaqueteDetalle>();
}

public class PlantillaPaqueteDetalle
{
    public int Id { get; set; }

    [Required]
    public int TipoPaqueteId { get; set; }
    public TipoPaquete? TipoPaquete { get; set; }

    [Required]
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    [Range(0.01, 100, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    [Display(Name = "Cantidad Requerida")]
    public decimal CantidadRequerida { get; set; }
}

public class Paquete
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Código QR de Rotulación")]
    public string CodigoSeguimiento { get; set; } = string.Empty; // Ej: PKG-2026-00012

    [Required]
    [Display(Name = "Familia Beneficiaria")]
    public int FamiliaBeneficiariaId { get; set; }
    public FamiliaBeneficiaria? FamiliaBeneficiaria { get; set; }

    [Required]
    [Display(Name = "Tipo de Paquete")]
    public int TipoPaqueteId { get; set; }
    public TipoPaquete? TipoPaquete { get; set; }

    [Display(Name = "Fecha de Creación / Armado")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [Required]
    [Display(Name = "Estado")]
    public EstadoPaquete Estado { get; set; } = EstadoPaquete.Pendiente;

    [Required]
    [StringLength(100)]
    [Display(Name = "Usuario Armador")]
    public string UsuarioArmadorId { get; set; } = string.Empty;

    // Navegación
    public ICollection<PaqueteDetalle> Detalles { get; set; } = new List<PaqueteDetalle>();
    public Entrega? Entrega { get; set; }
}

public class PaqueteDetalle
{
    public int Id { get; set; }

    [Required]
    public int PaqueteId { get; set; }
    public Paquete? Paquete { get; set; }

    [Required]
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    [Required]
    public int LoteId { get; set; } // Lote asignado mediante algoritmo FEFO
    public Lote? Lote { get; set; }

    [Range(0.01, 100)]
    [Display(Name = "Cantidad Extraída")]
    public decimal Cantidad { get; set; }

    [Display(Name = "Es Producto Sustituto")]
    public bool EsSustituto { get; set; } = false;

    public int? ProductoSustituidoId { get; set; }
    public Producto? ProductoSustituido { get; set; }
}

public class Entrega
{
    public int Id { get; set; }

    [Required]
    public int PaqueteId { get; set; }
    public Paquete? Paquete { get; set; }

    [Required]
    [Display(Name = "Fecha y Hora de Entrega")]
    public DateTime FechaHoraEntrega { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(100)]
    [Display(Name = "Voluntario Despachador")]
    public string VoluntarioDespachoId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Tipo de Receptor")]
    public TipoReceptor TipoReceptor { get; set; } = TipoReceptor.Titular;

    [Required]
    [StringLength(20)]
    [Display(Name = "DNI de quien retira")]
    public string DniReceptor { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    [Display(Name = "Nombre y Apellido de quien retira")]
    public string NombreReceptor { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Vínculo con el Titular")]
    public string? VinculoConTitular { get; set; } // Requerido si es TerceroAutorizado

    [Required(ErrorMessage = "La firma digital de conformidad es obligatoria.")]
    [Display(Name = "Firma Digital")]
    public string FirmaDigital { get; set; } = string.Empty; // Trazo / Imagen en Base64 o URL

    [Display(Name = "Entrega Concretada")]
    public bool Concretada { get; set; } = true;

    [StringLength(250)]
    [Display(Name = "Motivo (si no se concretó)")]
    public string? MotivoNoEntrega { get; set; }
}
