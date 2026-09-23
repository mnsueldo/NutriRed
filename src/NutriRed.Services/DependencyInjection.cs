using Microsoft.Extensions.DependencyInjection;
using NutriRed.Services.Implementations;
using NutriRed.Services.Interfaces;

namespace NutriRed.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddNutriRedServices(this IServiceCollection services)
    {
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IInventarioService, InventarioService>();
        return services;
    }
}
