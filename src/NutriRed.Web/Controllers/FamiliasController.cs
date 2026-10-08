using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.Interfaces;

namespace NutriRed.Web.Controllers
{
    [Authorize]
    public class FamiliasController : Controller
    {
        private readonly IFamiliaService _familiaService;

        public FamiliasController(IFamiliaService familiaService)
        {
            _familiaService = familiaService;
        }

        // GET: Familias
        public async Task<IActionResult> Index(bool mostrarTodos = false, string? busqueda = null, EstadoFamilia? estado = null)
        {
            // Si se especificó un estado o se pidió ver todos, incluimos inactivos para filtrar correctamente
            bool incluirInactivos = mostrarTodos || estado.HasValue;
            var result = await _familiaService.ObtenerTodasAsync(incluirInactivos: incluirInactivos, busqueda: busqueda);

            ViewBag.MostrarTodos = mostrarTodos;
            ViewBag.Busqueda = busqueda;
            ViewBag.Estado = estado;

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.ErrorMessage ?? "No se pudieron cargar las familias beneficiarias.";
                return View(Enumerable.Empty<FamiliaBeneficiaria>());
            }

            var data = result.Data;
            if (estado.HasValue)
            {
                data = data.Where(f => f.Estado == estado.Value);
            }

            return View(data);
        }

        // GET: Familias/Detalles/5
        public async Task<IActionResult> Detalles(int id)
        {
            var result = await _familiaService.ObtenerPorIdAsync(id, incluirHistorial: true);

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.ErrorMessage ?? "No se encontró el legajo familiar solicitado.";
                return RedirectToAction(nameof(Index));
            }

            return View(result.Data);
        }

        // GET: Familias/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Familias/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FamiliaBeneficiaria familia)
        {
            if (!ModelState.IsValid)
                return View(familia);

            var result = await _familiaService.CrearAsync(familia);

            if (!result.Success)
            {
                if (result.ErrorMessage != null && result.ErrorMessage.Contains("DNI"))
                {
                    ModelState.AddModelError(nameof(familia.DniTitular), result.ErrorMessage);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Error al registrar la familia.");
                }
                return View(familia);
            }

            TempData["Exito"] = "Legajo familiar registrado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Familias/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _familiaService.ObtenerPorIdAsync(id);

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.ErrorMessage ?? "No se encontró la familia solicitada.";
                return RedirectToAction(nameof(Index));
            }

            return View(result.Data);
        }

        // POST: Familias/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FamiliaBeneficiaria familia)
        {
            if (id != familia.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(familia);

            var result = await _familiaService.ActualizarAsync(familia);

            if (!result.Success)
            {
                if (result.ErrorMessage != null && result.ErrorMessage.Contains("DNI"))
                {
                    ModelState.AddModelError(nameof(familia.DniTitular), result.ErrorMessage);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Error al actualizar la familia.");
                }
                return View(familia);
            }

            TempData["Exito"] = "Legajo familiar actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Familias/CambiarEstado/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var resultFamilia = await _familiaService.ObtenerPorIdAsync(id);

            if (resultFamilia == null || !resultFamilia.Success || resultFamilia.Data == null)
            {
                TempData["Error"] = "El legajo familiar solicitado no existe.";
                return RedirectToAction(nameof(Index));
            }

            var familia = resultFamilia.Data;

            var nuevoEstado = familia.Estado == EstadoFamilia.Activo
                ? EstadoFamilia.Suspendido
                : EstadoFamilia.Activo;

            var result = await _familiaService.CambiarEstadoAsync(id, nuevoEstado);

            if (result.Success)
            {
                TempData["Exito"] = $"El estado del legajo se cambió a {nuevoEstado} exitosamente.";
            }
            else
            {
                TempData["Error"] = result.ErrorMessage ?? "No se pudo cambiar el estado de la familia.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Familias/ToggleEstado/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEstado(int id)
        {
            var familiaResult = await _familiaService.ObtenerPorIdAsync(id);
            if (!familiaResult.Success || familiaResult.Data == null)
            {
                TempData["Error"] = "No se encontró el legajo familiar.";
                return RedirectToAction(nameof(Index));
            }

            var familia = familiaResult.Data;

            familia.Estado = familia.Estado == EstadoFamilia.Activo
                ? EstadoFamilia.Suspendido
                : EstadoFamilia.Activo;

            var result = await _familiaService.ActualizarAsync(familia);

            if (result.Success)
            {
                TempData["Exito"] = $"El estado de la familia {familia.ApellidoTitular} ha sido actualizado a '{familia.Estado}'.";
            }
            else
            {
                TempData["Error"] = result.ErrorMessage ?? "Error al cambiar el estado de la familia.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}