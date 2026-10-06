import { solicitarApi } from './clienteHttp'

/** Llamada del conductor a un pasajero (refleja RegistroLlamadaDto). */
export interface RegistroLlamada {
  registroLlamadaId: number
  servicioPasajeroId: number
  /** Instante (UTC) en que el conductor pulsó "Llamar". */
  fechaHora: string
  /** Segundos aproximados (incluye el tiempo que timbró); `null` si no se pudo medir. */
  duracionAproximadaSegundos: number | null
  latitud: number | null
  longitud: number | null
}

/** Datos que identifican a un pasajero dentro de un servicio. */
export interface ReferenciaPasajero {
  empresaId: number
  jornadaId: number
  servicioId: number
  servicioPasajeroId: number
}

const rutaLlamadas = (p: ReferenciaPasajero) =>
  `/api/empresas/${p.empresaId}/jornadas/${p.jornadaId}/servicios/${p.servicioId}/pasajeros/${p.servicioPasajeroId}/llamadas`

/** Consume POST .../pasajeros/{id}/llamadas: el conductor deja constancia de que va a llamar al pasajero. */
export function registrarLlamada(pasajero: ReferenciaPasajero, latitud: number | null, longitud: number | null, token: string): Promise<RegistroLlamada> {
  return solicitarApi<RegistroLlamada>(rutaLlamadas(pasajero), { metodo: 'POST', cuerpo: { latitud, longitud }, token })
}

/** Consume PUT .../llamadas/{id}/duracion: guarda cuánto estuvo el conductor fuera de la app tras pulsar "Llamar". */
export function registrarDuracionLlamada(pasajero: ReferenciaPasajero, registroLlamadaId: number, segundos: number, token: string): Promise<RegistroLlamada> {
  return solicitarApi<RegistroLlamada>(`${rutaLlamadas(pasajero)}/${registroLlamadaId}/duracion`, { metodo: 'PUT', cuerpo: { segundos }, token })
}

/** Consume GET .../pasajeros/{id}/llamadas: lo pueden consultar el conductor, el propio pasajero y los coordinadores. */
export function obtenerLlamadas(pasajero: ReferenciaPasajero, token: string): Promise<RegistroLlamada[]> {
  return solicitarApi<RegistroLlamada[]>(rutaLlamadas(pasajero), { token })
}
