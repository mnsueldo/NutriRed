using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NutriRed.Domain.Entities;
using NutriRed.Domain.Enums;

namespace NutriRed.Data;

public static class DbInitializer
{
    private const string FirmaDemoBase64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAMgAAABkCAYAAADDhn8LAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAFTSURBVHhe7cExAQAAAMKg9U9tDQ8gAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgIsBFecAAafqH7MAAAAASUVORK5CYII=";

    public static async Task SeedAsync(NutriRedDbContext context)
    {
        // 1. Asegurar que la base de datos existe
        await context.Database.EnsureCreatedAsync();

        // Si ya existen las 5 familias oficiales y el catálogo completo (incluyendo Cacao con stock crítico), la base ya tiene el dataset exhaustivo
        if (await context.FamiliasBeneficiarias.AnyAsync(f => f.DniTitular == "20987654") &&
            await context.Productos.AnyAsync(p => p.CodigoBarras == "7790080022331"))
        {
            return;
        }

        // Si la base tiene datos previos parciales o de pruebas anteriores, limpiamos en orden de dependencias para garantizar integridad referencial
        if (await context.Categorias.AnyAsync())
        {
            context.Entregas.RemoveRange(context.Entregas);
            context.PaqueteDetalles.RemoveRange(context.PaqueteDetalles);
            context.Paquetes.RemoveRange(context.Paquetes);
            context.PlantillaPaqueteDetalles.RemoveRange(context.PlantillaPaqueteDetalles);
            context.TiposPaquete.RemoveRange(context.TiposPaquete);
            context.FamiliasBeneficiarias.RemoveRange(context.FamiliasBeneficiarias);
            context.MovimientosStock.RemoveRange(context.MovimientosStock);
            context.DonacionDetalles.RemoveRange(context.DonacionDetalles);
            context.Donaciones.RemoveRange(context.Donaciones);
            context.Lotes.RemoveRange(context.Lotes);
            context.Productos.RemoveRange(context.Productos);
            context.Donantes.RemoveRange(context.Donantes);
            context.Categorias.RemoveRange(context.Categorias);
            await context.SaveChangesAsync();
        }

        // ==========================================
        // 1. CATEGORÍAS DE ALIMENTOS
        // ==========================================
        var catGranos = new Categoria { Nombre = "Legumbres, Granos y Cereales", Descripcion = "Arroz, lentejas, porotos, garbanzos, avena", Activo = true };
        var catLacteos = new Categoria { Nombre = "Lácteos y Derivados", Descripcion = "Leche fluida, en polvo, quesos y derivados", Activo = true };
        var catPastas = new Categoria { Nombre = "Harinas y Pastas Secas", Descripcion = "Fideos secos, harina de trigo, premezclas", Activo = true };
        var catAceites = new Categoria { Nombre = "Aceites y Grasas", Descripcion = "Aceite de girasol, maíz, oliva", Activo = true };
        var catConservas = new Categoria { Nombre = "Enlatados y Conservas", Descripcion = "Puré de tomate, arvejas, choclo, atún", Activo = true };
        var catInfusiones = new Categoria { Nombre = "Desayuno e Infusiones", Descripcion = "Té, mate cocido, yerba mate, azúcar, cacao", Activo = true };

        context.Categorias.AddRange(catGranos, catLacteos, catPastas, catAceites, catConservas, catInfusiones);
        await context.SaveChangesAsync();

        // ==========================================
        // 2. PRODUCTOS (EAN Comerciales reales de Argentina)
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
            Descripcion = "Pasta seca de sémola de trigo candeal",
            CategoriaId = catPastas.Id,
            UnidadMedida = UnidadMedida.Gramos,
            StockMinimo = 30,
            Activo = true
        };

        var prodSpaghetti = new Producto
        {
            CodigoBarras = "7790040112240",
            Nombre = "Fideos Spaghetti 500g",
            Descripcion = "Pasta seca larga tipo tallarín",
            CategoriaId = catPastas.Id,
            UnidadMedida = UnidadMedida.Gramos,
            StockMinimo = 25,
            Activo = true
        };

        var prodAceite = new Producto
        {
            CodigoBarras = "7790272001011",
            Nombre = "Aceite de Girasol 900ml",
            Descripcion = "Botella PET 900ml refinado",
            CategoriaId = catAceites.Id,
            UnidadMedida = UnidadMedida.Litros,
            StockMinimo = 15,
            Activo = true
        };

        var prodLeche = new Producto
        {
            CodigoBarras = "7793940000018",
            Nombre = "Leche Entera UAT 1L",
            Descripcion = "Tetra brik larga vida homogeneizada",
            CategoriaId = catLacteos.Id,
            UnidadMedida = UnidadMedida.Litros,
            StockMinimo = 40,
            Activo = true
        };

        var prodLechePolvo = new Producto
        {
            CodigoBarras = "7793940000025",
            Nombre = "Leche en Polvo Entera 400g",
            Descripcion = "Bolsa aluminizada fortificada con vitaminas",
            CategoriaId = catLacteos.Id,
            UnidadMedida = UnidadMedida.Gramos,
            StockMinimo = 15,
            Activo = true
        };

        var prodLentejas = new Producto
        {
            CodigoBarras = "7791234567890",
            Nombre = "Lentejas Secas Seleccionadas 400g",
            Descripcion = "Paquete legumbres secas calidad superior",
            CategoriaId = catGranos.Id,
            UnidadMedida = UnidadMedida.Gramos,
            StockMinimo = 15,
            Activo = true
        };

