using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NutriRed.Data;
using NutriRed.Services.DTOs;
using NutriRed.Services.Interfaces;

namespace NutriRed.Services.Implementations;

public class ReporteService : IReporteService
{
    private readonly NutriRedDbContext _context;

    public ReporteService(NutriRedDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReporteInventarioItemDto>> ObtenerReporteInventarioAsync(int? categoriaId, string? estadoLote)
    {
        var query = _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Include(p => p.Lotes)
            .Where(p => p.Activo)
            .AsQueryable();

        if (categoriaId.HasValue)
            query = query.Where(p => p.CategoriaId == categoriaId.Value);

        var productos = await query.ToListAsync();
        var resultado = new List<ReporteInventarioItemDto>();
        var hoy = DateTime.Today;

        foreach (var p in productos)
        {
            var lotesActivos = p.Lotes.Where(l => l.CantidadDisponible > 0).ToList();
            decimal stockTotal = lotesActivos.Sum(l => l.CantidadDisponible);

            string estado = "Óptimo";
            if (lotesActivos.Any(l => l.FechaVencimiento <= hoy))
            {
                estado = "Vencido";
            }
            else if (lotesActivos.Any(l => l.FechaVencimiento <= hoy.AddDays(30)))
            {
                estado = "Próximo a vencer";
            }
            else if (stockTotal <= p.StockMinimo)
            {
                estado = "Atención";
            }

            if (!string.IsNullOrEmpty(estadoLote) && !estado.Equals(estadoLote, StringComparison.OrdinalIgnoreCase))
                continue;

            resultado.Add(new ReporteInventarioItemDto
            {
                Producto = p.Nombre,
                CodigoEan = p.CodigoBarras ?? "N/A",
                Categoria = p.Categoria?.Nombre ?? "Sin Categoría",
                StockTotal = stockTotal,
                UnidadMedida = p.UnidadMedida.ToString(),
                CantidadLotes = lotesActivos.Count,
                Estado = estado
            });
        }

        return resultado.OrderBy(r => r.Producto).ToList();
    }

    public async Task<List<ReporteDonacionItemDto>> ObtenerReporteDonacionesAsync(DateTime? desde, DateTime? hasta, int? donanteId)
    {
        var query = _context.Donaciones
            .AsNoTracking()
            .Include(d => d.Donante)
            .Include(d => d.Detalles)
            .AsQueryable();

        if (desde.HasValue)
            query = query.Where(d => d.FechaHora >= desde.Value);

        if (hasta.HasValue)
            query = query.Where(d => d.FechaHora <= hasta.Value.AddDays(1).AddTicks(-1));

        if (donanteId.HasValue)
            query = query.Where(d => d.DonanteId == donanteId.Value);

        return await query
            .OrderByDescending(d => d.FechaHora)
            .Select(d => new ReporteDonacionItemDto
            {
                Comprobante = $"DON-{d.FechaHora.Year}-{d.Id:D5}",
                FechaHora = d.FechaHora,
                Donante = d.Donante != null ? d.Donante.NombreRazonSocial : "Donante Anónimo",
                Documento = d.Donante != null ? (d.Donante.NumeroDocumento ?? "N/A") : "N/A",
                TipoDonante = d.Donante != null ? d.Donante.Tipo.ToString() : "Anónimo",
                CantidadItems = d.Detalles.Count,
                CantidadTotal = d.Detalles.Sum(dt => dt.Cantidad),
                Receptor = "Voluntario Receptor"
            })
            .ToListAsync();
    }

    public async Task<List<ReporteEntregaItemDto>> ObtenerReporteEntregasAsync(DateTime? desde, DateTime? hasta, int? tipoPaqueteId, string? voluntarioId)
    {
        var query = _context.Entregas
            .AsNoTracking()
            .Include(e => e.Paquete).ThenInclude(p => p.FamiliaBeneficiaria)
            .Include(e => e.Paquete).ThenInclude(p => p.TipoPaquete)
            .Include(e => e.Paquete).ThenInclude(p => p.Detalles)
            .AsQueryable();

        if (desde.HasValue)
            query = query.Where(e => e.FechaHoraEntrega >= desde.Value);

        if (hasta.HasValue)
            query = query.Where(e => e.FechaHoraEntrega <= hasta.Value.AddDays(1).AddTicks(-1));

        if (tipoPaqueteId.HasValue)
            query = query.Where(e => e.Paquete.TipoPaqueteId == tipoPaqueteId.Value);

        if (!string.IsNullOrEmpty(voluntarioId))
            query = query.Where(e => e.VoluntarioDespachoId == voluntarioId);

        return await query
            .OrderByDescending(e => e.FechaHoraEntrega)
            .Select(e => new ReporteEntregaItemDto
            {
                CodigoPaquete = e.Paquete != null ? e.Paquete.CodigoSeguimiento : "N/A",
                TipoKit = e.Paquete != null && e.Paquete.TipoPaquete != null ? e.Paquete.TipoPaquete.Nombre : "Familiar",
                FechaEntrega = e.FechaHoraEntrega,
                Familia = e.Paquete != null && e.Paquete.FamiliaBeneficiaria != null ? $"{e.Paquete.FamiliaBeneficiaria.ApellidoTitular}, {e.Paquete.FamiliaBeneficiaria.NombreTitular}" : "N/A",
                DniTitular = e.Paquete != null && e.Paquete.FamiliaBeneficiaria != null ? e.Paquete.FamiliaBeneficiaria.DniTitular : "N/A",
                Voluntario = e.VoluntarioDespachoId ?? "Voluntario General",
                TotalAlimentos = e.Paquete != null ? e.Paquete.Detalles.Count : 0
            })
            .ToListAsync();
    }

    public async Task<List<ReporteFamiliaItemDto>> ObtenerReporteFamiliasAsync(string? busqueda)
    {
        var query = _context.FamiliasBeneficiarias
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var term = busqueda.Trim().ToLower();
            query = query.Where(f =>
                EF.Functions.Like(f.ApellidoTitular.ToLower(), $"%{term}%") ||
                EF.Functions.Like(f.NombreTitular.ToLower(), $"%{term}%") ||
                EF.Functions.Like(f.DniTitular, $"%{term}%")
            );
        }

        var familias = await query.ToListAsync();

        return familias.Select(f => new ReporteFamiliaItemDto
        {
            Familia = $"{f.ApellidoTitular}, {f.NombreTitular}",
            DniTitular = f.DniTitular,
            Direccion = f.Direccion ?? "Sin dirección",
            Integrantes = f.CantidadIntegrantes,
            TotalKitsRecibidos = 0,
            UltimaEntrega = null,
            Estado = "Activa" // Estado predeterminado o mapeado según la propiedad de tu entidad
        }).OrderBy(f => f.Familia).ToList();
    }

