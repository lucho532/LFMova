import { EstadoPasajero } from '../modelos/enumeraciones'
import type { ServicioPasajero } from '../modelos/operacion'
import { aMilisegundosUtc } from './tiempoRuta'

/**
 * Qué pasajeros de una ruta se muestran y se incluyen en el informe. Los valores son los nombres del enum
 * FiltroAsistenciaRuta del backend, que la API recibe tal cual.
 */
export type FiltroAsistencia = 'TODOS' | 'ASISTIERON' | 'NO_ASISTIERON'

/**
 * Indica si un pasajero con ese estado entra en el filtro. Debe coincidir con ReglasAsistenciaRuta del
 * backend: asistió quien fue recogido; no asistió quien no se pudo recoger o avisó que no asistiría.
 */
export function cumpleFiltro(estado: number, filtro: FiltroAsistencia): boolean {
  if (filtro === 'ASISTIERON') return estado === EstadoPasajero.RECOGIDO
  if (filtro === 'NO_ASISTIERON') return estado === EstadoPasajero.NO_RECOGIDO || estado === EstadoPasajero.NO_ASISTIRA
  return true
}

/**
 * Orden cronológico de la ruta: primero quienes ya tienen resultado registrado, por la hora en que se
 * registró; después los que no llegaron a procesarse (no asistió, cancelado, pendiente), por su orden de recogida.
 */
export function ordenarCronologicamente(pasajeros: ServicioPasajero[]): ServicioPasajero[] {
  const procesados = pasajeros.filter((p) => p.horaProcesado).sort((a, b) => aMilisegundosUtc(a.horaProcesado!) - aMilisegundosUtc(b.horaProcesado!))
  const sinProcesar = pasajeros.filter((p) => !p.horaProcesado).sort((a, b) => a.orden - b.orden)
  return [...procesados, ...sinProcesar]
}

/** Un pasajero con su número de orden de recogida; `null` si no llegó a procesarse y por tanto no tiene turno. */
export interface PasajeroNumerado {
  pasajero: ServicioPasajero
  numero: number | null
}

/**
 * Pasajeros en orden cronológico con su número de recogida. El número es la posición en la ruta completa y
 * no cambia al filtrar: «No asistieron» puede empezar en el #3.
 */
export function numerarRuta(pasajeros: ServicioPasajero[]): PasajeroNumerado[] {
  let siguiente = 1
  return ordenarCronologicamente(pasajeros).map((pasajero) => ({ pasajero, numero: pasajero.horaProcesado ? siguiente++ : null }))
}
