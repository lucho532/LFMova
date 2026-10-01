using LFMova.Application.DTOs.Empresas;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Application.Mappers;
using LFMova.Application.Validators;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa los casos de uso de administración de empresas: creación
/// (junto con su primer coordinador), consulta y activación/desactivación.
/// No accede directamente a Entity Framework Core.
/// </summary>
public class EmpresaServicio : IEmpresaServicio
{
    private readonly IEmpresaRepositorio _empresaRepositorio;
    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly IUsuarioRolRepositorio _usuarioRolRepositorio;
    private readonly IRecuperacionContrasenaServicio _recuperacionContrasenaServicio;

    /// <summary>Crea el servicio con sus repositorios y el servicio de invitación por correo.</summary>
    public EmpresaServicio(
        IEmpresaRepositorio empresaRepositorio,
        IUsuarioRepositorio usuarioRepositorio,
        IUsuarioRolRepositorio usuarioRolRepositorio,
        IRecuperacionContrasenaServicio recuperacionContrasenaServicio)
    {
        _empresaRepositorio = empresaRepositorio;
        _usuarioRepositorio = usuarioRepositorio;
        _usuarioRolRepositorio = usuarioRolRepositorio;
        _recuperacionContrasenaServicio = recuperacionContrasenaServicio;
    }

    /// <inheritdoc />
    public async Task<EmpresaDto> CrearAsync(CrearEmpresaDto datos)
    {
        if (!CifValidador.EsValido(datos.Cif))
        {
            throw new InvalidOperationException("El CIF de la empresa es obligatorio.");
        }

        if (!DireccionValidador.EsValida(datos.Direccion))
        {
            throw new InvalidOperationException("La dirección de la empresa es obligatoria.");
        }

        if (!CedulaValidador.EsValida(datos.CedulaCoordinador))
        {
            throw new InvalidOperationException("La cédula del coordinador es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(datos.NombreCoordinador))
        {
            throw new InvalidOperationException("El nombre del coordinador es obligatorio.");
        }

        if (!TelefonoValidador.EsValido(datos.TelefonoCoordinador))
        {
            throw new InvalidOperationException("El teléfono del coordinador es obligatorio.");
        }

        if (!EmailValidador.EsValido(CorreoNormalizador.Normalizar(datos.CorreoCoordinador ?? string.Empty)))
        {
            throw new InvalidOperationException("El correo del coordinador es obligatorio.");
        }

        if (await _empresaRepositorio.ObtenerPorCifAsync(datos.Cif) is not null)
        {
            throw new InvalidOperationException("Ya existe una empresa registrada con este CIF.");
        }

        var usuarioCoordinador = await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(datos.CedulaCoordinador);

        // Una persona ya registrada (con o sin contraseña) puede ser el coordinador: se reutiliza su cuenta.
        if (usuarioCoordinador is null)
        {
            var usuarioConEseCorreo = await _usuarioRepositorio.ObtenerPorEmailAsync(CorreoNormalizador.Normalizar(datos.CorreoCoordinador));
            if (usuarioConEseCorreo is not null)
            {
                throw new InvalidOperationException("Ya existe una cuenta registrada con este correo.");
            }
        }
        else if (ReglasUsuarioRol.ViolaUnicidadDeCoordinador(usuarioCoordinador.UsuarioRoles.Where(r => r.Activo), 0))
        {
            throw new InvalidOperationException("Esa persona ya es coordinadora activa de otra empresa.");
        }

        var empresa = new Empresa
        {
            Nombre = datos.Nombre,
            Cif = datos.Cif,
            Direccion = datos.Direccion,
            Activa = true
        };

        await _empresaRepositorio.AgregarAsync(empresa);
        await _empresaRepositorio.GuardarCambiosAsync();

        if (usuarioCoordinador is null)
        {
            usuarioCoordinador = new Usuario
            {
                Cedula = datos.CedulaCoordinador,
                Email = CorreoNormalizador.Normalizar(datos.CorreoCoordinador),
                NombreCompleto = datos.NombreCoordinador,
                Telefono = datos.TelefonoCoordinador,
                PasswordHash = null,
                CorreoConfirmado = false,
                Activo = true
            };

            await _usuarioRepositorio.AgregarAsync(usuarioCoordinador);
            await _usuarioRepositorio.GuardarCambiosAsync();
        }

        await _usuarioRolRepositorio.AgregarAsync(new UsuarioRol
        {
            UsuarioId = usuarioCoordinador.UsuarioId,
            Rol = Rol.COORDINADOR,
            EmpresaId = empresa.EmpresaId,
            Activo = true
        });
        await _usuarioRolRepositorio.GuardarCambiosAsync();

        // Quien aún no tiene contraseña recibe la invitación por correo; quien ya tiene cuenta entra con la suya.
        if (usuarioCoordinador.PasswordHash is null)
        {
            await _recuperacionContrasenaServicio.EnviarInvitacionAsync(usuarioCoordinador, $"como coordinador de {empresa.Nombre}");
        }

        return EmpresaMapper.AEmpresaDto(empresa);
    }

    /// <inheritdoc />
    public async Task<EmpresaDto?> ObtenerPorIdAsync(int empresaId)
    {
        var empresa = await _empresaRepositorio.ObtenerPorIdAsync(empresaId);
        return empresa is null ? null : EmpresaMapper.AEmpresaDto(empresa);
    }

    /// <inheritdoc />
    public async Task<List<EmpresaDto>> ObtenerTodasAsync()
    {
        var empresas = await _empresaRepositorio.ObtenerTodasAsync();
        var resultado = new List<EmpresaDto>();

        foreach (var empresa in empresas)
        {
            var coordinadores = await _usuarioRolRepositorio.ObtenerPorEmpresaYRolAsync(empresa.EmpresaId, Rol.COORDINADOR);
            var usuarioPrincipal = coordinadores.FirstOrDefault(c => c.Activo)?.Usuario;
            resultado.Add(EmpresaMapper.AEmpresaDto(empresa, usuarioPrincipal?.Cedula, usuarioPrincipal?.NombreCompleto));
        }

        return resultado;
    }

    /// <inheritdoc />
    public async Task ActivarAsync(int empresaId)
    {
        var empresa = await ObtenerEmpresaOFallarAsync(empresaId);
        empresa.Activa = true;
        await _empresaRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task DesactivarAsync(int empresaId)
    {
        var empresa = await ObtenerEmpresaOFallarAsync(empresaId);
        empresa.Activa = false;
        await _empresaRepositorio.GuardarCambiosAsync();
    }

    private async Task<Empresa> ObtenerEmpresaOFallarAsync(int empresaId)
    {
        var empresa = await _empresaRepositorio.ObtenerPorIdAsync(empresaId);
        if (empresa is null)
        {
            throw new InvalidOperationException("La empresa indicada no existe.");
        }

        return empresa;
    }
}
