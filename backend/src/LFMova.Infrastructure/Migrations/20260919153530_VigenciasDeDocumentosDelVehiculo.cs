using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LFMova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VigenciasDeDocumentosDelVehiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "VigenciaSoat",
                table: "Vehiculos",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "VigenciaTecnomecanica",
                table: "Vehiculos",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VigenciaSoat",
                table: "Vehiculos");

            migrationBuilder.DropColumn(
                name: "VigenciaTecnomecanica",
                table: "Vehiculos");
        }
    }
}
