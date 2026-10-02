using Microsoft.AspNetCore.Mvc;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.Common;
using NutriRed.Services.DTOs;
using NutriRed.Services.Interfaces;
using NutriRed.Web.Models;

namespace NutriRed.Web.Controllers;

public class PaquetesController : Controller
{
    private readonly IPaqueteService _paqueteService;
    private readonly IFamiliaService _familiaService;
    private readonly IProductoService _productoService;
    private readonly ILogger<PaquetesController> _logger;

    public PaquetesController(
        IPaqueteService paqueteService,
        IFamiliaService familiaService,
        IProductoService productoService,
        ILogger<PaquetesController> logger)
    {
        _paqueteService = paqueteService;
        _familiaService = familiaService;
        _productoService = productoService;
        _logger = logger;
    }

    // GET: Paquetes
    public async Task<IActionResult> Index(EstadoPaquete? estado = null, string? codigo = null)
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
            return View(viewModel);
        }

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

        return View(viewModel);
    }

    // GET: Paquetes/Armar?familiaId=5&tipoPaqueteId=1
    public async Task<IActionResult> Armar(int? familiaId = null, int? tipoPaqueteId = null)
    {
        await CargarListasEnViewBagAsync();

        var model = new ArmarPaqueteViewModel
        {
            FamiliaId = familiaId,
            TipoPaqueteId = tipoPaqueteId
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

        var request = new ConfirmarArmadoPaqueteRequest
        {
            FamiliaId = model.FamiliaId!.Value,
            TipoPaqueteId = model.TipoPaqueteId!.Value,
            UsuarioArmadorId = string.IsNullOrWhiteSpace(model.UsuarioArmadorId) ? "operador_armado" : model.UsuarioArmadorId.Trim(),
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
}
