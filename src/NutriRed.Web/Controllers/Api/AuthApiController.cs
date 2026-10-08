using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NutriRed.Domain.Entities;

namespace NutriRed.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthApiController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AuthApiController> _logger;

    public AuthApiController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AuthApiController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint de autenticación para la app móvil Android.
    /// Valida credenciales contra ASP.NET Core Identity y emite token de sesión.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginApiRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { success = false, message = "Debe ingresar usuario/correo y contraseña." });
        }

        var input = request.Email.Trim().ToLowerInvariant();
        var pwd = request.Password.Trim();

        // Resolución flexible: si ingresa "admin" o "voluntario" sin dominio, resolvemos a su correo oficial
        var emailCandidate = input.Contains('@') ? input : $"{input}@nutrired.org";

        var user = await _userManager.FindByEmailAsync(emailCandidate) ??
                   await _userManager.FindByNameAsync(input);

        if (user == null || !user.Activo)
        {
            _logger.LogWarning("Intento de login móvil fallido: usuario no encontrado para '{Input}'", input);
            return Unauthorized(new { success = false, message = "Credenciales inválidas. Verifique usuario y contraseña." });
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, pwd);
        if (!passwordValid)
        {
            _logger.LogWarning("Intento de login móvil con contraseña incorrecta para '{Email}'", user.Email);
            return Unauthorized(new { success = false, message = "Credenciales inválidas. Verifique usuario y contraseña." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "Voluntario";
        var roleCode = primaryRole.Equals("Administrador", StringComparison.OrdinalIgnoreCase) ? "ADMIN" : "VOLUNTEER";

        // Token de sesión autenticado
        var token = $"nutrired_token_{user.Id}_{DateTime.UtcNow.Ticks}";

        _logger.LogInformation("Login móvil exitoso para usuario '{Email}' con rol '{Rol}'", user.Email, primaryRole);

        return Ok(new
        {
            success = true,
            token = token,
            user = new
            {
                id = user.Id,
                email = user.Email,
                full_name = user.NombreCompleto,
                fullName = user.NombreCompleto,
                role = roleCode
            }
        });
    }
}

public class LoginApiRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
