using Microsoft.EntityFrameworkCore;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;

namespace NutriRed.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(NutriRedDbContext context)
    {
        // 1. Asegurar que la base de datos existe
        await context.Database.EnsureCreatedAsync();

        // Si ya existen categorías, consideramos que la base ya fue sembrada
        if (await context.Categorias.AnyAsync())
        {
            return;
        }

        // ==========================================
        // 1. CATEGORÍAS DE ALIMENTOS
        // ==========================================
        var catGranos = new Categoria { Nombre = "Legumbres, Granos y Cereales", Descripcion = "Arroz, lentejas, porotos, garbanzos, avena", Activo = true };
        var catLacteos = new Categoria { Nombre = "Lácteos y Derivados", Descripcion = "Leche fluida, en polvo, yogures y quesos", Activo = true };
        var catPastas = new Categoria { Nombre = "Harinas y Pastas Secas", Descripcion = "Fideos secos, harina de trigo, premezclas", Activo = true };
        var catAceites = new Categoria { Nombre = "Aceites y Grasas", Descripcion = "Aceite de girasol, maíz, oliva", Activo = true };
        var catConservas = new Categoria { Nombre = "Enlatados y Conservas", Descripcion = "Puré de tomate, arvejas, choclo, atún", Activo = true };
        var catInfusiones = new Categoria { Nombre = "Desayuno e Infusiones", Descripcion = "Té, mate cocido, azúcar, cacao", Activo = true };

        context.Categorias.AddRange(catGranos, catLacteos, catPastas, catAceites, catConservas, catInfusiones);
        await context.SaveChangesAsync();

        // ==========================================
        // 2. PRODUCTOS (EAN Comerciales comunes de Argentina)
        // ==========================================
        var prodArroz = new Producto
        {
            CodigoBarras = "7790070412345",
            Nombre = "Arroz Largo Fino 1kg",
            Descripcion = "Paquete arroz blanco 00000",
            CategoriaId = catGranos.Id,
            UnidadMedida = UnidadMedida.Kilogramos,
            StockMinimo = 20,
            Activo = true
        };

        var prodFideos = new Producto
        {
            CodigoBarras = "7790040112233",
            Nombre = "Fideos Guiseros Tirabuzón 500g",
            Descripcion = "Pasta seca de sémola",
            CategoriaId = catPastas.Id,
            UnidadMedida = UnidadMedida.Gramos,
            StockMinimo = 30,
            Activo = true
        };

        var prodAceite = new Producto
        {
            CodigoBarras = "7790272001011",
            Nombre = "Aceite de Girasol 900ml",
            Descripcion = "Botella PET 900ml",
            CategoriaId = catAceites.Id,
            UnidadMedida = UnidadMedida.Litros,
            StockMinimo = 15,
            Activo = true
        };

        var prodLeche = new Producto
        {
            CodigoBarras = "7793940000018",
            Nombre = "Leche Entera UAT 1L",
            Descripcion = "Tetra brik larga vida",
            CategoriaId = catLacteos.Id,
            UnidadMedida = UnidadMedida.Litros,
            StockMinimo = 40,
            Activo = true
        };

        var prodLentejas = new Producto
        {
            CodigoBarras = "7791234567890",
            Nombre = "Lentejas Secas Seleccionadas 400g",
            Descripcion = "Paquete legumbres secas",
            CategoriaId = catGranos.Id,
            UnidadMedida = UnidadMedida.Gramos,
            StockMinimo = 15,
            Activo = true
        };

        var prodTomate = new Producto
        {
            CodigoBarras = "7790580123456",
            Nombre = "Puré de Tomate Tetra 520g",
            Descripcion = "Puré de tomate listo para usar",
            CategoriaId = catConservas.Id,
            UnidadMedida = UnidadMedida.Gramos,
            StockMinimo = 25,
            Activo = true
        };

        var prodHarina = new Producto
        {
            CodigoBarras = "7790070554433",
            Nombre = "Harina de Trigo 000 1kg",
            Descripcion = "Paquete papel 1kg para panificados",
            CategoriaId = catPastas.Id,
            UnidadMedida = UnidadMedida.Kilogramos,
            StockMinimo = 20,
            Activo = true
        };

        var prodAzucar = new Producto
        {
            CodigoBarras = "7790080011223",
            Nombre = "Azúcar Blanco Común 1kg",
            Descripcion = "Bolsa 1kg azúcar tipo A",
            CategoriaId = catInfusiones.Id,
            UnidadMedida = UnidadMedida.Kilogramos,
            StockMinimo = 15,
            Activo = true
        };

        context.Productos.AddRange(prodArroz, prodFideos, prodAceite, prodLeche, prodLentejas, prodTomate, prodHarina, prodAzucar);
        await context.SaveChangesAsync();

        // ==========================================
        // 3. DONANTES
        // ==========================================
        var donanteMayorista = new Donante
        {
            Tipo = TipoDonante.Institucion,
            NumeroDocumento = "30-71123456-9",
            NombreRazonSocial = "Distribuidora Mayorista Alianza S.A.",
            Telefono = "011-4567-8900",
            Email = "donaciones@alianzamayorista.com.ar",
            Activo = true
        };

        var donanteSuper = new Donante
        {
            Tipo = TipoDonante.Institucion,
            NumeroDocumento = "30-58472910-3",
            NombreRazonSocial = "Cadena Supermercados del Centro",
            Telefono = "011-5544-3322",
            Email = "contacto@supercentro.com.ar",
            Activo = true
        };

        var donanteParticular = new Donante
        {
            Tipo = TipoDonante.Individuo,
            NumeroDocumento = "32.456.789",
            NombreRazonSocial = "Carlos Alberto Gómez",
            Telefono = "11-6234-9876",
            Email = "carlos.gomez@gmail.com",
            Activo = true
        };

        var donanteAnonimo = new Donante
        {
            Tipo = TipoDonante.Anonimo,
            NombreRazonSocial = "Donante Anónimo",
            Activo = true
        };

        context.Donantes.AddRange(donanteMayorista, donanteSuper, donanteParticular, donanteAnonimo);
        await context.SaveChangesAsync();

        // ==========================================
        // 4. LOTES CON STOCK Y FECHAS ESCALONADAS (Para probar FEFO)
        // ==========================================
        var hoy = DateTime.Today;

        // Leche: Un lote vence en 20 días y otro en 90 días
        var loteLechePronto = new Lote
        {
            NumeroLote = "LEC-2026-A1",
            ProductoId = prodLeche.Id,
            FechaVencimiento = hoy.AddDays(20), // Próximo a vencer (Debe salir primero según FEFO)
            CantidadInicial = 50,
            CantidadDisponible = 50,
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-5)
        };

        var loteLecheLejano = new Lote
        {
            NumeroLote = "LEC-2026-B2",
            ProductoId = prodLeche.Id,
            FechaVencimiento = hoy.AddDays(120), // Vence más tarde
            CantidadInicial = 100,
            CantidadDisponible = 100,
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-2)
        };

        // Arroz: Dos lotes con distintos vencimientos
        var loteArroz1 = new Lote
        {
            NumeroLote = "ARR-LOT-01",
            ProductoId = prodArroz.Id,
            FechaVencimiento = hoy.AddMonths(4),
            CantidadInicial = 80,
            CantidadDisponible = 80,
            Estado = EstadoLote.Disponible
        };

        var loteArroz2 = new Lote
        {
            NumeroLote = "ARR-LOT-02",
            ProductoId = prodArroz.Id,
            FechaVencimiento = hoy.AddMonths(10),
            CantidadInicial = 120,
            CantidadDisponible = 120,
            Estado = EstadoLote.Disponible
        };

        // Fideos
        var loteFideos = new Lote
        {
            NumeroLote = "FID-2026-05",
            ProductoId = prodFideos.Id,
            FechaVencimiento = hoy.AddMonths(6),
            CantidadInicial = 150,
            CantidadDisponible = 150,
            Estado = EstadoLote.Disponible
        };

        // Aceite
        var loteAceite = new Lote
        {
            NumeroLote = "ACE-900-X1",
            ProductoId = prodAceite.Id,
            FechaVencimiento = hoy.AddMonths(8),
            CantidadInicial = 60,
            CantidadDisponible = 60,
            Estado = EstadoLote.Disponible
        };

        // Puré de Tomate
        var loteTomate = new Lote
        {
            NumeroLote = "TOM-520-L3",
            ProductoId = prodTomate.Id,
            FechaVencimiento = hoy.AddMonths(5),
            CantidadInicial = 70,
            CantidadDisponible = 70,
            Estado = EstadoLote.Disponible
        };

        // Lentejas
        var loteLentejas = new Lote
        {
            NumeroLote = "LEN-400-A",
            ProductoId = prodLentejas.Id,
            FechaVencimiento = hoy.AddMonths(12),
            CantidadInicial = 40,
            CantidadDisponible = 40,
            Estado = EstadoLote.Disponible
        };

        // Harina
        var loteHarina = new Lote
        {
            NumeroLote = "HAR-1KG-09",
            ProductoId = prodHarina.Id,
            FechaVencimiento = hoy.AddMonths(3),
            CantidadInicial = 50,
            CantidadDisponible = 50,
            Estado = EstadoLote.Disponible
        };

        context.Lotes.AddRange(loteLechePronto, loteLecheLejano, loteArroz1, loteArroz2, loteFideos, loteAceite, loteTomate, loteLentejas, loteHarina);
        await context.SaveChangesAsync();

        // ==========================================
        // 5. MOVIMIENTOS AUDITABLES INICIALES
        // ==========================================
        var movimientos = new List<MovimientoStock>
        {
            new() { LoteId = loteLechePronto.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 50, UsuarioId = "admin@nutrired.org", Observaciones = "Ingreso inicial donación Alianza S.A." },
            new() { LoteId = loteLecheLejano.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 100, UsuarioId = "admin@nutrired.org", Observaciones = "Ingreso inicial donación Alianza S.A." },
            new() { LoteId = loteArroz1.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 80, UsuarioId = "admin@nutrired.org", Observaciones = "Ingreso inicial donación SuperCentro" },
            new() { LoteId = loteFideos.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 150, UsuarioId = "admin@nutrired.org", Observaciones = "Ingreso inicial donación Carlos Gómez" },
            new() { LoteId = loteAceite.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 60, UsuarioId = "admin@nutrired.org", Observaciones = "Ingreso inicial donación Alianza S.A." }
        };

        context.MovimientosStock.AddRange(movimientos);
        await context.SaveChangesAsync();

        // ==========================================
        // 6. PLANTILLAS DE PAQUETES (Kits Nutricionales)
        // ==========================================
        var kitBasico = new TipoPaquete
        {
            Nombre = "Kit Nutricional Básico (1 a 3 Integrantes)",
            Descripcion = "Ración esencial quincenal para familias pequeñas",
            MinIntegrantes = 1,
            MaxIntegrantes = 3,
            Activo = true
        };

        var kitFamiliar = new TipoPaquete
        {
            Nombre = "Kit Nutricional Familiar (4 o más Integrantes)",
            Descripcion = "Ración reforzada quincenal para familias numerosas",
            MinIntegrantes = 4,
            MaxIntegrantes = 20,
            Activo = true
        };

        context.TiposPaquete.AddRange(kitBasico, kitFamiliar);
        await context.SaveChangesAsync();

        // Composición de las plantillas de paquetes
        context.PlantillaPaqueteDetalles.AddRange(
            // Items Kit Básico
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitBasico.Id, ProductoId = prodArroz.Id, CantidadRequerida = 1 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitBasico.Id, ProductoId = prodFideos.Id, CantidadRequerida = 2 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitBasico.Id, ProductoId = prodAceite.Id, CantidadRequerida = 1 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitBasico.Id, ProductoId = prodLeche.Id, CantidadRequerida = 2 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitBasico.Id, ProductoId = prodTomate.Id, CantidadRequerida = 2 },

            // Items Kit Familiar
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodArroz.Id, CantidadRequerida = 2 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodFideos.Id, CantidadRequerida = 4 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodAceite.Id, CantidadRequerida = 2 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodLeche.Id, CantidadRequerida = 4 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodLentejas.Id, CantidadRequerida = 2 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodHarina.Id, CantidadRequerida = 2 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodTomate.Id, CantidadRequerida = 3 }
        );
        await context.SaveChangesAsync();

        // ==========================================
        // 7. FAMILIAS BENEFICIARIAS
        // ==========================================
        var familia1 = new FamiliaBeneficiaria
        {
            DniTitular = "28.456.789",
            NombreTitular = "Mario",
            ApellidoTitular = "González",
            Telefono = "11-4567-8901",
            Direccion = "Calle 14 nro 543, Barrio Esperanza",
            CantidadIntegrantes = 3, // Debería corresponderle Kit Básico
            Estado = EstadoFamilia.Activo
        };

        var familia2 = new FamiliaBeneficiaria
        {
            DniTitular = "32.789.123",
            NombreTitular = "Romina",
            ApellidoTitular = "Fernández",
            Telefono = "11-9876-5432",
            Direccion = "Av. San Martín 2450, Piso 1 Dpto B",
            CantidadIntegrantes = 5, // Debería corresponderle Kit Familiar
            Estado = EstadoFamilia.Activo
        };

        var familia3 = new FamiliaBeneficiaria
        {
            DniTitular = "24.321.654",
            NombreTitular = "Carlos",
            ApellidoTitular = "Benítez",
            Telefono = "11-3322-1144",
            Direccion = "Pasaje Los Álamos 112",
            CantidadIntegrantes = 2, // Debería corresponderle Kit Básico
            Estado = EstadoFamilia.Activo
        };

        context.FamiliasBeneficiarias.AddRange(familia1, familia2, familia3);
        await context.SaveChangesAsync();
    }
}
