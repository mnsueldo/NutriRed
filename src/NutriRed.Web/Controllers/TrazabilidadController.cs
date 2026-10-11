using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NutriRed.Data;
using NutriRed.Domain.Entities;
using NutriRed.Web.Models;

namespace NutriRed.Web.Controllers
{
    [Authorize]
    public class TrazabilidadController : Controller
    {
        private readonly NutriRedDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TrazabilidadController(NutriRedDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            string? busqueda,
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            int? donanteId,
            int? tipoPaqueteId,
            string? voluntarioId)
        {
            var model = new TrazabilidadFiltroViewModel
            {
                Busqueda = busqueda,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                DonanteId = donanteId,
                TipoPaqueteId = tipoPaqueteId,
                VoluntarioId = voluntarioId
            };

            // 1. Cargar desplegables
            model.Donantes = await _context.Donantes
                .Where(d => d.Activo)
                .OrderBy(d => d.NombreRazonSocial)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.NombreRazonSocial })
                .ToListAsync();

            model.TiposPaquete = await _context.TiposPaquete
                .Where(t => t.Activo)
                .OrderBy(t => t.Nombre)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Nombre })
                .ToListAsync();

            // Desplegable de Voluntarios (solo rol Voluntario + históricos)
            var usuariosVoluntarios = await _userManager.GetUsersInRoleAsync("Voluntario");
            var voluntariosUsersList = usuariosVoluntarios
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = !string.IsNullOrWhiteSpace(u.NombreCompleto) ? u.NombreCompleto : u.UserName
                })
                .ToList();

            var voluntariosEntregas = await _context.Entregas
                .Where(e => !string.IsNullOrEmpty(e.VoluntarioDespachoId))
                .Select(e => e.VoluntarioDespachoId)
                .Distinct()
                .ToListAsync();

            foreach (var vNombre in voluntariosEntregas)
            {
                if (vNombre.Contains("Administrador", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!voluntariosUsersList.Any(x => x.Value == vNombre || x.Text.Equals(vNombre, StringComparison.OrdinalIgnoreCase)))
                {
                    voluntariosUsersList.Add(new SelectListItem { Value = vNombre, Text = vNombre });
                }
            }
            model.Voluntarios = voluntariosUsersList.OrderBy(x => x.Text).ToList();

            // 2. Verificar si hay filtros aplicados
            bool tieneFiltros = !string.IsNullOrWhiteSpace(busqueda) ||
                               fechaDesde.HasValue ||
                               fechaHasta.HasValue ||
                               donanteId.HasValue ||
                               tipoPaqueteId.HasValue ||
                               !string.IsNullOrEmpty(voluntarioId);

            // SI NO HAY FILTROS APLICADOS (ej: al presionar "Limpiar"), RETORNAMOS LA VISTA VACÍA
            if (!tieneFiltros)
            {
                return View(model);
            }

            // 3. Consulta filtrada
            var query = _context.Entregas
                .Include(e => e.Paquete).ThenInclude(p => p.FamiliaBeneficiaria)
                .Include(e => e.Paquete).ThenInclude(p => p.TipoPaquete)
                .Include(e => e.Paquete).ThenInclude(p => p.Detalles).ThenInclude(d => d.Lote).ThenInclude(l => l.Producto)
                .AsQueryable();

            if (fechaDesde.HasValue)
                query = query.Where(e => e.FechaHoraEntrega >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                query = query.Where(e => e.FechaHoraEntrega <= fechaHasta.Value.AddDays(1).AddTicks(-1));

            if (tipoPaqueteId.HasValue)
                query = query.Where(e => e.Paquete.TipoPaqueteId == tipoPaqueteId.Value);

            if (!string.IsNullOrEmpty(voluntarioId))
            {
                var userObj = await _context.Users.FirstOrDefaultAsync(u => u.Id == voluntarioId);
                if (userObj != null)
                {
                    query = query.Where(e =>
                        e.VoluntarioDespachoId == userObj.Id ||
                        e.VoluntarioDespachoId == userObj.NombreCompleto ||
                        e.VoluntarioDespachoId == userObj.UserName
                    );
                }
                else
                {
                    query = query.Where(e => e.VoluntarioDespachoId == voluntarioId);
                }
            }

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var term = busqueda.Trim().ToLower();

                if (term.Contains("(dni:") && term.EndsWith(")"))
                {
                    var inicioDni = term.IndexOf("(dni:") + 5;
                    var finDni = term.IndexOf(")", inicioDni);
                    if (finDni > inicioDni)
                    {
                        var dniExtraido = term.Substring(inicioDni, finDni - inicioDni).Trim();
                        query = query.Where(e =>
                            (e.Paquete.FamiliaBeneficiaria != null && e.Paquete.FamiliaBeneficiaria.DniTitular == dniExtraido) ||
                            e.DniReceptor == dniExtraido
                        );
                    }
                }
                else
                {
                    query = query.Where(e =>
                        EF.Functions.Like(e.Paquete.CodigoSeguimiento.ToLower(), $"%{term}%") ||
                        (e.Paquete.FamiliaBeneficiaria != null && (
                            EF.Functions.Like(e.Paquete.FamiliaBeneficiaria.ApellidoTitular.ToLower(), $"%{term}%") ||
                            EF.Functions.Like(e.Paquete.FamiliaBeneficiaria.NombreTitular.ToLower(), $"%{term}%") ||
                            EF.Functions.Like(e.Paquete.FamiliaBeneficiaria.DniTitular, $"%{term}%")
                        )) ||
                        e.Paquete.Detalles.Any(d => EF.Functions.Like(d.Lote.NumeroLote.ToLower(), $"%{term}%") || EF.Functions.Like(d.Lote.Producto.Nombre.ToLower(), $"%{term}%")) ||
                        EF.Functions.Like(e.NombreReceptor.ToLower(), $"%{term}%") ||
                        EF.Functions.Like(e.DniReceptor, $"%{term}%")
                    );
                }
            }

            var entregas = await query.OrderByDescending(e => e.FechaHoraEntrega).Take(20).ToListAsync();

            // 4. Mapear Donantes y Usuarios
            var loteIds = entregas
                .SelectMany(e => e.Paquete?.Detalles ?? Enumerable.Empty<NutriRed.Domain.Entities.PaqueteDetalle>())
                .Select(d => d.LoteId)
                .Distinct()
                .ToList();

            var donacionesPorLote = await _context.DonacionDetalles
                .Include(dd => dd.Donacion).ThenInclude(don => don.Donante)
                .Where(dd => loteIds.Contains(dd.LoteId))
                .GroupBy(dd => dd.LoteId)
                .ToDictionaryAsync(g => g.Key, g => g.Select(dd => dd.Donacion).FirstOrDefault());

            var usuarioIds = entregas.Select(e => e.VoluntarioDespachoId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            var usuarios = await _context.Users
                .Where(u => usuarioIds.Contains(u.Id) || usuarioIds.Contains(u.NombreCompleto) || usuarioIds.Contains(u.UserName))
                .ToListAsync();

            // 5. Armar DTOs
            foreach (var e in entregas)
            {
                var fam = e.Paquete?.FamiliaBeneficiaria;
                string voluntarioNombre = "Voluntario General";
                string voluntarioUserName = "voluntario";

                if (!string.IsNullOrWhiteSpace(e.VoluntarioDespachoId))
                {
                    var userObj = usuarios.FirstOrDefault(u =>
                        u.Id == e.VoluntarioDespachoId ||
                        u.NombreCompleto.Equals(e.VoluntarioDespachoId, StringComparison.OrdinalIgnoreCase) ||
                        u.UserName.Equals(e.VoluntarioDespachoId, StringComparison.OrdinalIgnoreCase));

                    if (userObj != null)
                    {
                        voluntarioNombre = !string.IsNullOrWhiteSpace(userObj.NombreCompleto) ? userObj.NombreCompleto : userObj.UserName;
                        voluntarioUserName = userObj.UserName ?? "voluntario";
                    }
                    else
                    {
                        voluntarioNombre = e.VoluntarioDespachoId;
                        voluntarioUserName = e.VoluntarioDespachoId.ToLower().Replace(" ", "_");
                    }
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

                    if (donanteId.HasValue && idDonanteVal != donanteId.Value)
                        continue;

                    grupoDto.Items.Add(new TrazabilidadItemDto
                    {
                        LoteId = d.LoteId,
                        NumeroLote = d.Lote?.NumeroLote ?? "N/A",
                        NombreProducto = d.Lote?.Producto?.Nombre ?? "Alimento",
                        CodigoEan = d.Lote?.Producto?.CodigoBarras ?? "N/A", // <-- Asignamos el Código EAN
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

                model.EntregasGrouped.Add(grupoDto);
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