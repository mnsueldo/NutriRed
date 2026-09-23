using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.Common;

namespace NutriRed.Services.Interfaces;

public interface IFamiliaService
{
    Task<OperationResult<IEnumerable<FamiliaBeneficiaria>>> ObtenerTodasAsync(bool soloActivas = true);
    Task<OperationResult<FamiliaBeneficiaria>> ObtenerPorIdAsync(int id, bool incluirHistorial = false);
    Task<OperationResult<FamiliaBeneficiaria>> BuscarPorDniAsync(string dni);
    Task<OperationResult<FamiliaBeneficiaria>> CrearAsync(FamiliaBeneficiaria familia);
    Task<OperationResult<FamiliaBeneficiaria>> ActualizarAsync(FamiliaBeneficiaria familia);
    Task<OperationResult> CambiarEstadoAsync(int id, EstadoFamilia nuevoEstado);
}
