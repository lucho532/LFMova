using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.DTOs.Programaciones;
using LFMova.Application.DTOs.Servicios;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Resultado de interpretar un archivo contra los datos de la empresa: la hoja leída, la vista previa
/// que ve el coordinador y la sede encontrada para cada grupo. No guarda nada por sí mismo.
/// </summary>
public sealed record AnalisisImportacion(HojaProgramacion Hoja, VistaPreviaImportacionDto Vista, List<Sede?> SedePorGrupo);

/// <summary>Unidad operativa que puede recibir pasajeros en el reparto automático, con la capacidad de su vehículo.</summary>
public sealed record UnidadDisponible(int UnidadOperativaId, int Capacidad);

/// <summary>Fila de la hoja que todavía no es pasajera de ningún servicio, junto con su programación de transporte.</summary>
public sealed record Pendiente(FilaHoja Fila, ProgramacionDto Programacion);

/// <summary>Identifica una ruta real (jornada + sede + tipo + fecha + hora), sin importar en qué bloque del Excel venía cada fila.</summary>
public sealed record ClaveRuta(DateOnly ClaveJornada, DateOnly FechaServicio, int SedeId, TipoServicio Tipo, TimeOnly Hora);

/// <summary>Pendientes de una misma ruta real, ya reunidos desde todos los bloques del Excel que la traían.</summary>
public sealed record RutaPendiente(Sede Sede, string Titulo, List<Pendiente> Pendientes);

/// <summary>
/// Pendientes que van juntos en un mismo servicio: con la unidad que los lleva (o sin unidad, si no
/// alcanzaron) y, si corresponde, el servicio ya existente que se completa en vez de crear uno nuevo.
/// </summary>
public sealed record DestinoRuta(int? Unidad, List<Pendiente> Filas, ServicioDto? ServicioExistente);

/// <summary>Una zona posible para un barrio: su grupo de reparto (zona o corredor), su macrozona y si es de Villamaría.</summary>
public sealed record ZonaCandidata(string Grupo, string? MacroZona, bool EsVillamaria);
