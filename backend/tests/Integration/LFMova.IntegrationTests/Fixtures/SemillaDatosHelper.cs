using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Data;

namespace LFMova.IntegrationTests.Fixtures;

/// <summary>
/// Crea datos base directamente en la base de datos de pruebas (bypaseando
/// la API) para preparar escenarios de prueba de integración. No sustituye a
/// las pruebas que ejercitan la API mediante HTTP; solo prepara el estado
/// inicial necesario para ellas.
/// </summary>
public static class SemillaDatosHelper
{
    /// <summary>Crea y persiste una empresa activa.</summary>
    public static async Task<Empresa> CrearEmpresaAsync(LFMovaDbContext contexto, string nombre)
    {
        var empresa = new Empresa { Nombre = nombre, Cif = $"CIF-{nombre}", Direccion = "Calle Falsa 123", Activa = true };
        contexto.Empresas.Add(empresa);
        await contexto.SaveChangesAsync();
        return empresa;
    }

    /// <summary>
    /// Crea un usuario con contraseña ya establecida (cuenta activada) y le
    /// asigna el rol indicado.
    /// </summary>
    public static async Task<Usuario> CrearUsuarioConRolAsync(
        LFMovaDbContext contexto,
        IHasheadorContrasenas hasheador,
        string cedula,
        string contrasena,
        Rol rol,
        int? empresaId)
    {
        var usuario = new Usuario
        {
            Cedula = cedula,
            NombreCompleto = cedula,
            PasswordHash = hasheador.Hashear(contrasena),
            CorreoConfirmado = true,
            Activo = true
        };
        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync();

        contexto.UsuarioRoles.Add(new UsuarioRol
        {
            UsuarioId = usuario.UsuarioId,
            Rol = rol,
            EmpresaId = empresaId,
            Activo = true
        });
        await contexto.SaveChangesAsync();

        return usuario;
    }
}
