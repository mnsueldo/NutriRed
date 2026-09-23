using Microsoft.EntityFrameworkCore;
using NutriRed.Data;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.Common;
using NutriRed.Services.DTOs;
using NutriRed.Services.Interfaces;

namespace NutriRed.Services.Implementations;

public class EntregaService : IEntregaService
{
    private readonly NutriRedDbContext _context;

    public EntregaService(NutriRedDbContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<IEnumerable<PaquetePendienteDespachoDto>>> ObtenerPaquetesListosParaDespachoAsync()
    {
        try
        {
            var paquetes = await _context.Paquetes
                .AsNoTracking()
                .Include(p => p.FamiliaBeneficiaria)
                .Include(p => p.TipoPaquete)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Lote)
                .Where(p => p.Estado == EstadoPaquete.Preparado)
                .OrderBy(p => p.FechaCreacion)
                .ToListAsync();

            var resultado = paquetes.Select(MapearAPendienteDto).ToList();
            return OperationResult<IEnumerable<PaquetePendienteDespachoDto>>.Ok(resultado);
        }
        catch (Exception ex)
        {
            return OperationResult<IEnumerable<PaquetePendienteDespachoDto>>.Fail($"Error al consultar paquetes listos para despacho: {ex.Message}");
        }
    }

    public async Task<OperationResult<PaquetePendienteDespachoDto>> ConsultarPaquetePorQrAsync(string codigoSeguimiento)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(codigoSeguimiento))
            {
                return OperationResult<PaquetePendienteDespachoDto>.Fail("El código QR no puede estar vacío.");
            }

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
                return OperationResult<PaquetePendienteDespachoDto>.Fail($"No se encontró ningún paquete con el código QR '{codigoNormalizado}'.");
            }

            // Regla de Negocio (Pág. 10 del PDF):
            // "Paquete no preparado o ya entregado: Notificar que el paquete no está habilitado para despacho."
            if (paquete.Estado != EstadoPaquete.Preparado)
            {
                return OperationResult<PaquetePendienteDespachoDto>.Fail("Paquete no preparado o ya entregado: Notificar que el paquete no está habilitado para despacho.");
            }

            return OperationResult<PaquetePendienteDespachoDto>.Ok(MapearAPendienteDto(paquete));
        }
        catch (Exception ex)
        {
            return OperationResult<PaquetePendienteDespachoDto>.Fail($"Error al consultar el código QR: {ex.Message}");
        }
    }

    public async Task<OperationResult<ComprobanteEntregaDto>> ConfirmarEntregaAsync(RegistrarEntregaRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CodigoSeguimiento))
            {
                return OperationResult<ComprobanteEntregaDto>.Fail("El código de seguimiento es obligatorio.");
            }

            var codigoNormalizado = request.CodigoSeguimiento.Trim();

            var paquete = await _context.Paquetes
                .Include(p => p.FamiliaBeneficiaria)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Lote)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.ProductoSustituido)
                .FirstOrDefaultAsync(p => p.CodigoSeguimiento == codigoNormalizado);

            if (paquete == null)
            {
                return OperationResult<ComprobanteEntregaDto>.Fail($"El paquete con código '{codigoNormalizado}' no existe.");
            }

            // Regla de Negocio (Pág. 10 del PDF):
            // "Paquete no preparado o ya entregado: Notificar que el paquete no está habilitado para despacho."
            if (paquete.Estado != EstadoPaquete.Preparado)
            {
                return OperationResult<ComprobanteEntregaDto>.Fail("Paquete no preparado o ya entregado: Notificar que el paquete no está habilitado para despacho.");
            }

            // Regla de Negocio (Pág. 10 del PDF):
            // "Firma digital vacía: Impedir la confirmación de la entrega si el campo de firma está vacío."
            if (string.IsNullOrWhiteSpace(request.FirmaDigital))
            {
                return OperationResult<ComprobanteEntregaDto>.Fail("Firma digital vacía: Impedir la confirmación de la entrega si el campo de firma está vacío.");
            }

            // Regla de Negocio (Pág. 10 del PDF):
            // "Discrepancia de identidad: Advertir si el receptor no coincide con el titular ni se encuentra autorizado para retirar el paquete."
            var dniReceptorNormalizado = request.DniReceptor.Trim();

            if (request.TipoReceptor == TipoReceptor.Titular)
            {
                var dniTitular = paquete.FamiliaBeneficiaria?.DniTitular?.Trim() ?? string.Empty;
                if (!string.Equals(dniReceptorNormalizado, dniTitular, StringComparison.OrdinalIgnoreCase))
                {
                    return OperationResult<ComprobanteEntregaDto>.Fail("Discrepancia de identidad: Advertir si el receptor no coincide con el titular ni se encuentra autorizado para retirar el paquete.");
                }
            }
            else // Tercero Autorizado
            {
                if (string.IsNullOrWhiteSpace(request.VinculoConTitular))
                {
                    return OperationResult<ComprobanteEntregaDto>.Fail("Datos incompletos: Se debe especificar el vínculo o parentesco del tercero autorizado con el titular.");
                }

                if (string.IsNullOrWhiteSpace(request.NombreReceptor))
                {
                    return OperationResult<ComprobanteEntregaDto>.Fail("Datos incompletos: Se requiere el nombre completo de la persona autorizada que retira físicamente.");
                }
            }

            // 1. Asentar la Entrega formal
            var entrega = new Entrega
            {
                PaqueteId = paquete.Id,
                FechaHoraEntrega = DateTime.UtcNow,
                VoluntarioDespachoId = string.IsNullOrWhiteSpace(request.VoluntarioDespachoId) ? "voluntario_terreno" : request.VoluntarioDespachoId,
                TipoReceptor = request.TipoReceptor,
                DniReceptor = dniReceptorNormalizado,
                NombreReceptor = request.NombreReceptor.Trim(),
                VinculoConTitular = request.VinculoConTitular?.Trim(),
                FirmaDigital = request.FirmaDigital.Trim(),
                Concretada = true
            };

            _context.Entregas.Add(entrega);

            // 2. Transición de Estado e Inmutabilidad (Pág. 10 del PDF):
            // "Cambiar el estado del paquete de PREPARADO a ENTREGADO y bloquearlo contra futuras modificaciones (inmutable)"
            paquete.Estado = EstadoPaquete.Entregado;

            await _context.SaveChangesAsync();

            var comprobante = new ComprobanteEntregaDto
            {
                EntregaId = entrega.Id,
                PaqueteId = paquete.Id,
                CodigoSeguimiento = paquete.CodigoSeguimiento,
                FechaHoraEntrega = entrega.FechaHoraEntrega,
                VoluntarioDespachoId = entrega.VoluntarioDespachoId,
                FamiliaTitular = $"{paquete.FamiliaBeneficiaria?.ApellidoTitular}, {paquete.FamiliaBeneficiaria?.NombreTitular}",
                DniFamilia = paquete.FamiliaBeneficiaria?.DniTitular ?? string.Empty,
                TipoReceptor = entrega.TipoReceptor,
                DniReceptor = entrega.DniReceptor,
                NombreReceptor = entrega.NombreReceptor,
                VinculoConTitular = entrega.VinculoConTitular,
                FirmaDigital = entrega.FirmaDigital,
                Concretada = true,
                AlimentosEntregados = paquete.Detalles.Select(d => new PaqueteItemDto
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

            return OperationResult<ComprobanteEntregaDto>.Ok(comprobante);
        }
        catch (Exception ex)
        {
            return OperationResult<ComprobanteEntregaDto>.Fail($"Error al confirmar la entrega del paquete: {ex.Message}");
        }
    }

    public async Task<OperationResult> RegistrarEntregaNoConcretadaAsync(RegistrarEntregaFallidaRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CodigoSeguimiento))
            {
                return OperationResult.Fail("El código de seguimiento es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(request.MotivoNoEntrega))
            {
                return OperationResult.Fail("Debe ingresar el motivo por el cual no se concretó la entrega (ej. Ausente, Rechazado).");
            }

            var codigoNormalizado = request.CodigoSeguimiento.Trim();

            var paquete = await _context.Paquetes
                .Include(p => p.FamiliaBeneficiaria)
                .FirstOrDefaultAsync(p => p.CodigoSeguimiento == codigoNormalizado);

            if (paquete == null)
            {
                return OperationResult.Fail($"El paquete con código '{codigoNormalizado}' no existe.");
            }

            if (paquete.Estado != EstadoPaquete.Preparado)
            {
                return OperationResult.Fail("El paquete no se encuentra en estado Preparado.");
            }

            // Regla de Negocio (Pág. 10 del PDF):
            // "Entrega no concretada: Permitir marcar el paquete como no entregado con registro de motivo (ej. ausente, rechazado)"
            var entregaFallida = new Entrega
            {
                PaqueteId = paquete.Id,
                FechaHoraEntrega = DateTime.UtcNow,
                VoluntarioDespachoId = string.IsNullOrWhiteSpace(request.VoluntarioDespachoId) ? "voluntario_terreno" : request.VoluntarioDespachoId,
                TipoReceptor = TipoReceptor.Titular,
                DniReceptor = paquete.FamiliaBeneficiaria?.DniTitular ?? "N/A",
                NombreReceptor = paquete.FamiliaBeneficiaria?.NombreTitular ?? "N/A",
                FirmaDigital = "NO_CONCRETADA",
                Concretada = false,
                MotivoNoEntrega = request.MotivoNoEntrega.Trim()
            };

            _context.Entregas.Add(entregaFallida);
            await _context.SaveChangesAsync();

            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Error al registrar la entrega no concretada: {ex.Message}");
        }
    }

    public async Task<OperationResult<ComprobanteEntregaDto>> ObtenerComprobanteEntregaAsync(int entregaId)
    {
        try
        {
            var entrega = await _context.Entregas
                .AsNoTracking()
                .Include(e => e.Paquete)
                    .ThenInclude(p => p!.FamiliaBeneficiaria)
                .Include(e => e.Paquete)
                    .ThenInclude(p => p!.Detalles)
                        .ThenInclude(d => d.Producto)
                .Include(e => e.Paquete)
                    .ThenInclude(p => p!.Detalles)
                        .ThenInclude(d => d.Lote)
                .Include(e => e.Paquete)
                    .ThenInclude(p => p!.Detalles)
                        .ThenInclude(d => d.ProductoSustituido)
                .FirstOrDefaultAsync(e => e.Id == entregaId);

            if (entrega == null || entrega.Paquete == null)
            {
                return OperationResult<ComprobanteEntregaDto>.Fail("La constancia de entrega solicitada no existe.");
            }

            var comprobante = new ComprobanteEntregaDto
            {
                EntregaId = entrega.Id,
                PaqueteId = entrega.PaqueteId,
                CodigoSeguimiento = entrega.Paquete.CodigoSeguimiento,
                FechaHoraEntrega = entrega.FechaHoraEntrega,
                VoluntarioDespachoId = entrega.VoluntarioDespachoId,
                FamiliaTitular = $"{entrega.Paquete.FamiliaBeneficiaria?.ApellidoTitular}, {entrega.Paquete.FamiliaBeneficiaria?.NombreTitular}",
                DniFamilia = entrega.Paquete.FamiliaBeneficiaria?.DniTitular ?? string.Empty,
                TipoReceptor = entrega.TipoReceptor,
                DniReceptor = entrega.DniReceptor,
                NombreReceptor = entrega.NombreReceptor,
                VinculoConTitular = entrega.VinculoConTitular,
                FirmaDigital = entrega.FirmaDigital,
                Concretada = entrega.Concretada,
                MotivoNoEntrega = entrega.MotivoNoEntrega,
                AlimentosEntregados = entrega.Paquete.Detalles.Select(d => new PaqueteItemDto
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

            return OperationResult<ComprobanteEntregaDto>.Ok(comprobante);
        }
        catch (Exception ex)
        {
            return OperationResult<ComprobanteEntregaDto>.Fail($"Error al consultar el comprobante de entrega: {ex.Message}");
        }
    }

    private static PaquetePendienteDespachoDto MapearAPendienteDto(Paquete paquete)
    {
        return new PaquetePendienteDespachoDto
        {
            PaqueteId = paquete.Id,
            CodigoSeguimiento = paquete.CodigoSeguimiento,
            FamiliaId = paquete.FamiliaBeneficiariaId,
            FamiliaTitular = paquete.FamiliaBeneficiaria != null
                ? $"{paquete.FamiliaBeneficiaria.ApellidoTitular}, {paquete.FamiliaBeneficiaria.NombreTitular}"
                : "Sin Titular",
            DniTitular = paquete.FamiliaBeneficiaria?.DniTitular ?? string.Empty,
            CantidadIntegrantes = paquete.FamiliaBeneficiaria?.CantidadIntegrantes ?? 0,
            Direccion = paquete.FamiliaBeneficiaria?.Direccion ?? string.Empty,
            Telefono = paquete.FamiliaBeneficiaria?.Telefono,
            TipoPaqueteNombre = paquete.TipoPaquete?.Nombre ?? "Kit",
            FechaCreacion = paquete.FechaCreacion,
            Alimentos = paquete.Detalles.Select(d => new PaqueteItemDto
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
