using TransportApp.Application.DTOs.Empleados;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Mappers;
using TransportApp.Application.Validators;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Rules;

namespace TransportApp.Application.Implementations;

/// <summary>
/// Implementa los casos de uso de gestión directa de empleados de una
/// empresa. Aplica <see cref="ReglasMultiempresa"/> para garantizar que un
/// empleado solamente se consulte o modifique dentro del contexto de su
/// propia empresa. No accede directamente a Entity Framework Core.
/// </summary>
public class EmpleadoServicio : IEmpleadoServicio
{
    private readonly IEmpleadoRepositorio _empleadoRepositorio;

    /// <summary>Crea el servicio con su repositorio.</summary>
    public EmpleadoServicio(IEmpleadoRepositorio empleadoRepositorio)
    {
        _empleadoRepositorio = empleadoRepositorio;
    }

    /// <inheritdoc />
    public async Task<List<EmpleadoDto>> ObtenerPorEmpresaAsync(int empresaId)
    {
        var empleados = await _empleadoRepositorio.ObtenerPorEmpresaAsync(empresaId);
        return empleados.Select(EmpleadoMapper.AEmpleadoDto).ToList();
    }

    /// <inheritdoc />
    public async Task<EmpleadoDto?> ObtenerPorIdAsync(int empresaId, int empleadoId)
    {
        var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(empleadoId);
        if (empleado is null || !ReglasMultiempresa.EmpleadoPerteneceAEmpresa(empleado, empresaId))
        {
            return null;
        }

        return EmpleadoMapper.AEmpleadoDto(empleado);
    }

    /// <inheritdoc />
    public async Task ActualizarAsync(int empresaId, int empleadoId, ActualizarEmpleadoDto datos)
    {
        if (!TelefonoValidador.EsValido(datos.Telefono))
        {
            throw new InvalidOperationException("El teléfono del empleado es obligatorio.");
        }

        var empleado = await ObtenerEmpleadoDeLaEmpresaOFallarAsync(empresaId, empleadoId);

        empleado.NombreCompleto = datos.NombreCompleto;
        empleado.Telefono = datos.Telefono;
        empleado.Direccion = datos.Direccion;
        empleado.Barrio = datos.Barrio;

        await _empleadoRepositorio.GuardarCambiosAsync();
    }

    private async Task<Empleado> ObtenerEmpleadoDeLaEmpresaOFallarAsync(int empresaId, int empleadoId)
    {
        var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(empleadoId);
        if (empleado is null || !ReglasMultiempresa.EmpleadoPerteneceAEmpresa(empleado, empresaId))
        {
            throw new InvalidOperationException("El empleado indicado no existe en esta empresa.");
        }

        return empleado;
    }
}