        var prodGarbanzos = new Producto
        {
            CodigoBarras = "7791234567807",
            Nombre = "Garbanzos Secos Seleccionados 400g",
            Descripcion = "Paquete legumbres secas para guisados",
            CategoriaId = catGranos.Id,
            UnidadMedida = UnidadMedida.Gramos,
            StockMinimo = 15,
            Activo = true
        };

        var prodTomate = new Producto
        {
            CodigoBarras = "7790580123456",
            Nombre = "Puré de Tomate Tetra 520g",
            Descripcion = "Puré de tomate listo para usar sin TACC",
            CategoriaId = catConservas.Id,
            UnidadMedida = UnidadMedida.Gramos,
            StockMinimo = 25,
            Activo = true
        };

        var prodArvejas = new Producto
        {
            CodigoBarras = "7790580987654",
            Nombre = "Arvejas en Lata 300g",
            Descripcion = "Lata con conserva de arvejas tiernas",
            CategoriaId = catConservas.Id,
            UnidadMedida = UnidadMedida.Gramos,
            StockMinimo = 20,
            Activo = true
        };

        var prodAtun = new Producto
        {
            CodigoBarras = "7798123456789",
            Nombre = "Atún al Natural en Lata 170g",
            Descripcion = "Lomo de atún desmenuzado en agua y sal",
            CategoriaId = catConservas.Id,
            UnidadMedida = UnidadMedida.Gramos,
            StockMinimo = 15,
            Activo = true
        };

        var prodHarina = new Producto
        {
            CodigoBarras = "7790070554433",
            Nombre = "Harina de Trigo 000 1kg",
            Descripcion = "Paquete papel 1kg para panificados y pastas",
            CategoriaId = catPastas.Id,
            UnidadMedida = UnidadMedida.Kilogramos,
            StockMinimo = 20,
            Activo = true
        };

        var prodAzucar = new Producto
        {
            CodigoBarras = "7790080011223",
            Nombre = "Azúcar Blanco Común 1kg",
            Descripcion = "Bolsa 1kg azúcar tipo A primera calidad",
            CategoriaId = catInfusiones.Id,
            UnidadMedida = UnidadMedida.Kilogramos,
            StockMinimo = 15,
            Activo = true
        };

        var prodYerba = new Producto
        {
            CodigoBarras = "7790290001234",
            Nombre = "Yerba Mate Tradicional 500g",
            Descripcion = "Paquete 500g con palo estacionamiento natural",
            CategoriaId = catInfusiones.Id,
            UnidadMedida = UnidadMedida.Gramos,
            StockMinimo = 20,
            Activo = true
        };

        var prodCacao = new Producto
        {
            CodigoBarras = "7790080022331",
            Nombre = "Cacao en Polvo Chocolatado 360g",
            Descripcion = "Bolsa doypack cacao dulce instantáneo para desayuno",
            CategoriaId = catInfusiones.Id,
            UnidadMedida = UnidadMedida.Gramos,
            StockMinimo = 10, // Stock mínimo 10; tendrá stock actual 4 (dispara alerta de stock crítico)
            Activo = true
        };

        context.Productos.AddRange(
            prodArroz, prodFideos, prodSpaghetti, prodAceite, prodLeche,
            prodLechePolvo, prodLentejas, prodGarbanzos, prodTomate, prodArvejas,
            prodAtun, prodHarina, prodAzucar, prodYerba, prodCacao
        );
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
        // 4. LOTES CON STOCK Y FECHAS ESCALONADAS (Para probar FEFO y Semáforos)
        // ==========================================
        var hoy = DateTime.Today;

