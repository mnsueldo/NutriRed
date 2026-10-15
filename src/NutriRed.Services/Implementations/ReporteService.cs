using Microsoft.EntityFrameworkCore;
using NutriRed.Data;
using NutriRed.Domain.Entities;
using NutriRed.Services.DTOs;
using NutriRed.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NutriRed.Services.Implementations;

public class ReporteService : IReporteService
{
    private readonly NutriRedDbContext _context;

    public ReporteService(NutriRedDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReporteInventarioItemDto>> ObtenerReporteInventarioAsync(
        int? categoriaId,
        string? busquedaInventario,
        string? estadoStock,
        string? semaforoFefo)
    {
        var query = _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Include(p => p.Lotes)
            .Where(p => p.Activo)
            .AsQueryable();

        if (categoriaId.HasValue)
            query = query.Where(p => p.CategoriaId == categoriaId.Value);

        if (!string.IsNullOrWhiteSpace(busquedaInventario))
        {
            var term = busquedaInventario.Trim().ToLower();
            query = query.Where(p =>
                (p.Nombre != null && EF.Functions.Like(p.Nombre.ToLower(), $"%{term}%")) ||
                (p.CodigoBarras != null && EF.Functions.Like(p.CodigoBarras.ToLower(), $"%{term}%"))
            );
        }

        var productos = await query.ToListAsync();
        var resultado = new List<ReporteInventarioItemDto>();
        var hoy = DateTime.Today;

        foreach (var p in productos)
        {
            var lotesDisponibles = p.Lotes.Where(l => l.CantidadDisponible > 0).ToList();

            if (!lotesDisponibles.Any())
            {
                string situacionStockAgotado = "Agotado / Sin stock";
                string estadoFefoAgotado = "Sin Stock";

                if (!string.IsNullOrEmpty(estadoStock) && !situacionStockAgotado.Equals(estadoStock, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(semaforoFefo) && !semaforoFefo.Contains("Sin Stock", StringComparison.OrdinalIgnoreCase))
                    continue;

                resultado.Add(new ReporteInventarioItemDto
                {
                    Producto = p.Nombre,
                    CodigoEan = p.CodigoBarras ?? "N/A",
                    Categoria = p.Categoria?.Nombre ?? "Sin Categoría",
                    NumeroLote = "Sin Lotes Aptos",
                    FechaVencimiento = DateTime.MinValue,
                    FechaIngreso = DateTime.MinValue,
                    UsuarioIngreso = "-",
                    StockDisponible = 0,
                    UnidadMedida = p.UnidadMedida.ToString(),
                    Estado = estadoFefoAgotado
                });

                continue;
            }

            foreach (var l in lotesDisponibles)
            {
                int diasRestantes = (l.FechaVencimiento - hoy).Days;

                string estadoFefo = "Óptimo (> 15 días)";
                if (diasRestantes <= 0) estadoFefo = "Crítico / Vencido";
                else if (diasRestantes <= 5) estadoFefo = "Urgente (<= 5 días)";
                else if (diasRestantes <= 15) estadoFefo = "Atención (<= 15 días)";

                if (!string.IsNullOrEmpty(semaforoFefo) && !estadoFefo.Equals(semaforoFefo, StringComparison.OrdinalIgnoreCase))
                    continue;

                string situacionStock = l.CantidadDisponible <= p.StockMinimo ? "Bajo stock mínimo" : "Con stock suficiente";

                if (!string.IsNullOrEmpty(estadoStock) && !situacionStock.Equals(estadoStock, StringComparison.OrdinalIgnoreCase))
                    continue;

                resultado.Add(new ReporteInventarioItemDto
                {
                    Producto = p.Nombre,
                    CodigoEan = p.CodigoBarras ?? "N/A",
                    Categoria = p.Categoria?.Nombre ?? "Sin Categoría",
                    NumeroLote = l.NumeroLote,
                    FechaVencimiento = l.FechaVencimiento,
                    FechaIngreso = l.FechaIngreso,
                    UsuarioIngreso = "Voluntario / Sistema",
                    StockDisponible = l.CantidadDisponible,
                    UnidadMedida = p.UnidadMedida.ToString(),
                    Estado = estadoFefo
                });
            }
        }

        return resultado.OrderBy(r => r.Producto).ThenBy(r => r.FechaVencimiento).ToList();
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
            query = query.Where(d => d.FechaHora <= hasta.Value.AddDays(1).AddSeconds(-1));

        if (donanteId.HasValue)
            query = query.Where(d => d.DonanteId == donanteId.Value);

        var donaciones = await query.OrderByDescending(d => d.FechaHora).ToListAsync();

        return donaciones.Select(d => new ReporteDonacionItemDto
        {
            Id = d.Id,
            Comprobante = d.CodigoComprobante ?? $"DON-{d.Id:D5}",
            FechaHora = d.FechaHora,
            Donante = d.Donante != null ? d.Donante.NombreRazonSocial : "Donante Anónimo",
            Documento = d.Donante != null ? (d.Donante.NumeroDocumento ?? "N/A") : "N/A",
            TipoDonante = d.Donante != null ? d.Donante.Tipo.ToString() : "Individuo",
            CantidadItems = d.Detalles?.Count ?? 0,
            CantidadTotal = d.Detalles?.Sum(dt => dt.Cantidad) ?? 0,
            Receptor = d.VoluntarioReceptorId ?? "Sistema"
        }).ToList();
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
            query = query.Where(e => e.Paquete != null && e.Paquete.TipoPaqueteId == tipoPaqueteId.Value);

        if (!string.IsNullOrEmpty(voluntarioId))
            query = query.Where(e => e.VoluntarioDespachoId == voluntarioId);

        var entregas = await query.OrderByDescending(e => e.FechaHoraEntrega).ToListAsync();

        return entregas.Select(e => new ReporteEntregaItemDto
        {
            Id = e.PaqueteId,
            CodigoPaquete = e.Paquete != null ? e.Paquete.CodigoSeguimiento : "N/A",
            TipoKit = e.Paquete != null && e.Paquete.TipoPaquete != null ? e.Paquete.TipoPaquete.Nombre : "Familiar",
            FechaEntrega = e.FechaHoraEntrega,
            Familia = e.Paquete != null && e.Paquete.FamiliaBeneficiaria != null ? $"{e.Paquete.FamiliaBeneficiaria.ApellidoTitular}, {e.Paquete.FamiliaBeneficiaria.NombreTitular}" : "N/A",
            DniTitular = e.Paquete != null && e.Paquete.FamiliaBeneficiaria != null ? e.Paquete.FamiliaBeneficiaria.DniTitular : "N/A",
            Voluntario = e.VoluntarioDespachoId ?? "Voluntario General",
            TotalAlimentos = e.Paquete != null ? e.Paquete.Detalles.Count : 0
        }).ToList();
    }

    public async Task<List<ReporteFamiliaItemDto>> ObtenerReporteFamiliasAsync(DateTime? desde, DateTime? hasta, string? busqueda)
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
        var familiaIds = familias.Select(f => f.Id).ToList();

        var entregaQuery = _context.Entregas
            .AsNoTracking()
            .Include(e => e.Paquete)
                .ThenInclude(p => p.FamiliaBeneficiaria)
            .Where(e => e.Paquete != null && e.Paquete.FamiliaBeneficiaria != null && familiaIds.Contains(e.Paquete.FamiliaBeneficiaria.Id))
            .AsQueryable();

        if (desde.HasValue)
            entregaQuery = entregaQuery.Where(e => e.FechaHoraEntrega >= desde.Value);

        if (hasta.HasValue)
            entregaQuery = entregaQuery.Where(e => e.FechaHoraEntrega <= hasta.Value.AddDays(1).AddTicks(-1));

        var entregas = await entregaQuery.ToListAsync();

        var entregasPorFamilia = entregas
            .Where(e => e.Paquete?.FamiliaBeneficiaria != null)
            .GroupBy(e => e.Paquete!.FamiliaBeneficiaria.Id)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.FechaHoraEntrega).ToList());

