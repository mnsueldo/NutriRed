using Microsoft.AspNetCore.Mvc;

namespace NutriRed.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthApiController : ControllerBase
{
    private readonly ILogger<AuthApiController> _logger;

    public AuthApiController(ILogger<AuthApiController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Endpoint de autenticación para la app móvil Android.
    /// Valida credenciales de voluntarios y administradores.
    /// </summary>
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginApiRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { success = false, message = "Debe ingresar usuario/correo y contraseña." });
        }

        var email = request.Email.Trim().ToLower();
        var pwd = request.Password.Trim();

        // Usuarios y roles predefinidos del sistema NutriRed
        if ((email == "admin" || email == "admin@nutrired.org") && pwd == "123456")
        {
            return Ok(new
            {
                success = true,
                token = "nutrired_token_admin_2026",
                user = new
                {
                    id = "USR-001",
                    email = "admin@nutrired.org",
                    full_name = "Administrador General",
                    fullName = "Administrador General",
                    role = "ADMIN"
                }
            });
        }

        if ((email == "voluntario" || email == "voluntario@nutrired.org" || email.StartsWith("voluntario")) && pwd == "123456")
        {
            return Ok(new
            {
                success = true,
                token = "nutrired_token_voluntario_2026",
                user = new
                {
                    id = "USR-002",
                    email = "voluntario@nutrired.org",
                    full_name = "Voluntario Recepción y Despacho",
                    fullName = "Voluntario Recepción y Despacho",
                    role = "VOLUNTEER"
                }
            });
        }

        return Unauthorized(new { success = false, message = "Credenciales inválidas. Verifique usuario y contraseña." });
    }
}

public class LoginApiRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
