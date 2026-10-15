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
    public int Cancelados => Paquetes.Count(p => p.Estado == EstadoPaquete.Cancelado);

    // Contadores globales para métricas superiores fijas
    public int TotalGlobal { get; set; }
    public int PreparadosGlobal { get; set; }
    public int EntregadosGlobal { get; set; }
    public int CanceladosGlobal { get; set; }
}

public class ItemArmadoViewModel
{
    [Required]
    public int ProductoId { get; set; }

    public string? NombreProducto { get; set; }
    public string? CodigoBarras { get; set; }
    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }

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

public class PlantillasIndexViewModel
{
    public IEnumerable<TipoPaquete> TiposPaquete { get; set; } = Enumerable.Empty<TipoPaquete>();
}

public class ItemPlantillaViewModel
{
    public int? Id { get; set; }

    [Required]
    public int ProductoId { get; set; }
    public string? NombreProducto { get; set; }
    public string? CodigoBarras { get; set; }
    public string? CategoriaNombre { get; set; }

    [Required]
    [Range(0.01, 100, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public decimal CantidadRequerida { get; set; }
}

public class EditarPlantillaViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del kit es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    [Display(Name = "Nombre del Kit")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "La descripción no puede superar 250 caracteres.")]
    [Display(Name = "Descripción del Kit")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "El mínimo de integrantes es obligatorio.")]
    [Range(1, 30, ErrorMessage = "El mínimo debe ser entre 1 y 30.")]
    [Display(Name = "Mínimo de Integrantes")]
    public int MinIntegrantes { get; set; } = 1;

    [Required(ErrorMessage = "El máximo de integrantes es obligatorio.")]
    [Range(1, 50, ErrorMessage = "El máximo debe ser entre 1 y 50.")]
    [Display(Name = "Máximo de Integrantes")]
    public int MaxIntegrantes { get; set; } = 3;

    [Display(Name = "Kit Activo")]
    public bool Activo { get; set; } = true;

    public List<ItemPlantillaViewModel> Items { get; set; } = new();
}

public class CrearPlantillaViewModel
{
    [Required(ErrorMessage = "El nombre del kit es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    [Display(Name = "Nombre del Kit")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "La descripción no puede superar 250 caracteres.")]
    [Display(Name = "Descripción del Kit")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "El mínimo de integrantes es obligatorio.")]
    [Range(1, 30, ErrorMessage = "El mínimo debe ser entre 1 y 30.")]
    [Display(Name = "Mínimo de Integrantes")]
    public int MinIntegrantes { get; set; } = 1;

    [Required(ErrorMessage = "El máximo de integrantes es obligatorio.")]
    [Range(1, 50, ErrorMessage = "El máximo debe ser entre 1 y 50.")]
    [Display(Name = "Máximo de Integrantes")]
    public int MaxIntegrantes { get; set; } = 3;

    [Display(Name = "Kit Activo")]
    public bool Activo { get; set; } = true;
}
