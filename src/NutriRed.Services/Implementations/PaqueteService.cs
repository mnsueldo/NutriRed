using Microsoft.EntityFrameworkCore;
using NutriRed.Data;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.Common;
using NutriRed.Services.DTOs;
using NutriRed.Services.Interfaces;

namespace NutriRed.Services.Implementations;

public class PaqueteService : IPaqueteService
{
    private readonly NutriRedDbContext _context;
    private readonly IInventarioService _inventarioService;

    public PaqueteService(NutriRedDbContext context, IInventarioService inventarioService)
    {
        _context = context;
        _inventarioService = inventarioService;
    }

    /// <summary>
    /// Fase 1 del Armado Inteligente:
    /// Evalúa la familia, determina el kit nutricional correspondiente a sus integrantes
    /// y simula la asignación FEFO de lotes, detectando faltantes para permitir sustitutos.
    /// </summary>
    public async Task<OperationResult<PropuestaPaqueteDto>> SugerirPaqueteParaFamiliaAsync(int familiaId, int? tipoPaqueteIdPersonalizado = null)
    {
        try
        {
            var familia = await _context.FamiliasBeneficiarias.FindAsync(familiaId);
            if (familia == null)
            {
                return OperationResult<PropuestaPaqueteDto>.Fail("Familia inactiva o inexistente: La familia seleccionada no existe en el sistema.");
            }

            // Regla de Negocio (Pág. 9 del PDF):
            // "Si la familia seleccionada está en estado SUSPENDIDO o no existe, bloquear la creación del paquete indicando el estado del legajo"
            if (familia.Estado == EstadoFamilia.Suspendido)
            {
                return OperationResult<PropuestaPaqueteDto>.Fail("Familia inactiva o inexistente: La familia seleccionada se encuentra en estado SUSPENDIDO y no puede recibir paquetes asistenciales.");
            }

            TipoPaquete? tipoPaquete;

            if (tipoPaqueteIdPersonalizado.HasValue)
            {
                tipoPaquete = await _context.TiposPaquete
                    .Include(t => t.ItemsPlantilla)
                        .ThenInclude(i => i.Producto)
                    .FirstOrDefaultAsync(t => t.Id == tipoPaqueteIdPersonalizado.Value && t.Activo);
            }
            else
            {
                // Sugerencia Automática por Rango de Integrantes (RF2)
                tipoPaquete = await _context.TiposPaquete
                    .Include(t => t.ItemsPlantilla)
                        .ThenInclude(i => i.Producto)
                    .FirstOrDefaultAsync(t => t.Activo &&
                                              t.MinIntegrantes <= familia.CantidadIntegrantes &&
                                              t.MaxIntegrantes >= familia.CantidadIntegrantes);
            }

            // Regla de Negocio (Pág. 9 del PDF):
            // "Plantilla no configurada: Bloquear el armado si el tamaño familiar no coincide con ningún tipo de paquete activo"
            if (tipoPaquete == null)
            {
                return OperationResult<PropuestaPaqueteDto>.Fail($"Plantilla no configurada: No existe un tipo de paquete configurado para una familia de {familia.CantidadIntegrantes} integrantes.");
            }

            var propuesta = new PropuestaPaqueteDto
            {
                FamiliaId = familia.Id,
                FamiliaTitular = $"{familia.ApellidoTitular}, {familia.NombreTitular}",
                DniTitular = familia.DniTitular,
                CantidadIntegrantes = familia.CantidadIntegrantes,
                TipoPaqueteId = tipoPaquete.Id,
                TipoPaqueteNombre = tipoPaquete.Nombre
            };

            // Evaluar FEFO para cada alimento de la plantilla
            foreach (var item in tipoPaquete.ItemsPlantilla)
            {
                var fefoResult = await _inventarioService.CalcularAsignacionFefoAsync(item.ProductoId, item.CantidadRequerida);

                var itemPropuesta = new ItemPropuestaDto
                {
                    ProductoId = item.ProductoId,
                    NombreProducto = item.Producto?.Nombre ?? "Producto Desconocido",
                    CodigoBarras = item.Producto?.CodigoBarras ?? string.Empty,
                    CantidadRequerida = item.CantidadRequerida
                };

                if (fefoResult.Success && fefoResult.Data != null)
                {
                    itemPropuesta.CantidadDisponible = fefoResult.Data.CantidadAsignada;
                    itemPropuesta.LotesAsignados = fefoResult.Data.Asignaciones;
                }

                propuesta.Items.Add(itemPropuesta);
            }

            return OperationResult<PropuestaPaqueteDto>.Ok(propuesta);
        }
        catch (Exception ex)
        {
            return OperationResult<PropuestaPaqueteDto>.Fail($"Error al generar la sugerencia de paquete: {ex.Message}");
        }
    }

