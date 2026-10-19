using LFMova.Application.DTOs.Programaciones;
using LFMova.Application.Interfaces;
using LFMova.Application.Mappers;
using LFMova.Application.Validators;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa los casos de uso de administración de programaciones de
/// transporte de una empresa. Aplica <see cref="ReglasMultiempresa"/> para
/// garantizar que el empleado y la sede de la programación pertenezcan a la
/// misma empresa. La dirección y el barrio son obligatorios tanto para
/// <see cref="TipoServicio.ENTRADA"/> como para <see cref="TipoServicio.SALIDA"/>:
/// en ambos casos representan el extremo del trayecto correspondiente al
/// empleado (recogida en <c>ENTRADA</c>, entrega en <c>SALIDA</c>); el otro
/// extremo siempre es la sede indicada por <c>SedeId</c>. No decide
/// autorización: eso se verifica en la capa de Api.
/// </summary>
public class ProgramacionTransporteServicio : IProgramacionTransporteServicio
{
    private readonly IProgramacionTransporteRepositorio _programacionRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;
    private readonly ISedeRepositorio _sedeRepositorio;

    /// <summary>Crea el servicio con sus repositorios.</summary>
    public ProgramacionTransporteServicio(
        IProgramacionTransporteRepositorio programacionRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        ISedeRepositorio sedeRepositorio)
    {
        _programacionRepositorio = programacionRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
        _sedeRepositorio = sedeRepositorio;
    }

    /// <inheritdoc />
    public async Task<ProgramacionDto> CrearAsync(int empresaId, CrearProgramacionDto datos)
    {
        var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(datos.EmpleadoId);
        if (empleado is null || !ReglasMultiempresa.EmpleadoPerteneceAEmpresa(empleado, empresaId))
        {
            throw new InvalidOperationException("El empleado indicado no existe en esta empresa.");
        }

        var sede = await ValidarSedeDeLaEmpresaAsync(empresaId, datos.SedeId);
        ValidarCamposObligatorios(datos.Fecha, datos.Tipo, datos.DireccionRecogida, datos.BarrioRecogida);

        var programacion = new ProgramacionTransporte
        {
            EmpresaId = empresaId,
            EmpleadoId = datos.EmpleadoId,
            SedeId = sede.SedeId,
            Fecha = datos.Fecha,
            Hora = datos.Hora,
            Tipo = datos.Tipo,
            DireccionRecogida = datos.DireccionRecogida,
            BarrioRecogida = datos.BarrioRecogida
        };

        await _programacionRepositorio.AgregarAsync(programacion);
        try
        {
            await _programacionRepositorio.GuardarCambiosAsync();
        }
        catch (Exception)
        {
            // Otra petición concurrente (por ejemplo, dos importaciones solapadas del mismo Excel) pudo
            // haber creado primero la programación para este mismo viaje (empleado + sede + fecha + hora +
            // tipo): se descarta este intento fallido y se reutiliza la ya creada en vez de duplicarla o
            // abortar toda la importación.
            _programacionRepositorio.Descartar(programacion);
            var programacionConcurrente = await _programacionRepositorio.ObtenerPorClaveAsync(
                programacion.EmpleadoId, programacion.SedeId, programacion.Fecha, programacion.Hora, programacion.Tipo);
            if (programacionConcurrente is null)
            {
                throw;
            }

            programacion = programacionConcurrente;
        }

        return ProgramacionTransporteMapper.AProgramacionDto(programacion);
    }

    /// <inheritdoc />
    public async Task<ProgramacionDto?> ObtenerPorIdAsync(int empresaId, int programacionTransporteId)
    {
        var programacion = await _programacionRepositorio.ObtenerPorIdAsync(programacionTransporteId);
        if (programacion is null || !ReglasMultiempresa.ProgramacionPerteneceAEmpresa(programacion, empresaId))
        {
            return null;
        }

        return ProgramacionTransporteMapper.AProgramacionDto(programacion);
    }

    /// <inheritdoc />
    public async Task<List<ProgramacionDto>> ObtenerPorEmpresaAsync(int empresaId)
    {
        var programaciones = await _programacionRepositorio.ObtenerPorEmpresaAsync(empresaId);
        return programaciones.Select(ProgramacionTransporteMapper.AProgramacionDto).ToList();
    }

    /// <inheritdoc />
    public async Task ActualizarAsync(int empresaId, int programacionTransporteId, ActualizarProgramacionDto datos)
    {
        var programacion = await _programacionRepositorio.ObtenerPorIdAsync(programacionTransporteId);
        if (programacion is null || !ReglasMultiempresa.ProgramacionPerteneceAEmpresa(programacion, empresaId))
        {
            throw new InvalidOperationException("La programación indicada no existe en esta empresa.");
        }

        var sede = await ValidarSedeDeLaEmpresaAsync(empresaId, datos.SedeId);
        ValidarCamposObligatorios(datos.Fecha, datos.Tipo, datos.DireccionRecogida, datos.BarrioRecogida);

        programacion.SedeId = sede.SedeId;
        programacion.Fecha = datos.Fecha;
        programacion.Hora = datos.Hora;
        programacion.Tipo = datos.Tipo;
        programacion.DireccionRecogida = datos.DireccionRecogida;
        programacion.BarrioRecogida = datos.BarrioRecogida;

        await _programacionRepositorio.GuardarCambiosAsync();
    }

    private async Task<Sede> ValidarSedeDeLaEmpresaAsync(int empresaId, int sedeId)
    {
        var sede = await _sedeRepositorio.ObtenerPorIdAsync(sedeId);
        if (sede is null || !ReglasMultiempresa.SedePerteneceAEmpresa(sede, empresaId))
        {
            throw new InvalidOperationException("La sede indicada no existe en esta empresa.");
        }

        return sede;
    }

    private static void ValidarCamposObligatorios(DateOnly fecha, TipoServicio tipo, string direccionRecogida, string barrioRecogida)
    {
        if (!FechaProgramacionValidador.EsValida(fecha))
        {
            throw new InvalidOperationException("La fecha de la programación es obligatoria.");
        }

        if (!TipoServicioValidador.EsValido(tipo))
        {
            throw new InvalidOperationException("El tipo de programación indicado no es válido.");
        }

        if (string.IsNullOrWhiteSpace(direccionRecogida))
        {
            throw new InvalidOperationException("La dirección de recogida es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(barrioRecogida))
        {
            throw new InvalidOperationException("El barrio de recogida es obligatorio.");
        }
    }
}
