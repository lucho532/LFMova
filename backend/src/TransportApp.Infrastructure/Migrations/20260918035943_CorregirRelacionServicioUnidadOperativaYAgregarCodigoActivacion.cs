using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TransportApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CorregirRelacionServicioUnidadOperativaYAgregarCodigoActivacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jornadas_UnidadesOperativas_UnidadOperativaId",
                table: "Jornadas");

            migrationBuilder.DropIndex(
                name: "IX_Jornadas_UnidadOperativaId",
                table: "Jornadas");

            migrationBuilder.DropColumn(
                name: "UnidadOperativaId",
                table: "Jornadas");

            migrationBuilder.AddColumn<int>(
                name: "UnidadOperativaId",
                table: "Servicios",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CodigosActivacion",
                columns: table => new
                {
                    CodigoActivacionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    ConductorId = table.Column<int>(type: "integer", nullable: false),
                    CodigoHash = table.Column<string>(type: "text", nullable: false),
                    FechaGeneracion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Utilizado = table.Column<bool>(type: "boolean", nullable: false),
                    IntentosFallidos = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodigosActivacion", x => x.CodigoActivacionId);
                    table.ForeignKey(
                        name: "FK_CodigosActivacion_Conductores_ConductorId",
                        column: x => x.ConductorId,
                        principalTable: "Conductores",
                        principalColumn: "ConductorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CodigosActivacion_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Servicios_UnidadOperativaId",
                table: "Servicios",
                column: "UnidadOperativaId");

            migrationBuilder.CreateIndex(
                name: "IX_CodigosActivacion_ConductorId",
                table: "CodigosActivacion",
                column: "ConductorId");

            migrationBuilder.CreateIndex(
                name: "IX_CodigosActivacion_UsuarioId",
                table: "CodigosActivacion",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Servicios_UnidadesOperativas_UnidadOperativaId",
                table: "Servicios",
                column: "UnidadOperativaId",
                principalTable: "UnidadesOperativas",
                principalColumn: "UnidadOperativaId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Servicios_UnidadesOperativas_UnidadOperativaId",
                table: "Servicios");

            migrationBuilder.DropTable(
                name: "CodigosActivacion");

            migrationBuilder.DropIndex(
                name: "IX_Servicios_UnidadOperativaId",
                table: "Servicios");

            migrationBuilder.DropColumn(
                name: "UnidadOperativaId",
                table: "Servicios");

            migrationBuilder.AddColumn<int>(
                name: "UnidadOperativaId",
                table: "Jornadas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Jornadas_UnidadOperativaId",
                table: "Jornadas",
                column: "UnidadOperativaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jornadas_UnidadesOperativas_UnidadOperativaId",
                table: "Jornadas",
                column: "UnidadOperativaId",
                principalTable: "UnidadesOperativas",
                principalColumn: "UnidadOperativaId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
