using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.Common;
using NutriRed.Services.DTOs;

namespace NutriRed.Services.Interfaces;

public interface IPaqueteService
{
    Task<OperationResult<PropuestaPaqueteDto>> SugerirPaqueteParaFamiliaAsync(int familiaId, int? tipoPaqueteIdPersonalizado = null);
    Task<OperationResult<PaqueteDto>> ConfirmarArmadoPaqueteAsync(ConfirmarArmadoPaqueteRequest request);
    Task<OperationResult<PaqueteDto>> ObtenerPorIdAsync(int id);
    Task<OperationResult<PaqueteDto>> ObtenerPorCodigoSeguimientoAsync(string codigoSeguimiento);
    Task<OperationResult<IEnumerable<PaqueteDto>>> ObtenerTodosAsync(EstadoPaquete? estado = null);
    Task<OperationResult<IEnumerable<TipoPaquete>>> ObtenerTiposPaqueteAsync();
}
