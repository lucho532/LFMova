using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LFMova.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarClaveUnicaProgramacionTransporte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProgramacionesTransporte_EmpleadoId_SedeId_Fecha_Hora_Tipo",
                table: "ProgramacionesTransporte",
                columns: new[] { "EmpleadoId", "SedeId", "Fecha", "Hora", "Tipo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProgramacionesTransporte_EmpleadoId_SedeId_Fecha_Hora_Tipo",
                table: "ProgramacionesTransporte");
        }
    }
}
