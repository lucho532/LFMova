using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LFMova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRegistroLlamadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosLlamada",
                columns: table => new
                {
                    RegistroLlamadaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServicioPasajeroId = table.Column<int>(type: "integer", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DuracionAproximadaSegundos = table.Column<int>(type: "integer", nullable: true),
                    Latitud = table.Column<double>(type: "double precision", nullable: true),
                    Longitud = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosLlamada", x => x.RegistroLlamadaId);
                    table.ForeignKey(
                        name: "FK_RegistrosLlamada_ServiciosPasajero_ServicioPasajeroId",
                        column: x => x.ServicioPasajeroId,
                        principalTable: "ServiciosPasajero",
                        principalColumn: "ServicioPasajeroId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosLlamada_ServicioPasajeroId",
                table: "RegistrosLlamada",
                column: "ServicioPasajeroId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosLlamada");
        }
    }
}
