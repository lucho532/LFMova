const PREFIJO = 'lfmova.ubicacionVista.'

/**
 * Recuerda en este dispositivo qué ubicación compartida por un pasajero ya
 * abrió el conductor, para no seguir avisándole de ella. Se guarda el
 * instante en que se compartió: si el pasajero comparte otra, vuelve a
 * contar como nueva. No se envía al servidor.
 */
export function ubicacionCompartidaVista(servicioPasajeroId: number, fechaHoraCompartida: string | null): boolean {
  try {
    return localStorage.getItem(PREFIJO + servicioPasajeroId) === (fechaHoraCompartida ?? '')
  } catch {
    return false
  }
}

/** Marca como vista la ubicación compartida actual de ese pasajero. */
export function marcarUbicacionCompartidaVista(servicioPasajeroId: number, fechaHoraCompartida: string | null): void {
  try {
    localStorage.setItem(PREFIJO + servicioPasajeroId, fechaHoraCompartida ?? '')
  } catch {
    // Sin almacenamiento local el aviso simplemente se sigue mostrando.
  }
}
