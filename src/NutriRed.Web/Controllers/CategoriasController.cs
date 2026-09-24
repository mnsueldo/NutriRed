using Microsoft.AspNetCore.Mvc;
using NutriRed.Domain.Entities;
using NutriRed.Services.Interfaces;

namespace NutriRed.Web.Controllers;

public class CategoriasController : Controller
{
    private readonly ICategoriaService _categoriaService;

    public CategoriasController(ICategoriaService categoriaService)
    {
        _categoriaService = categoriaService;
    }

    // GET: /Categorias
    public async Task<IActionResult> Index(bool verTodas = false)
    {
        var result = await _categoriaService.ObtenerTodasAsync(soloActivas: !verTodas);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return View(Enumerable.Empty<Categoria>());
        }

        ViewBag.VerTodas = verTodas;
        return View(result.Data);
    }

    // GET: /Categorias/Create
    public IActionResult Create()
    {
        return View(new Categoria { Activo = true });
    }

    // POST: /Categorias/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Categoria categoria)
    {
        if (!ModelState.IsValid)
        {
            return View(categoria);
        }

        var result = await _categoriaService.CrearAsync(categoria);

        if (!result.Success)
        {
            // Incorporamos el error retornado por la lógica de negocio al ModelState
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Error al registrar la categoría.");
            return View(categoria);
        }

        TempData["Exito"] = $"Categoría '{result.Data?.Nombre}' creada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Categorias/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            TempData["Error"] = "Identificador de categoría no válido.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _categoriaService.ObtenerPorIdAsync(id.Value);

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.ErrorMessage ?? "No se encontró la categoría solicitada.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    // POST: /Categorias/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Categoria categoria)
    {
        if (id != categoria.Id)
        {
            TempData["Error"] = "El identificador de la ruta no coincide con el de la categoría.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            return View(categoria);
        }

        var result = await _categoriaService.ActualizarAsync(categoria);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Error al actualizar la categoría.");
            return View(categoria);
        }

        TempData["Exito"] = $"Categoría '{result.Data?.Nombre}' actualizada con éxito.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Categorias/CambiarEstado/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, bool nuevoEstado)
    {
        var result = await _categoriaService.CambiarEstadoAsync(id, nuevoEstado);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Exito"] = nuevoEstado 
                ? "La categoría ha sido activada con éxito." 
                : "La categoría ha sido desactivada con éxito.";
        }

        return RedirectToAction(nameof(Index));
    }
}
