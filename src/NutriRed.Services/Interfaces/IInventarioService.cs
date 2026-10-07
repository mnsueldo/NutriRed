using NutriRed.Domain.Entities;
using NutriRed.Services.Common;
using NutriRed.Services.DTOs;

namespace NutriRed.Services.Interfaces;

public interface IInventarioService
{
    Task<OperationResult<IEnumerable<StockProductoDto>>> ObtenerStockConsolidadoAsync();
    Task<OperationResult<IEnumerable<Lote>>> ObtenerLotesPorProductoAsync(int productoId, bool soloDisponibles = true);
    Task<OperationResult<Lote>> ObtenerLotePorIdAsync(int loteId);
    Task<OperationResult<ResultadoFefoDto>> CalcularAsignacionFefoAsync(int productoId, decimal cantidadRequerida);
    Task<OperationResult<MovimientoStock>> RegistrarAjusteOMermaAsync(AjusteMermaRequest request);
    Task<OperationResult<IEnumerable<Lote>>> ObtenerLotesProximosAVencerAsync(int diasUmbral = 30);
    Task<OperationResult<IEnumerable<MovimientoStock>>> ObtenerMovimientosPorProductoAsync(int productoId);
    Task<OperationResult<decimal>> ObtenerTotalMermasKgAsync();
    Task<OperationResult<decimal>> ObtenerTotalAlimentosDistribuidosAsync();
    Task<OperationResult<List<DistribucionMensualDto>>> ObtenerDistribucionMensualAsync();

}