    /// <summary>
    /// Fase 2 del Armado:
    /// Confirma la orden (sea la sugerida o modificada manualmente con sustitutos),
    /// descuenta atómicamente el stock físico de los lotes y genera el código QR (RF2).
    /// </summary>
    public async Task<OperationResult<PaqueteDto>> ConfirmarArmadoPaqueteAsync(ConfirmarArmadoPaqueteRequest request)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return OperationResult<PaqueteDto>.Fail("No se puede armar un paquete vacío.");
        }

        var familia = await _context.FamiliasBeneficiarias.FindAsync(request.FamiliaId);
        if (familia == null || familia.Estado == EstadoFamilia.Suspendido)
        {
            return OperationResult<PaqueteDto>.Fail("La familia receptora no es válida o está suspendida.");
        }

        var tipoPaquete = await _context.TiposPaquete.FindAsync(request.TipoPaqueteId);
        if (tipoPaquete == null)
        {
            return OperationResult<PaqueteDto>.Fail("El tipo de paquete especificado no existe.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Validar disponibilidad FEFO para cada alimento antes de descontar
            var asignacionesPorProducto = new Dictionary<int, ResultadoFefoDto>();

            foreach (var item in request.Items)
            {
                var fefo = await _inventarioService.CalcularAsignacionFefoAsync(item.ProductoId, item.Cantidad);

                // Regla de Negocio (Pág. 9 del PDF):
                // "Stock insuficiente: Si se intenta confirmar un paquete con ítems en estado FALTANTE sin haber aplicado un sustituto válido, bloquear"
                if (!fefo.Success || fefo.Data == null || !fefo.Data.CubiertoTotalmente)
                {
                    await transaction.RollbackAsync();
                    return OperationResult<PaqueteDto>.Fail("Stock insuficiente: No se puede preparar el paquete: existen productos con stock insuficiente.");
                }

                asignacionesPorProducto[item.ProductoId] = fefo.Data;
            }

            // 2. Generar Código QR Único de Rotulación (ej: PKG-2026-00001)
            int anioActual = DateTime.UtcNow.Year;
            int totalPaquetesAnio = await _context.Paquetes
                .CountAsync(p => p.FechaCreacion.Year == anioActual);

            string codigoSeguimiento = $"PKG-{anioActual}-{(totalPaquetesAnio + 1):D5}";

            // 3. Crear el Paquete en estado Preparado
            var paquete = new Paquete
            {
                CodigoSeguimiento = codigoSeguimiento,
                FamiliaBeneficiariaId = request.FamiliaId,
                TipoPaqueteId = request.TipoPaqueteId,
                FechaCreacion = DateTime.UtcNow,
                Estado = EstadoPaquete.Preparado,
                UsuarioArmadorId = string.IsNullOrWhiteSpace(request.UsuarioArmadorId) ? "operador_deposito" : request.UsuarioArmadorId
            };

            _context.Paquetes.Add(paquete);
            await _context.SaveChangesAsync();

            var itemsDto = new List<PaqueteItemDto>();

            // 4. Descontar Stock de los Lotes asignados por FEFO y asentar movimientos
            foreach (var item in request.Items)
            {
                var fefoData = asignacionesPorProducto[item.ProductoId];
                var producto = await _context.Productos.FindAsync(item.ProductoId);
                Producto? productoSustituido = null;

                if (item.EsSustituto && item.ProductoSustituidoId.HasValue)
                {
                    productoSustituido = await _context.Productos.FindAsync(item.ProductoSustituidoId.Value);
                }

                foreach (var asignacion in fefoData.Asignaciones)
                {
                    var loteDb = await _context.Lotes.FindAsync(asignacion.LoteId);
                    if (loteDb == null || loteDb.CantidadDisponible < asignacion.CantidadAsignada)
                    {
                        await transaction.RollbackAsync();
                        return OperationResult<PaqueteDto>.Fail("Error al actualizar el stock: Conflicto de concurrencia en existencias de lote.");
                    }

                    // Descuento físico de inventario
                    loteDb.CantidadDisponible -= asignacion.CantidadAsignada;
                    if (loteDb.CantidadDisponible == 0)
                    {
                        loteDb.Estado = EstadoLote.Agotado;
                    }

                    // Guardar detalle del paquete con el lote exacto utilizado
                    var detalle = new PaqueteDetalle
                    {
                        PaqueteId = paquete.Id,
                        ProductoId = item.ProductoId,
                        LoteId = loteDb.Id,
                        Cantidad = asignacion.CantidadAsignada,
                        EsSustituto = item.EsSustituto,
                        ProductoSustituidoId = item.ProductoSustituidoId
                    };
                    _context.PaqueteDetalles.Add(detalle);

                    // Movimiento auditable de egreso
                    var movimiento = new MovimientoStock
                    {
                        LoteId = loteDb.Id,
                        TipoMovimiento = TipoMovimiento.EgresoPaquete,
                        Motivo = MotivoMovimiento.EntregaPaquete,
                        Cantidad = asignacion.CantidadAsignada,
                        Fecha = DateTime.UtcNow,
                        UsuarioId = paquete.UsuarioArmadorId,
                        Observaciones = $"Asignado a Paquete {codigoSeguimiento}"
                    };
                    _context.MovimientosStock.Add(movimiento);

                    itemsDto.Add(new PaqueteItemDto
                    {
                        ProductoId = item.ProductoId,
                        NombreProducto = producto?.Nombre ?? "Alimento",
                        CodigoBarras = producto?.CodigoBarras ?? string.Empty,
                        LoteId = loteDb.Id,
                        NumeroLote = loteDb.NumeroLote,
                        FechaVencimiento = loteDb.FechaVencimiento,
                        Cantidad = asignacion.CantidadAsignada,
                        EsSustituto = item.EsSustituto,
                        NombreProductoSustituido = productoSustituido?.Nombre
                    });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var paqueteDto = new PaqueteDto
            {
                Id = paquete.Id,
                CodigoSeguimiento = paquete.CodigoSeguimiento,
                FamiliaId = familia.Id,
                FamiliaTitular = $"{familia.ApellidoTitular}, {familia.NombreTitular}",
                DniTitular = familia.DniTitular,
                TipoPaqueteId = tipoPaquete.Id,
                TipoPaqueteNombre = tipoPaquete.Nombre,
                Estado = paquete.Estado,
                FechaCreacion = paquete.FechaCreacion,
                UsuarioArmadorId = paquete.UsuarioArmadorId,
                Items = itemsDto
            };

            return OperationResult<PaqueteDto>.Ok(paqueteDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return OperationResult<PaqueteDto>.Fail($"Error en la transacción al preparar el paquete: {ex.Message}");
        }
    }

    public async Task<OperationResult<PaqueteDto>> ObtenerPorIdAsync(int id)
    {
        try
        {
            var paquete = await _context.Paquetes
                .AsNoTracking()
                .Include(p => p.FamiliaBeneficiaria)
                .Include(p => p.TipoPaquete)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Lote)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.ProductoSustituido)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (paquete == null)
            {
                return OperationResult<PaqueteDto>.Fail("El paquete solicitado no existe.");
            }

            return OperationResult<PaqueteDto>.Ok(MapearAPaqueteDto(paquete));
        }
        catch (Exception ex)
        {
            return OperationResult<PaqueteDto>.Fail($"Error al buscar el paquete: {ex.Message}");
        }
    }

    public async Task<OperationResult<PaqueteDto>> ObtenerPorCodigoSeguimientoAsync(string codigoSeguimiento)
    {
        try
        {
            var codigoNormalizado = codigoSeguimiento.Trim();

            var paquete = await _context.Paquetes
                .AsNoTracking()
                .Include(p => p.FamiliaBeneficiaria)
                .Include(p => p.TipoPaquete)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Lote)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.ProductoSustituido)
                .FirstOrDefaultAsync(p => p.CodigoSeguimiento == codigoNormalizado);

            if (paquete == null)
            {
                return OperationResult<PaqueteDto>.Fail($"No se encontró ningún paquete con el código QR '{codigoNormalizado}'.");
            }

            return OperationResult<PaqueteDto>.Ok(MapearAPaqueteDto(paquete));
        }
        catch (Exception ex)
        {
            return OperationResult<PaqueteDto>.Fail($"Error al consultar el paquete por código: {ex.Message}");
        }
    }

    public async Task<OperationResult<IEnumerable<PaqueteDto>>> ObtenerTodosAsync(EstadoPaquete? estado = null)
    {
        try
        {
            var query = _context.Paquetes
                .AsNoTracking()
                .Include(p => p.FamiliaBeneficiaria)
                .Include(p => p.TipoPaquete)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Lote)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.ProductoSustituido)
                .AsQueryable();

            if (estado.HasValue)
            {
                query = query.Where(p => p.Estado == estado.Value);
            }

            var paquetes = await query
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();

            var dtos = paquetes.Select(MapearAPaqueteDto).ToList();
            return OperationResult<IEnumerable<PaqueteDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return OperationResult<IEnumerable<PaqueteDto>>.Fail($"Error al obtener la lista de paquetes: {ex.Message}");
        }
    }

    public async Task<OperationResult<IEnumerable<TipoPaquete>>> ObtenerTiposPaqueteAsync()
    {
        try
        {
            var tipos = await _context.TiposPaquete
                .AsNoTracking()
                .Include(t => t.ItemsPlantilla)
                    .ThenInclude(i => i.Producto)
                .Where(t => t.Activo)
                .OrderBy(t => t.MinIntegrantes)
                .ToListAsync();

            return OperationResult<IEnumerable<TipoPaquete>>.Ok(tipos);
        }
        catch (Exception ex)
        {
            return OperationResult<IEnumerable<TipoPaquete>>.Fail($"Error al obtener los tipos de paquetes: {ex.Message}");
        }
    }

    private static PaqueteDto MapearAPaqueteDto(Paquete paquete)
    {
        return new PaqueteDto
        {
            Id = paquete.Id,
            CodigoSeguimiento = paquete.CodigoSeguimiento,
            FamiliaId = paquete.FamiliaBeneficiariaId,
            FamiliaTitular = paquete.FamiliaBeneficiaria != null
                ? $"{paquete.FamiliaBeneficiaria.ApellidoTitular}, {paquete.FamiliaBeneficiaria.NombreTitular}"
                : "Sin Titular",
            DniTitular = paquete.FamiliaBeneficiaria?.DniTitular ?? string.Empty,
            TipoPaqueteId = paquete.TipoPaqueteId,
            TipoPaqueteNombre = paquete.TipoPaquete?.Nombre ?? "Kit Personalizado",
            Estado = paquete.Estado,
            FechaCreacion = paquete.FechaCreacion,
            UsuarioArmadorId = paquete.UsuarioArmadorId,
            Items = paquete.Detalles.Select(d => new PaqueteItemDto
            {
                ProductoId = d.ProductoId,
                NombreProducto = d.Producto?.Nombre ?? "Alimento",
                CodigoBarras = d.Producto?.CodigoBarras ?? string.Empty,
                LoteId = d.LoteId,
                NumeroLote = d.Lote?.NumeroLote ?? string.Empty,
                FechaVencimiento = d.Lote?.FechaVencimiento ?? DateTime.MinValue,
                Cantidad = d.Cantidad,
                EsSustituto = d.EsSustituto,
                NombreProductoSustituido = d.ProductoSustituido?.Nombre
            }).ToList()
        };
    }
}
