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
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;

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
                    model.ReporteDonaciones = todasDonaciones.Skip((model.PaginaActual - 1) * pageSize).Take(pageSize).ToList();
                    break;
                case "entregas":
                    var todasEntregas = await _reporteService.ObtenerReporteEntregasAsync(fechaDesde, fechaHasta, tipoPaqueteId, voluntarioId);
                    model.TotalRegistros = todasEntregas.Count;
                    model.TotalPaginas = (int)Math.Ceiling(model.TotalRegistros / (double)pageSize);
                    if (model.TotalPaginas < 1) model.TotalPaginas = 1;
                    model.ReporteEntregas = todasEntregas.Skip((model.PaginaActual - 1) * pageSize).Take(pageSize).ToList();
                    break;
                case "familias":
                    var todasFamilias = await _reporteService.ObtenerReporteFamiliasAsync(fechaDesde, fechaHasta, busqueda);
                    model.TotalRegistros = todasFamilias.Count;
                    model.TotalPaginas = (int)Math.Ceiling(model.TotalRegistros / (double)pageSize);
                    if (model.TotalPaginas < 1) model.TotalPaginas = 1;
                    model.ReporteFamilias = todasFamilias.Skip((model.PaginaActual - 1) * pageSize).Take(pageSize).ToList();
                    break;
                case "trazabilidad":
                    var todaTrazabilidad = await _reporteService.ObtenerReporteTrazabilidadAsync(fechaDesde, fechaHasta, busqueda);
                    model.TotalRegistros = todaTrazabilidad.Count;
                    model.TotalPaginas = (int)Math.Ceiling(model.TotalRegistros / (double)pageSize);
                    if (model.TotalPaginas < 1) model.TotalPaginas = 1;
                    model.ReporteTrazabilidad = todaTrazabilidad.Skip((model.PaginaActual - 1) * pageSize).Take(pageSize).ToList();
                    break;
                case "inventario":
                default:
                    var todosInventario = await _reporteService.ObtenerReporteInventarioAsync(categoriaId, busquedaInventario, estadoStock, semaforoFefo);
                    model.TotalRegistros = todosInventario.Count;
                    model.TotalPaginas = (int)Math.Ceiling(model.TotalRegistros / (double)pageSize);
                    if (model.TotalPaginas < 1) model.TotalPaginas = 1;
                    model.ReporteInventario = todosInventario.Skip((model.PaginaActual - 1) * pageSize).Take(pageSize).ToList();
                    break;
            }

            return View(model);
        }

        // Exportación Real a CSV / Excel / PDF
        [HttpGet]
        public async Task<IActionResult> Exportar(string formato, string tab = "inventario", DateTime? fechaDesde = null, DateTime? fechaHasta = null, int? categoriaId = null, string? busquedaInventario = null, string? estadoStock = null, string? semaforoFefo = null, int? donanteId = null, int? tipoPaqueteId = null, string? voluntarioId = null, string? busqueda = null)
        {
            var sb = new StringBuilder();
            string fechaStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"Reporte_{tab}_{fechaStr}";
            string contentType = "text/csv";
            string extension = "csv";

            if (tab.Equals("inventario", StringComparison.OrdinalIgnoreCase))
            {
                var datos = await _reporteService.ObtenerReporteInventarioAsync(categoriaId, busquedaInventario, estadoStock, semaforoFefo);
                sb.AppendLine("Producto;EAN;Categoría;Lote;Vencimiento;Stock;Unidad;Estado");
                foreach (var d in datos)
                {
                    sb.AppendLine($"\"{d.Producto}\";\"{d.CodigoEan}\";\"{d.Categoria}\";\"{d.NumeroLote}\";\"{d.FechaVencimiento:dd/MM/yyyy}\";{d.StockDisponible};\"{d.UnidadMedida}\";\"{d.Estado}\"");
                }
            }
            else if (tab.Equals("donaciones", StringComparison.OrdinalIgnoreCase))
            {
                var datos = await _reporteService.ObtenerReporteDonacionesAsync(fechaDesde, fechaHasta, donanteId);
                sb.AppendLine("Comprobante;FechaHora;Donante;Documento;TipoDonante;Items;CantidadTotal;Receptor");
                foreach (var d in datos)
                {
                    sb.AppendLine($"\"{d.Comprobante}\";\"{d.FechaHora:dd/MM/yyyy HH:mm}\";\"{d.Donante}\";\"{d.Documento}\";\"{d.TipoDonante}\";{d.CantidadItems};{d.CantidadTotal};\"{d.Receptor}\"");
                }
            }
            else if (tab.Equals("entregas", StringComparison.OrdinalIgnoreCase))
            {
                var datos = await _reporteService.ObtenerReporteEntregasAsync(fechaDesde, fechaHasta, tipoPaqueteId, voluntarioId);
                sb.AppendLine("CodigoPaquete;FechaEntrega;Familia;DniTitular;TipoKit;Voluntario;TotalAlimentos");
                foreach (var d in datos)
                {
                    sb.AppendLine($"\"{d.CodigoPaquete}\";\"{d.FechaEntrega:dd/MM/yyyy HH:mm}\";\"{d.Familia}\";\"{d.DniTitular}\";\"{d.TipoKit}\";\"{d.Voluntario}\";{d.TotalAlimentos}");
                }
            }
            else if (tab.Equals("familias", StringComparison.OrdinalIgnoreCase))
            {
                var datos = await _reporteService.ObtenerReporteFamiliasAsync(fechaDesde, fechaHasta, busqueda);
                sb.AppendLine("Familia;DniTitular;Direccion;Integrantes;KitsRecibidos;UltimaEntrega;Estado");
                foreach (var d in datos)
                {
                    sb.AppendLine($"\"{d.Familia}\";\"{d.DniTitular}\";\"{d.Direccion}\";{d.Integrantes};{d.TotalKitsRecibidos};\"{d.UltimaEntrega:dd/MM/yyyy}\";\"{d.Estado}\"");
                }
            }
            else if (tab.Equals("trazabilidad", StringComparison.OrdinalIgnoreCase))
            {
                var datos = await _reporteService.ObtenerReporteTrazabilidadAsync(fechaDesde, fechaHasta, busqueda);
                sb.AppendLine("CodigoPaquete;Alimento;NumeroLote;DonanteOrigen;FamiliaBeneficiaria;FechaEntrega");
                foreach (var d in datos)
                {
                    sb.AppendLine($"\"{d.CodigoPaquete}\";\"{d.Alimento}\";\"{d.NumeroLote}\";\"{d.DonanteOrigen}\";\"{d.FamiliaBeneficiaria}\";\"{d.FechaEntrega:dd/MM/yyyy}\"");
                }
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();

            if (formato?.ToLower() == "excel")
            {
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                extension = "xlsx";
            }
            else if (formato?.ToLower() == "pdf")
            {
                // 1. Obtener los datos de forma asincrónica ANTES de construir el PDF
                List<ReporteInventarioItemDto> datosInventario = new();
                if (tab.Equals("inventario", StringComparison.OrdinalIgnoreCase))
                {
                    datosInventario = await _reporteService.ObtenerReporteInventarioAsync(categoriaId, busquedaInventario, estadoStock, semaforoFefo);
                }

                // 2. Generación de PDF profesional y estructurado con QuestPDF
                var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                        page.Margin(30);
                        page.PageColor(QuestPDF.Helpers.Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10).FontColor(QuestPDF.Helpers.Colors.Grey.Darken3));

                        // Encabezado del PDF
                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("NutriRed - Banco de Alimentos").Bold().FontSize(18).FontColor("#198754");
                                col.Item().Text($"Reporte Consolidado de {tab.ToUpper()}").Medium().FontSize(12).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                            });
                            row.ConstantItem(150).AlignRight().Column(col =>
                            {
                                col.Item().Text($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                                col.Item().Text("Sistema de Trazabilidad").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                            });
                        });

                        // Contenido Principal (Tabla Estructurada)
                        page.Content().PaddingVertical(15).Column(col =>
                        {
                            if (tab.Equals("inventario", StringComparison.OrdinalIgnoreCase))
                            {
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3); // Producto
                                        columns.RelativeColumn(1.5f); // Categoría
                                        columns.RelativeColumn(1.5f); // Lote
                                        columns.RelativeColumn(1.2f); // Vencimiento
                                        columns.RelativeColumn(1f); // Stock
                                        columns.RelativeColumn(1.5f); // Estado
                                    });

                                    // Cabecera de la Tabla
                                    table.Header(header =>
                                    {
                                        header.Cell().Background("#198754").Padding(5).Text("Producto / EAN").Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                        header.Cell().Background("#198754").Padding(5).Text("Categoría").Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                        header.Cell().Background("#198754").Padding(5).Text("Lote").Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                        header.Cell().Background("#198754").Padding(5).Text("Vencimiento").Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                        header.Cell().Background("#198754").Padding(5).Text("Stock").Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                        header.Cell().Background("#198754").Padding(5).Text("Estado").Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                    });

                                    // Filas de Datos
                                    foreach (var d in datosInventario)
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text(d.Producto);
                                        table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text(d.Categoria);
                                        table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text(d.NumeroLote);
                                        table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text(d.FechaVencimiento != DateTime.MinValue ? d.FechaVencimiento.ToString("dd/MM/yyyy") : "-");
                                        table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text($"{d.StockDisponible} {d.UnidadMedida}");
                                        table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text(d.Estado);
                                    }
                                });
                            }
                            else
                            {
                                col.Item().Text(sb.ToString()).FontSize(9);
                            }
                        });

                        // Pie de Página
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                            x.Span(" de ");
                            x.TotalPages();
                        });
                    });
                }).GeneratePdf();

                bytes = pdfBytes;
                contentType = "application/pdf";
                extension = "pdf";
            }

            return File(bytes, contentType, $"{fileName}.{extension}");
        }
    }
}