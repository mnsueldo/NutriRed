using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriRed.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Donantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NombreRazonSocial = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donantes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FamiliasBeneficiarias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DniTitular = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NombreTitular = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApellidoTitular = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CantidadIntegrantes = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamiliasBeneficiarias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposPaquete",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    MinIntegrantes = table.Column<int>(type: "int", nullable: false),
                    MaxIntegrantes = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposPaquete", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoBarras = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CategoriaId = table.Column<int>(type: "int", nullable: false),
                    UnidadMedida = table.Column<int>(type: "int", nullable: false),
                    StockMinimo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Productos_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Donaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoComprobante = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DonanteId = table.Column<int>(type: "int", nullable: true),
                    VoluntarioReceptorId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Donaciones_Donantes_DonanteId",
                        column: x => x.DonanteId,
                        principalTable: "Donantes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Paquetes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoSeguimiento = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FamiliaBeneficiariaId = table.Column<int>(type: "int", nullable: false),
                    TipoPaqueteId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    UsuarioArmadorId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paquetes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Paquetes_FamiliasBeneficiarias_FamiliaBeneficiariaId",
                        column: x => x.FamiliaBeneficiariaId,
                        principalTable: "FamiliasBeneficiarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Paquetes_TiposPaquete_TipoPaqueteId",
                        column: x => x.TipoPaqueteId,
                        principalTable: "TiposPaquete",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroLote = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CantidadInicial = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CantidadDisponible = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaIngreso = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lotes_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlantillaPaqueteDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoPaqueteId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    CantidadRequerida = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantillaPaqueteDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantillaPaqueteDetalles_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlantillaPaqueteDetalles_TiposPaquete_TipoPaqueteId",
                        column: x => x.TipoPaqueteId,
                        principalTable: "TiposPaquete",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Entregas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaqueteId = table.Column<int>(type: "int", nullable: false),
                    FechaHoraEntrega = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VoluntarioDespachoId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TipoReceptor = table.Column<int>(type: "int", nullable: false),
                    DniReceptor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NombreReceptor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    VinculoConTitular = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FirmaDigital = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Concretada = table.Column<bool>(type: "bit", nullable: false),
                    MotivoNoEntrega = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entregas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entregas_Paquetes_PaqueteId",
                        column: x => x.PaqueteId,
                        principalTable: "Paquetes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DonacionDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonacionId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    LoteId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonacionDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonacionDetalles_Donaciones_DonacionId",
                        column: x => x.DonacionId,
                        principalTable: "Donaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DonacionDetalles_Lotes_LoteId",
                        column: x => x.LoteId,
                        principalTable: "Lotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonacionDetalles_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosStock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoteId = table.Column<int>(type: "int", nullable: false),
                    TipoMovimiento = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Lotes_LoteId",
                        column: x => x.LoteId,
                        principalTable: "Lotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaqueteDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaqueteId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    LoteId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EsSustituto = table.Column<bool>(type: "bit", nullable: false),
                    ProductoSustituidoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaqueteDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaqueteDetalles_Lotes_LoteId",
                        column: x => x.LoteId,
                        principalTable: "Lotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaqueteDetalles_Paquetes_PaqueteId",
                        column: x => x.PaqueteId,
                        principalTable: "Paquetes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaqueteDetalles_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaqueteDetalles_Productos_ProductoSustituidoId",
                        column: x => x.ProductoSustituidoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonacionDetalles_DonacionId",
                table: "DonacionDetalles",
                column: "DonacionId");

            migrationBuilder.CreateIndex(
                name: "IX_DonacionDetalles_LoteId",
                table: "DonacionDetalles",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_DonacionDetalles_ProductoId",
                table: "DonacionDetalles",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Donaciones_CodigoComprobante",
                table: "Donaciones",
                column: "CodigoComprobante",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Donaciones_DonanteId",
                table: "Donaciones",
                column: "DonanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_PaqueteId",
                table: "Entregas",
                column: "PaqueteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lotes_ProductoId",
                table: "Lotes",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_LoteId",
                table: "MovimientosStock",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_PaqueteDetalles_LoteId",
                table: "PaqueteDetalles",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_PaqueteDetalles_PaqueteId",
                table: "PaqueteDetalles",
                column: "PaqueteId");

            migrationBuilder.CreateIndex(
                name: "IX_PaqueteDetalles_ProductoId",
                table: "PaqueteDetalles",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_PaqueteDetalles_ProductoSustituidoId",
                table: "PaqueteDetalles",
                column: "ProductoSustituidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Paquetes_CodigoSeguimiento",
                table: "Paquetes",
                column: "CodigoSeguimiento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Paquetes_FamiliaBeneficiariaId",
                table: "Paquetes",
                column: "FamiliaBeneficiariaId");

            migrationBuilder.CreateIndex(
                name: "IX_Paquetes_TipoPaqueteId",
                table: "Paquetes",
                column: "TipoPaqueteId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillaPaqueteDetalles_ProductoId",
                table: "PlantillaPaqueteDetalles",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillaPaqueteDetalles_TipoPaqueteId",
                table: "PlantillaPaqueteDetalles",
                column: "TipoPaqueteId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CategoriaId",
                table: "Productos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CodigoBarras",
                table: "Productos",
                column: "CodigoBarras",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonacionDetalles");

            migrationBuilder.DropTable(
                name: "Entregas");

            migrationBuilder.DropTable(
                name: "MovimientosStock");

            migrationBuilder.DropTable(
                name: "PaqueteDetalles");

            migrationBuilder.DropTable(
                name: "PlantillaPaqueteDetalles");

            migrationBuilder.DropTable(
                name: "Donaciones");

            migrationBuilder.DropTable(
                name: "Lotes");

            migrationBuilder.DropTable(
                name: "Paquetes");

            migrationBuilder.DropTable(
                name: "Donantes");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "FamiliasBeneficiarias");

            migrationBuilder.DropTable(
                name: "TiposPaquete");

            migrationBuilder.DropTable(
                name: "Categorias");
        }
    }
}
