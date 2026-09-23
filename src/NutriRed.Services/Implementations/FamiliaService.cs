using Microsoft.EntityFrameworkCore;
using NutriRed.Data;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.Common;
using NutriRed.Services.Interfaces;

namespace NutriRed.Services.Implementations;

public class FamiliaService : IFamiliaService
{
    private readonly NutriRedDbContext _context;

    public FamiliaService(NutriRedDbContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<IEnumerable<FamiliaBeneficiaria>>> ObtenerTodasAsync(bool soloActivas = true)
    {
        try
        {
            var query = _context.FamiliasBeneficiarias.AsNoTracking();

            if (soloActivas)
            {
                query = query.Where(f => f.Estado == EstadoFamilia.Activo);
            }

            var familias = await query
                .OrderBy(f => f.ApellidoTitular)
                .ThenBy(f => f.NombreTitular)
                .ToListAsync();

            return OperationResult<IEnumerable<FamiliaBeneficiaria>>.Ok(familias);
        }
        catch (Exception ex)
        {
            return OperationResult<IEnumerable<FamiliaBeneficiaria>>.Fail($"Error al obtener las familias beneficiarias: {ex.Message}");
        }
    }

    public async Task<OperationResult<FamiliaBeneficiaria>> ObtenerPorIdAsync(int id, bool incluirHistorial = false)
    {
        try
        {
            var query = _context.FamiliasBeneficiarias.AsNoTracking();

            if (incluirHistorial)
            {
                query = query
                    .Include(f => f.HistorialPaquetes.OrderByDescending(p => p.FechaCreacion))
                        .ThenInclude(p => p.TipoPaquete)
                    .Include(f => f.HistorialPaquetes)
                        .ThenInclude(p => p.Entrega);
            }

            var familia = await query.FirstOrDefaultAsync(f => f.Id == id);

            if (familia == null)
            {
                return OperationResult<FamiliaBeneficiaria>.Fail("La familia beneficiaria solicitada no existe.");
            }

            return OperationResult<FamiliaBeneficiaria>.Ok(familia);
        }
        catch (Exception ex)
        {
            return OperationResult<FamiliaBeneficiaria>.Fail($"Error al buscar la familia beneficiaria: {ex.Message}");
        }
    }

    public async Task<OperationResult<FamiliaBeneficiaria>> BuscarPorDniAsync(string dni)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dni))
            {
                return OperationResult<FamiliaBeneficiaria>.Fail("El DNI no puede estar vacío.");
            }

            var dniNormalizado = dni.Trim();

            var familia = await _context.FamiliasBeneficiarias
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.DniTitular == dniNormalizado);

            if (familia == null)
            {
                return OperationResult<FamiliaBeneficiaria>.Fail($"No se encontró ninguna familia registrada con el DNI '{dniNormalizado}'.");
            }

            return OperationResult<FamiliaBeneficiaria>.Ok(familia);
        }
        catch (Exception ex)
        {
            return OperationResult<FamiliaBeneficiaria>.Fail($"Error al consultar familia por DNI: {ex.Message}");
        }
    }

    public async Task<OperationResult<FamiliaBeneficiaria>> CrearAsync(FamiliaBeneficiaria familia)
    {
        try
        {
            var dniNormalizado = familia.DniTitular.Trim();

            // Regla de Negocio: DNI de titular único
            bool yaExiste = await _context.FamiliasBeneficiarias
                .AnyAsync(f => f.DniTitular.ToLower() == dniNormalizado.ToLower());

            if (yaExiste)
            {
                return OperationResult<FamiliaBeneficiaria>.Fail($"Ya existe una familia beneficiaria registrada con el DNI '{dniNormalizado}'.");
            }

            if (familia.CantidadIntegrantes < 1)
            {
                return OperationResult<FamiliaBeneficiaria>.Fail("La cantidad de integrantes debe ser al menos 1.");
            }

            familia.DniTitular = dniNormalizado;
            familia.NombreTitular = familia.NombreTitular.Trim();
            familia.ApellidoTitular = familia.ApellidoTitular.Trim();
            familia.Direccion = familia.Direccion.Trim();
            familia.Telefono = familia.Telefono?.Trim();
            familia.FechaAlta = DateTime.UtcNow;
            familia.Estado = EstadoFamilia.Activo;

            _context.FamiliasBeneficiarias.Add(familia);
            await _context.SaveChangesAsync();

            return OperationResult<FamiliaBeneficiaria>.Ok(familia);
        }
        catch (Exception ex)
        {
            return OperationResult<FamiliaBeneficiaria>.Fail($"Error al registrar la familia beneficiaria: {ex.Message}");
        }
    }

    public async Task<OperationResult<FamiliaBeneficiaria>> ActualizarAsync(FamiliaBeneficiaria familia)
    {
        try
        {
            var entidadExistente = await _context.FamiliasBeneficiarias.FindAsync(familia.Id);
            if (entidadExistente == null)
            {
                return OperationResult<FamiliaBeneficiaria>.Fail("La familia que intenta actualizar no existe.");
            }

            var dniNormalizado = familia.DniTitular.Trim();

            // Regla de Negocio: No duplicar DNI con otra familia
            bool dniDuplicado = await _context.FamiliasBeneficiarias
                .AnyAsync(f => f.Id != familia.Id && f.DniTitular.ToLower() == dniNormalizado.ToLower());

            if (dniDuplicado)
            {
                return OperationResult<FamiliaBeneficiaria>.Fail($"El DNI '{dniNormalizado}' ya pertenece a otra familia registrada.");
            }

            if (familia.CantidadIntegrantes < 1)
            {
                return OperationResult<FamiliaBeneficiaria>.Fail("La cantidad de integrantes debe ser al menos 1.");
            }

            entidadExistente.DniTitular = dniNormalizado;
            entidadExistente.NombreTitular = familia.NombreTitular.Trim();
            entidadExistente.ApellidoTitular = familia.ApellidoTitular.Trim();
            entidadExistente.Direccion = familia.Direccion.Trim();
            entidadExistente.Telefono = familia.Telefono?.Trim();
            entidadExistente.CantidadIntegrantes = familia.CantidadIntegrantes;
            entidadExistente.Estado = familia.Estado;

            await _context.SaveChangesAsync();

            return OperationResult<FamiliaBeneficiaria>.Ok(entidadExistente);
        }
        catch (Exception ex)
        {
            return OperationResult<FamiliaBeneficiaria>.Fail($"Error al actualizar la familia beneficiaria: {ex.Message}");
        }
    }

    public async Task<OperationResult> CambiarEstadoAsync(int id, EstadoFamilia nuevoEstado)
    {
        try
        {
            var familia = await _context.FamiliasBeneficiarias.FindAsync(id);
            if (familia == null)
            {
                return OperationResult.Fail("La familia beneficiaria solicitada no existe.");
            }

            familia.Estado = nuevoEstado;
            await _context.SaveChangesAsync();

            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Error al modificar el estado de la familia: {ex.Message}");
        }
    }
}
