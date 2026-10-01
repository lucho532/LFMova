import type { Mensaje } from '../modelos/mensaje'
import type { ServicioDelEmpleado } from '../modelos/servicioEmpleado'
import { solicitarApi } from './clienteHttp'

/** Consume GET /api/empleados/mis-servicios: los servicios del empleado autenticado. */
export function obtenerMisServicios(token: string): Promise<ServicioDelEmpleado[]> {
  return solicitarApi<ServicioDelEmpleado[]>('/api/empleados/mis-servicios', { token })
}

const rutaPasajero = (s: { empresaId: number; jornadaId: number; servicioId: number; servicioPasajeroId: number }) =>
  `/api/empresas/${s.empresaId}/jornadas/${s.jornadaId}/servicios/${s.servicioId}/pasajeros/${s.servicioPasajeroId}`

/** Datos opcionales al confirmar asistencia (refleja ConfirmarServicioPasajeroDto). */
export interface DatosConfirmacion {
  nuevaDireccion?: string
  nuevoBarrio?: string
  latitud?: number
  longitud?: number
  establecerComoHabitual?: boolean
}

/** Consume POST .../pasajeros/{id}/confirmar: el empleado confirma que asistirá (opcionalmente con otra dirección). */
export function confirmarAsistencia(servicio: { empresaId: number; jornadaId: number; servicioId: number; servicioPasajeroId: number }, datos: DatosConfirmacion, token: string): Promise<void> {
  return solicitarApi<void>(`${rutaPasajero(servicio)}/confirmar`, { metodo: 'POST', cuerpo: datos, token })
}

/** Consume POST .../pasajeros/{id}/no-asistira. */
export function marcarNoAsistire(servicio: { empresaId: number; jornadaId: number; servicioId: number; servicioPasajeroId: number }, token: string): Promise<void> {
  return solicitarApi<void>(`${rutaPasajero(servicio)}/no-asistira`, { metodo: 'POST', token })
}

/** Consume PUT .../pasajeros/{id}/ubicacion: el empleado comparte su ubicación actual con el conductor. */
export function compartirMiUbicacion(servicio: { empresaId: number; jornadaId: number; servicioId: number; servicioPasajeroId: number }, latitud: number, longitud: number, token: string): Promise<void> {
  return solicitarApi<void>(`${rutaPasajero(servicio)}/ubicacion`, { metodo: 'PUT', cuerpo: { latitud, longitud }, token })
}

/** Consume GET .../pasajeros/{id}/mensajes. */
export function obtenerMensajesConConductor(servicio: { empresaId: number; jornadaId: number; servicioId: number; servicioPasajeroId: number }, token: string): Promise<Mensaje[]> {
  return solicitarApi<Mensaje[]>(`${rutaPasajero(servicio)}/mensajes`, { token })
}

/** Consume POST .../pasajeros/{id}/mensajes. */
export function enviarMensajeAlConductor(servicio: { empresaId: number; jornadaId: number; servicioId: number; servicioPasajeroId: number }, contenido: string, token: string): Promise<Mensaje> {
  return solicitarApi<Mensaje>(`${rutaPasajero(servicio)}/mensajes`, { metodo: 'POST', cuerpo: { contenido }, token })
}
