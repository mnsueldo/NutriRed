using NutriRed.Services.Common;
using NutriRed.Services.DTOs;

namespace NutriRed.Services.Interfaces;

public interface IEntregaService
{
    Task<OperationResult<IEnumerable<PaquetePendienteDespachoDto>>> ObtenerPaquetesListosParaDespachoAsync();
    Task<OperationResult<PaquetePendienteDespachoDto>> ConsultarPaquetePorQrAsync(string codigoSeguimiento);
    Task<OperationResult<ComprobanteEntregaDto>> ConfirmarEntregaAsync(RegistrarEntregaRequest request);
    Task<OperationResult> RegistrarEntregaNoConcretadaAsync(RegistrarEntregaFallidaRequest request);
    Task<OperationResult<ComprobanteEntregaDto>> ObtenerComprobanteEntregaAsync(int entregaId);
}
