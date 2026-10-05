using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LFMova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarFacturacionPorConductor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CierresMensuales",
                columns: table => new
                {
                    CierreMensualId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    Anio = table.Column<int>(type: "integer", nullable: false),
                    Mes = table.Column<int>(type: "integer", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConductoresActivos = table.Column<int>(type: "integer", nullable: false),
                    RutasFinalizadas = table.Column<int>(type: "integer", nullable: false),
                    PasajerosTransportados = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CierresMensuales", x => x.CierreMensualId);
                    table.ForeignKey(
                        name: "FK_CierresMensuales_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsosConductor",
                columns: table => new
                {
                    UsoConductorId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    ServicioId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    ConductorId = table.Column<int>(type: "integer", nullable: false),
                    Cedula = table.Column<string>(type: "text", nullable: false),
                    NombreConductor = table.Column<string>(type: "text", nullable: false),
                    Placa = table.Column<string>(type: "text", nullable: false),
                    PasajerosTransportados = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsosConductor", x => x.UsoConductorId);
                    table.ForeignKey(
                        name: "FK_UsosConductor_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CierresMensuales_EmpresaId_Anio_Mes",
                table: "CierresMensuales",
                columns: new[] { "EmpresaId", "Anio", "Mes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsosConductor_EmpresaId_Fecha",
                table: "UsosConductor",
                columns: new[] { "EmpresaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_UsosConductor_ServicioId",
                table: "UsosConductor",
                column: "ServicioId",
                unique: true);

            // Rutas que ya estaban finalizadas antes de existir el registro: se anotan con los datos
            // actuales de su conductor para que el primer mes de facturación no salga incompleto. El día
            // es el de su finalización en hora de Colombia (UTC-5).
            migrationBuilder.Sql("""
                INSERT INTO "UsosConductor" ("EmpresaId", "ServicioId", "Fecha", "ConductorId", "Cedula", "NombreConductor", "Placa", "PasajerosTransportados")
                SELECT j."EmpresaId", s."ServicioId",
                       COALESCE((s."HoraFinReal" AT TIME ZONE 'UTC' - INTERVAL '5 hours')::date, s."Fecha"),
                       c."ConductorId", u."Cedula", c."NombreCompleto", v."Placa",
                       (SELECT count(*) FROM "ServiciosPasajero" sp WHERE sp."ServicioId" = s."ServicioId" AND sp."Estado" = 4)
                FROM "Servicios" s
                JOIN "Jornadas" j ON j."JornadaId" = s."JornadaId"
                JOIN "UnidadesOperativas" uo ON uo."UnidadOperativaId" = s."UnidadOperativaId"
                JOIN "Conductores" c ON c."ConductorId" = uo."ConductorId"
                JOIN "Usuarios" u ON u."UsuarioId" = c."UsuarioId"
                JOIN "Vehiculos" v ON v."VehiculoId" = uo."VehiculoId"
                WHERE s."Estado" = 5;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CierresMensuales");

            migrationBuilder.DropTable(
                name: "UsosConductor");
        }
    }
}
