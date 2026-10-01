namespace TransportApp.Domain.Entities;

/// <summary>
/// Guarda, para una empresa, el orden de columnas que el coordinador definió
/// la primera vez que creó una ruta pegando filas copiadas de un Excel
/// externo (ver <c>ImportacionExcelServicio.CrearRutaPegadaAsync</c>): así no
/// tiene que volver a indicarlo en cada pegado, solo la primera vez o cuando
/// decida crear un esqueleto nuevo porque cambió el formato de su fuente.
/// Hay como máximo una por empresa: crear una nueva reemplaza a la anterior.
/// No decide ni valida el contenido pegado en sí; eso corresponde a la capa
/// de aplicación.
/// </summary>
public class PlantillaColumnasPegado
{
    /// <summary>Identificador único de la plantilla.</summary>
    public int PlantillaColumnasPegadoId { get; set; }

    /// <summary>Empresa a la que pertenece esta plantilla.</summary>
    public int EmpresaId { get; set; }

    /// <summary>
    /// Campos, en el mismo orden en que aparecen las columnas en el Excel
    /// externo del coordinador (por ejemplo <c>["CEDULA", "NOMBRE",
    /// "APELLIDOS", "BARRIO", "DIRECCION", "IGNORAR", "CELULAR"]</c>). Debe
    /// contener, una sola vez cada uno, los cinco campos obligatorios del
    /// pasajero (cédula, nombre, celular, dirección y barrio); "APELLIDOS"
    /// es opcional (como máximo una vez) para cuando el nombre viene
    /// dividido en dos columnas, e "IGNORAR" marca columnas del Excel que no
    /// corresponden a ningún dato del pasajero (sin límite de repeticiones).
    /// Esa validación la aplica la capa de aplicación al guardar.
    /// </summary>
    public List<string> ColumnasEnOrden { get; set; } = new();

    /// <summary>Empresa a la que pertenece esta plantilla.</summary>
    public Empresa? Empresa { get; set; }
}
