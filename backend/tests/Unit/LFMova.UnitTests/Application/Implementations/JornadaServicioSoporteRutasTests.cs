using LFMova.Application.DTOs.Jornadas;
using LFMova.Application.Implementations;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Implementations;

/// <summary>
/// Pruebas del soporte de rutas que recibe cada conductor por correo al publicar una jornada. Usan los
/// mismos repositorios falsos que el resto de pruebas de <see cref="JornadaServicio"/>.
/// </summary>
public partial class JornadaServicioTests
{
    private static Conductor ConductorConCorreo(int conductorId, int usuarioId, string correo) => new()
    {
        ConductorId = conductorId,
        UsuarioId = usuarioId,
        NombreCompleto = $"Conductor {conductorId}",
        Activo = true,
        Usuario = new Usuario { UsuarioId = usuarioId, Email = correo }
    };

    [Fact]
    public async Task PublicarAsync_EnviaACadaConductorUnSoporteSoloConSusRutas()
    {
        var servicioRepo = new ServicioRepositorioFalso();
        var unidadRepo = new UnidadOperativaRepositorioFalso(
            new UnidadOperativa { UnidadOperativaId = 1, ConductorId = 10, VehiculoId = 1, Activa = true },
            new UnidadOperativa { UnidadOperativaId = 2, ConductorId = 11, VehiculoId = 2, Activa = true });
        var conductorRepo = new ConductorRepositorioFalso(ConductorConCorreo(10, 100, "uno@pruebas.test"), ConductorConCorreo(11, 101, "dos@pruebas.test"));
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso(
            new ServicioPasajero { ServicioPasajeroId = 1, ServicioId = 1, EmpleadoId = 20, Orden = 1 },
            new ServicioPasajero { ServicioPasajeroId = 2, ServicioId = 1, EmpleadoId = 21, Orden = 2 },
            new ServicioPasajero { ServicioPasajeroId = 3, ServicioId = 2, EmpleadoId = 22, Orden = 1 });
        var correo = new CorreoSoporteFalso();
        var servicio = CrearServicio(new JornadaRepositorioFalso(), servicioRepo, unidadRepo, conductorRepo, pasajeroRepo, servicioCorreo: correo);
        var jornada = await servicio.CrearAsync(1, new CrearJornadaDto { FechaOperativa = new DateOnly(2026, 1, 20) });
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 1, JornadaId = jornada.JornadaId, UnidadOperativaId = 1, Estado = EstadoServicio.ASIGNADO });
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 2, JornadaId = jornada.JornadaId, UnidadOperativaId = 2, Estado = EstadoServicio.ASIGNADO });

        await servicio.PublicarAsync(1, jornada.JornadaId);

        Assert.Equal(2, correo.Enviados.Count);
        // El generador falso devuelve un byte por pasajero: cada conductor recibe solo los de sus rutas.
        var soporteUno = Assert.Single(correo.Enviados, e => e.Destino == "uno@pruebas.test");
        Assert.Equal(2, soporteUno.Adjunto.Contenido.Length);
        Assert.Equal("rutas-2026-01-20.xlsx", soporteUno.Adjunto.NombreArchivo);
        var soporteDos = Assert.Single(correo.Enviados, e => e.Destino == "dos@pruebas.test");
        Assert.Single(soporteDos.Adjunto.Contenido);
    }

    [Fact]
    public async Task PublicarAsync_AlVolverAPublicar_ReenviaElSoporteConTodasLasRutasYaPublicadasDelConductor()
    {
        var servicioRepo = new ServicioRepositorioFalso();
        var unidadRepo = new UnidadOperativaRepositorioFalso(new UnidadOperativa { UnidadOperativaId = 1, ConductorId = 10, VehiculoId = 1, Activa = true });
        var conductorRepo = new ConductorRepositorioFalso(ConductorConCorreo(10, 100, "uno@pruebas.test"));
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso(
            new ServicioPasajero { ServicioPasajeroId = 1, ServicioId = 1, EmpleadoId = 20, Orden = 1 },
            new ServicioPasajero { ServicioPasajeroId = 2, ServicioId = 2, EmpleadoId = 21, Orden = 1 });
        var correo = new CorreoSoporteFalso();
        var servicio = CrearServicio(new JornadaRepositorioFalso(), servicioRepo, unidadRepo, conductorRepo, pasajeroRepo, servicioCorreo: correo);
        var jornada = await servicio.CrearAsync(1, new CrearJornadaDto { FechaOperativa = new DateOnly(2026, 1, 20) });
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 1, JornadaId = jornada.JornadaId, UnidadOperativaId = 1, Estado = EstadoServicio.PUBLICADO, HoraProgramada = new TimeOnly(5, 0) });
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 2, JornadaId = jornada.JornadaId, UnidadOperativaId = 1, Estado = EstadoServicio.ASIGNADO, HoraProgramada = new TimeOnly(14, 0) });

        await servicio.PublicarAsync(1, jornada.JornadaId);

        // Solo se publicó la segunda ruta, pero el soporte trae las dos: el último correo es siempre el vigente.
        var soporte = Assert.Single(correo.Enviados);
        Assert.Equal(2, soporte.Adjunto.Contenido.Length);
    }

    [Fact]
    public async Task PublicarAsync_NoEnviaSoporte_SiNoSePublicoNadaNuevoOElConductorNoTieneCorreo()
    {
        var servicioRepo = new ServicioRepositorioFalso();
        var unidadRepo = new UnidadOperativaRepositorioFalso(
            new UnidadOperativa { UnidadOperativaId = 1, ConductorId = 10, VehiculoId = 1, Activa = true },
            new UnidadOperativa { UnidadOperativaId = 2, ConductorId = 11, VehiculoId = 2, Activa = true });
        var conductorRepo = new ConductorRepositorioFalso(
            ConductorConCorreo(10, 100, "uno@pruebas.test"),
            new Conductor { ConductorId = 11, UsuarioId = 101, Activo = true, Usuario = new Usuario { UsuarioId = 101, Email = null } });
        var correo = new CorreoSoporteFalso();
        var servicio = CrearServicio(new JornadaRepositorioFalso(), servicioRepo, unidadRepo, conductorRepo, servicioCorreo: correo);
        var jornada = await servicio.CrearAsync(1, new CrearJornadaDto { FechaOperativa = new DateOnly(2026, 1, 20) });
        // El conductor 10 ya tenía su ruta publicada; solo se publica ahora la del conductor 11, que no tiene correo.
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 1, JornadaId = jornada.JornadaId, UnidadOperativaId = 1, Estado = EstadoServicio.PUBLICADO });
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 2, JornadaId = jornada.JornadaId, UnidadOperativaId = 2, Estado = EstadoServicio.ASIGNADO });

        await servicio.PublicarAsync(1, jornada.JornadaId);

        Assert.Empty(correo.Enviados);
    }

    [Fact]
    public async Task PublicarAsync_PublicaYNotifica_AunqueFalleElCorreoDelSoporte()
    {
        var servicioRepo = new ServicioRepositorioFalso();
        var unidadRepo = new UnidadOperativaRepositorioFalso(new UnidadOperativa { UnidadOperativaId = 1, ConductorId = 10, VehiculoId = 1, Activa = true });
        var conductorRepo = new ConductorRepositorioFalso(ConductorConCorreo(10, 100, "uno@pruebas.test"));
        var notificacionRepo = new NotificacionRepositorioFalso();
        var correo = new CorreoSoporteFalso { Falla = true };
        var servicio = CrearServicio(new JornadaRepositorioFalso(), servicioRepo, unidadRepo, conductorRepo, notificacionRepositorio: notificacionRepo, servicioCorreo: correo);
        var jornada = await servicio.CrearAsync(1, new CrearJornadaDto { FechaOperativa = new DateOnly(2026, 1, 20) });
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 1, JornadaId = jornada.JornadaId, UnidadOperativaId = 1, Estado = EstadoServicio.ASIGNADO });

        await servicio.PublicarAsync(1, jornada.JornadaId);

        Assert.Equal(EstadoServicio.PUBLICADO, servicioRepo.Servicios[0].Estado);
        Assert.Contains(notificacionRepo.Notificaciones, n => n.UsuarioId == 100);
    }
}
