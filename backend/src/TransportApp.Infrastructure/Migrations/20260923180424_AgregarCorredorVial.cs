using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TransportApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCorredorVial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CorredorVialId",
                table: "Zonas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CorredoresViales",
                columns: table => new
                {
                    CorredorVialId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorredoresViales", x => x.CorredorVialId);
                    table.ForeignKey(
                        name: "FK_CorredoresViales_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Zonas_CorredorVialId",
                table: "Zonas",
                column: "CorredorVialId");

            migrationBuilder.CreateIndex(
                name: "IX_CorredoresViales_EmpresaId",
                table: "CorredoresViales",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Zonas_CorredoresViales_CorredorVialId",
                table: "Zonas",
                column: "CorredorVialId",
                principalTable: "CorredoresViales",
                principalColumn: "CorredorVialId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Zonas_CorredoresViales_CorredorVialId",
                table: "Zonas");

            migrationBuilder.DropTable(
                name: "CorredoresViales");

            migrationBuilder.DropIndex(
                name: "IX_Zonas_CorredorVialId",
                table: "Zonas");

            migrationBuilder.DropColumn(
                name: "CorredorVialId",
                table: "Zonas");
        }
    }
}
