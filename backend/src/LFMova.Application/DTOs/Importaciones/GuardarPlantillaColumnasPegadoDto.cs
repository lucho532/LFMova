namespace LFMova.Application.DTOs.Importaciones;

/// <summary>
/// Datos para guardar (crear o reemplazar) la plantilla de orden de columnas
/// de la empresa, en el orden en que aparecen en el Excel externo del
/// coordinador. Debe traer, cada uno exactamente una vez, los cinco campos
/// obligatorios del pasajero (CEDULA, NOMBRE, CELULAR, DIRECCION, BARRIO).
/// NOMBRE puede ser el nombre completo, o solo el nombre si el Excel trae
/// los apellidos en otra columna aparte marcada como APELLIDOS (opcional,
/// como máximo una vez). Cualquier otra columna del Excel que no sea uno de
/// estos datos se marca como IGNORAR (puede repetirse las veces que haga falta).
/// </summary>
public class GuardarPlantillaColumnasPegadoDto
{
    /// <summary>Campos, en el orden en que vienen en el Excel externo del coordinador.</summary>
    public List<string> ColumnasEnOrden { get; set; } = new();
}
