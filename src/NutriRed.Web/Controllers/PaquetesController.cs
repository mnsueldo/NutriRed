using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.Common;
using NutriRed.Services.DTOs;
using NutriRed.Services.Interfaces;
using NutriRed.Web.Models;

namespace NutriRed.Web.Controllers;

[Authorize]
public class PaquetesController : Controller
{
    private readonly IPaqueteService _paqueteService;
    private readonly IFamiliaService _familiaService;
    private readonly IProductoService _productoService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PaquetesController> _logger;

    public PaquetesController(
        IPaqueteService paqueteService,
        IFamiliaService familiaService,
        IProductoService productoService,
        UserManager<ApplicationUser> userManager,
        ILogger<PaquetesController> logger)
    {
        _paqueteService = paqueteService;
        _familiaService = familiaService;
        _productoService = productoService;
        _userManager = userManager;
        _logger = logger;
    }

    // GET: Paquetes
    public async Task<IActionResult> Index(EstadoPaquete? estado = null, string? codigo = null, int pagina = 1)
    {
        var viewModel = new PaqueteIndexViewModel
        {
            EstadoFiltro = estado,
            CodigoSeguimiento = codigo?.Trim()
        };

        if (!string.IsNullOrWhiteSpace(viewModel.CodigoSeguimiento))
        {
            var singleResult = await _paqueteService.ObtenerPorCodigoSeguimientoAsync(viewModel.CodigoSeguimiento);
            if (singleResult.Success && singleResult.Data != null)
            {
                viewModel.Paquetes = new List<PaqueteDto> { singleResult.Data };
            }
            else
            {
                TempData["Advertencia"] = $"No se encontró ningún paquete con el código de seguimiento '{viewModel.CodigoSeguimiento}'.";
                viewModel.Paquetes = Enumerable.Empty<PaqueteDto>();
            }
        }
        else
        {
            var result = await _paqueteService.ObtenerTodosAsync(estado);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.ErrorMessage ?? "No se pudieron obtener los paquetes armados.";
                viewModel.Paquetes = Enumerable.Empty<PaqueteDto>();
            }
            else
            {
                viewModel.Paquetes = result.Data;
            }
        }

        // Obtener totales globales estables para el panel superior de métricas
        var todosResult = await _paqueteService.ObtenerTodosAsync(null);
        var todos = todosResult.Success && todosResult.Data != null ? todosResult.Data.ToList() : new List<PaqueteDto>();
        viewModel.TotalGlobal = todos.Count;
        viewModel.PreparadosGlobal = todos.Count(p => p.Estado == EstadoPaquete.Preparado);
        viewModel.EntregadosGlobal = todos.Count(p => p.Estado == EstadoPaquete.Entregado);
        viewModel.CanceladosGlobal = todos.Count(p => p.Estado == EstadoPaquete.Cancelado);

        var listaPaquetes = viewModel.Paquetes.ToList();
        var (itemsPaginados, paginacion) = listaPaquetes.Paginar(
            pagina,
            tamanoPagina: 10,
            accion: nameof(Index),
            controlador: "Paquetes",
            parametrosRuta: new Dictionary<string, string>
            {
                ["estado"] = estado?.ToString() ?? "",
                ["codigo"] = codigo ?? ""
            }
        );

        viewModel.Paquetes = itemsPaginados;
        ViewBag.Paginacion = paginacion;

