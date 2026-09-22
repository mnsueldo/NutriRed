using System.ComponentModel.DataAnnotations;
using NutriRed.Domain.Enums;

namespace NutriRed.Domain.Entities;

public class Categoria
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    [Display(Name = "Nombre de la Categoría")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "La descripción no puede superar los 250 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    // Navegación
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}

public class Producto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El código de barras comercial (EAN) es obligatorio.")]
    [StringLength(50, ErrorMessage = "El código de barras no puede superar los 50 caracteres.")]
    [Display(Name = "Código de Barras (EAN)")]
    public string CodigoBarras { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    [Display(Name = "Nombre del Alimento")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "La descripción no puede superar los 300 caracteres.")]
    [Display(Name = "Descripción / Presentación")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Debe asociar una categoría.")]
    [Display(Name = "Categoría")]
    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
    [Display(Name = "Unidad de Medida")]
    public UnidadMedida UnidadMedida { get; set; }

    [Range(0, 100000, ErrorMessage = "El stock mínimo debe ser un número positivo.")]
    [Display(Name = "Stock Mínimo de Alerta")]
    public decimal StockMinimo { get; set; } = 10;

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    // Navegación a lotes físicos
    public ICollection<Lote> Lotes { get; set; } = new List<Lote>();
}

public class Lote
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El código o número de lote es obligatorio.")]
    [StringLength(50, ErrorMessage = "El número de lote no puede superar los 50 caracteres.")]
    [Display(Name = "Número de Lote")]
    public string NumeroLote { get; set; } = string.Empty;

    [Required(ErrorMessage = "El producto es obligatorio.")]
    [Display(Name = "Producto")]
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    [Required(ErrorMessage = "La fecha de vencimiento es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Vencimiento")]
    public DateTime FechaVencimiento { get; set; }

    [Range(0.01, 1000000, ErrorMessage = "La cantidad inicial debe ser mayor a cero.")]
    [Display(Name = "Cantidad Inicial")]
    public decimal CantidadInicial { get; set; }

    [Range(0, 1000000, ErrorMessage = "La cantidad disponible no puede ser negativa.")]
    [Display(Name = "Cantidad Disponible")]
    public decimal CantidadDisponible { get; set; }

    [Required]
    [Display(Name = "Estado")]
    public EstadoLote Estado { get; set; } = EstadoLote.Disponible;

    [Display(Name = "Fecha de Ingreso")]
    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;

    // Navegación
    public ICollection<MovimientoStock> Movimientos { get; set; } = new List<MovimientoStock>();
}
