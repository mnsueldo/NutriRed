using Microsoft.EntityFrameworkCore;
using NutriRed.Data;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.Common;
using NutriRed.Services.DTOs;
using NutriRed.Services.Interfaces;

namespace NutriRed.Services.Implementations;

public class InventarioService : IInventarioService
{
    private readonly NutriRedDbContext _context;

    public InventarioService(NutriRedDbContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<IEnumerable<StockProductoDto>>> ObtenerStockConsolidadoAsync()
    {
        try
        {
            var hoy = DateTime.Today;

            var productos = await _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.Lotes)
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            var resultado = productos.Select(p =>
            {
                var lotesAptos = p.Lotes
                    .Where(l => l.Estado == EstadoLote.Disponible && l.FechaVencimiento > hoy && l.CantidadDisponible > 0)
                    .ToList();

                var stockActual = lotesAptos.Sum(l => l.CantidadDisponible);
                var proximoVencimiento = lotesAptos.OrderBy(l => l.FechaVencimiento).FirstOrDefault()?.FechaVencimiento;

                return new StockProductoDto
                {
                    ProductoId = p.Id,
                    CodigoBarras = p.CodigoBarras,
                    NombreProducto = p.Nombre,
                    Categoria = p.Categoria?.Nombre ?? "Sin Categoría",
                    UnidadMedida = p.UnidadMedida,
                    StockActual = stockActual,
                    StockMinimo = p.StockMinimo,
                    CantidadLotesActivos = lotesAptos.Count,
                    ProximoVencimiento = proximoVencimiento
                };
            }).ToList();

            return OperationResult<IEnumerable<StockProductoDto>>.Ok(resultado);
        }
        catch (Exception ex)
        {
            return OperationResult<IEnumerable<StockProductoDto>>.Fail($"Error al obtener el inventario consolidado: {ex.Message}");
        }
    }

    public async Task<OperationResult<IEnumerable<Lote>>> ObtenerLotesPorProductoAsync(int productoId, bool soloDisponibles = true)
    {
        try
        {
            var query = _context.Lotes
                .AsNoTracking()
                .Include(l => l.Producto)
                .Where(l => l.ProductoId == productoId);

            if (soloDisponibles)
            {
                var hoy = DateTime.Today;
                query = query.Where(l => l.Estado == EstadoLote.Disponible && l.CantidadDisponible > 0 && l.FechaVencimiento > hoy);
            }

            var lotes = await query.OrderBy(l => l.FechaVencimiento).ToListAsync();
            return OperationResult<IEnumerable<Lote>>.Ok(lotes);
        }
        catch (Exception ex)
        {
            return OperationResult<IEnumerable<Lote>>.Fail($"Error al obtener los lotes del producto: {ex.Message}");
        }
    }

    public async Task<OperationResult<Lote>> ObtenerLotePorIdAsync(int loteId)
    {
        try
        {
            var lote = await _context.Lotes
                .AsNoTracking()
                .Include(l => l.Producto)
                .Include(l => l.Movimientos)
                .FirstOrDefaultAsync(l => l.Id == loteId);

            if (lote == null)
            {
                return OperationResult<Lote>.Fail("El lote solicitado no existe.");
            }

            return OperationResult<Lote>.Ok(lote);
        }
        catch (Exception ex)
        {
            return OperationResult<Lote>.Fail($"Error al consultar el lote: {ex.Message}");
        }
    }

