using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NutriRed.Data;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.DTOs;
using NutriRed.Services.Interfaces;
using NutriRed.Web.Models;

namespace NutriRed.Web.Controllers;

public class InventarioController : Controller
{
    private readonly IInventarioService _inventarioService;
    private readonly NutriRedDbContext _context;

    public InventarioController(IInventarioService inventarioService, NutriRedDbContext context)
    {
        _inventarioService = inventarioService;
        _context = context;
    }

    // GET: /Inventario / /Inventario/Index (Stock Consolidado & FEFO)
    public async Task<IActionResult> Index(int diasAlerta = 30)
    {
        var resultadoStock = await _inventarioService.ObtenerStockConsolidadoAsync();
        var stockConsolidado = resultadoStock.Data ?? Enumerable.Empty<StockProductoDto>();

        // Obtener las categorías de la base de datos para cargar el desplegable de filtro
        var categorias = await _context.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => c.Nombre)
            .ToListAsync();

        ViewBag.Categorias = categorias;

        return View(stockConsolidado);
    }

    // GET: /Inventario/Historial?productoId=5
    public async Task<IActionResult> Historial(int productoId)
    {
        var producto = await _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == productoId);

        if (producto == null)
        {
            TempData["Error"] = "El producto especificado no existe.";
            return RedirectToAction(nameof(Index));
        }

        var movimientos = await _context.MovimientosStock
            .AsNoTracking()
            .Include(m => m.Lote)
            .Where(m => m.Lote != null && m.Lote.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();

        ViewBag.Producto = producto;
        return View(movimientos);
    }

    // GET: /Inventario/RegistrarMerma
    public async Task<IActionResult> RegistrarMerma(int? loteId)
    {
        var model = new RegistrarMermaViewModel();

        if (loteId.HasValue)
        {
            var resultadoLote = await _inventarioService.ObtenerLotePorIdAsync(loteId.Value);
            if (resultadoLote.Success && resultadoLote.Data != null)
            {
                var lote = resultadoLote.Data;
                model.ProductoId = lote.ProductoId;
                model.LoteId = lote.Id;
                model.CodigoLote = lote.NumeroLote;
                model.NombreProducto = lote.Producto?.Nombre;
                model.ExistenciaActual = lote.CantidadDisponible;
            }
        }

        await CargarDesplegablesMermaAsync(model.ProductoId, model.LoteId);
        return View(model);
    }

    // POST: /Inventario/RegistrarMerma
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarMerma(RegistrarMermaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await CargarDesplegablesMermaAsync(model.ProductoId, model.LoteId);
            return View(model);
        }

        var request = new AjusteMermaRequest
        {
            LoteId = model.LoteId,
            Cantidad = model.Cantidad,
            TipoMovimiento = TipoMovimiento.BajaPorMerma,
            Motivo = model.Motivo,
            Observaciones = model.Observaciones,
            UsuarioId = null
        };

        var resultado = await _inventarioService.RegistrarAjusteOMermaAsync(request);

        if (resultado.Success)
        {
            TempData["Exito"] = "La baja/merma de stock fue asentada correctamente en la auditoría del sistema.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, resultado.ErrorMessage ?? "Ocurrió un error al procesar la baja de stock.");
        await CargarDesplegablesMermaAsync(model.ProductoId, model.LoteId);
        return View(model);
    }

    // GET: /Inventario/ObtenerLotesPorProducto?productoId=5
    [HttpGet]
    public async Task<IActionResult> ObtenerLotesPorProducto(int productoId)
    {
        var lotes = await _context.Lotes
            .AsNoTracking()
            .Where(l => l.ProductoId == productoId && l.CantidadDisponible > 0)
            .OrderBy(l => l.FechaVencimiento)
            .Select(l => new
            {
                id = l.Id,
                texto = $"[{l.NumeroLote}] — Disponible: {l.CantidadDisponible} (Vence: {l.FechaVencimiento:dd/MM/yyyy})"
            })
            .ToListAsync();

        return Json(lotes);
    }

    private async Task CargarDesplegablesMermaAsync(int? productoIdSeleccionado = null, int? loteSeleccionadoId = null)
    {
        var productos = await _context.Productos
            .AsNoTracking()
            .Where(p => p.Lotes.Any(l => l.CantidadDisponible > 0))
            .OrderBy(p => p.Nombre)
            .Select(p => new
            {
                Id = p.Id,
                Texto = $"{p.Nombre} (EAN: {p.CodigoBarras})"
            })
            .ToListAsync();

        ViewBag.ProductosSelectList = new SelectList(productos, "Id", "Texto", productoIdSeleccionado);

        if (productoIdSeleccionado.HasValue)
        {
            var lotes = await _context.Lotes
                .AsNoTracking()
                .Where(l => l.ProductoId == productoIdSeleccionado.Value && l.CantidadDisponible > 0)
                .OrderBy(l => l.FechaVencimiento)
                .Select(l => new
                {
                    Id = l.Id,
                    Texto = $"[{l.NumeroLote}] — Disponible: {l.CantidadDisponible} (Vence: {l.FechaVencimiento:dd/MM/yyyy})"
                })
                .ToListAsync();

            ViewBag.LotesSelectList = new SelectList(lotes, "Id", "Texto", loteSeleccionadoId);
        }
        else
        {
            ViewBag.LotesSelectList = new SelectList(Enumerable.Empty<object>(), "Id", "Texto");
        }

        var motivosValidos = new[] {
            MotivoMovimiento.Vencido,
            MotivoMovimiento.Deteriorado,
            MotivoMovimiento.RoturaEnvase,
            MotivoMovimiento.AjusteInventario
        };

        ViewBag.MotivosSelectList = new SelectList(
            motivosValidos.Select(m => new { Id = (int)m, Nombre = m.ToString() }),
            "Id",
            "Nombre"
        );
    }
}