using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.DTOs.Programaciones;
using LFMova.Application.DTOs.Servicios;
using LFMova.Application.DTOs.ServiciosPasajero;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Agrega una persona como pasajera de un servicio ya creado: asegura al empleado (lo crea, lo vincula o
/// lo pasa de empresa), asegura su programación de transporte y la asigna al servicio, dejando la
/// dirección usada en el pasajero para el histórico. Lo comparten la creación de una ruta pegada y la
/// importación de rutas armadas. No decide en qué ruta va cada persona ni valida el formato de las filas.
/// </summary>
public class AgregadorPasajeroServicio
{
    private readonly IProgramacionTransporteServicio _programacionServicio;
    private readonly IServicioPasajeroServicio _servicioPasajeroServicio;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly INotificacionServicio _notificacionServicio;
    private readonly AseguradorEmpleadoImportacion _aseguradorEmpleado;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public AgregadorPasajeroServicio(
        IProgramacionTransporteServicio programacionServicio,
        IServicioPasajeroServicio servicioPasajeroServicio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        INotificacionServicio notificacionServicio,
        AseguradorEmpleadoImportacion aseguradorEmpleado)
    {
        _programacionServicio = programacionServicio;
        _servicioPasajeroServicio = servicioPasajeroServicio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _notificacionServicio = notificacionServicio;
        _aseguradorEmpleado = aseguradorEmpleado;
    }

    /// <summary>
    /// Asegura al empleado y su programación de transporte y lo asigna como pasajero del servicio. Falla
    /// si esa persona ya está asignada a una ruta de ese mismo horario. <paramref name="programaciones"/>
    /// son las programaciones ya existentes de la empresa (cargadas una vez por quien llama) y se
    /// completa con las que se creen aquí.
    /// </summary>
    public async Task AgregarAsync(
        int empresaId, ServicioDto servicio, FilaHoja fila, List<ProgramacionDto> programaciones, ResultadoImportacionDto resultado, bool notificarConductorSiTieneUnidad = true)
    {
        var empleado = await _aseguradorEmpleado.AsegurarAsync(empresaId, fila, resultado);

        var programacion = programaciones.FirstOrDefault(p =>
            p.EmpleadoId == empleado.EmpleadoId && p.SedeId == servicio.SedeId && p.Fecha == servicio.Fecha && p.Hora == servicio.HoraProgramada && p.Tipo == servicio.Tipo);

        if (programacion is not null && await _servicioPasajeroRepositorio.ObtenerPorProgramacionAsync(programacion.ProgramacionTransporteId) is not null)
        {
            throw new InvalidOperationException("Esa persona ya está asignada a una ruta de ese horario.");
        }

        if (programacion is null)
        {
            programacion = await _programacionServicio.CrearAsync(empresaId, new CrearProgramacionDto
            {
                EmpleadoId = empleado.EmpleadoId,
                SedeId = servicio.SedeId,
                Fecha = servicio.Fecha,
                Hora = servicio.HoraProgramada,
                Tipo = servicio.Tipo,
                DireccionRecogida = fila.Direccion,
                BarrioRecogida = fila.Barrio
            });
            programaciones.Add(programacion);
            resultado.ProgramacionesCreadas++;
        }

        var pasajeroCreado = await _servicioPasajeroServicio.CrearAsync(
            empresaId, servicio.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = programacion.ProgramacionTransporteId });
        resultado.PasajerosAsignados++;

        // Si la ruta ya tenía conductor, agregar un pasajero de última hora la modifica: se le avisa para
        // que no se entere solo al llegar y encontrar una parada que no esperaba. No aplica cuando el
        // pasajero se agrega como parte de la creación inicial de la ruta (ver notificarConductorSiTieneUnidad):
        // ahí no es "de última hora", es la ruta completa recién armada.
        if (servicio.UnidadOperativaId is not null && notificarConductorSiTieneUnidad
            && ReglasEstadoServicio.EsVisibleParaConductorYEmpleado(servicio.Estado))
        {
            var usuarioIdConductor = await _servicioPasajeroServicio.ObtenerUsuarioIdConductorAsync(empresaId, pasajeroCreado.ServicioPasajeroId);
            if (usuarioIdConductor is not null)
            {
                await _notificacionServicio.CrearAsync(
                    usuarioIdConductor.Value,
                    "RUTA_MODIFICADA",
                    "Tu ruta cambió",
                    $"Se agregó un pasajero de última hora a tu {FormatoOperacion.DescribirRuta(servicio.Tipo, servicio.HoraProgramada, servicio.Fecha, servicio.NombreSede)}.");
            }
        }
    }
}
