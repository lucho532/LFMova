using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Resuelve a qué empleado corresponde la cédula de una fila importada: lo crea, lo vincula o lo pasa
/// de empresa según el caso (ver <c>AGENTS.md</c> §15), manteniendo un único empleado por cédula.
/// No crea programaciones ni servicios, y no decide autorización.
/// </summary>
public class AseguradorEmpleadoImportacion
{
    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly IUsuarioRolRepositorio _usuarioRolRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public AseguradorEmpleadoImportacion(
        IUsuarioRepositorio usuarioRepositorio,
        IUsuarioRolRepositorio usuarioRolRepositorio,
        IEmpleadoRepositorio empleadoRepositorio)
    {
        _usuarioRepositorio = usuarioRepositorio;
        _usuarioRolRepositorio = usuarioRolRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
    }

    /// <summary>
    /// Devuelve el empleado de la cédula dentro de la empresa, creándolo o
    /// vinculándolo según el caso: cuenta inexistente (se crea una cuenta
    /// pendiente de que la persona se registre), cuenta ya registrada sin
    /// perfil de empleado (se vincula) o empleado de otra empresa (cambio
    /// automático de empresa; ver <c>AGENTS.md</c> §15).
    /// </summary>
    public async Task<Empleado> AsegurarAsync(int empresaId, FilaHoja fila, ResultadoImportacionDto resultado)
    {
        var usuario = await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(fila.Cedula);
        var usuarioEsNuevo = usuario is null;

        if (usuario is null)
        {
            usuario = new Usuario
            {
                Cedula = fila.Cedula,
                NombreCompleto = fila.NombreCompleto,
                Telefono = fila.Celular,
                PasswordHash = null,
                CorreoConfirmado = false,
                Activo = true
            };
            await _usuarioRepositorio.AgregarAsync(usuario);
            try
            {
                await _usuarioRepositorio.GuardarCambiosAsync();
            }
            catch (Exception)
            {
                // Otra importación concurrente sobre la misma cédula (por ejemplo, un reintento por doble
                // clic mientras la primera todavía estaba procesando) pudo haber creado este mismo usuario
                // entre la consulta de arriba y este guardado: si ya existe, se usa ese en vez de abortar
                // toda la importación por un choque de llave única que en realidad no es un error de datos.
                // Se descarta primero el usuario fallido: si se dejara marcado como pendiente de agregar,
                // el próximo GuardarCambiosAsync lo reintentaría y fallaría siempre con el mismo error.
                _usuarioRepositorio.Descartar(usuario);
                var usuarioConcurrente = await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(fila.Cedula);
                if (usuarioConcurrente is null)
                {
                    throw;
                }

                usuario = usuarioConcurrente;
                usuarioEsNuevo = false;
            }
        }

        var empleado = await _empleadoRepositorio.ObtenerPorUsuarioIdAsync(usuario.UsuarioId);

        if (empleado is not null && empleado.EmpresaId == empresaId)
        {
            return empleado;
        }

        if (empleado is null)
        {
            empleado = new Empleado
            {
                UsuarioId = usuario.UsuarioId,
                EmpresaId = empresaId,
                NombreCompleto = fila.NombreCompleto,
                Telefono = fila.Celular,
                Direccion = fila.Direccion,
                Barrio = fila.Barrio,
                Activo = true
            };
            await _empleadoRepositorio.AgregarAsync(empleado);
            try
            {
                await _empleadoRepositorio.GuardarCambiosAsync();
            }
            catch (Exception)
            {
                // Mismo caso de carrera que con el usuario, pero para el empleado (único por UsuarioId).
                _empleadoRepositorio.Descartar(empleado);
                var empleadoConcurrente = await _empleadoRepositorio.ObtenerPorUsuarioIdAsync(usuario.UsuarioId);
                if (empleadoConcurrente is null)
                {
                    throw;
                }

                empleado = empleadoConcurrente;
                empleado.EmpresaId = empresaId;
            }

            if (usuarioEsNuevo)
            {
                resultado.EmpleadosCreados++;
            }
            else
            {
                resultado.EmpleadosVinculados++;
            }
        }
        else
        {
            empleado.EmpresaId = empresaId;
            resultado.EmpleadosVinculados++;
        }

        var rolEmpleado = usuario.UsuarioRoles.FirstOrDefault(r => r.Rol == Rol.EMPLEADO);
        if (rolEmpleado is null)
        {
            await _usuarioRolRepositorio.AgregarAsync(new UsuarioRol { UsuarioId = usuario.UsuarioId, Rol = Rol.EMPLEADO, EmpresaId = empresaId, Activo = true });
            await _usuarioRolRepositorio.GuardarCambiosAsync();
        }
        else
        {
            rolEmpleado.EmpresaId = empresaId;
            rolEmpleado.Activo = true;
        }

        await _empleadoRepositorio.GuardarCambiosAsync();
        return empleado;
    }

    /// <summary>
    /// Indica, sin guardar nada, qué pasaría con la cédula al importar: <c>NUEVO</c> (no tiene cuenta),
    /// <c>CUENTA_REGISTRADA</c> (tiene cuenta pero no es empleado), <c>EXISTENTE</c> (ya es empleado de
    /// la empresa) o <c>CAMBIA_DE_EMPRESA</c> (es empleado de otra empresa).
    /// </summary>
    public async Task<string> ClasificarAsync(int empresaId, string cedula)
    {
        var usuario = await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(cedula);
        if (usuario is null)
        {
            return "NUEVO";
        }

        var empleado = await _empleadoRepositorio.ObtenerPorUsuarioIdAsync(usuario.UsuarioId);
        if (empleado is null)
        {
            return "CUENTA_REGISTRADA";
        }

        return empleado.EmpresaId == empresaId ? "EXISTENTE" : "CAMBIA_DE_EMPRESA";
    }
}
