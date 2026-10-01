namespace TransportApp.Application.Interfaces;

/// <summary>
/// Detecta servicios de <c>ENTRADA</c> cuya ruta debería haber sido iniciada
/// en aproximadamente una hora y todavía no fue iniciada, notificando al
/// conductor responsable (ver <c>spec.md</c> §38 de <c>plan.md</c> y
/// <c>tasks.md</c> T094). La alerta no constituye una orden de inicio ni
/// modifica el servicio. Pensado para ser invocado periódicamente por un
/// proceso en segundo plano.
/// </summary>
public interface IAlertaEjecucionServicio
{
    /// <summary>
    /// Revisa todos los servicios de <c>ENTRADA</c> en estado
    /// <c>PUBLICADO</c> y notifica al conductor responsable de cada uno que
    /// esté dentro de la ventana de alerta. No distingue si ya se notificó
    /// en una ejecución anterior: una llamada repetida dentro de la misma
    /// ventana puede volver a notificar (ver nota en <c>tasks.md</c> T094).
    /// </summary>
    Task VerificarRutasNoIniciadasAsync();
}
