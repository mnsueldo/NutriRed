using Microsoft.AspNetCore.Mvc;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.DTOs;
using NutriRed.Services.Interfaces;
using NutriRed.Web.Models;

namespace NutriRed.Web.Controllers;

public class DonacionesController : Controller
{
    private readonly IDonacionService _donacionService;
    private readonly IProductoService _productoService;
    private readonly ILogger<DonacionesController> _logger;

    public DonacionesController(
        IDonacionService donacionService,
        IProductoService productoService,
        ILogger<DonacionesController> logger)
    {
        _donacionService = donacionService;
        _productoService = productoService;
        _logger = logger;
    }

    // GET: Donaciones
    public async Task<IActionResult> Index(DateTime? desde = null, DateTime? hasta = null, string? codigo = null)
    {
        var viewModel = new DonacionIndexViewModel
        {
            FechaDesde = desde,
            FechaHasta = hasta,
            CodigoComprobante = codigo?.Trim()
        };

        if (!string.IsNullOrWhiteSpace(viewModel.CodigoComprobante))
        {
            var singleResult = await _donacionService.ObtenerPorCodigoAsync(viewModel.CodigoComprobante);
            if (singleResult.Success && singleResult.Data != null)
            {
                viewModel.Donaciones = new List<DonacionComprobanteDto> { singleResult.Data };
            }
            else
            {
                TempData["Advertencia"] = $"No se encontró ninguna donación con el comprobante '{viewModel.CodigoComprobante}'.";
                viewModel.Donaciones = Enumerable.Empty<DonacionComprobanteDto>();
            }
            return View(viewModel);
        }

        var result = await _donacionService.ObtenerTodasAsync(desde, hasta);
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.ErrorMessage ?? "No se pudo recuperar el historial de donaciones.";
            viewModel.Donaciones = Enumerable.Empty<DonacionComprobanteDto>();
        }
        else
        {
            viewModel.Donaciones = result.Data;
        }

        return View(viewModel);
    }

    // GET: Donaciones/Registrar
    public async Task<IActionResult> Registrar()
    {
        await CargarProductosEnViewBagAsync();
        var model = new RegistrarDonacionViewModel();
        return View(model);
    }

    // POST: Donaciones/Registrar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(RegistrarDonacionViewModel model)
    {
        // 1. Validaciones de Negocio Específicas de Donación (RF1)
        if (model.Items == null || !model.Items.Any())
        {
            ModelState.AddModelError(string.Empty, "Detalle vacío: Debe registrar al menos un alimento en la lista de recepción.");
        }

        if (model.TipoDonante != TipoDonante.Anonimo)
        {
            if (string.IsNullOrWhiteSpace(model.NombreRazonSocial))
            {
                ModelState.AddModelError(nameof(model.NombreRazonSocial), "El nombre o razón social es obligatorio para personas físicas o instituciones.");
            }
            if (string.IsNullOrWhiteSpace(model.NumeroDocumento))
            {
                ModelState.AddModelError(nameof(model.NumeroDocumento), "El DNI o CUIT es obligatorio para personas físicas o instituciones.");
            }
        }

        var hoy = DateTime.Today;
        if (model.Items != null)
        {
            for (int i = 0; i < model.Items.Count; i++)
            {
                var item = model.Items[i];
                if (item.Cantidad <= 0)
                {
                    ModelState.AddModelError($"Items[{i}].Cantidad", $"La cantidad recibida para '{item.NombreProducto ?? item.CodigoBarras}' debe ser mayor a cero.");
                }

                if (item.FechaVencimiento.Date <= hoy)
                {
                    ModelState.AddModelError($"Items[{i}].FechaVencimiento", $"El producto '{item.NombreProducto ?? item.CodigoBarras}' no puede recibirse vencido o con vencimiento en la fecha de hoy.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            await CargarProductosEnViewBagAsync();
            return View(model);
        }

        // 2. Mapeo seguro hacia el DTO de negocio
        var request = new RegistrarDonacionRequest
        {
            TipoDonante = model.TipoDonante,
            NumeroDocumento = model.TipoDonante == TipoDonante.Anonimo ? null : model.NumeroDocumento?.Trim(),
            NombreRazonSocial = model.TipoDonante == TipoDonante.Anonimo ? "Donante Anónimo" : model.NombreRazonSocial?.Trim(),
            Telefono = model.TipoDonante == TipoDonante.Anonimo ? null : model.Telefono?.Trim(),
            Email = model.TipoDonante == TipoDonante.Anonimo ? null : model.Email?.Trim(),
            VoluntarioReceptorId = string.IsNullOrWhiteSpace(model.VoluntarioReceptorId) ? "voluntario_deposito" : model.VoluntarioReceptorId.Trim(),
            Observaciones = model.Observaciones?.Trim(),
            Items = model.Items!.Select(i => new DonacionItemRequest
            {
                CodigoBarras = i.CodigoBarras.Trim(),
                Cantidad = i.Cantidad,
                FechaVencimiento = i.FechaVencimiento.Date,
                NumeroLote = string.IsNullOrWhiteSpace(i.NumeroLote) ? null : i.NumeroLote.Trim()
            }).ToList()
        };

        // 3. Ejecución atómica en la capa de servicios
        var result = await _donacionService.RegistrarDonacionAsync(request);

        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Ocurrió un error al registrar la donación.");
            await CargarProductosEnViewBagAsync();
            return View(model);
        }

        TempData["Exito"] = $"Donación registrada con éxito. Se generó el Comprobante Oficial N° {result.Data.CodigoComprobante}.";
        return RedirectToAction(nameof(Comprobante), new { id = result.Data.DonacionId });
    }

    // GET: Donaciones/Comprobante/5
    public async Task<IActionResult> Comprobante(int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var result = await _donacionService.ObtenerPorIdAsync(id);
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.ErrorMessage ?? "No se encontró el comprobante de donación solicitado.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    /// <summary>
    /// Recarga defensiva del catálogo de productos activos para los desplegables y autocompletado.
    /// </summary>
    private async Task CargarProductosEnViewBagAsync()
    {
        var result = await _productoService.ObtenerTodosAsync(soloActivos: true);
        ViewBag.Productos = result.Success && result.Data != null 
            ? result.Data.ToList() 
            : new List<Producto>();
    }
}