        return familias.Select(f =>
        {
            entregasPorFamilia.TryGetValue(f.Id, out var entregasFamilia);
            entregasFamilia ??= new List<Entrega>();

            int totalKits = entregasFamilia.Count;
            DateTime? ultimaEntrega = entregasFamilia.FirstOrDefault()?.FechaHoraEntrega;

            return new ReporteFamiliaItemDto
            {
                Familia = $"{f.ApellidoTitular}, {f.NombreTitular}",
                DniTitular = f.DniTitular,
                Direccion = f.Direccion ?? "Sin dirección",
                Integrantes = f.CantidadIntegrantes,
                TotalKitsRecibidos = totalKits,
                UltimaEntrega = ultimaEntrega,
                Estado = "Activa"
            };
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

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var term = busqueda.Trim().ToLower();
            query = query.Where(e =>
                (e.Paquete != null && e.Paquete.CodigoSeguimiento != null && EF.Functions.Like(e.Paquete.CodigoSeguimiento.ToLower(), $"%{term}%")) ||
                (e.Paquete != null && e.Paquete.FamiliaBeneficiaria != null && (EF.Functions.Like(e.Paquete.FamiliaBeneficiaria.ApellidoTitular.ToLower(), $"%{term}%") || EF.Functions.Like(e.Paquete.FamiliaBeneficiaria.DniTitular, $"%{term}%")))
            );
        }

        var entregas = await query.ToListAsync();
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