using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NutriRed.Domain.Entities;
using NutriRed.Services.Interfaces;

namespace NutriRed.Web.Controllers;

public class ProductosController : Controller
{
    private readonly IProductoService _productoService;
    private readonly ICategoriaService _categoriaService;
    private readonly IInventarioService _inventarioService;

    public ProductosController(
        IProductoService productoService,
        ICategoriaService categoriaService,
        IInventarioService inventarioService)
    {
        _productoService = productoService;
        _categoriaService = categoriaService;
        _inventarioService = inventarioService;
    }

    // GET: /Productos
    public async Task<IActionResult> Index(string? busqueda, int? categoriaId, bool verTodos = false)
    {
        var productosResult = await _productoService.ObtenerTodosAsync(soloActivos: !verTodos);

        if (!productosResult.Success)
        {
            TempData["Error"] = productosResult.ErrorMessage;
            return View(Enumerable.Empty<Producto>());
        }

        var productos = productosResult.Data ?? Enumerable.Empty<Producto>();

        // Filtro por término de búsqueda (Nombre o Código de Barras EAN)
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim();
            productos = productos.Where(p =>
                p.Nombre.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                p.CodigoBarras.Contains(termino, StringComparison.OrdinalIgnoreCase)
            );
        }

        // Filtro por Categoría
        if (categoriaId.HasValue && categoriaId.Value > 0)
        {
            productos = productos.Where(p => p.CategoriaId == categoriaId.Value);
        }

        // Cargamos categorías para el selector de filtro en la vista
        var categoriasResult = await _categoriaService.ObtenerTodasAsync(soloActivas: true);
        var categorias = categoriasResult.Success && categoriasResult.Data != null
            ? categoriasResult.Data
            : Enumerable.Empty<Categoria>();

        ViewBag.CategoriasFiltro = new SelectList(categorias, nameof(Categoria.Id), nameof(Categoria.Nombre), categoriaId);
        ViewBag.Busqueda = busqueda;
        ViewBag.CategoriaId = categoriaId;
        ViewBag.VerTodos = verTodos;

        return View(productos.OrderBy(p => p.Nombre).ToList());
    }

    // GET: /Productos/Detalles/5
    public async Task<IActionResult> Detalles(int? id)
    {
        if (!id.HasValue)
        {
            TempData["Error"] = "Identificador de producto no válido.";
            return RedirectToAction(nameof(Index));
        }

        var productoResult = await _productoService.ObtenerPorIdAsync(id.Value);

        if (!productoResult.Success || productoResult.Data == null)
        {
            TempData["Error"] = productoResult.ErrorMessage ?? "Alimento no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        // Consultamos los lotes físicos asociados a este alimento
        var lotesResult = await _inventarioService.ObtenerLotesPorProductoAsync(id.Value, soloDisponibles: false);
        var lotesList = lotesResult.Success && lotesResult.Data != null 
            ? lotesResult.Data.OrderBy(l => l.FechaVencimiento).Select(l => new Models.LoteItemViewModel
            {
                Id = l.Id,
                NumeroLote = l.NumeroLote,
                FechaVencimiento = l.FechaVencimiento,
                CantidadDisponible = l.CantidadDisponible,
                CantidadInicial = l.CantidadInicial,
                Estado = l.Estado
            }).ToList() 
            : new List<Models.LoteItemViewModel>();

        var viewModel = new Models.ProductoDetalleViewModel
        {
            Producto = productoResult.Data,
            Lotes = lotesList
        };

        return View(viewModel);
    }

    // GET: /Productos/Create
    public async Task<IActionResult> Create()
    {
        await CargarCategoriasSelectListAsync();
        return View(new Producto 
        { 
            Activo = true, 
            StockMinimo = 10,
            UnidadMedida = Domain.Enums.UnidadMedida.Kilogramos 
        });
    }

    // POST: /Productos/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Producto producto)
    {
        if (!ModelState.IsValid)
        {
            // Buenas prácticas del Tutor: Recargar la lista desplegable ante error de validación
            await CargarCategoriasSelectListAsync(producto.CategoriaId);
            return View(producto);
        }

        var result = await _productoService.CrearAsync(producto);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Error al registrar el alimento.");
            await CargarCategoriasSelectListAsync(producto.CategoriaId);
            return View(producto);
        }

        TempData["Exito"] = $"Alimento '{result.Data?.Nombre}' incorporado al catálogo exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Productos/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            TempData["Error"] = "Identificador de producto no válido.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _productoService.ObtenerPorIdAsync(id.Value);

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.ErrorMessage ?? "Alimento no encontrado en el catálogo.";
            return RedirectToAction(nameof(Index));
        }

        await CargarCategoriasSelectListAsync(result.Data.CategoriaId);
        return View(result.Data);
    }

    // POST: /Productos/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Producto producto)
    {
        if (id != producto.Id)
        {
            TempData["Error"] = "El identificador de ruta no coincide con el del producto.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            await CargarCategoriasSelectListAsync(producto.CategoriaId);
            return View(producto);
        }

        var result = await _productoService.ActualizarAsync(producto);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Error al actualizar el alimento.");
            await CargarCategoriasSelectListAsync(producto.CategoriaId);
            return View(producto);
        }

        TempData["Exito"] = $"Alimento '{result.Data?.Nombre}' actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Productos/CambiarEstado/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, bool nuevoEstado)
    {
        var result = nuevoEstado 
            ? await _productoService.ActivarAsync(id) 
            : await _productoService.DesactivarAsync(id);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Exito"] = nuevoEstado 
                ? "El alimento ha sido reactivado en el catálogo." 
                : "El alimento ha sido desactivado (baja lógica) preservando sus lotes históricos.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task CargarCategoriasSelectListAsync(int? categoriaSeleccionadaId = null)
    {
        var categoriasResult = await _categoriaService.ObtenerTodasAsync(soloActivas: true);
        var categorias = categoriasResult.Success && categoriasResult.Data != null
            ? categoriasResult.Data
            : Enumerable.Empty<Categoria>();

        ViewBag.Categorias = new SelectList(categorias, nameof(Categoria.Id), nameof(Categoria.Nombre), categoriaSeleccionadaId);
    }
}
