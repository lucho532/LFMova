using LFMova.Application.Interfaces;

namespace LFMova.Api.BackgroundServices;

/// <summary>
/// Ejecuta periódicamente la alerta de ruta no iniciada (ver <c>tasks.md</c>
/// T094). El intervalo de sondeo es una decisión de implementación, no una
/// regla de negocio, y puede ajustarse sin afectar el comportamiento
/// definido en <c>spec.md</c>/<c>plan.md</c>. Cada verificación vuelve a
/// avisar de las rutas que siguen sin iniciarse (es decir, un aviso cada 15
/// minutos); <c>AlertaEjecucionServicio</c> evita solo los avisos repetidos
/// en menos de 10 minutos. Solo corre mientras el API está en marcha.
/// </summary>
public class AlertaEjecucionBackgroundService : BackgroundService
{
    private static readonly TimeSpan IntervaloDeSondeo = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _fabricaDeAlcance;
    private readonly ILogger<AlertaEjecucionBackgroundService> _logger;

    /// <summary>Crea el servicio en segundo plano con su fábrica de alcances de inyección de dependencias.</summary>
    public AlertaEjecucionBackgroundService(IServiceScopeFactory fabricaDeAlcance, ILogger<AlertaEjecucionBackgroundService> logger)
    {
        _fabricaDeAlcance = fabricaDeAlcance;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var alcance = _fabricaDeAlcance.CreateScope();
                var alertaEjecucionServicio = alcance.ServiceProvider.GetRequiredService<IAlertaEjecucionServicio>();
                await alertaEjecucionServicio.VerificarRutasNoIniciadasAsync();
            }
            catch (Exception excepcion)
            {
                _logger.LogError(excepcion, "Error al verificar rutas no iniciadas.");
            }

            try
            {
                await Task.Delay(IntervaloDeSondeo, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada al detener la aplicación.
            }
        }
    }
}
