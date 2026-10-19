using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LFMova.Infrastructure.Migrations
{
    /// <summary>
    /// Migración solo de datos: deja en minúsculas y sin espacios los correos
    /// ya guardados, que desde ahora siempre se guardan y se comparan así (ver
    /// <c>CorreoNormalizador</c>). No cambia el esquema. No tiene vuelta atrás:
    /// las mayúsculas originales no se conservan.
    /// </summary>
    public partial class NormalizarCorreosEnMinusculas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Usuarios\" SET \"Email\" = lower(trim(\"Email\")) WHERE \"Email\" IS NOT NULL AND \"Email\" <> lower(trim(\"Email\"));");
            migrationBuilder.Sql("UPDATE \"InvitacionesEmpresa\" SET \"Correo\" = lower(trim(\"Correo\")) WHERE \"Correo\" <> lower(trim(\"Correo\"));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
