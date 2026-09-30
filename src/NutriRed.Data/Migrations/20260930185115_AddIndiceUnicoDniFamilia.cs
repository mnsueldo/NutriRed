using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriRed.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIndiceUnicoDniFamilia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DniTitular",
                table: "FamiliasBeneficiarias",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_FamiliasBeneficiarias_DniTitular",
                table: "FamiliasBeneficiarias",
                column: "DniTitular",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FamiliasBeneficiarias_DniTitular",
                table: "FamiliasBeneficiarias");

            migrationBuilder.AlterColumn<string>(
                name: "DniTitular",
                table: "FamiliasBeneficiarias",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
