using LFMova.Application.DTOs.Importaciones;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define los casos de uso de importación de la programación diaria desde un
/// Excel: validar (sin guardar) e importar. No decide autorización: eso se
/// verifica en la capa de Api antes de invocar estos métodos.
/// </summary>
public interface IImportacionExcelServicio
{
    /// <summary>
    /// Interpreta el archivo y devuelve qué contiene y qué pasaría al
    /// importarlo, sin guardar nada.
    /// </summary>
    Task<VistaPreviaImportacionDto> ValidarAsync(int empresaId, Stream archivo);

    /// <summary>
    /// Importa el archivo: crea o vincula empleados, crea las programaciones,
    /// la jornada de la fecha operativa, sus servicios y asigna los pasajeros.
    /// Si se indica una unidad operativa, se asigna a todos los servicios; si se pide repartir, cada servicio se divide entre las unidades libres según su capacidad.
    /// La sede es opcional en el Excel: si un grupo de pasajeros no trae una
    /// sede reconocida, se usa <paramref name="sedeGeneralId"/> (elegida por
    /// el coordinador); si tampoco se indica, falla.
    /// Falla si el archivo tiene errores. Es repetible: lo ya importado no se
    /// duplica.
    /// </summary>
    Task<ResultadoImportacionDto> ImportarAsync(
        int empresaId, int usuarioCoordinadorId, string nombreArchivo, Stream archivo, DateOnly fechaOperativa, int? unidadOperativaId, bool repartirEntreUnidades, int? sedeGeneralId);

    /// <summary>
    /// Crea una ruta vacía a mano para una unidad operativa (o reutiliza la
    /// que ya exista con esa misma fecha, hora, tipo, sede y unidad), sin
    /// darle de alta ningún pasajero: sirve para abrir un destino nuevo antes
    /// de moverle pasajeros desde otra ruta (por ejemplo, para dividir una
    /// ruta que quedó con demasiados barrios distintos). Crea la jornada de
    /// la fecha si hace falta. La unidad debe pertenecer a un conductor con
    /// vinculación activa a la empresa y no tener ya otra ruta en ese mismo
    /// horario.
    /// </summary>
    Task<ResultadoImportacionDto> CrearRutaVaciaAsync(int empresaId, CrearRutaVaciaDto datos);

    /// <summary>
    /// Crea una ruta nueva a partir de pasajeros pegados desde un Excel
    /// externo (el pegado y el orden de columnas ya se resolvieron en el
    /// frontend): siempre crea una ruta nueva, sin conductor asignado
    /// (<c>PENDIENTE_ASIGNACION</c>), para asignárselo después desde la
    /// tarjeta. Las filas inválidas o con cédula repetida dentro del mismo
    /// pegado se omiten con un aviso en vez de abortar toda la creación.
    /// </summary>
    Task<ResultadoImportacionDto> CrearRutaPegadaAsync(int empresaId, CrearRutaPegadaDto datos);
}
