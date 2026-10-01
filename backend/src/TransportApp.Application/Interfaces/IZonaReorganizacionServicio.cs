namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define los casos de uso para reorganizar las zonas ya registradas de una
/// empresa: mover un barrio de una zona a otra, unir dos zonas en una y
/// eliminar una zona. No crea zonas ni edita sus datos (ver <see cref="IZonaServicio"/>).
/// </summary>
public interface IZonaReorganizacionServicio
{
    /// <summary>
    /// Pasa un barrio de la zona de origen a la zona de destino. Si la zona de
    /// origen se queda sin barrios, se elimina para no dejar una zona vacía duplicada.
    /// </summary>
    Task MoverBarrioAsync(int empresaId, int zonaOrigenId, string barrio, int zonaDestinoId);

    /// <summary>
    /// Une la zona de origen con la de destino: todos sus barrios pasan a la de
    /// destino (que conserva su nombre, macrozona y corredor) y la de origen se elimina.
    /// </summary>
    Task UnirAsync(int empresaId, int zonaOrigenId, int zonaDestinoId);

    /// <summary>Elimina por completo una zona de la empresa; sus barrios quedan sin zona.</summary>
    Task EliminarAsync(int empresaId, int zonaId);
}
