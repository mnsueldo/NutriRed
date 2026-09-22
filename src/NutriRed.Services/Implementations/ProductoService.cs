using Microsoft.EntityFrameworkCore;
using NutriRed.Data;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;
using NutriRed.Services.Common;
using NutriRed.Services.Interfaces;

namespace NutriRed.Services.Implementations;

public class ProductoService : IProductoService
{
    private readonly NutriRedDbContext _context;

    public ProductoService(NutriRedDbContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<IEnumerable<Producto>>> ObtenerTodosAsync(bool soloActivos = true)
    {
        try
        {
            var query = _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.Lotes.Where(l => l.Estado == EstadoLote.Disponible && l.FechaVencimiento > DateTime.Today))
                .AsQueryable();

            if (soloActivos)
            {
                query = query.Where(p => p.Activo);
            }

            var productos = await query.OrderBy(p => p.Nombre).ToListAsync();
            return OperationResult<IEnumerable<Producto>>.Ok(productos);
        }
        catch (Exception ex)
        {
            return OperationResult<IEnumerable<Producto>>.Fail($"Error al obtener los productos: {ex.Message}");
        }
    }

    public async Task<OperationResult<Producto>> ObtenerPorIdAsync(int id)
    {
        try
        {
            var producto = await _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.Lotes.Where(l => l.CantidadDisponible > 0))
                .FirstOrDefaultAsync(p => p.Id == id);

            if (producto == null)
            {
                return OperationResult<Producto>.Fail("El producto solicitado no existe.");
            }

            return OperationResult<Producto>.Ok(producto);
        }
        catch (Exception ex)
        {
            return OperationResult<Producto>.Fail($"Error al buscar el producto: {ex.Message}");
        }
    }

    public async Task<OperationResult<Producto>> BuscarPorEanAsync(string ean)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ean))
            {
                return OperationResult<Producto>.Fail("El código de barras no puede estar vacío.");
            }

            var codigoNormalizado = ean.Trim();

            var producto = await _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.Lotes.Where(l => l.Estado == EstadoLote.Disponible && l.FechaVencimiento > DateTime.Today))
                .FirstOrDefaultAsync(p => p.CodigoBarras == codigoNormalizado);

            if (producto == null)
            {
                // Mensaje exacto requerido por la especificación de software NutriRed (pág. 7)
                return OperationResult<Producto>.Fail("Producto no registrado en el catálogo. Solicite su alta al administrador.");
            }

            if (!producto.Activo)
            {
                return OperationResult<Producto>.Fail("El producto se encuentra inactivo en el catálogo.");
            }

            return OperationResult<Producto>.Ok(producto);
        }
        catch (Exception ex)
        {
            return OperationResult<Producto>.Fail($"Error al consultar el código de barras: {ex.Message}");
        }
    }

    public async Task<OperationResult<IEnumerable<Producto>>> ObtenerPorCategoriaAsync(int categoriaId)
    {
        try
        {
            var productos = await _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Where(p => p.CategoriaId == categoriaId && p.Activo)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            return OperationResult<IEnumerable<Producto>>.Ok(productos);
        }
        catch (Exception ex)
        {
            return OperationResult<IEnumerable<Producto>>.Fail($"Error al consultar productos por categoría: {ex.Message}");
        }
    }

    public async Task<OperationResult<Producto>> CrearAsync(Producto producto)
    {
        try
        {
            var eanNormalizado = producto.CodigoBarras.Trim();

            // Regla de Negocio: Código EAN único
            bool eanExiste = await _context.Productos
                .AnyAsync(p => p.CodigoBarras.ToLower() == eanNormalizado.ToLower());

            if (eanExiste)
            {
                return OperationResult<Producto>.Fail($"Ya existe un producto registrado con el código de barras '{eanNormalizado}'.");
            }

            // Validar que la categoría exista y esté activa
            var categoriaExiste = await _context.Categorias
                .AnyAsync(c => c.Id == producto.CategoriaId && c.Activo);

            if (!categoriaExiste)
            {
                return OperationResult<Producto>.Fail("La categoría asignada no es válida o se encuentra inactiva.");
            }

            producto.CodigoBarras = eanNormalizado;
            producto.Nombre = producto.Nombre.Trim();
            producto.Descripcion = producto.Descripcion?.Trim();
            producto.Activo = true;

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return OperationResult<Producto>.Ok(producto);
        }
        catch (Exception ex)
        {
            return OperationResult<Producto>.Fail($"Error al registrar el producto: {ex.Message}");
        }
    }

    public async Task<OperationResult<Producto>> ActualizarAsync(Producto producto)
    {
        try
        {
            var entidadExistente = await _context.Productos.FindAsync(producto.Id);
            if (entidadExistente == null)
            {
                return OperationResult<Producto>.Fail("El producto que intenta actualizar no existe.");
            }

            var eanNormalizado = producto.CodigoBarras.Trim();

            // Regla de Negocio: No duplicar EAN con otro producto
            bool eanDuplicado = await _context.Productos
                .AnyAsync(p => p.Id != producto.Id && p.CodigoBarras.ToLower() == eanNormalizado.ToLower());

            if (eanDuplicado)
            {
                return OperationResult<Producto>.Fail($"El código de barras '{eanNormalizado}' ya está en uso por otro producto.");
            }

            // Validar categoría
            var categoriaExiste = await _context.Categorias
                .AnyAsync(c => c.Id == producto.CategoriaId && c.Activo);

            if (!categoriaExiste)
            {
                return OperationResult<Producto>.Fail("La categoría seleccionada no es válida o está inactiva.");
            }

            entidadExistente.CodigoBarras = eanNormalizado;
            entidadExistente.Nombre = producto.Nombre.Trim();
            entidadExistente.Descripcion = producto.Descripcion?.Trim();
            entidadExistente.CategoriaId = producto.CategoriaId;
            entidadExistente.UnidadMedida = producto.UnidadMedida;
            entidadExistente.StockMinimo = producto.StockMinimo;
            entidadExistente.Activo = producto.Activo;

            await _context.SaveChangesAsync();

            return OperationResult<Producto>.Ok(entidadExistente);
        }
        catch (Exception ex)
        {
            return OperationResult<Producto>.Fail($"Error al actualizar el producto: {ex.Message}");
        }
    }

    public async Task<OperationResult> DesactivarAsync(int id)
    {
        try
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return OperationResult.Fail("El producto solicitado no existe.");
            }

            // Baja lógica (Soft delete) para proteger la trazabilidad de lotes y donaciones
            producto.Activo = false;
            await _context.SaveChangesAsync();

            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Error al desactivar el producto: {ex.Message}");
        }
    }

    public async Task<OperationResult> ActivarAsync(int id)
    {
        try
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return OperationResult.Fail("El producto solicitado no existe.");
            }

            producto.Activo = true;
            await _context.SaveChangesAsync();

            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Error al activar el producto: {ex.Message}");
        }
    }
}
