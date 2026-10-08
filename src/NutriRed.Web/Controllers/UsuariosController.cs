using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NutriRed.Domain.Entities;
using NutriRed.Web.Models;

namespace NutriRed.Web.Controllers;

/// <summary>
/// Módulo de Gestión de Usuarios y Seguridad.
/// Exclusivo para administradores del banco de alimentos.
/// </summary>
[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UsuariosController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// Padrón general de usuarios del sistema con sus roles y estados.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.FechaRegistro)
            .ToListAsync();

        var listaViewModels = new List<UsuarioItemViewModel>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            listaViewModels.Add(new UsuarioItemViewModel
            {
                Id = u.Id,
                Email = u.Email ?? u.UserName ?? string.Empty,
                NombreCompleto = u.NombreCompleto,
                Rol = roles.FirstOrDefault() ?? "Sin Rol",
                FechaRegistro = u.FechaRegistro,
                Activo = u.Activo
            });
        }

        return View(listaViewModels);
    }

    /// <summary>
    /// Formulario para dar de alta a un nuevo voluntario o administrador.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        var model = new CrearUsuarioViewModel
        {
            RolSeleccionado = "Voluntario",
            RolesDisponibles = await ObtenerRolesSelectListAsync()
        };

        return View(model);
    }

    /// <summary>
    /// Procesa la creación de un nuevo usuario en ASP.NET Core Identity.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearUsuarioViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.RolesDisponibles = await ObtenerRolesSelectListAsync();
            return View(model);
        }

        var emailLimpio = model.Email.Trim().ToLowerInvariant();

        // Validar que el correo no esté registrado
        var usuarioExistente = await _userManager.FindByEmailAsync(emailLimpio);
        if (usuarioExistente != null)
        {
            ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario registrado con este correo electrónico.");
            model.RolesDisponibles = await ObtenerRolesSelectListAsync();
            return View(model);
        }

        var nuevoUsuario = new ApplicationUser
        {
            UserName = emailLimpio,
            Email = emailLimpio,
            EmailConfirmed = true,
            NombreCompleto = model.NombreCompleto.Trim(),
            FechaRegistro = DateTime.UtcNow,
            Activo = true
        };

        var resultadoCreacion = await _userManager.CreateAsync(nuevoUsuario, model.Password.Trim());
        if (!resultadoCreacion.Succeeded)
        {
            foreach (var error in resultadoCreacion.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.RolesDisponibles = await ObtenerRolesSelectListAsync();
            return View(model);
        }

        // Asignar el rol seleccionado
        var rolAsignar = model.RolSeleccionado.Trim();
        if (await _roleManager.RoleExistsAsync(rolAsignar))
        {
            await _userManager.AddToRoleAsync(nuevoUsuario, rolAsignar);
        }

        _logger.LogInformation("Administrador {Admin} creó al usuario {Email} con rol {Rol}.",
            User.Identity?.Name, nuevoUsuario.Email, rolAsignar);

        TempData["Exito"] = $"Usuario '{nuevoUsuario.NombreCompleto}' creado exitosamente con rol {rolAsignar}.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Activa o desactiva la cuenta de un usuario (bloqueo lógico sin borrar auditoría).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(string id, bool nuevoEstado)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            TempData["Error"] = "Identificador de usuario no válido.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario == null)
        {
            TempData["Error"] = "Usuario no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        // Regla de seguridad esencial: no permitir que el administrador se desactive a sí mismo
        var currentUserId = _userManager.GetUserId(User);
        if (usuario.Id == currentUserId)
        {
            TempData["Error"] = "No puede desactivar su propia cuenta activa de administrador.";
            return RedirectToAction(nameof(Index));
        }

        usuario.Activo = nuevoEstado;
        var result = await _userManager.UpdateAsync(usuario);

        if (!result.Success())
        {
            TempData["Error"] = "No se pudo actualizar el estado del usuario.";
        }
        else
        {
            TempData["Exito"] = nuevoEstado
                ? $"La cuenta de {usuario.NombreCompleto} ha sido reactivada."
                : $"La cuenta de {usuario.NombreCompleto} ha sido desactivada temporalmente.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IEnumerable<SelectListItem>> ObtenerRolesSelectListAsync()
    {
        var roles = await _roleManager.Roles
            .OrderBy(r => r.Name)
            .Select(r => r.Name!)
            .ToListAsync();

        return roles.Select(r => new SelectListItem
        {
            Value = r,
            Text = r
        });
    }
}

internal static class IdentityResultExtensions
{
    public static bool Success(this IdentityResult result) => result.Succeeded;
}
