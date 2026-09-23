using Microsoft.EntityFrameworkCore;
using NutriRed.Data;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.Common;
using NutriRed.Services.DTOs;
using NutriRed.Services.Interfaces;

namespace NutriRed.Services.Implementations;

public class DonacionService : IDonacionService
{
    private readonly NutriRedDbContext _context;

    public DonacionService(NutriRedDbContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<DonacionComprobanteDto>> RegistrarDonacionAsync(RegistrarDonacionRequest request)
    {
        // 1. Validaciones Previas (Requerimiento RF1 - Pág. 7 del PDF)
        if (request.Items == null || !request.Items.Any())
        {
            return OperationResult<DonacionComprobanteDto>.Fail("Detalle vacío: Bloquear la confirmación de la donación si no se ha cargado al menos un producto en la lista.");
        }

        if (request.TipoDonante != TipoDonante.Anonimo)
        {
            if (string.IsNullOrWhiteSpace(request.NombreRazonSocial) || string.IsNullOrWhiteSpace(request.NumeroDocumento))
            {
                return OperationResult<DonacionComprobanteDto>.Fail("Datos de donante incompletos: Impedir avanzar si se selecciona persona física o institución y falta el nombre o el documento.");
            }
        }

        var hoy = DateTime.Today;

        foreach (var item in request.Items)
        {
            if (item.Cantidad <= 0)
            {
                return OperationResult<DonacionComprobanteDto>.Fail($"Cantidad inválida: La cantidad recibida para el producto con código '{item.CodigoBarras}' debe ser mayor a cero.");
            }

            if (item.FechaVencimiento.Date <= hoy)
            {
                return OperationResult<DonacionComprobanteDto>.Fail("No se pueden recibir alimentos vencidos o que vencen en el día.");
            }
        }

        // 2. Ejecución Atómica (Transacción Todo o Nada)
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // A. Gestión del Donante
            int? donanteId = null;

            if (request.TipoDonante == TipoDonante.Anonimo)
            {
                var donanteAnonimo = await _context.Donantes
                    .FirstOrDefaultAsync(d => d.Tipo == TipoDonante.Anonimo);

                if (donanteAnonimo == null)
                {
                    donanteAnonimo = new Donante
                    {
                        Tipo = TipoDonante.Anonimo,
                        NombreRazonSocial = "Donante Anónimo",
                        Activo = true
                    };
                    _context.Donantes.Add(donanteAnonimo);
                    await _context.SaveChangesAsync();
                }
                donanteId = donanteAnonimo.Id;
            }
            else
            {
                var docNormalizado = request.NumeroDocumento!.Trim();
                var donanteExistente = await _context.Donantes
                    .FirstOrDefaultAsync(d => d.NumeroDocumento == docNormalizado);

                if (donanteExistente == null)
                {
                    donanteExistente = new Donante
                    {
                        Tipo = request.TipoDonante,
                        NumeroDocumento = docNormalizado,
                        NombreRazonSocial = request.NombreRazonSocial!.Trim(),
                        Telefono = request.Telefono?.Trim(),
                        Email = request.Email?.Trim(),
                        Activo = true
                    };
                    _context.Donantes.Add(donanteExistente);
                    await _context.SaveChangesAsync();
                }
                donanteId = donanteExistente.Id;
            }

            // B. Generar Código Secuencial Único (ej: DON-2026-00001)
            int anioActual = DateTime.UtcNow.Year;
            int totalDonacionesAnio = await _context.Donaciones
                .CountAsync(d => d.FechaHora.Year == anioActual);

            string codigoComprobante = $"DON-{anioActual}-{(totalDonacionesAnio + 1):D5}";

            // C. Crear la Cabecera de Donación
            var donacion = new Donacion
            {
                CodigoComprobante = codigoComprobante,
                FechaHora = DateTime.UtcNow,
                DonanteId = donanteId,
                VoluntarioReceptorId = string.IsNullOrWhiteSpace(request.VoluntarioReceptorId) ? "voluntario_deposito" : request.VoluntarioReceptorId,
                Observaciones = request.Observaciones?.Trim()
            };

            _context.Donaciones.Add(donacion);
            await _context.SaveChangesAsync();

            var detallesDto = new List<DonacionDetalleDto>();
            decimal totalUnidades = 0;

            // D. Procesar cada Alimento Donado
            for (int i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];
                var ean = item.CodigoBarras.Trim();

                var producto = await _context.Productos
                    .Include(p => p.Categoria)
                    .FirstOrDefaultAsync(p => p.CodigoBarras == ean);

                if (producto == null || !producto.Activo)
                {
                    await transaction.RollbackAsync();
                    return OperationResult<DonacionComprobanteDto>.Fail("Producto no registrado en el catálogo. Solicite su alta al administrador.");
                }

                // Generar o asignar número de lote
                string numeroLote = string.IsNullOrWhiteSpace(item.NumeroLote)
                    ? $"LOT-{DateTime.UtcNow:yyMMdd}-{producto.Id}-{(i + 1):D2}"
                    : item.NumeroLote.Trim();

                // Crear el Lote Físico con existencias activas para FEFO
                var lote = new Lote
                {
                    NumeroLote = numeroLote,
                    ProductoId = producto.Id,
                    FechaVencimiento = item.FechaVencimiento.Date,
                    CantidadInicial = item.Cantidad,
                    CantidadDisponible = item.Cantidad,
                    Estado = EstadoLote.Disponible,
                    FechaIngreso = DateTime.UtcNow
                };

                _context.Lotes.Add(lote);
                await _context.SaveChangesAsync();

                // Vincular al detalle de la donación
                var detalle = new DonacionDetalle
                {
                    DonacionId = donacion.Id,
                    ProductoId = producto.Id,
                    LoteId = lote.Id,
                    Cantidad = item.Cantidad
                };

                _context.DonacionDetalles.Add(detalle);

                // Crear registro de auditoría en Movimientos de Stock
                var movimiento = new MovimientoStock
                {
                    LoteId = lote.Id,
                    TipoMovimiento = TipoMovimiento.IngresoDonacion,
                    Motivo = MotivoMovimiento.DonacionRecibida,
                    Cantidad = item.Cantidad,
                    Fecha = DateTime.UtcNow,
                    UsuarioId = donacion.VoluntarioReceptorId,
                    Observaciones = $"Ingreso por Donación {codigoComprobante}"
                };

                _context.MovimientosStock.Add(movimiento);

                totalUnidades += item.Cantidad;

                detallesDto.Add(new DonacionDetalleDto
                {
                    ProductoId = producto.Id,
                    CodigoBarras = producto.CodigoBarras,
                    NombreProducto = producto.Nombre,
                    Categoria = producto.Categoria?.Nombre ?? "Sin Categoría",
                    UnidadMedida = producto.UnidadMedida,
                    Cantidad = item.Cantidad,
                    NumeroLote = lote.NumeroLote,
                    FechaVencimiento = lote.FechaVencimiento
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var donante = await _context.Donantes.FindAsync(donanteId);

            var comprobante = new DonacionComprobanteDto
            {
                DonacionId = donacion.Id,
                CodigoComprobante = donacion.CodigoComprobante,
                FechaHora = donacion.FechaHora,
                VoluntarioReceptorId = donacion.VoluntarioReceptorId,
                DonanteNombre = donante?.NombreRazonSocial ?? "Anónimo",
                DonanteDocumento = donante?.NumeroDocumento,
                TipoDonante = donante?.Tipo ?? TipoDonante.Anonimo,
                Observaciones = donacion.Observaciones,
                TotalUnidadesRecibidas = totalUnidades,
                Detalles = detallesDto
            };

            return OperationResult<DonacionComprobanteDto>.Ok(comprobante);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return OperationResult<DonacionComprobanteDto>.Fail($"Error en la transacción al registrar la donación: {ex.Message}");
        }
    }

    public async Task<OperationResult<DonacionComprobanteDto>> ObtenerPorIdAsync(int id)
    {
        try
        {
            var donacion = await _context.Donaciones
                .AsNoTracking()
                .Include(d => d.Donante)
                .Include(d => d.Detalles)
                    .ThenInclude(det => det.Producto)
                        .ThenInclude(p => p!.Categoria)
                .Include(d => d.Detalles)
                    .ThenInclude(det => det.Lote)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (donacion == null)
            {
                return OperationResult<DonacionComprobanteDto>.Fail("La donación solicitada no existe.");
            }

            return OperationResult<DonacionComprobanteDto>.Ok(MapearAComprobante(donacion));
        }
        catch (Exception ex)
        {
            return OperationResult<DonacionComprobanteDto>.Fail($"Error al buscar la donación: {ex.Message}");
        }
    }

    public async Task<OperationResult<DonacionComprobanteDto>> ObtenerPorCodigoAsync(string codigoComprobante)
    {
        try
        {
            var donacion = await _context.Donaciones
                .AsNoTracking()
                .Include(d => d.Donante)
                .Include(d => d.Detalles)
                    .ThenInclude(det => det.Producto)
                        .ThenInclude(p => p!.Categoria)
                .Include(d => d.Detalles)
                    .ThenInclude(det => det.Lote)
                .FirstOrDefaultAsync(d => d.CodigoComprobante == codigoComprobante.Trim());

            if (donacion == null)
            {
                return OperationResult<DonacionComprobanteDto>.Fail($"No se encontró la donación con código '{codigoComprobante}'.");
            }

            return OperationResult<DonacionComprobanteDto>.Ok(MapearAComprobante(donacion));
        }
        catch (Exception ex)
        {
            return OperationResult<DonacionComprobanteDto>.Fail($"Error al buscar la donación: {ex.Message}");
        }
    }

    public async Task<OperationResult<IEnumerable<DonacionComprobanteDto>>> ObtenerTodasAsync(DateTime? fechaDesde = null, DateTime? fechaHasta = null)
    {
        try
        {
            var query = _context.Donaciones
                .AsNoTracking()
                .Include(d => d.Donante)
                .Include(d => d.Detalles)
                    .ThenInclude(det => det.Producto)
                        .ThenInclude(p => p!.Categoria)
                .Include(d => d.Detalles)
                    .ThenInclude(det => det.Lote)
                .AsQueryable();

            if (fechaDesde.HasValue)
            {
                query = query.Where(d => d.FechaHora >= fechaDesde.Value);
            }

            if (fechaHasta.HasValue)
            {
                query = query.Where(d => d.FechaHora <= fechaHasta.Value);
            }

            var donaciones = await query
                .OrderByDescending(d => d.FechaHora)
                .ToListAsync();

            var resultado = donaciones.Select(MapearAComprobante).ToList();
            return OperationResult<IEnumerable<DonacionComprobanteDto>>.Ok(resultado);
        }
        catch (Exception ex)
        {
            return OperationResult<IEnumerable<DonacionComprobanteDto>>.Fail($"Error al consultar las donaciones: {ex.Message}");
        }
    }

    private static DonacionComprobanteDto MapearAComprobante(Donacion donacion)
    {
        return new DonacionComprobanteDto
        {
            DonacionId = donacion.Id,
            CodigoComprobante = donacion.CodigoComprobante,
            FechaHora = donacion.FechaHora,
            VoluntarioReceptorId = donacion.VoluntarioReceptorId,
            DonanteNombre = donacion.Donante?.NombreRazonSocial ?? "Anónimo",
            DonanteDocumento = donacion.Donante?.NumeroDocumento,
            TipoDonante = donacion.Donante?.Tipo ?? TipoDonante.Anonimo,
            Observaciones = donacion.Observaciones,
            TotalUnidadesRecibidas = donacion.Detalles.Sum(det => det.Cantidad),
            Detalles = donacion.Detalles.Select(det => new DonacionDetalleDto
            {
                ProductoId = det.ProductoId,
                CodigoBarras = det.Producto?.CodigoBarras ?? string.Empty,
                NombreProducto = det.Producto?.Nombre ?? "Producto Desconocido",
                Categoria = det.Producto?.Categoria?.Nombre ?? "Sin Categoría",
                UnidadMedida = det.Producto?.UnidadMedida ?? UnidadMedida.Unidades,
                Cantidad = det.Cantidad,
                NumeroLote = det.Lote?.NumeroLote ?? string.Empty,
                FechaVencimiento = det.Lote?.FechaVencimiento ?? DateTime.MinValue
            }).ToList()
        };
    }
}
