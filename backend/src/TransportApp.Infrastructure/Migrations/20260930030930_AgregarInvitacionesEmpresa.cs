using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TransportApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarInvitacionesEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvitacionesEmpresa",
                columns: table => new
                {
                    InvitacionEmpresaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    Cedula = table.Column<string>(type: "text", nullable: false),
                    Correo = table.Column<string>(type: "text", nullable: false),
                    UsuarioInvitadorId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaAceptacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsuarioAceptanteId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitacionesEmpresa", x => x.InvitacionEmpresaId);
                    table.ForeignKey(
                        name: "FK_InvitacionesEmpresa_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvitacionesEmpresa_Usuarios_UsuarioAceptanteId",
                        column: x => x.UsuarioAceptanteId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvitacionesEmpresa_Usuarios_UsuarioInvitadorId",
                        column: x => x.UsuarioInvitadorId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvitacionesEmpresa_EmpresaId_Cedula",
                table: "InvitacionesEmpresa",
                columns: new[] { "EmpresaId", "Cedula" });

            migrationBuilder.CreateIndex(
                name: "IX_InvitacionesEmpresa_UsuarioAceptanteId",
                table: "InvitacionesEmpresa",
                column: "UsuarioAceptanteId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitacionesEmpresa_UsuarioInvitadorId",
                table: "InvitacionesEmpresa",
                column: "UsuarioInvitadorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvitacionesEmpresa");
        }
    }
}
