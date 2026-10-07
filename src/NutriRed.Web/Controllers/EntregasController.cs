using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.DTOs;
using NutriRed.Services.Interfaces;
using NutriRed.Web.Models;

namespace NutriRed.Web.Controllers;

[Authorize]
public class EntregasController : Controller
{
    private readonly IEntregaService _entregaService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<EntregasController> _logger;

    public EntregasController(
        IEntregaService entregaService,
        UserManager<ApplicationUser> userManager,
        ILogger<EntregasController> logger)
    {
        _entregaService = entregaService;
        _userManager = userManager;
        _logger = logger;
    }

    // GET: Entregas
    public async Task<IActionResult> Index(string? codigoQr = null, string? tab = "pendientes", string? estado = null)
    {
        // Búsqueda directa por código QR escaneado o escrito
        if (!string.IsNullOrWhiteSpace(codigoQr))
        {
            var qrResult = await _entregaService.ConsultarPaquetePorQrAsync(codigoQr.Trim());
            if (qrResult.Success && qrResult.Data != null)
            {
                return RedirectToAction(nameof(Despachar), new { codigo = qrResult.Data.CodigoSeguimiento });
            }

            TempData["Advertencia"] = qrResult.ErrorMessage ?? $"No se encontró ningún paquete preparado con el código QR '{codigoQr}'.";
        }

        var resultadoListos = await _entregaService.ObtenerPaquetesListosParaDespachoAsync();
        var resultadoHistorial = await _entregaService.ObtenerHistorialDespachosAsync();

        var listos = (resultadoListos.Data ?? Enumerable.Empty<PaquetePendienteDespachoDto>()).ToList();
        var todos = (resultadoHistorial.Data ?? Enumerable.Empty<PaqueteDespachoHistorialDto>()).ToList();

        var filtrados = todos.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(estado) && Enum.TryParse<EstadoPaquete>(estado, true, out var estadoFiltro))
        {
            filtrados = filtrados.Where(p => p.Estado == estadoFiltro);
        }

        var model = new EntregaIndexViewModel
        {
            PaquetesListos = listos,
            HistorialPaquetes = filtrados.ToList(),
            CodigoQrBusqueda = codigoQr,
            TabActiva = string.IsNullOrWhiteSpace(tab) ? "pendientes" : tab,
            FiltroEstado = estado,
            TotalEntregados = todos.Count(p => p.Estado == EstadoPaquete.Entregado),
            TotalCancelados = todos.Count(p => p.Estado == EstadoPaquete.Cancelado),
            TotalTodos = todos.Count
        };

