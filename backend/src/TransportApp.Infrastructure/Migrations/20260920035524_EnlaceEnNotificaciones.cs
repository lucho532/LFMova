using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnlaceEnNotificaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Enlace",
                table: "Notificaciones",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Enlace",
                table: "Notificaciones");
        }
    }
}
