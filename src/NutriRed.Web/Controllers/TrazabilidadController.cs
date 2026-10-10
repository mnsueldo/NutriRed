using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NutriRed.Data;
using NutriRed.Web.Models;

namespace NutriRed.Web.Controllers
{
    [Authorize]
    public class TrazabilidadController : Controller
    {
        private readonly NutriRedDbContext _context;

        public TrazabilidadController(NutriRedDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? busqueda, DateTime? fechaDesde, DateTime? fechaHasta, int? donanteId, int? tipoPaqueteId)
        {
            var model = new TrazabilidadFiltroViewModel
            {
                Busqueda = busqueda,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                DonanteId = donanteId,
                TipoPaqueteId = tipoPaqueteId
            };

            // 1. Cargar desplegables para la UI
            model.Donantes = await _context.Donantes
                .Where(d => d.Activo)
                .OrderBy(d => d.NombreRazonSocial)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.NombreRazonSocial
                }).ToListAsync();

            model.TiposPaquete = await _context.TiposPaquete
                .Where(t => t.Activo)
                .OrderBy(t => t.Nombre)
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = t.Nombre
                }).ToListAsync();

            // Si no se aplicó ningún filtro, retornar vista vacía
            if (string.IsNullOrWhiteSpace(busqueda) && !fechaDesde.HasValue && !fechaHasta.HasValue && !donanteId.HasValue && !tipoPaqueteId.HasValue)
            {
                return View(model);
            }

            // 2. Consulta base con todas las relaciones cargadas
            var query = _context.Entregas
                .Include(e => e.Paquete)
                    .ThenInclude(p => p.FamiliaBeneficiaria)
                .Include(e => e.Paquete)
                    .ThenInclude(p => p.TipoPaquete)
                .Include(e => e.Paquete)
                    .ThenInclude(p => p.Detalles)
                        .ThenInclude(d => d.Lote)
                            .ThenInclude(l => l.Producto)
                .AsQueryable();

            // Filtros de fecha
            if (fechaDesde.HasValue)
                query = query.Where(e => e.FechaHoraEntrega >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                query = query.Where(e => e.FechaHoraEntrega <= fechaHasta.Value.AddDays(1).AddTicks(-1));

            // Filtro por Tipo de Kit
            if (tipoPaqueteId.HasValue)
                query = query.Where(e => e.Paquete.TipoPaqueteId == tipoPaqueteId.Value);

            // Búsqueda parcial y flexible por texto libre
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var term = busqueda.Trim().ToLower();

                // 1. Si el usuario seleccionó una sugerencia completa como "González, Mario (DNI: 28456789)"
                if (term.Contains("(dni:") && term.EndsWith(")"))
                {
                    // Extraemos solo el DNI (ejemplo: 28456789)
                    var inicioDni = term.IndexOf("(dni:") + 5;
                    var finDni = term.IndexOf(")", inicioDni);
                    if (finDni > inicioDni)
                    {
                        var dniExtraido = term.Substring(inicioDni, finDni - inicioDni).Trim();

                        // Filtramos directamente por DNI exacto del titular o del receptor
                        query = query.Where(e =>
                            (e.Paquete.FamiliaBeneficiaria != null && e.Paquete.FamiliaBeneficiaria.DniTitular == dniExtraido) ||
                            e.DniReceptor == dniExtraido
                        );
                    }
                }
                else
                {
                    // 2. Búsqueda por texto libre convencional (si el usuario escribió libremente)
                    query = query.Where(e =>
                        // Búsqueda en Paquete
                        EF.Functions.Like(e.Paquete.CodigoSeguimiento.ToLower(), $"%{term}%") ||

                        // Búsqueda en Familia (Apellido, Nombre o DNI)
                        (e.Paquete.FamiliaBeneficiaria != null && (
                            EF.Functions.Like(e.Paquete.FamiliaBeneficiaria.ApellidoTitular.ToLower(), $"%{term}%") ||
                            EF.Functions.Like(e.Paquete.FamiliaBeneficiaria.NombreTitular.ToLower(), $"%{term}%") ||
                            EF.Functions.Like(e.Paquete.FamiliaBeneficiaria.DniTitular, $"%{term}%") ||
                            EF.Functions.Like((e.Paquete.FamiliaBeneficiaria.ApellidoTitular + " " + e.Paquete.FamiliaBeneficiaria.NombreTitular).ToLower(), $"%{term}%") ||
                            EF.Functions.Like((e.Paquete.FamiliaBeneficiaria.NombreTitular + " " + e.Paquete.FamiliaBeneficiaria.ApellidoTitular).ToLower(), $"%{term}%")
                        )) ||

                        // Búsqueda en Lote o Nombre del Producto
                        e.Paquete.Detalles.Any(d =>
                            EF.Functions.Like(d.Lote.NumeroLote.ToLower(), $"%{term}%") ||
                            EF.Functions.Like(d.Lote.Producto.Nombre.ToLower(), $"%{term}%")
                        ) ||

                        // Búsqueda en Datos de Receptor
                        EF.Functions.Like(e.NombreReceptor.ToLower(), $"%{term}%") ||
                        EF.Functions.Like(e.DniReceptor, $"%{term}%")
                    );
                }
            }

            var entregas = await query.ToListAsync();

            // 3. Consultar Donantes asociados a los Lotes en memoria
            var loteIds = entregas
                .SelectMany(e => e.Paquete?.Detalles ?? Enumerable.Empty<NutriRed.Domain.Entities.PaqueteDetalle>())
                .Select(d => d.LoteId)
                .Distinct()
                .ToList();

            var donacionesPorLote = await _context.DonacionDetalles
                .Include(dd => dd.Donacion)
                    .ThenInclude(don => don.Donante)
                .Where(dd => loteIds.Contains(dd.LoteId))
                .GroupBy(dd => dd.LoteId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(dd => dd.Donacion).FirstOrDefault()
                );

            // 4. Consultar Usuarios
            var usuarioIds = entregas.Select(e => e.VoluntarioDespachoId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var usuarios = await _context.Users.Where(u => usuarioIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);

            // 5. Construcción del resultado
            foreach (var e in entregas)
            {
                var fam = e.Paquete?.FamiliaBeneficiaria;

                string voluntarioNombre = "Voluntario General";
                string voluntarioUserName = "voluntario";

                if (!string.IsNullOrEmpty(e.VoluntarioDespachoId) && usuarios.TryGetValue(e.VoluntarioDespachoId, out var userObj))
                {
                    voluntarioNombre = !string.IsNullOrWhiteSpace(userObj.NombreCompleto) ? userObj.NombreCompleto : (userObj.UserName ?? "Voluntario");
                    voluntarioUserName = userObj.UserName ?? "voluntario";
                }

                var grupoDto = new TrazabilidadEntregaGroupDto
                {
                    PaqueteId = e.PaqueteId,
                    CodigoPaquete = e.Paquete?.CodigoSeguimiento ?? "N/A",
                    TipoPaquete = e.Paquete?.TipoPaquete?.Nombre ?? "Familiar",
                    FechaArmado = e.Paquete?.FechaCreacion ?? DateTime.MinValue,

                    FamiliaId = fam?.Id ?? 0,
                    NombreFamilia = fam != null ? $"{fam.ApellidoTitular}, {fam.NombreTitular}" : "N/A",
                    DniRepresentante = fam?.DniTitular ?? "N/A",
                    DireccionFamilia = fam?.Direccion ?? "N/A",

                    VoluntarioNombre = voluntarioNombre,
                    VoluntarioUserName = voluntarioUserName,
                    FechaEntrega = e.FechaHoraEntrega
                };

                foreach (var d in e.Paquete?.Detalles ?? Enumerable.Empty<NutriRed.Domain.Entities.PaqueteDetalle>())
                {
                    string nombreDonante = "N/A";
                    string tipoDonante = "Institución";
                    string documentoDonante = "N/A";
                    DateTime fechaDonacion = DateTime.MinValue;
                    int donacionIdVal = 0;
                    int idDonanteVal = 0;

                    if (donacionesPorLote.TryGetValue(d.LoteId, out var donacionObj) && donacionObj != null)
                    {
                        donacionIdVal = donacionObj.Id;
                        fechaDonacion = donacionObj.FechaHora;

                        if (donacionObj.Donante != null)
                        {
                            idDonanteVal = donacionObj.Donante.Id;
                            nombreDonante = donacionObj.Donante.NombreRazonSocial;
                            tipoDonante = donacionObj.Donante.Tipo.ToString();
                            documentoDonante = donacionObj.Donante.NumeroDocumento ?? "N/A";
                        }
                    }

                    // Filtro secundario por Donante (si fue seleccionado en el desplegable)
                    if (donanteId.HasValue && idDonanteVal != donanteId.Value)
                    {
                        continue;
                    }

                    grupoDto.Items.Add(new TrazabilidadItemDto
                    {
                        LoteId = d.LoteId,
                        NumeroLote = d.Lote?.NumeroLote ?? "N/A",
                        NombreProducto = d.Lote?.Producto?.Nombre ?? "Alimento",
                        FechaVencimiento = d.Lote?.FechaVencimiento,
                        Cantidad = d.Cantidad,
                        UnidadMedida = d.Lote?.Producto?.UnidadMedida.ToString() ?? "kg",

                        DonacionId = donacionIdVal,
                        DonanteId = idDonanteVal,
                        NombreDonante = nombreDonante,
                        TipoDonante = tipoDonante,
                        DocumentoDonante = documentoDonante,
                        FechaDonacion = fechaDonacion
                    });
                }

                if (grupoDto.Items.Any())
                {
                    model.EntregasGrouped.Add(grupoDto);
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> BuscarSugerencias(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<string>());
            }

            // Invocación limpia de la función sugerir_trazabilidad de Supabase/PostgreSQL
            var sugerencias = await _context.Database
                .SqlQueryRaw<string>("SELECT * FROM sugerir_trazabilidad({0})", term)
                .ToListAsync();

            return Json(sugerencias);
        }
    }
}