using Microsoft.EntityFrameworkCore;
using NutriRed.Data;
using NutriRed.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de Base de Datos con SQL Server
builder.Services.AddDbContext<NutriRedDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Registro de Servicios de Negocio (NutriRed.Services)
builder.Services.AddNutriRedServices();

// 3. Soporte para Vistas MVC y Controladores de API
builder.Services.AddControllersWithViews();

// 4. Documentación Swagger para la API de Android
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Sembrado de Datos Iniciales (Seed Data en desarrollo / inicio)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<NutriRedDbContext>();
        await DbInitializer.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
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

app.UseHttpsRedirection();
app.UseRouting();

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
