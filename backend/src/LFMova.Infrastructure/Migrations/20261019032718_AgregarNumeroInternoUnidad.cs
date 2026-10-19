using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LFMova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarNumeroInternoUnidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NumerosInternosUnidad",
                columns: table => new
                {
                    NumeroInternoUnidadId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    UnidadOperativaId = table.Column<int>(type: "integer", nullable: false),
                    Numero = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumerosInternosUnidad", x => x.NumeroInternoUnidadId);
                    table.ForeignKey(
                        name: "FK_NumerosInternosUnidad_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NumerosInternosUnidad_UnidadesOperativas_UnidadOperativaId",
                        column: x => x.UnidadOperativaId,
                        principalTable: "UnidadesOperativas",
                        principalColumn: "UnidadOperativaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NumerosInternosUnidad_EmpresaId_Numero",
                table: "NumerosInternosUnidad",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NumerosInternosUnidad_EmpresaId_UnidadOperativaId",
                table: "NumerosInternosUnidad",
                columns: new[] { "EmpresaId", "UnidadOperativaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NumerosInternosUnidad_UnidadOperativaId",
                table: "NumerosInternosUnidad",
                column: "UnidadOperativaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NumerosInternosUnidad");
        }
    }
}
