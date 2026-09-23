using NutriRed.Services.Common;
using NutriRed.Services.DTOs;

namespace NutriRed.Services.Interfaces;

public interface IDonacionService
{
    Task<OperationResult<DonacionComprobanteDto>> RegistrarDonacionAsync(RegistrarDonacionRequest request);
    Task<OperationResult<DonacionComprobanteDto>> ObtenerPorIdAsync(int id);
    Task<OperationResult<DonacionComprobanteDto>> ObtenerPorCodigoAsync(string codigoComprobante);
    Task<OperationResult<IEnumerable<DonacionComprobanteDto>>> ObtenerTodasAsync(DateTime? fechaDesde = null, DateTime? fechaHasta = null);
}