    /// <summary>
    /// Algoritmo FEFO (First Expired, First Out):
    /// Selecciona de forma inteligente y cronológica los lotes aptos con vencimiento más cercano
    /// hasta cubrir la cantidad requerida para un producto.
    /// </summary>
    public async Task<OperationResult<ResultadoFefoDto>> CalcularAsignacionFefoAsync(int productoId, decimal cantidadRequerida)
    {
        try
        {
            if (cantidadRequerida <= 0)
            {
                return OperationResult<ResultadoFefoDto>.Fail("La cantidad requerida debe ser mayor a cero.");
            }

            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null)
            {
                return OperationResult<ResultadoFefoDto>.Fail("El producto no existe en el catálogo.");
            }

            var hoy = DateTime.Today;

            // Selección FEFO: Lotes disponibles, no vencidos y con stock, ordenados por vencimiento ascendente
            var lotesAptos = await _context.Lotes
                .Where(l => l.ProductoId == productoId
                            && l.Estado == EstadoLote.Disponible
                            && l.CantidadDisponible > 0
                            && l.FechaVencimiento > hoy)
                .OrderBy(l => l.FechaVencimiento)
                .ToListAsync();

            var resultado = new ResultadoFefoDto
            {
                ProductoId = producto.Id,
                NombreProducto = producto.Nombre,
                CantidadRequerida = cantidadRequerida
            };

            decimal cantidadPendiente = cantidadRequerida;

            foreach (var lote in lotesAptos)
            {
                if (cantidadPendiente <= 0) break;

                decimal aExtraer = Math.Min(lote.CantidadDisponible, cantidadPendiente);

                resultado.Asignaciones.Add(new AsignacionLoteDto
                {
                    LoteId = lote.Id,
                    NumeroLote = lote.NumeroLote,
                    FechaVencimiento = lote.FechaVencimiento,
                    CantidadAsignada = aExtraer
                });

                resultado.CantidadAsignada += aExtraer;
                cantidadPendiente -= aExtraer;
            }

            return OperationResult<ResultadoFefoDto>.Ok(resultado);
        }
        catch (Exception ex)
        {
            return OperationResult<ResultadoFefoDto>.Fail($"Error al ejecutar el algoritmo FEFO: {ex.Message}");
        }
    }

    public async Task<OperationResult<MovimientoStock>> RegistrarAjusteOMermaAsync(AjusteMermaRequest request)
    {
        try
        {
            if (request.Cantidad <= 0)
            {
                return OperationResult<MovimientoStock>.Fail("La cantidad a ajustar debe ser mayor a cero.");
            }

            var lote = await _context.Lotes.FindAsync(request.LoteId);
            if (lote == null)
            {
                return OperationResult<MovimientoStock>.Fail("El lote especificado no existe.");
            }

            // Regla de Negocio (pág. 9 especificación):
            // "Si se intenta dar de baja una cantidad mayor al stock actual del lote, rechazar"
            if (request.Cantidad > lote.CantidadDisponible)
            {
                return OperationResult<MovimientoStock>.Fail("La cantidad a descontar supera las existencias disponibles en el lote.");
            }

            // Descontar la cantidad
            lote.CantidadDisponible -= request.Cantidad;

            // Actualizar estado si se agotó o se marcó como vencido
            if (lote.CantidadDisponible == 0)
            {
                lote.Estado = request.Motivo == MotivoMovimiento.Vencido ? EstadoLote.Vencido : EstadoLote.Agotado;
            }

            // Crear el registro auditable de movimiento
            var movimiento = new MovimientoStock
            {
                LoteId = lote.Id,
                TipoMovimiento = request.TipoMovimiento,
                Motivo = request.Motivo,
                Cantidad = request.Cantidad,
                Fecha = DateTime.UtcNow,
                UsuarioId = string.IsNullOrWhiteSpace(request.UsuarioId) ? "sistema" : request.UsuarioId,
                Observaciones = request.Observaciones
            };

            _context.MovimientosStock.Add(movimiento);
            await _context.SaveChangesAsync();

            return OperationResult<MovimientoStock>.Ok(movimiento);
        }
        catch (Exception ex)
        {
            return OperationResult<MovimientoStock>.Fail($"Error al registrar el ajuste de stock: {ex.Message}");
        }
    }

    public async Task<OperationResult<IEnumerable<Lote>>> ObtenerLotesProximosAVencerAsync(int diasUmbral = 30)
    {
        try
        {
            var hoy = DateTime.Today;
            var fechaLimite = hoy.AddDays(diasUmbral);

            var lotes = await _context.Lotes
                .AsNoTracking()
                .Include(l => l.Producto)
                .Where(l => l.Estado == EstadoLote.Disponible
                            && l.CantidadDisponible > 0
                            && l.FechaVencimiento > hoy
                            && l.FechaVencimiento <= fechaLimite)
                .OrderBy(l => l.FechaVencimiento)
                .ToListAsync();

            return OperationResult<IEnumerable<Lote>>.Ok(lotes);
        }
        catch (Exception ex)
        {
            return OperationResult<IEnumerable<Lote>>.Fail($"Error al consultar lotes próximos a vencer: {ex.Message}");
        }
    }
}
