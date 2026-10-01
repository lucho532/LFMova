using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TransportApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMacroZonaYBarrerasGeograficas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MacroZonaId",
                table: "Zonas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BarrerasGeograficas",
                columns: table => new
                {
                    BarreraGeograficaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    BarrioA = table.Column<string>(type: "text", nullable: false),
                    BarrioB = table.Column<string>(type: "text", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarrerasGeograficas", x => x.BarreraGeograficaId);
                    table.ForeignKey(
                        name: "FK_BarrerasGeograficas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MacroZonas",
                columns: table => new
                {
                    MacroZonaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MacroZonas", x => x.MacroZonaId);
                    table.ForeignKey(
                        name: "FK_MacroZonas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Zonas_MacroZonaId",
                table: "Zonas",
                column: "MacroZonaId");

            migrationBuilder.CreateIndex(
                name: "IX_BarrerasGeograficas_EmpresaId",
                table: "BarrerasGeograficas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_MacroZonas_EmpresaId",
                table: "MacroZonas",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Zonas_MacroZonas_MacroZonaId",
                table: "Zonas",
                column: "MacroZonaId",
                principalTable: "MacroZonas",
                principalColumn: "MacroZonaId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Zonas_MacroZonas_MacroZonaId",
                table: "Zonas");

            migrationBuilder.DropTable(
                name: "BarrerasGeograficas");

            migrationBuilder.DropTable(
                name: "MacroZonas");

            migrationBuilder.DropIndex(
                name: "IX_Zonas_MacroZonaId",
                table: "Zonas");

            migrationBuilder.DropColumn(
                name: "MacroZonaId",
                table: "Zonas");
        }
    }
}
