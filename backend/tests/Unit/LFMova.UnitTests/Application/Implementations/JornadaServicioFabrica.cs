using LFMova.Application.DTOs.Soportes;
using LFMova.Application.Implementations;
using LFMova.Application.Implementations.Jornadas;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;

namespace LFMova.UnitTests.Application.Implementations;

/// <summary>
/// Arma un <see cref="JornadaServicio"/> real con sus colaboradores a partir de los repositorios y
/// servicios que cada prueba decide (normalmente falsos en memoria), igual que lo haría el contenedor
/// de dependencias. No contiene aserciones ni datos de prueba.
/// </summary>
internal static class JornadaServicioFabrica
{
    /// <summary>Crea el servicio conectando sus colaboradores con las dependencias indicadas.</summary>
    public static JornadaServicio Crear(
        IJornadaRepositorio jornadaRepositorio,
        IServicioRepositorio servicioRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        IProgramacionTransporteRepositorio programacionRepositorio,
        INotificacionServicio notificacionServicio,
        IServicioCorreo? servicioCorreo = null)
        => new(
            jornadaRepositorio, servicioRepositorio, unidadOperativaRepositorio, conductorRepositorio, servicioPasajeroRepositorio,
            empleadoRepositorio, notificacionServicio,
            new DepuradorJornada(jornadaRepositorio, servicioRepositorio, servicioPasajeroRepositorio, programacionRepositorio),
            new EnviadorSoporteRutas(
                servicioRepositorio, unidadOperativaRepositorio, conductorRepositorio, servicioPasajeroRepositorio, empleadoRepositorio,
                new EmpresaRepositorioFijo(), new GeneradorSoporteFalso(), servicioCorreo ?? new CorreoSoporteFalso()));

    /// <summary>Devuelve siempre una empresa con el nombre "Empresa de prueba"; no guarda nada.</summary>
    private class EmpresaRepositorioFijo : IEmpresaRepositorio
    {
        public Task<Empresa?> ObtenerPorIdAsync(int empresaId)
            => Task.FromResult<Empresa?>(new Empresa { EmpresaId = empresaId, Nombre = "Empresa de prueba" });

        public Task<Empresa?> ObtenerPorCifAsync(string cif) => Task.FromResult<Empresa?>(null);

        public Task<List<Empresa>> ObtenerTodasAsync() => Task.FromResult(new List<Empresa>());

        public Task AgregarAsync(Empresa empresa) => Task.CompletedTask;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    /// <summary>En vez de un Excel, devuelve un byte por cada pasajero del soporte: basta para comprobar qué se incluyó.</summary>
    private class GeneradorSoporteFalso : IGeneradorSoporteRutas
    {
        public byte[] Generar(SoporteRutasConductor soporte) => new byte[soporte.Rutas.Sum(r => r.Pasajeros.Count)];
    }
}

/// <summary>
/// Doble del servicio de correo para las pruebas del soporte de rutas: conserva los correos con adjunto
/// "enviados" y, si se le pide, falla al enviar para comprobar que la publicación no se deshace.
/// </summary>
internal class CorreoSoporteFalso : IServicioCorreo
{
    public readonly List<(string Destino, string Asunto, AdjuntoCorreo Adjunto)> Enviados = new();

    /// <summary>Si es <c>true</c>, todo envío lanza una excepción, como cuando el proveedor de correo está caído.</summary>
    public bool Falla { get; set; }

    public Task EnviarAsync(string destinatarioEmail, string destinatarioNombre, string asunto, string cuerpoHtml) => Task.CompletedTask;

    public Task EnviarConAdjuntoAsync(string destinatarioEmail, string destinatarioNombre, string asunto, string cuerpoHtml, AdjuntoCorreo adjunto)
    {
        if (Falla)
        {
            throw new InvalidOperationException("El proveedor de correo no responde.");
        }

        Enviados.Add((destinatarioEmail, asunto, adjunto));
        return Task.CompletedTask;
    }
}
