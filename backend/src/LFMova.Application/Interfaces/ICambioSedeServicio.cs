namespace LFMova.Application.Interfaces;

/// <summary>
/// Caso de uso del coordinador que corrige la sede de una ruta ya creada (por ejemplo, el archivo traía otra sede
/// o no traía ninguna). No decide autorización: eso se verifica en la capa de Api.
/// </summary>
public interface ICambioSedeServicio
{
    /// <summary>
    /// Cambia la sede del servicio y, con ella, la de la programación de cada uno de sus pasajeros, para que la
    /// necesidad de transporte siga coincidiendo con la ruta. Solo una ruta que todavía no ha salido (ni terminado ni
    /// cancelado). Si estaba publicada vuelve a ASIGNADO para que el coordinador la revise y la publique de nuevo, y
    /// se avisa a su conductor. Falla si la sede no es una sede activa de la empresa, si el cambio haría que la unidad
    /// del servicio se cruce con otra ruta suya que antes no se cruzaba, o si algún pasajero ya tiene una
    /// programación en la sede nueva a esa misma hora. No modifica el histórico de los pasajeros (su dirección de recogida).
    /// </summary>
    Task CambiarSedeAsync(int empresaId, int servicioId, int sedeId);
}