        return View(model);
    }

    // GET: Entregas/Despachar?codigo=PKG-2026-00001
    public async Task<IActionResult> Despachar(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return RedirectToAction(nameof(Index));
        }

        var resultado = await _entregaService.ConsultarPaquetePorQrAsync(codigo.Trim());
        if (!resultado.Success || resultado.Data == null)
        {
            TempData["Error"] = resultado.ErrorMessage ?? "El paquete no existe o no está habilitado para despacho.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.GetUserAsync(User);
        var nombreOperador = user?.NombreCompleto ?? User.Identity?.Name ?? "Operador Despacho";

        var model = new DespacharPaqueteViewModel
        {
            CodigoSeguimiento = resultado.Data.CodigoSeguimiento,
            Paquete = resultado.Data,
            TipoReceptor = TipoReceptor.Titular,
            DniReceptor = resultado.Data.DniTitular,
            NombreReceptor = resultado.Data.FamiliaTitular,
            VoluntarioDespachoId = nombreOperador
        };

        return View(model);
    }

    // POST: Entregas/Despachar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Despachar(DespacharPaqueteViewModel model)
    {
        // Seguridad e inmutabilidad: resolver el operador autenticado en sesión
        var user = await _userManager.GetUserAsync(User);
        var nombreOperador = user?.NombreCompleto ?? User.Identity?.Name ?? "Operador Despacho";
        model.VoluntarioDespachoId = nombreOperador;

        // Reglas de Negocio RF3:
        if (model.TipoReceptor == TipoReceptor.TerceroAutorizado && string.IsNullOrWhiteSpace(model.VinculoConTitular))
        {
            ModelState.AddModelError(nameof(model.VinculoConTitular), "Debe especificar el parentesco o vínculo con el titular para terceros autorizados.");
        }

        if (string.IsNullOrWhiteSpace(model.FirmaDigital) || model.FirmaDigital.Length < 100)
        {
            ModelState.AddModelError(nameof(model.FirmaDigital), "Firma digital vacía: Impedir la confirmación de la entrega si el campo de firma está vacío.");
        }

        if (!ModelState.IsValid)
        {
            var recargarPaquete = await _entregaService.ConsultarPaquetePorQrAsync(model.CodigoSeguimiento);
            model.Paquete = recargarPaquete.Data;
            return View(model);
        }

        var request = new RegistrarEntregaRequest
        {
            CodigoSeguimiento = model.CodigoSeguimiento.Trim(),
            TipoReceptor = model.TipoReceptor,
            DniReceptor = model.DniReceptor.Trim(),
            NombreReceptor = model.NombreReceptor.Trim(),
            VinculoConTitular = model.TipoReceptor == TipoReceptor.TerceroAutorizado ? model.VinculoConTitular?.Trim() : null,
            FirmaDigital = model.FirmaDigital,
            VoluntarioDespachoId = nombreOperador
        };

        var resultado = await _entregaService.ConfirmarEntregaAsync(request);

        if (!resultado.Success || resultado.Data == null)
        {
            ModelState.AddModelError(string.Empty, resultado.ErrorMessage ?? "Error al confirmar la entrega del paquete.");
            var recargarPaquete = await _entregaService.ConsultarPaquetePorQrAsync(model.CodigoSeguimiento);
            model.Paquete = recargarPaquete.Data;
            return View(model);
        }

        TempData["Exito"] = $"¡Entrega confirmada con éxito para la familia {resultado.Data.FamiliaTitular}! Se generó el Comprobante Oficial N° {resultado.Data.EntregaId}.";
        return RedirectToAction(nameof(Comprobante), new { id = resultado.Data.EntregaId });
    }

    // GET: Entregas/Comprobante/5
    public async Task<IActionResult> Comprobante(int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var resultado = await _entregaService.ObtenerComprobanteEntregaAsync(id);
        if (!resultado.Success || resultado.Data == null)
        {
            TempData["Error"] = resultado.ErrorMessage ?? "No se encontró el comprobante de entrega solicitado.";
            return RedirectToAction(nameof(Index));
        }

        return View(resultado.Data);
    }

    // POST: Entregas/RegistrarNoConcretada
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarNoConcretada(RegistrarEntregaFallidaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Debe especificar un motivo válido de no entrega.";
            return RedirectToAction(nameof(Despachar), new { codigo = model.CodigoSeguimiento });
        }

        var user = await _userManager.GetUserAsync(User);
        var nombreOperador = user?.NombreCompleto ?? User.Identity?.Name ?? "Operador Despacho";
        model.VoluntarioDespachoId = nombreOperador;

        var request = new RegistrarEntregaFallidaRequest
        {
            CodigoSeguimiento = model.CodigoSeguimiento.Trim(),
            MotivoNoEntrega = model.MotivoNoEntrega.Trim(),
            VoluntarioDespachoId = nombreOperador
        };

        var resultado = await _entregaService.RegistrarEntregaNoConcretadaAsync(request);

        if (!resultado.Success)
        {
            TempData["Error"] = resultado.ErrorMessage ?? "Ocurrió un error al registrar el intento fallido.";
        }
        else
        {
            TempData["Advertencia"] = $"Se asentó el intento de entrega no concretado para el paquete '{model.CodigoSeguimiento}' por motivo: {model.MotivoNoEntrega}.";
        }

        return RedirectToAction(nameof(Index));
    }
}
