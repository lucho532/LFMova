/*
 * La API serializa las enumeraciones como enteros (ver el contrato OpenAPI).
 * Estos valores reflejan el orden de LFMova.Domain.Enums; los nombres
 * que se muestran al usuario salen de estas tablas, nunca se inventan estados.
 */

export const TipoServicio = { ENTRADA: 0, SALIDA: 1 } as const

export const NOMBRES_TIPO_SERVICIO: Record<number, string> = { 0: 'Entrada', 1: 'Salida' }

export const EstadoServicio = {
  BORRADOR: 0,
  PENDIENTE_ASIGNACION: 1,
  ASIGNADO: 2,
  PUBLICADO: 3,
  EN_CURSO: 4,
  FINALIZADO: 5,
  CANCELADO: 6,
} as const

export const EstadoPasajero = {
  PROGRAMADO: 0,
  CONFIRMADO: 1,
  NO_ASISTIRA: 2,
  CONDUCTOR_LLEGO: 3,
  RECOGIDO: 4,
  NO_RECOGIDO: 5,
  CANCELADO: 6,
} as const

export const NOMBRES_ESTADO_SERVICIO: Record<number, string> = {
  0: 'Borrador',
  1: 'Pendiente de asignación',
  2: 'Asignado',
  3: 'Publicado',
  4: 'En curso',
  5: 'Finalizado',
  6: 'Cancelado',
}

export const NOMBRES_ESTADO_PASAJERO: Record<number, string> = {
  0: 'Programado',
  1: 'Confirmado',
  2: 'No asistirá',
  3: 'Conductor llegó',
  4: 'Recogido',
  5: 'No recogido',
  6: 'Cancelado',
}

/** Devuelve el nombre legible de un valor de enumeración, o el número si no se conoce. */
export function nombreDe(tabla: Record<number, string>, valor: number): string {
  return tabla[valor] ?? String(valor)
}

/** "07:30:00" -> "07:30". */
export function formatearHora(hora: string): string {
  return hora.slice(0, 5)
}
