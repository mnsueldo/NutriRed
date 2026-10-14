using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NutriRed.Data;
using NutriRed.Domain.Entities;
using NutriRed.Services.DTOs;
using NutriRed.Services.Interfaces;
using NutriRed.Web.Models;

namespace NutriRed.Web.Controllers
{
    [Authorize]
    public class ReportesController : Controller
    {
        private readonly NutriRedDbContext _context;
        private readonly IReporteService _reporteService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportesController(
            NutriRedDbContext context,
            IReporteService reporteService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _reporteService = reporteService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            string tab = "inventario",
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            int? categoriaId = null,
            string? busquedaInventario = null,
            string? estadoStock = null,
            string? semaforoFefo = null,
            int? donanteId = null,
            int? tipoPaqueteId = null,
            string? voluntarioId = null,
            string? busqueda = null,
            int pagina = 1)
        {
            var model = new ReportesIndexViewModel
            {
                TabActiva = tab,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                CategoriaId = categoriaId,
                BusquedaInventario = busquedaInventario,
                EstadoStock = estadoStock,
                SemaforoFefo = semaforoFefo,
                DonanteId = donanteId,
                TipoPaqueteId = tipoPaqueteId,
                VoluntarioId = voluntarioId,
                Busqueda = busqueda,
                PaginaActual = pagina < 1 ? 1 : pagina
            };

            // Cargar Selects (Categorías, Donantes, etc.)
            model.Categorias = await _context.Categorias.Where(c => c.Activo)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Nombre }).ToListAsync();
            model.Donantes = await _context.Donantes.Where(d => d.Activo)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.NombreRazonSocial }).ToListAsync();
            model.TiposPaquete = await _context.TiposPaquete.Where(t => t.Activo)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Nombre }).ToListAsync();

            var voluntariosUsers = await _userManager.GetUsersInRoleAsync("Voluntario");
            model.Voluntarios = voluntariosUsers
                .Select(u => new SelectListItem { Value = u.Id, Text = !string.IsNullOrEmpty(u.NombreCompleto) ? u.NombreCompleto : u.UserName }).ToList();

            int pageSize = 10;

            switch (tab.ToLower())
            {
                case "donaciones":
                    var todasDonaciones = await _reporteService.ObtenerReporteDonacionesAsync(fechaDesde, fechaHasta, donanteId);
                    model.TotalRegistros = todasDonaciones.Count;
                    model.TotalPaginas = (int)Math.Ceiling(model.TotalRegistros / (double)pageSize);
                    if (model.TotalPaginas < 1) model.TotalPaginas = 1;

                    model.ReporteDonaciones = todasDonaciones
                        .Skip((model.PaginaActual - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                    break;
                case "entregas":
                    model.ReporteEntregas = await _reporteService.ObtenerReporteEntregasAsync(fechaDesde, fechaHasta, tipoPaqueteId, voluntarioId);
                    break;
                case "familias":
                    model.ReporteFamilias = await _reporteService.ObtenerReporteFamiliasAsync(busqueda);
                    break;
                case "trazabilidad":
                    model.ReporteTrazabilidad = await _reporteService.ObtenerReporteTrazabilidadAsync(fechaDesde, fechaHasta, busqueda);
                    break;
                case "inventario":
                default:
                    var todosInventario = await _reporteService.ObtenerReporteInventarioAsync(categoriaId, busquedaInventario, estadoStock, semaforoFefo);
                    model.TotalRegistros = todosInventario.Count;
                    model.TotalPaginas = (int)Math.Ceiling(model.TotalRegistros / (double)pageSize);
                    if (model.TotalPaginas < 1) model.TotalPaginas = 1;

                    model.ReporteInventario = todosInventario
                        .Skip((model.PaginaActual - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                    break;
            }

            return View(model);
        }
    }
}