    public async Task<List<ReporteTrazabilidadItemDto>> ObtenerReporteTrazabilidadAsync(DateTime? desde, DateTime? hasta, string? busqueda)
    {
        var query = _context.Entregas
            .AsNoTracking()
            .Include(e => e.Paquete).ThenInclude(p => p.FamiliaBeneficiaria)
            .Include(e => e.Paquete).ThenInclude(p => p.Detalles).ThenInclude(d => d.Lote).ThenInclude(l => l.Producto)
            .AsQueryable();

        if (desde.HasValue)
            query = query.Where(e => e.FechaHoraEntrega >= desde.Value);

        if (hasta.HasValue)
            query = query.Where(e => e.FechaHoraEntrega <= hasta.Value.AddDays(1).AddTicks(-1));

        var entregas = await query.Take(50).ToListAsync();
        var resultado = new List<ReporteTrazabilidadItemDto>();

        foreach (var e in entregas)
        {
            if (e.Paquete?.Detalles != null)
            {
                foreach (var d in e.Paquete.Detalles)
                {
                    resultado.Add(new ReporteTrazabilidadItemDto
                    {
                        CodigoPaquete = e.Paquete?.CodigoSeguimiento ?? "N/A",
                        Alimento = d.Lote?.Producto?.Nombre ?? "Alimento",
                        NumeroLote = d.Lote?.NumeroLote ?? "N/A",
                        DonanteOrigen = "Donante Registrado",
                        FamiliaBeneficiaria = e.Paquete?.FamiliaBeneficiaria != null ? $"{e.Paquete.FamiliaBeneficiaria.ApellidoTitular}, {e.Paquete.FamiliaBeneficiaria.NombreTitular}" : "N/A",
                        FechaEntrega = e.FechaHoraEntrega
                    });
                }
            }
        }

        return resultado;
    }
}