        return View(viewModel);
    }

    // GET: Paquetes/Armar?familiaId=5&tipoPaqueteId=1
    public async Task<IActionResult> Armar(int? familiaId = null, int? tipoPaqueteId = null)
    {
        await CargarListasEnViewBagAsync();

        var user = await _userManager.GetUserAsync(User);
        var nombreOperador = user?.NombreCompleto ?? User.Identity?.Name ?? "Operador Armado";

        var model = new ArmarPaqueteViewModel
        {
            FamiliaId = familiaId,
            TipoPaqueteId = tipoPaqueteId,
            UsuarioArmadorId = nombreOperador
        };

        if (familiaId.HasValue && familiaId.Value > 0)
        {
            var sugerenciaResult = await _paqueteService.SugerirPaqueteParaFamiliaAsync(familiaId.Value, tipoPaqueteId);

            if (!sugerenciaResult.Success || sugerenciaResult.Data == null)
            {
                TempData["Advertencia"] = sugerenciaResult.ErrorMessage ?? "No se pudo generar la propuesta de kit para la familia.";
            }
            else
            {
                model.Propuesta = sugerenciaResult.Data;
                model.TipoPaqueteId = sugerenciaResult.Data.TipoPaqueteId;

                // Precargar los ítems sugeridos con FEFO
                model.Items = sugerenciaResult.Data.Items.Select(i => new ItemArmadoViewModel
                {
                    ProductoId = i.ProductoId,
                    NombreProducto = i.NombreProducto,
                    CodigoBarras = i.CodigoBarras,
                    CategoriaId = i.CategoriaId,
                    CategoriaNombre = i.CategoriaNombre,
                    Cantidad = i.CantidadRequerida,
                    EsSustituto = false,
                    ProductoSustituidoId = null
                }).ToList();
            }
        }

        return View(model);
    }

    // GET: Paquetes/ObtenerPropuestaJson?familiaId=5&tipoPaqueteId=1
    [HttpGet]
    public async Task<IActionResult> ObtenerPropuestaJson(int familiaId, int? tipoPaqueteId = null)
    {
        if (familiaId <= 0)
        {
            return Json(new { success = false, errorMessage = "Debe seleccionar una familia válida." });
        }

        var result = await _paqueteService.SugerirPaqueteParaFamiliaAsync(familiaId, tipoPaqueteId);
        return Json(result);
    }

    // POST: Paquetes/ConfirmarArmado
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarArmado(ArmarPaqueteViewModel model)
    {
        if (!model.FamiliaId.HasValue || model.FamiliaId.Value <= 0)
        {
            ModelState.AddModelError(nameof(model.FamiliaId), "Debe seleccionar una familia beneficiaria.");
        }

        // Fallback Inteligente (Opción 3): Si los ítems no vienen en el payload
        // (por ejemplo, el usuario seleccionó la familia y dio clic a Confirmar directamente sin simular):
        if ((model.Items == null || !model.Items.Any()) && model.FamiliaId.HasValue && model.FamiliaId.Value > 0)
        {
            var sugerenciaResult = await _paqueteService.SugerirPaqueteParaFamiliaAsync(model.FamiliaId.Value, model.TipoPaqueteId);
            if (!sugerenciaResult.Success || sugerenciaResult.Data == null)
            {
                ModelState.AddModelError(string.Empty, sugerenciaResult.ErrorMessage ?? "No se pudo determinar el kit para la familia.");
                await CargarListasEnViewBagAsync();
                return View(nameof(Armar), model);
            }

            model.Propuesta = sugerenciaResult.Data;
            model.TipoPaqueteId = sugerenciaResult.Data.TipoPaqueteId;

            // Si hay faltantes de stock, no se puede confirmar automáticamente:
            // mostramos la advertencia y desplegamos la pantalla para sustitución:
            if (sugerenciaResult.Data.HayFaltantes)
            {
                ModelState.AddModelError(string.Empty, "¡Atención! Se detectaron faltantes de stock para este kit. Revise la receta y aplique sustitutos antes de confirmar.");
                model.Items = sugerenciaResult.Data.Items.Select(i => new ItemArmadoViewModel
                {
                    ProductoId = i.ProductoId,
                    NombreProducto = i.NombreProducto,
                    CodigoBarras = i.CodigoBarras,
                    CategoriaId = i.CategoriaId,
                    CategoriaNombre = i.CategoriaNombre,
                    Cantidad = i.CantidadRequerida,
                    EsSustituto = false,
                    ProductoSustituidoId = null
                }).ToList();

                await CargarListasEnViewBagAsync();
                return View(nameof(Armar), model);
            }

            // Si el stock está 100% cubierto, generamos la lista de ítems sugerida automáticamente
            model.Items = sugerenciaResult.Data.Items.Select(i => new ItemArmadoViewModel
            {
                ProductoId = i.ProductoId,
                NombreProducto = i.NombreProducto,
                CodigoBarras = i.CodigoBarras,
                CategoriaId = i.CategoriaId,
                CategoriaNombre = i.CategoriaNombre,
                Cantidad = i.CantidadRequerida,
                EsSustituto = false,
                ProductoSustituidoId = null
            }).ToList();
        }

        if (!model.TipoPaqueteId.HasValue || model.TipoPaqueteId.Value <= 0)
        {
            ModelState.AddModelError(nameof(model.TipoPaqueteId), "Debe seleccionar la plantilla de kit correspondiente.");
        }

        if (model.Items == null || !model.Items.Any())
        {
            ModelState.AddModelError(string.Empty, "No se puede armar un paquete sin alimentos en la receta.");
        }
        else
        {
            for (int i = 0; i < model.Items.Count; i++)
            {
                if (model.Items[i].Cantidad <= 0)
                {
                    ModelState.AddModelError($"Items[{i}].Cantidad", $"La cantidad de '{model.Items[i].NombreProducto}' debe ser mayor a cero.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            await CargarListasEnViewBagAsync();
            if (model.FamiliaId.HasValue && model.Propuesta == null)
            {
                var sugerencia = await _paqueteService.SugerirPaqueteParaFamiliaAsync(model.FamiliaId.Value, model.TipoPaqueteId);
                if (sugerencia.Success) model.Propuesta = sugerencia.Data;
            }
            return View(nameof(Armar), model);
        }

        // Seguridad e inmutabilidad: resolver el operador autenticado en sesión
        var user = await _userManager.GetUserAsync(User);
        var nombreOperador = user?.NombreCompleto ?? User.Identity?.Name ?? "Operador Armado";
        model.UsuarioArmadorId = nombreOperador;

        var request = new ConfirmarArmadoPaqueteRequest
        {
            FamiliaId = model.FamiliaId!.Value,
            TipoPaqueteId = model.TipoPaqueteId!.Value,
            UsuarioArmadorId = nombreOperador,
            Items = model.Items!.Select(i => new ItemArmadoRequest
            {
                ProductoId = i.ProductoId,
                Cantidad = i.Cantidad,
                EsSustituto = i.EsSustituto,
                ProductoSustituidoId = i.ProductoSustituidoId
            }).ToList()
        };

        var result = await _paqueteService.ConfirmarArmadoPaqueteAsync(request);

        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Ocurrió un error al confirmar el armado del paquete.");
            await CargarListasEnViewBagAsync();
            if (model.FamiliaId.HasValue && model.Propuesta == null)
            {
                var sugerencia = await _paqueteService.SugerirPaqueteParaFamiliaAsync(model.FamiliaId.Value, model.TipoPaqueteId);
                if (sugerencia.Success) model.Propuesta = sugerencia.Data;
            }
            return View(nameof(Armar), model);
        }

        TempData["Exito"] = $"¡Paquete armado con éxito! Rótulo QR generado: {result.Data.CodigoSeguimiento}. Se descontaron los lotes de stock según algoritmo FEFO.";
        return RedirectToAction(nameof(Detalles), new { id = result.Data.Id });
    }

    // GET: Paquetes/Detalles/5 o Paquetes/Detalles?codigo=PKG-2026-00001
    public async Task<IActionResult> Detalles(int? id, string? codigo = null)
    {
        OperationResult<PaqueteDto> result;

        if (!string.IsNullOrWhiteSpace(codigo))
        {
            result = await _paqueteService.ObtenerPorCodigoSeguimientoAsync(codigo.Trim());
        }
        else if (id.HasValue && id.Value > 0)
        {
            result = await _paqueteService.ObtenerPorIdAsync(id.Value);
        }
        else
        {
            return NotFound();
        }

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.ErrorMessage ?? "No se encontró el paquete solicitado.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    /// <summary>
    /// Recarga defensiva de listas desplegables (Familias activas, Tipos de Kit y Alimentos para sustituciones)
    /// </summary>
    private async Task CargarListasEnViewBagAsync()
    {
        var familiasResult = await _familiaService.ObtenerTodasAsync(incluirInactivos: false);
        ViewBag.Familias = familiasResult.Success && familiasResult.Data != null
            ? familiasResult.Data.ToList()
            : new List<FamiliaBeneficiaria>();

        var tiposResult = await _paqueteService.ObtenerTiposPaqueteAsync();
        ViewBag.TiposPaquete = tiposResult.Success && tiposResult.Data != null
            ? tiposResult.Data.ToList()
            : new List<TipoPaquete>();

        var productosResult = await _productoService.ObtenerTodosAsync(soloActivos: true);
        ViewBag.Productos = productosResult.Success && productosResult.Data != null
            ? productosResult.Data.ToList()
            : new List<Producto>();
    }

    // GET: Paquetes/Plantillas
    public async Task<IActionResult> Plantillas()
    {
        var result = await _paqueteService.ObtenerTiposPaqueteAsync();
        var model = new PlantillasIndexViewModel
        {
            TiposPaquete = result.Success && result.Data != null ? result.Data : Enumerable.Empty<TipoPaquete>()
        };
        return View(model);
    }

    // GET: Paquetes/CrearPlantilla
    [Authorize(Roles = "Administrador")]
    public IActionResult CrearPlantilla()
    {
        var model = new CrearPlantillaViewModel
        {
            MinIntegrantes = 1,
            MaxIntegrantes = 3,
            Activo = true
        };
        return View(model);
    }

    // POST: Paquetes/CrearPlantilla
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CrearPlantilla(CrearPlantillaViewModel model)
    {
        if (model.MinIntegrantes > model.MaxIntegrantes)
        {
            ModelState.AddModelError(nameof(model.MaxIntegrantes), "El máximo de integrantes debe ser mayor o igual al mínimo.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _paqueteService.CrearTipoPaqueteAsync(
            model.Nombre,
            model.Descripcion,
            model.MinIntegrantes,
            model.MaxIntegrantes,
            model.Activo);

        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo crear la plantilla de kit.");
            return View(model);
        }

        TempData["Exito"] = $"Plantilla '{result.Data.Nombre}' creada exitosamente. Ahora puedes configurar sus alimentos.";
        return RedirectToAction(nameof(EditarPlantilla), new { id = result.Data.Id });
    }

    // GET: Paquetes/EditarPlantilla/3
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> EditarPlantilla(int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var result = await _paqueteService.ObtenerTipoPaquetePorIdAsync(id);
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.ErrorMessage ?? "No se encontró la plantilla solicitada.";
            return RedirectToAction(nameof(Plantillas));
        }

        var prodResult = await _productoService.ObtenerTodosAsync(soloActivos: true);
        ViewBag.ProductosCatalogo = prodResult.Success && prodResult.Data != null
            ? prodResult.Data.ToList()
            : new List<Producto>();

        var tipo = result.Data;
        var model = new EditarPlantillaViewModel
        {
            Id = tipo.Id,
            Nombre = tipo.Nombre,
            Descripcion = tipo.Descripcion,
            MinIntegrantes = tipo.MinIntegrantes,
            MaxIntegrantes = tipo.MaxIntegrantes,
            Activo = tipo.Activo,
            Items = tipo.ItemsPlantilla.Select(i => new ItemPlantillaViewModel
            {
                Id = i.Id,
                ProductoId = i.ProductoId,
                NombreProducto = i.Producto?.Nombre ?? "Alimento",
                CodigoBarras = i.Producto?.CodigoBarras,
                CategoriaNombre = i.Producto?.Categoria?.Nombre ?? "General",
                CantidadRequerida = i.CantidadRequerida
            }).ToList()
        };

        return View(model);
    }

    // POST: Paquetes/EditarPlantilla
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> EditarPlantilla(EditarPlantillaViewModel model)
    {
        if (model.MinIntegrantes > model.MaxIntegrantes)
        {
            ModelState.AddModelError(nameof(model.MaxIntegrantes), "El máximo de integrantes debe ser mayor o igual al mínimo.");
        }

        if (!ModelState.IsValid)
        {
            var prodResult = await _productoService.ObtenerTodosAsync(soloActivos: true);
            ViewBag.ProductosCatalogo = prodResult.Success && prodResult.Data != null
                ? prodResult.Data.ToList()
                : new List<Producto>();
            return View(model);
        }

        var itemsList = model.Items?
            .Where(i => i.ProductoId > 0 && i.CantidadRequerida > 0)
            .Select(i => (i.ProductoId, i.CantidadRequerida))
            .ToList() ?? new List<(int, decimal)>();

        var result = await _paqueteService.ActualizarPlantillaAsync(
            model.Id,
            model.Nombre,
            model.Descripcion,
            model.MinIntegrantes,
            model.MaxIntegrantes,
            model.Activo,
            itemsList);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo actualizar la plantilla.");
            var prodResult = await _productoService.ObtenerTodosAsync(soloActivos: true);
            ViewBag.ProductosCatalogo = prodResult.Success && prodResult.Data != null
                ? prodResult.Data.ToList()
                : new List<Producto>();
            return View(model);
        }

        TempData["Exito"] = $"La receta de '{model.Nombre}' fue actualizada con éxito.";
        return RedirectToAction(nameof(Plantillas));
    }

    // POST: Paquetes/EliminarPlantilla/3
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> EliminarPlantilla(int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var result = await _paqueteService.EliminarTipoPaqueteAsync(id);
        if (result.Success)
        {
            TempData["Exito"] = "La receta de kit fue eliminada exitosamente.";
        }
        else
        {
            TempData["Error"] = result.ErrorMessage ?? "No se pudo eliminar la receta de kit.";
        }

        return RedirectToAction(nameof(Plantillas));
    }
}
