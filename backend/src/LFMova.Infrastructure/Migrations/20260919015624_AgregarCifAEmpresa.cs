using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LFMova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCifAEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cif",
                table: "Empresas",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Las empresas ya existentes (creadas antes de este campo) quedan con Cif = "" por el
            // defaultValue de arriba; antes de exigir unicidad les asignamos un valor único derivado
            // de su EmpresaId para no romper el índice único que se crea a continuación.
            migrationBuilder.Sql(
                "UPDATE \"Empresas\" SET \"Cif\" = 'LEGACY-' || \"EmpresaId\" WHERE \"Cif\" = '';");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_Cif",
                table: "Empresas",
                column: "Cif",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Empresas_Cif",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "Cif",
                table: "Empresas");
        }
    }
}
