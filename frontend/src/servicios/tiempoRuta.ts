/**
 * Utilidades para mostrar los instantes reales de una ruta. Llegan en UTC, a veces sin zona explícita,
 * y se muestran en la hora local del dispositivo (AGENTS.md §41). No consultan la API.
 */

/** Milisegundos de un instante ISO en UTC; si no trae zona explícita se completa con `Z`. */
export function aMilisegundosUtc(iso: string): number {
  return new Date(/[zZ]|[+-]\d{2}:?\d{2}$/.test(iso) ? iso : `${iso}Z`).getTime()
}

/** Hora local (HH:mm) de un instante ISO, o `null` si no hay. */
export function horaLocal(iso: string | null): string | null {
  if (!iso) return null
  return new Date(aMilisegundosUtc(iso)).toLocaleTimeString('es', { hour: '2-digit', minute: '2-digit' })
}

/** Duración entre dos instantes ISO, por ejemplo "1 h 13 min". */
export function duracion(inicio: string | null, fin: string | null): string | null {
  if (!inicio || !fin) return null
  const minutos = Math.max(0, Math.round((aMilisegundosUtc(fin) - aMilisegundosUtc(inicio)) / 60000))
  return minutos >= 60 ? `${Math.floor(minutos / 60)} h ${minutos % 60} min` : `${minutos} min`
}

/** Segundos enteros entre dos instantes ISO, o `null` si falta alguno. Nunca negativos. */
export function segundosEntre(inicio: string | null, fin: string | null): number | null {
  if (!inicio || !fin) return null
  return Math.max(0, Math.floor((aMilisegundosUtc(fin) - aMilisegundosUtc(inicio)) / 1000))
}

/** Formatea segundos como cronómetro `mm:ss`. */
export function formatearCronometro(segundos: number): string {
  const minutos = Math.floor(segundos / 60)
  return `${String(minutos).padStart(2, '0')}:${String(segundos % 60).padStart(2, '0')}`
}
