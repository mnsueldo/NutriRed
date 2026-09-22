using Microsoft.EntityFrameworkCore;
using NutriRed.Data;
using NutriRed.Domain.Entities;
using NutriRed.Services.Common;
using NutriRed.Services.Interfaces;

namespace NutriRed.Services.Implementations;

public class CategoriaService : ICategoriaService
{
    private readonly NutriRedDbContext _context;

    public CategoriaService(NutriRedDbContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<IEnumerable<Categoria>>> ObtenerTodasAsync(bool soloActivas = true)
    {
        try
        {
            var query = _context.Categorias.AsNoTracking();

            if (soloActivas)
            {
                query = query.Where(c => c.Activo);
            }

            var categorias = await query.OrderBy(c => c.Nombre).ToListAsync();
            return OperationResult<IEnumerable<Categoria>>.Ok(categorias);
        }
        catch (Exception ex)
        {
            return OperationResult<IEnumerable<Categoria>>.Fail($"Error al obtener las categorías: {ex.Message}");
        }
    }

    public async Task<OperationResult<Categoria>> ObtenerPorIdAsync(int id)
    {
        try
        {
            var categoria = await _context.Categorias
                .AsNoTracking()
                .Include(c => c.Productos)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categoria == null)
            {
                return OperationResult<Categoria>.Fail("La categoría solicitada no existe.");
            }

            return OperationResult<Categoria>.Ok(categoria);
        }
        catch (Exception ex)
        {
            return OperationResult<Categoria>.Fail($"Error al buscar la categoría: {ex.Message}");
        }
    }

    public async Task<OperationResult<Categoria>> CrearAsync(Categoria categoria)
    {
        try
        {
            var nombreNormalizado = categoria.Nombre.Trim();

            // Regla de Negocio: No permitir categorías con nombres duplicados
            bool yaExiste = await _context.Categorias
                .AnyAsync(c => c.Nombre.ToLower() == nombreNormalizado.ToLower());

            if (yaExiste)
            {
                return OperationResult<Categoria>.Fail($"Ya existe una categoría registrada con el nombre '{nombreNormalizado}'.");
            }

            categoria.Nombre = nombreNormalizado;
            categoria.Descripcion = categoria.Descripcion?.Trim();

            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();

            return OperationResult<Categoria>.Ok(categoria);
        }
        catch (Exception ex)
        {
            return OperationResult<Categoria>.Fail($"Error al registrar la categoría: {ex.Message}");
        }
    }

    public async Task<OperationResult<Categoria>> ActualizarAsync(Categoria categoria)
    {
        try
        {
            var entidadExistente = await _context.Categorias.FindAsync(categoria.Id);
            if (entidadExistente == null)
            {
                return OperationResult<Categoria>.Fail("La categoría que intenta actualizar no existe.");
            }

            var nombreNormalizado = categoria.Nombre.Trim();

            // Regla de Negocio: No duplicar nombre con otra categoría distinta
            bool nombreDuplicado = await _context.Categorias
                .AnyAsync(c => c.Id != categoria.Id && c.Nombre.ToLower() == nombreNormalizado.ToLower());

            if (nombreDuplicado)
            {
                return OperationResult<Categoria>.Fail($"Ya existe otra categoría con el nombre '{nombreNormalizado}'.");
            }

            entidadExistente.Nombre = nombreNormalizado;
            entidadExistente.Descripcion = categoria.Descripcion?.Trim();
            entidadExistente.Activo = categoria.Activo;

            await _context.SaveChangesAsync();

            return OperationResult<Categoria>.Ok(entidadExistente);
        }
        catch (Exception ex)
        {
            return OperationResult<Categoria>.Fail($"Error al actualizar la categoría: {ex.Message}");
        }
    }

    public async Task<OperationResult> CambiarEstadoAsync(int id, bool activo)
    {
        try
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
            {
                return OperationResult.Fail("La categoría solicitada no existe.");
            }

            categoria.Activo = activo;
            await _context.SaveChangesAsync();

            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Error al modificar el estado de la categoría: {ex.Message}");
        }
    }
}
