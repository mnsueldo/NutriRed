using NutriRed.Domain.Entities;
using NutriRed.Services.Common;

namespace NutriRed.Services.Interfaces;

public interface ICategoriaService
{
    Task<OperationResult<IEnumerable<Categoria>>> ObtenerTodasAsync(bool soloActivas = true);
    Task<OperationResult<Categoria>> ObtenerPorIdAsync(int id);
    Task<OperationResult<Categoria>> CrearAsync(Categoria categoria);
    Task<OperationResult<Categoria>> ActualizarAsync(Categoria categoria);
    Task<OperationResult> CambiarEstadoAsync(int id, bool activo);
}
