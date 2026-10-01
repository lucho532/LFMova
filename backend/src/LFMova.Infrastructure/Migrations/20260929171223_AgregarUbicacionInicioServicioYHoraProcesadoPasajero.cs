using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LFMova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarUbicacionInicioServicioYHoraProcesadoPasajero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "HoraProcesado",
                table: "ServiciosPasajero",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LatitudInicio",
                table: "Servicios",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LongitudInicio",
                table: "Servicios",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HoraProcesado",
                table: "ServiciosPasajero");

            migrationBuilder.DropColumn(
                name: "LatitudInicio",
                table: "Servicios");

            migrationBuilder.DropColumn(
                name: "LongitudInicio",
                table: "Servicios");
        }
    }
}
