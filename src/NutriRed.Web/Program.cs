using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NutriRed.Data;
using NutriRed.Domain.Entities;
using NutriRed.Services;

// Compatibilidad de fechas para PostgreSQL (Npgsql) en Supabase
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de Base de Datos Híbrida (SQL Server / Supabase PostgreSQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

var isPostgreSql = connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
                   connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                   connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
                   connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase) ||
                   connectionString.Contains("User Id=", StringComparison.OrdinalIgnoreCase) ||
                   connectionString.Contains("Port=", StringComparison.OrdinalIgnoreCase);

if (isPostgreSql)
{
    builder.Services.AddDbContext<NutriRedDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<NutriRedDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// 2. Registro de Servicios de Negocio (NutriRed.Services)
builder.Services.AddNutriRedServices();

// 2.1 Configuración de ASP.NET Core Identity con Roles
builder.Services.AddIdentity<NutriRed.Domain.Entities.ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
{
    // Opciones de contraseña flexibles para facilitar desarrollo y pruebas móviles
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<NutriRedDbContext>()
.AddDefaultTokenProviders();

// Rutas y configuración de Cookies de autenticación para la Web MVC
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = "NutriRed.Auth";
});

// 3. Soporte para Vistas MVC y Controladores de API
builder.Services.AddControllersWithViews();

// 4. Documentación Swagger para la API de Android
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 5. Configuración de CORS para clientes móviles y desarrollo
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Sembrado de Datos Iniciales (Seed Data en desarrollo / inicio)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    // 1. Usuarios y Roles de ASP.NET Core Identity (Indispensable para el inicio de sesión)
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await DbInitializer.SeedIdentityAsync(userManager, roleManager);
        logger.LogInformation("Usuarios de Identity verificados y sembrados exitosamente.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ocurrió un error al sembrar los usuarios de Identity.");
    }

    // 2. Catálogo de Dominio y Datos Operativos
    try
    {
        var context = services.GetRequiredService<NutriRedDbContext>();
        await DbInitializer.SeedAsync(context);
        logger.LogInformation("Datos del dominio y catálogo sembrados exitosamente.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ocurrió un error al sembrar los datos iniciales de NutriRed.");
    }
}

// Configuración del pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseRouting();

// CORS debe ejecutarse después de UseRouting y antes de UseAuthorization
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Rutas para API REST (/api/...)
app.MapControllers();

// Rutas para MVC Razor (/controller/action/id)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
