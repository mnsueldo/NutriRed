using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace NutriRed.Domain.Entities;

/// <summary>
/// Representa a un usuario del sistema NutriRed (administrador, coordinador o voluntario).
/// Extiende IdentityUser para incorporar atributos específicos del dominio.
/// </summary>
public class ApplicationUser : IdentityUser
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string NombreCompleto { get; set; } = string.Empty;

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public bool Activo { get; set; } = true;
}
