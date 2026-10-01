using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UbicacionDeFinalizacionDelServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "LatitudFinalizacion",
                table: "Servicios",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LongitudFinalizacion",
                table: "Servicios",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatitudFinalizacion",
                table: "Servicios");

            migrationBuilder.DropColumn(
                name: "LongitudFinalizacion",
                table: "Servicios");
        }
    }
}
