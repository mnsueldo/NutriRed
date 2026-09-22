using Microsoft.EntityFrameworkCore;
using NutriRed.Domain.Entities;

namespace NutriRed.Data;

public class NutriRedDbContext : DbContext
{
    public NutriRedDbContext(DbContextOptions<NutriRedDbContext> options) : base(options)
    {
    }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Lote> Lotes => Set<Lote>();
    public DbSet<Donante> Donantes => Set<Donante>();
    public DbSet<Donacion> Donaciones => Set<Donacion>();
    public DbSet<DonacionDetalle> DonacionDetalles => Set<DonacionDetalle>();
    public DbSet<MovimientoStock> MovimientosStock => Set<MovimientoStock>();
    public DbSet<FamiliaBeneficiaria> FamiliasBeneficiarias => Set<FamiliaBeneficiaria>();
    public DbSet<TipoPaquete> TiposPaquete => Set<TipoPaquete>();
    public DbSet<PlantillaPaqueteDetalle> PlantillaPaqueteDetalles => Set<PlantillaPaqueteDetalle>();
    public DbSet<Paquete> Paquetes => Set<Paquete>();
    public DbSet<PaqueteDetalle> PaqueteDetalles => Set<PaqueteDetalle>();
    public DbSet<Entrega> Entregas => Set<Entrega>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Índices Únicos para Reglas de Negocio ---
        modelBuilder.Entity<Producto>()
            .HasIndex(p => p.CodigoBarras)
            .IsUnique();

        modelBuilder.Entity<Donacion>()
            .HasIndex(d => d.CodigoComprobante)
            .IsUnique();

        modelBuilder.Entity<Paquete>()
            .HasIndex(p => p.CodigoSeguimiento)
            .IsUnique();

        // --- Configuración de Precisión Decimal (18,2) para SQL Server ---
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }

        // --- Restricciones de Borrado para evitar ciclos en SQL Server ---
        modelBuilder.Entity<DonacionDetalle>()
            .HasOne(d => d.Producto)
            .WithMany()
            .HasForeignKey(d => d.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DonacionDetalle>()
            .HasOne(d => d.Lote)
            .WithMany()
            .HasForeignKey(d => d.LoteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaqueteDetalle>()
            .HasOne(pd => pd.Producto)
            .WithMany()
            .HasForeignKey(pd => pd.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaqueteDetalle>()
            .HasOne(pd => pd.Lote)
            .WithMany()
            .HasForeignKey(pd => pd.LoteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaqueteDetalle>()
            .HasOne(pd => pd.ProductoSustituido)
            .WithMany()
            .HasForeignKey(pd => pd.ProductoSustituidoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Entrega>()
            .HasOne(e => e.Paquete)
            .WithOne(p => p.Entrega)
            .HasForeignKey<Entrega>(e => e.PaqueteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
