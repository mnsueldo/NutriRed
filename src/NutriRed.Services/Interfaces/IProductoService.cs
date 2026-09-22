using NutriRed.Domain.Entities;
using NutriRed.Services.Common;

namespace NutriRed.Services.Interfaces;

public interface IProductoService
{
    Task<OperationResult<IEnumerable<Producto>>> ObtenerTodosAsync(bool soloActivos = true);
    Task<OperationResult<Producto>> ObtenerPorIdAsync(int id);
    Task<OperationResult<Producto>> BuscarPorEanAsync(string ean);
    Task<OperationResult<IEnumerable<Producto>>> ObtenerPorCategoriaAsync(int categoriaId);
    Task<OperationResult<Producto>> CrearAsync(Producto producto);
    Task<OperationResult<Producto>> ActualizarAsync(Producto producto);
    Task<OperationResult> DesactivarAsync(int id);
    Task<OperationResult> ActivarAsync(int id);
}
