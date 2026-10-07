using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.DTOs;
using NutriRed.Services.Interfaces;
using NutriRed.Web.Models;

namespace NutriRed.Web.Controllers;

[Authorize]
public class InventarioController : Controller
{
    private readonly IInventarioService _inventarioService;
    private readonly IProductoService _productoService;
    private readonly ICategoriaService _categoriaService;

    public InventarioController(
        IInventarioService inventarioService,
        IProductoService productoService,
        ICategoriaService categoriaService)
    {
        _inventarioService = inventarioService;
        _productoService = productoService;
        _categoriaService = categoriaService;
    }

    // GET: /Inventario / /Inventario/Index (Stock Consolidado & FEFO)
    public async Task<IActionResult> Index(int diasAlerta = 30)
    {
        var resultadoStock = await _inventarioService.ObtenerStockConsolidadoAsync();
        var stockConsolidado = resultadoStock.Data ?? Enumerable.Empty<StockProductoDto>();

        // Obtener las categorías activas a través del servicio de categorías
        var resultadoCategorias = await _categoriaService.ObtenerTodasAsync(soloActivas: true);
        var categorias = resultadoCategorias.Data?
            .Select(c => c.Nombre)
            .OrderBy(n => n)
            .ToList() ?? new List<string>();

        ViewBag.Categorias = categorias;

        return View(stockConsolidado);
    }

    // GET: /Inventario/Historial?productoId=5
    public async Task<IActionResult> Historial(int productoId)
    {
        var resultadoProducto = await _productoService.ObtenerPorIdAsync(productoId);
        if (!resultadoProducto.Success || resultadoProducto.Data == null)
        {
            TempData["Error"] = resultadoProducto.ErrorMessage ?? "El producto especificado no existe.";
            return RedirectToAction(nameof(Index));
        }

        var resultadoMovimientos = await _inventarioService.ObtenerMovimientosPorProductoAsync(productoId);
        var movimientos = resultadoMovimientos.Data ?? Enumerable.Empty<MovimientoStock>();

        ViewBag.Producto = resultadoProducto.Data;
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
            Observaciones = model.Observaciones?.Trim(),
            UsuarioId = User.Identity?.Name ?? "operador_deposito"
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
        var resultadoProducto = await _productoService.ObtenerPorIdAsync(productoId);
        var unidad = resultadoProducto.Data?.UnidadMedida.ToString() ?? "Unidades";

        var resultadoLotes = await _inventarioService.ObtenerLotesPorProductoAsync(productoId, soloDisponibles: true);
        var lotes = (resultadoLotes.Data ?? Enumerable.Empty<Lote>())
            .Select(l => new
            {
                id = l.Id,
                texto = $"[{l.NumeroLote}] — Disponible: {l.CantidadDisponible:N0} {unidad} (Vence: {l.FechaVencimiento:dd/MM/yyyy})",
                disponible = (int)l.CantidadDisponible,
                unidadMedida = unidad
            })
            .ToList();

        return Json(new { unidad, lotes });
    }

    /// <summary>
    /// Recarga defensiva de listas desplegables utilizando servicios de negocio (desacoplado de DbContext)
    /// </summary>
    private async Task CargarDesplegablesMermaAsync(int? productoIdSeleccionado = null, int? loteSeleccionadoId = null)
    {
        var resultadoProductos = await _productoService.ObtenerTodosAsync(soloActivos: true);
        var productos = (resultadoProductos.Data ?? Enumerable.Empty<Producto>())
            .OrderBy(p => p.Nombre)
            .Select(p => new
            {
                Id = p.Id,
                Texto = $"{p.Nombre} (EAN: {p.CodigoBarras})"
            })
            .ToList();

        ViewBag.ProductosSelectList = new SelectList(productos, "Id", "Texto", productoIdSeleccionado);

        if (productoIdSeleccionado.HasValue)
        {
            var resultadoProducto = await _productoService.ObtenerPorIdAsync(productoIdSeleccionado.Value);
            var unidad = resultadoProducto.Data?.UnidadMedida.ToString() ?? "Unidades";

            var resultadoLotes = await _inventarioService.ObtenerLotesPorProductoAsync(productoIdSeleccionado.Value, soloDisponibles: true);
            var lotes = (resultadoLotes.Data ?? Enumerable.Empty<Lote>())
                .Select(l => new
                {
                    Id = l.Id,
                    Texto = $"[{l.NumeroLote}] — Disponible: {l.CantidadDisponible:N0} {unidad} (Vence: {l.FechaVencimiento:dd/MM/yyyy})"
                })
                .ToList();

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