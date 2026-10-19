using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LFMova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarUbicacionCompartidaAServicioPasajero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaHoraUbicacionCompartida",
                table: "ServiciosPasajero",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LatitudCompartida",
                table: "ServiciosPasajero",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LongitudCompartida",
                table: "ServiciosPasajero",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaHoraUbicacionCompartida",
                table: "ServiciosPasajero");

            migrationBuilder.DropColumn(
                name: "LatitudCompartida",
                table: "ServiciosPasajero");

            migrationBuilder.DropColumn(
                name: "LongitudCompartida",
                table: "ServiciosPasajero");
        }
    }
}
