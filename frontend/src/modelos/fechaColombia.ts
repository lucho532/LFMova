/** Colombia usa UTC−5 todo el año (sin horario de verano). */
const DESFASE_COLOMBIA_MS = -5 * 60 * 60 * 1000

/**
 * Fecha de hoy en Colombia (AAAA-MM-DD). Las fechas de las rutas son de
 * Colombia (ver AGENTS.md §41): usar la fecha UTC haría que, desde las
 * 7:00 p. m., "hoy" ya fuera mañana. No depende de la zona horaria del
 * dispositivo.
 */
export function hoyColombia(): string {
  return new Date(Date.now() + DESFASE_COLOMBIA_MS).toISOString().slice(0, 10)
}