        // Leche: Un lote próximo (vence en 5 días -> URGENTE FEFO), otro a 120 días
        var loteLechePronto = new Lote
        {
            NumeroLote = "LEC-2026-A1",
            ProductoId = prodLeche.Id,
            FechaVencimiento = hoy.AddDays(5),
            CantidadInicial = 60,
            CantidadDisponible = 46, // 60 - 14 consumidos en paquetes
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-20)
        };

        var loteLecheLejano = new Lote
        {
            NumeroLote = "LEC-2026-B2",
            ProductoId = prodLeche.Id,
            FechaVencimiento = hoy.AddDays(120),
            CantidadInicial = 120,
            CantidadDisponible = 120,
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-2)
        };

        // Leche en Polvo
        var loteLechePolvo = new Lote
        {
            NumeroLote = "LEP-400-P1",
            ProductoId = prodLechePolvo.Id,
            FechaVencimiento = hoy.AddMonths(10),
            CantidadInicial = 40,
            CantidadDisponible = 40,
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-2)
        };

        // Arroz: Lote 1 a 4 meses, Lote 2 a 10 meses
        var loteArroz1 = new Lote
        {
            NumeroLote = "ARR-LOT-01",
            ProductoId = prodArroz.Id,
            FechaVencimiento = hoy.AddMonths(4),
            CantidadInicial = 100,
            CantidadDisponible = 93, // 100 - 7 consumidos en paquetes
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-20)
        };

        var loteArroz2 = new Lote
        {
            NumeroLote = "ARR-LOT-02",
            ProductoId = prodArroz.Id,
            FechaVencimiento = hoy.AddMonths(10),
            CantidadInicial = 150,
            CantidadDisponible = 150,
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-2)
        };

        // Fideos Tirabuzón: Uno activo y uno vencido para auditoría
        var loteFideos1 = new Lote
        {
            NumeroLote = "FID-2026-05",
            ProductoId = prodFideos.Id,
            FechaVencimiento = hoy.AddMonths(6),
            CantidadInicial = 150,
            CantidadDisponible = 139, // 150 - 11 consumidos en paquetes
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-14)
        };

        var loteFideosVencido = new Lote
        {
            NumeroLote = "FID-2025-OLD",
            ProductoId = prodFideos.Id,
            FechaVencimiento = hoy.AddDays(-15),
            CantidadInicial = 20,
            CantidadDisponible = 0,
            Estado = EstadoLote.Vencido,
            FechaIngreso = DateTime.UtcNow.AddMonths(-6)
        };

        // Fideos Spaghetti
        var loteSpaghetti = new Lote
        {
            NumeroLote = "SPA-2026-01",
            ProductoId = prodSpaghetti.Id,
            FechaVencimiento = hoy.AddMonths(7),
            CantidadInicial = 80,
            CantidadDisponible = 76, // 80 - 4 consumidos en paquetes
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-2)
        };

        // Aceite
        var loteAceite = new Lote
        {
            NumeroLote = "ACE-900-X1",
            ProductoId = prodAceite.Id,
            FechaVencimiento = hoy.AddMonths(8),
            CantidadInicial = 80,
            CantidadDisponible = 73, // 80 - 7 consumidos en paquetes
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-20)
        };

        // Puré de Tomate: Uno en atención (14 días -> ATENCIÓN FEFO) y otro a 6 meses
        var loteTomatePronto = new Lote
        {
            NumeroLote = "TOM-520-L1",
            ProductoId = prodTomate.Id,
            FechaVencimiento = hoy.AddDays(14),
            CantidadInicial = 80,
            CantidadDisponible = 67, // 80 - 11 consumidos - 2 por merma = 67
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-14)
        };

        var loteTomateLejano = new Lote
        {
            NumeroLote = "TOM-520-L2",
            ProductoId = prodTomate.Id,
            FechaVencimiento = hoy.AddMonths(6),
            CantidadInicial = 100,
            CantidadDisponible = 100,
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-2)
        };

        // Lentejas
        var loteLentejas = new Lote
        {
            NumeroLote = "LEN-400-A",
            ProductoId = prodLentejas.Id,
            FechaVencimiento = hoy.AddMonths(12),
            CantidadInicial = 60,
            CantidadDisponible = 56, // 60 - 4 consumidos
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-2)
        };

        // Garbanzos
        var loteGarbanzos = new Lote
        {
            NumeroLote = "GAR-400-B",
            ProductoId = prodGarbanzos.Id,
            FechaVencimiento = hoy.AddMonths(11),
            CantidadInicial = 50,
            CantidadDisponible = 50,
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-2)
        };

        // Arvejas
        var loteArvejas = new Lote
        {
            NumeroLote = "ARV-300-K1",
            ProductoId = prodArvejas.Id,
            FechaVencimiento = hoy.AddMonths(9),
            CantidadInicial = 60,
            CantidadDisponible = 56, // 60 - 4 consumidos
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-14)
        };

        // Atún
        var loteAtun = new Lote
        {
            NumeroLote = "ATU-170-M1",
            ProductoId = prodAtun.Id,
            FechaVencimiento = hoy.AddMonths(14),
            CantidadInicial = 50,
            CantidadDisponible = 46, // 50 - 4 consumidos
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-14)
        };

        // Harina
        var loteHarina = new Lote
        {
            NumeroLote = "HAR-1KG-09",
            ProductoId = prodHarina.Id,
            FechaVencimiento = hoy.AddMonths(3),
            CantidadInicial = 70,
            CantidadDisponible = 66, // 70 - 4 consumidos
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-20)
        };

        // Azúcar
        var loteAzucar = new Lote
        {
            NumeroLote = "AZU-1KG-22",
            ProductoId = prodAzucar.Id,
            FechaVencimiento = hoy.AddMonths(15),
            CantidadInicial = 60,
            CantidadDisponible = 53, // 60 - 7 consumidos
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-7)
        };

        // Yerba Mate
        var loteYerba = new Lote
        {
            NumeroLote = "YER-500-Z3",
            ProductoId = prodYerba.Id,
            FechaVencimiento = hoy.AddMonths(8),
            CantidadInicial = 50,
            CantidadDisponible = 47, // 50 - 3 consumidos
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-7)
        };

        // Cacao en Polvo: STOCK CRÍTICO (Solo 4 unidades vs Stock Mínimo de 10)
        var loteCacao = new Lote
        {
            NumeroLote = "CAC-360-C1",
            ProductoId = prodCacao.Id,
            FechaVencimiento = hoy.AddMonths(5),
            CantidadInicial = 4,
            CantidadDisponible = 4,
            Estado = EstadoLote.Disponible,
            FechaIngreso = DateTime.UtcNow.AddDays(-7)
        };

        context.Lotes.AddRange(
            loteLechePronto, loteLecheLejano, loteLechePolvo,
            loteArroz1, loteArroz2,
            loteFideos1, loteFideosVencido, loteSpaghetti,
            loteAceite,
            loteTomatePronto, loteTomateLejano,
            loteLentejas, loteGarbanzos,
            loteArvejas, loteAtun,
            loteHarina, loteAzucar, loteYerba, loteCacao
        );
        await context.SaveChangesAsync();

        // ==========================================
        // 5. DONACIONES HISTÓRICAS Y DETALLES (RF1)
        // ==========================================
        var donacion1 = new Donacion
        {
            CodigoComprobante = "DON-2026-00001",
            FechaHora = DateTime.UtcNow.AddDays(-20),
            DonanteId = donanteMayorista.Id,
            VoluntarioReceptorId = "Laura Gómez",
            Observaciones = "Convenio mensual institucional de alimentos secos y lácteos."
        };

        var donacion2 = new Donacion
        {
            CodigoComprobante = "DON-2026-00002",
            FechaHora = DateTime.UtcNow.AddDays(-14),
            DonanteId = donanteSuper.Id,
            VoluntarioReceptorId = "Pablo Cavallero",
            Observaciones = "Campaña solidaria en sucursales - Enlatados y fideos secos."
        };

        var donacion3 = new Donacion
        {
            CodigoComprobante = "DON-2026-00003",
            FechaHora = DateTime.UtcNow.AddDays(-7),
            DonanteId = donanteParticular.Id,
            VoluntarioReceptorId = "Martín Sueldo",
            Observaciones = "Donación particular destinada a desayunos comunitarios."
        };

        var donacion4 = new Donacion
        {
            CodigoComprobante = "DON-2026-00004",
            FechaHora = DateTime.UtcNow.AddDays(-2),
            DonanteId = donanteAnonimo.Id,
            VoluntarioReceptorId = "Rosaura Limache Caballero",
            Observaciones = "Colecta comunitaria barrial de alimentos no perecederos."
        };

        context.Donaciones.AddRange(donacion1, donacion2, donacion3, donacion4);
        await context.SaveChangesAsync();

        // Detalles de donaciones
        context.DonacionDetalles.AddRange(
            // DON-1
            new DonacionDetalle { DonacionId = donacion1.Id, ProductoId = prodLeche.Id, LoteId = loteLechePronto.Id, Cantidad = 60 },
            new DonacionDetalle { DonacionId = donacion1.Id, ProductoId = prodArroz.Id, LoteId = loteArroz1.Id, Cantidad = 100 },
            new DonacionDetalle { DonacionId = donacion1.Id, ProductoId = prodAceite.Id, LoteId = loteAceite.Id, Cantidad = 80 },
            new DonacionDetalle { DonacionId = donacion1.Id, ProductoId = prodHarina.Id, LoteId = loteHarina.Id, Cantidad = 70 },

            // DON-2
            new DonacionDetalle { DonacionId = donacion2.Id, ProductoId = prodFideos.Id, LoteId = loteFideos1.Id, Cantidad = 150 },
            new DonacionDetalle { DonacionId = donacion2.Id, ProductoId = prodTomate.Id, LoteId = loteTomatePronto.Id, Cantidad = 80 },
            new DonacionDetalle { DonacionId = donacion2.Id, ProductoId = prodArvejas.Id, LoteId = loteArvejas.Id, Cantidad = 60 },
            new DonacionDetalle { DonacionId = donacion2.Id, ProductoId = prodAtun.Id, LoteId = loteAtun.Id, Cantidad = 50 },

            // DON-3
            new DonacionDetalle { DonacionId = donacion3.Id, ProductoId = prodAzucar.Id, LoteId = loteAzucar.Id, Cantidad = 60 },
            new DonacionDetalle { DonacionId = donacion3.Id, ProductoId = prodYerba.Id, LoteId = loteYerba.Id, Cantidad = 50 },
            new DonacionDetalle { DonacionId = donacion3.Id, ProductoId = prodCacao.Id, LoteId = loteCacao.Id, Cantidad = 4 },

            // DON-4
            new DonacionDetalle { DonacionId = donacion4.Id, ProductoId = prodLeche.Id, LoteId = loteLecheLejano.Id, Cantidad = 120 },
            new DonacionDetalle { DonacionId = donacion4.Id, ProductoId = prodArroz.Id, LoteId = loteArroz2.Id, Cantidad = 150 },
            new DonacionDetalle { DonacionId = donacion4.Id, ProductoId = prodLentejas.Id, LoteId = loteLentejas.Id, Cantidad = 60 },
            new DonacionDetalle { DonacionId = donacion4.Id, ProductoId = prodGarbanzos.Id, LoteId = loteGarbanzos.Id, Cantidad = 50 },
            new DonacionDetalle { DonacionId = donacion4.Id, ProductoId = prodTomate.Id, LoteId = loteTomateLejano.Id, Cantidad = 100 },
            new DonacionDetalle { DonacionId = donacion4.Id, ProductoId = prodSpaghetti.Id, LoteId = loteSpaghetti.Id, Cantidad = 80 },
            new DonacionDetalle { DonacionId = donacion4.Id, ProductoId = prodLechePolvo.Id, LoteId = loteLechePolvo.Id, Cantidad = 40 }
        );
        await context.SaveChangesAsync();

        // ==========================================
        // 6. MOVIMIENTOS AUDITABLES (KÁRDEX & MERMAS)
        // ==========================================
        var movimientosIngresos = new List<MovimientoStock>
        {
            new() { LoteId = loteLechePronto.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 60, Fecha = DateTime.UtcNow.AddDays(-20), UsuarioId = "Laura Gómez", Observaciones = "Ingreso por donación DON-2026-00001" },
            new() { LoteId = loteArroz1.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 100, Fecha = DateTime.UtcNow.AddDays(-20), UsuarioId = "Laura Gómez", Observaciones = "Ingreso por donación DON-2026-00001" },
            new() { LoteId = loteAceite.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 80, Fecha = DateTime.UtcNow.AddDays(-20), UsuarioId = "Laura Gómez", Observaciones = "Ingreso por donación DON-2026-00001" },
            new() { LoteId = loteHarina.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 70, Fecha = DateTime.UtcNow.AddDays(-20), UsuarioId = "Laura Gómez", Observaciones = "Ingreso por donación DON-2026-00001" },

            new() { LoteId = loteFideos1.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 150, Fecha = DateTime.UtcNow.AddDays(-14), UsuarioId = "Pablo Cavallero", Observaciones = "Ingreso por donación DON-2026-00002" },
            new() { LoteId = loteTomatePronto.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 80, Fecha = DateTime.UtcNow.AddDays(-14), UsuarioId = "Pablo Cavallero", Observaciones = "Ingreso por donación DON-2026-00002" },
            new() { LoteId = loteArvejas.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 60, Fecha = DateTime.UtcNow.AddDays(-14), UsuarioId = "Pablo Cavallero", Observaciones = "Ingreso por donación DON-2026-00002" },
            new() { LoteId = loteAtun.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 50, Fecha = DateTime.UtcNow.AddDays(-14), UsuarioId = "Pablo Cavallero", Observaciones = "Ingreso por donación DON-2026-00002" },

            new() { LoteId = loteAzucar.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 60, Fecha = DateTime.UtcNow.AddDays(-7), UsuarioId = "Martín Sueldo", Observaciones = "Ingreso por donación DON-2026-00003" },
            new() { LoteId = loteYerba.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 50, Fecha = DateTime.UtcNow.AddDays(-7), UsuarioId = "Martín Sueldo", Observaciones = "Ingreso por donación DON-2026-00003" },
            new() { LoteId = loteCacao.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 4, Fecha = DateTime.UtcNow.AddDays(-7), UsuarioId = "Martín Sueldo", Observaciones = "Ingreso por donación DON-2026-00003" },

            new() { LoteId = loteLecheLejano.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 120, Fecha = DateTime.UtcNow.AddDays(-2), UsuarioId = "Rosaura Limache Caballero", Observaciones = "Ingreso por donación DON-2026-00004" },
            new() { LoteId = loteArroz2.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 150, Fecha = DateTime.UtcNow.AddDays(-2), UsuarioId = "Rosaura Limache Caballero", Observaciones = "Ingreso por donación DON-2026-00004" },
            new() { LoteId = loteLentejas.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 60, Fecha = DateTime.UtcNow.AddDays(-2), UsuarioId = "Rosaura Limache Caballero", Observaciones = "Ingreso por donación DON-2026-00004" },
            new() { LoteId = loteGarbanzos.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 50, Fecha = DateTime.UtcNow.AddDays(-2), UsuarioId = "Rosaura Limache Caballero", Observaciones = "Ingreso por donación DON-2026-00004" },
            new() { LoteId = loteTomateLejano.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 100, Fecha = DateTime.UtcNow.AddDays(-2), UsuarioId = "Rosaura Limache Caballero", Observaciones = "Ingreso por donación DON-2026-00004" },
            new() { LoteId = loteSpaghetti.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 80, Fecha = DateTime.UtcNow.AddDays(-2), UsuarioId = "Rosaura Limache Caballero", Observaciones = "Ingreso por donación DON-2026-00004" },
            new() { LoteId = loteLechePolvo.Id, TipoMovimiento = TipoMovimiento.IngresoDonacion, Motivo = MotivoMovimiento.DonacionRecibida, Cantidad = 40, Fecha = DateTime.UtcNow.AddDays(-2), UsuarioId = "Rosaura Limache Caballero", Observaciones = "Ingreso por donación DON-2026-00004" },

            // Caso de Merma / Baja por rotura de envase en kárdex
            new() { LoteId = loteTomatePronto.Id, TipoMovimiento = TipoMovimiento.BajaPorMerma, Motivo = MotivoMovimiento.RoturaEnvase, Cantidad = 2, Fecha = DateTime.UtcNow.AddDays(-12), UsuarioId = "Laura Gómez", Observaciones = "Envases con filtración dañados durante maniobra de estiba interna" }
        };

        context.MovimientosStock.AddRange(movimientosIngresos);
        await context.SaveChangesAsync();

        // ==========================================
        // 7. PLANTILLAS DE PAQUETES (Kits Nutricionales)
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

        context.PlantillaPaqueteDetalles.AddRange(
            // Items Kit Básico
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitBasico.Id, ProductoId = prodArroz.Id, CantidadRequerida = 1 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitBasico.Id, ProductoId = prodFideos.Id, CantidadRequerida = 2 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitBasico.Id, ProductoId = prodAceite.Id, CantidadRequerida = 1 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitBasico.Id, ProductoId = prodLeche.Id, CantidadRequerida = 2 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitBasico.Id, ProductoId = prodTomate.Id, CantidadRequerida = 2 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitBasico.Id, ProductoId = prodAzucar.Id, CantidadRequerida = 1 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitBasico.Id, ProductoId = prodYerba.Id, CantidadRequerida = 1 },

            // Items Kit Familiar
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodArroz.Id, CantidadRequerida = 2 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodFideos.Id, CantidadRequerida = 3 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodSpaghetti.Id, CantidadRequerida = 2 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodAceite.Id, CantidadRequerida = 2 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodLeche.Id, CantidadRequerida = 4 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodTomate.Id, CantidadRequerida = 3 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodLentejas.Id, CantidadRequerida = 2 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodHarina.Id, CantidadRequerida = 2 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodAzucar.Id, CantidadRequerida = 2 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodArvejas.Id, CantidadRequerida = 2 },
            new PlantillaPaqueteDetalle { TipoPaqueteId = kitFamiliar.Id, ProductoId = prodAtun.Id, CantidadRequerida = 2 }
        );
        await context.SaveChangesAsync();

        // ==========================================
        // 8. FAMILIAS BENEFICIARIAS (Con Casos de Borde)
        // ==========================================
        var familia1 = new FamiliaBeneficiaria
        {
            DniTitular = "28456789",
            NombreTitular = "Mario",
            ApellidoTitular = "González",
            Telefono = "11-4567-8901",
            Direccion = "Calle 14 nro 543, Barrio Esperanza",
            CantidadIntegrantes = 3, // Kit Básico
            Estado = EstadoFamilia.Activo,
            FechaAlta = DateTime.UtcNow.AddMonths(-3)
        };

        var familia2 = new FamiliaBeneficiaria
        {
            DniTitular = "32789123",
            NombreTitular = "Romina",
            ApellidoTitular = "Fernández",
            Telefono = "11-9876-5432",
            Direccion = "Av. San Martín 2450, Piso 1 Dpto B",
            CantidadIntegrantes = 5, // Kit Familiar
            Estado = EstadoFamilia.Activo,
            FechaAlta = DateTime.UtcNow.AddMonths(-2)
        };

        var familia3 = new FamiliaBeneficiaria
        {
            DniTitular = "24321654",
            NombreTitular = "Carlos",
            ApellidoTitular = "Benítez",
            Telefono = "11-3322-1144",
            Direccion = "Pasaje Los Álamos 112",
            CantidadIntegrantes = 2, // Kit Básico
            Estado = EstadoFamilia.Activo,
            FechaAlta = DateTime.UtcNow.AddMonths(-1)
        };

        var familia4 = new FamiliaBeneficiaria
        {
            DniTitular = "35123456",
            NombreTitular = "Miriam",
            ApellidoTitular = "Juárez",
            Telefono = "11-6677-8899",
            Direccion = "Av. Rivadavia 8830, Casa 4",
            CantidadIntegrantes = 6, // Kit Familiar numerosa
            Estado = EstadoFamilia.Activo,
            FechaAlta = DateTime.UtcNow.AddDays(-25)
        };

        var familia5 = new FamiliaBeneficiaria
        {
            DniTitular = "20987654",
            NombreTitular = "Roberto",
            ApellidoTitular = "Domínguez",
            Telefono = "11-2244-6688",
            Direccion = "Calle Belgrano 410",
            CantidadIntegrantes = 4,
            Estado = EstadoFamilia.Suspendido, // Caso de prueba de bloqueo
            FechaAlta = DateTime.UtcNow.AddMonths(-4)
        };

        context.FamiliasBeneficiarias.AddRange(familia1, familia2, familia3, familia4, familia5);
        await context.SaveChangesAsync();

        // ==========================================
        // 9. PAQUETES ASISTENCIALES Y ENTREGAS (Ciclo de Vida Completo)
        // ==========================================

        // --- PAQUETE 1: Entregado con remito y firma digital ---
        var paquete1 = new Paquete
        {
            CodigoSeguimiento = "PKG-2026-00001",
            FamiliaBeneficiariaId = familia1.Id,
            TipoPaqueteId = kitBasico.Id,
            FechaCreacion = DateTime.UtcNow.AddDays(-5),
            Estado = EstadoPaquete.Entregado,
            UsuarioArmadorId = "Pablo Cavallero"
        };
        context.Paquetes.Add(paquete1);
        await context.SaveChangesAsync();

        context.PaqueteDetalles.AddRange(
            new PaqueteDetalle { PaqueteId = paquete1.Id, ProductoId = prodArroz.Id, LoteId = loteArroz1.Id, Cantidad = 1 },
            new PaqueteDetalle { PaqueteId = paquete1.Id, ProductoId = prodFideos.Id, LoteId = loteFideos1.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete1.Id, ProductoId = prodAceite.Id, LoteId = loteAceite.Id, Cantidad = 1 },
            new PaqueteDetalle { PaqueteId = paquete1.Id, ProductoId = prodLeche.Id, LoteId = loteLechePronto.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete1.Id, ProductoId = prodTomate.Id, LoteId = loteTomatePronto.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete1.Id, ProductoId = prodAzucar.Id, LoteId = loteAzucar.Id, Cantidad = 1 },
            new PaqueteDetalle { PaqueteId = paquete1.Id, ProductoId = prodYerba.Id, LoteId = loteYerba.Id, Cantidad = 1 }
        );

        var entrega1 = new Entrega
        {
            PaqueteId = paquete1.Id,
            FechaHoraEntrega = DateTime.UtcNow.AddDays(-4),
            VoluntarioDespachoId = "Laura Gómez (App Movil)",
            TipoReceptor = TipoReceptor.Titular,
            DniReceptor = "28456789",
            NombreReceptor = "Mario González",
            FirmaDigital = FirmaDemoBase64,
            Concretada = true
        };
        context.Entregas.Add(entrega1);

        // Movimiento de kárdex egreso paquete 1
        context.MovimientosStock.Add(new MovimientoStock
        {
            LoteId = loteLechePronto.Id,
            TipoMovimiento = TipoMovimiento.EgresoPaquete,
            Motivo = MotivoMovimiento.EntregaPaquete,
            Cantidad = 2,
            Fecha = DateTime.UtcNow.AddDays(-4),
            UsuarioId = "Laura Gómez (App Movil)",
            Observaciones = "Despacho final paquete PKG-2026-00001 a titular Mario González"
        });

        // --- PAQUETE 2: Preparado (Listo en depósito para escanear y entregar desde la App Móvil) ---
        var paquete2 = new Paquete
        {
            CodigoSeguimiento = "PKG-2026-00002",
            FamiliaBeneficiariaId = familia2.Id,
            TipoPaqueteId = kitFamiliar.Id,
            FechaCreacion = DateTime.UtcNow.AddDays(-1),
            Estado = EstadoPaquete.Preparado,
            UsuarioArmadorId = "Martín Sueldo"
        };
        context.Paquetes.Add(paquete2);
        await context.SaveChangesAsync();

        context.PaqueteDetalles.AddRange(
            new PaqueteDetalle { PaqueteId = paquete2.Id, ProductoId = prodArroz.Id, LoteId = loteArroz1.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete2.Id, ProductoId = prodFideos.Id, LoteId = loteFideos1.Id, Cantidad = 3 },
            new PaqueteDetalle { PaqueteId = paquete2.Id, ProductoId = prodSpaghetti.Id, LoteId = loteSpaghetti.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete2.Id, ProductoId = prodAceite.Id, LoteId = loteAceite.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete2.Id, ProductoId = prodLeche.Id, LoteId = loteLechePronto.Id, Cantidad = 4 },
            new PaqueteDetalle { PaqueteId = paquete2.Id, ProductoId = prodTomate.Id, LoteId = loteTomatePronto.Id, Cantidad = 3 },
            new PaqueteDetalle { PaqueteId = paquete2.Id, ProductoId = prodLentejas.Id, LoteId = loteLentejas.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete2.Id, ProductoId = prodHarina.Id, LoteId = loteHarina.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete2.Id, ProductoId = prodAzucar.Id, LoteId = loteAzucar.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete2.Id, ProductoId = prodArvejas.Id, LoteId = loteArvejas.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete2.Id, ProductoId = prodAtun.Id, LoteId = loteAtun.Id, Cantidad = 2 }
        );

        // --- PAQUETE 3: Preparado (Segundo paquete listo para despacho) ---
        var paquete3 = new Paquete
        {
            CodigoSeguimiento = "PKG-2026-00003",
            FamiliaBeneficiariaId = familia3.Id,
            TipoPaqueteId = kitBasico.Id,
            FechaCreacion = DateTime.UtcNow.AddHours(-10),
            Estado = EstadoPaquete.Preparado,
            UsuarioArmadorId = "Rosaura Limache Caballero"
        };
        context.Paquetes.Add(paquete3);
        await context.SaveChangesAsync();

        context.PaqueteDetalles.AddRange(
            new PaqueteDetalle { PaqueteId = paquete3.Id, ProductoId = prodArroz.Id, LoteId = loteArroz1.Id, Cantidad = 1 },
            new PaqueteDetalle { PaqueteId = paquete3.Id, ProductoId = prodFideos.Id, LoteId = loteFideos1.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete3.Id, ProductoId = prodAceite.Id, LoteId = loteAceite.Id, Cantidad = 1 },
            new PaqueteDetalle { PaqueteId = paquete3.Id, ProductoId = prodLeche.Id, LoteId = loteLechePronto.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete3.Id, ProductoId = prodTomate.Id, LoteId = loteTomatePronto.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete3.Id, ProductoId = prodAzucar.Id, LoteId = loteAzucar.Id, Cantidad = 1 },
            new PaqueteDetalle { PaqueteId = paquete3.Id, ProductoId = prodYerba.Id, LoteId = loteYerba.Id, Cantidad = 1 }
        );

        // --- PAQUETE 4: Cancelado / Intento fallido con motivo de auditoría ---
        var paquete4 = new Paquete
        {
            CodigoSeguimiento = "PKG-2026-00004",
            FamiliaBeneficiariaId = familia4.Id,
            TipoPaqueteId = kitFamiliar.Id,
            FechaCreacion = DateTime.UtcNow.AddDays(-3),
            Estado = EstadoPaquete.Cancelado,
            UsuarioArmadorId = "Laura Gómez"
        };
        context.Paquetes.Add(paquete4);
        await context.SaveChangesAsync();

        context.PaqueteDetalles.AddRange(
            new PaqueteDetalle { PaqueteId = paquete4.Id, ProductoId = prodArroz.Id, LoteId = loteArroz1.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete4.Id, ProductoId = prodFideos.Id, LoteId = loteFideos1.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete4.Id, ProductoId = prodSpaghetti.Id, LoteId = loteSpaghetti.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete4.Id, ProductoId = prodAceite.Id, LoteId = loteAceite.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete4.Id, ProductoId = prodLeche.Id, LoteId = loteLechePronto.Id, Cantidad = 4 },
            new PaqueteDetalle { PaqueteId = paquete4.Id, ProductoId = prodTomate.Id, LoteId = loteTomatePronto.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete4.Id, ProductoId = prodLentejas.Id, LoteId = loteLentejas.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete4.Id, ProductoId = prodHarina.Id, LoteId = loteHarina.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete4.Id, ProductoId = prodAzucar.Id, LoteId = loteAzucar.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete4.Id, ProductoId = prodArvejas.Id, LoteId = loteArvejas.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete4.Id, ProductoId = prodAtun.Id, LoteId = loteAtun.Id, Cantidad = 2 }
        );

        var entrega4 = new Entrega
        {
            PaqueteId = paquete4.Id,
            FechaHoraEntrega = DateTime.UtcNow.AddDays(-2),
            VoluntarioDespachoId = "Laura Gómez (App Movil)",
            TipoReceptor = TipoReceptor.Titular,
            DniReceptor = "35123456",
            NombreReceptor = "Miriam Juárez",
            FirmaDigital = "SIN_FIRMA",
            Concretada = false,
            MotivoNoEntrega = "Titular ausente en domicilio tras reiteradas visitas de entrega"
        };
        context.Entregas.Add(entrega4);

        // --- PAQUETE 5: Pendiente (Representación del estado Pendiente en métricas) ---
        var paquete5 = new Paquete
        {
            CodigoSeguimiento = "PKG-2026-00005",
            FamiliaBeneficiariaId = familia4.Id,
            TipoPaqueteId = kitBasico.Id,
            FechaCreacion = DateTime.UtcNow.AddHours(-2),
            Estado = EstadoPaquete.Pendiente,
            UsuarioArmadorId = "Pablo Cavallero"
        };
        context.Paquetes.Add(paquete5);
        await context.SaveChangesAsync();

        context.PaqueteDetalles.AddRange(
            new PaqueteDetalle { PaqueteId = paquete5.Id, ProductoId = prodArroz.Id, LoteId = loteArroz1.Id, Cantidad = 1 },
            new PaqueteDetalle { PaqueteId = paquete5.Id, ProductoId = prodFideos.Id, LoteId = loteFideos1.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete5.Id, ProductoId = prodAceite.Id, LoteId = loteAceite.Id, Cantidad = 1 },
            new PaqueteDetalle { PaqueteId = paquete5.Id, ProductoId = prodLeche.Id, LoteId = loteLechePronto.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete5.Id, ProductoId = prodTomate.Id, LoteId = loteTomatePronto.Id, Cantidad = 2 },
            new PaqueteDetalle { PaqueteId = paquete5.Id, ProductoId = prodAzucar.Id, LoteId = loteAzucar.Id, Cantidad = 1 },
            new PaqueteDetalle { PaqueteId = paquete5.Id, ProductoId = prodYerba.Id, LoteId = loteYerba.Id, Cantidad = 1 }
        );

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Inicializa los roles oficiales del sistema y las cuentas de usuario por defecto
    /// para el equipo de desarrollo, administradores y voluntarios de operaciones.
    /// </summary>
    public static async Task SeedIdentityAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // 1. Roles Oficiales
        string[] roles = ["Administrador", "Voluntario"];
        foreach (var rol in roles)
        {
            if (!await roleManager.RoleExistsAsync(rol))
            {
                await roleManager.CreateAsync(new IdentityRole(rol));
            }
        }

        // 2. Usuarios del Equipo y Cuentas Preconfiguradas
        var usuarios = new (string Email, string Nombre, string Rol, string Password)[]
        {
            ("admin@nutrired.org", "Administrador General", "Administrador", "Admin123!"),
            ("pablo@nutrired.org", "Pablo Cavallero", "Administrador", "Admin123!"),
            ("rosaura@nutrired.org", "Rosaura Limache Caballero", "Administrador", "Admin123!"),
            ("martin@nutrired.org", "Martín Sueldo", "Administrador", "Admin123!"),
            ("laura.gomez@nutrired.org", "Laura Gómez", "Voluntario", "123456"),
            ("voluntario@nutrired.org", "Voluntario de Operaciones", "Voluntario", "123456")
        };

        foreach (var (email, nombre, rol, password) in usuarios)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    NombreCompleto = nombre,
                    FechaRegistro = DateTime.UtcNow,
                    Activo = true
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, rol);
                }
            }
            else
            {
                // Asegurar que el nombre completo esté actualizado
                if (user.NombreCompleto != nombre)
                {
                    user.NombreCompleto = nombre;
                    await userManager.UpdateAsync(user);
                }

                // Asegurar que tenga el rol asignado
                if (!await userManager.IsInRoleAsync(user, rol))
                {
                    await userManager.AddToRoleAsync(user, rol);
                }

                // Resetear contraseña al estándar del seed de desarrollo
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                await userManager.ResetPasswordAsync(user, token, password);
            }
        }
    }
}
