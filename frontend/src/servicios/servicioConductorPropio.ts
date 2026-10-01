import type { UnidadDeTrabajo } from '../modelos/conductor'
import type { Mensaje } from '../modelos/mensaje'
import type { Servicio } from '../modelos/operacion'
import { ErrorApi, solicitarApi } from './clienteHttp'

const urlBaseApi = import.meta.env.VITE_API_URL as string

const rutaServicio = (s: { empresaId: number; jornadaId: number; servicioId: number }) =>
  `/api/empresas/${s.empresaId}/jornadas/${s.jornadaId}/servicios/${s.servicioId}`

/** Consume GET /api/conductores/mis-servicios: los servicios asignados al conductor autenticado. */
export function obtenerMisServiciosConductor(token: string): Promise<Servicio[]> {
  return solicitarApi<Servicio[]>('/api/conductores/mis-servicios', { token })
}

/**
 * Consume POST .../servicios/{id}/iniciar. Solo el conductor asignado puede hacerlo.
 * `latitud`/`longitud` son la ubicación real del conductor en ese momento (opcional: si el dispositivo
 * no la entrega, el servicio se inicia igual).
 */
export function iniciarServicio(
  servicio: { empresaId: number; jornadaId: number; servicioId: number },
  token: string,
  latitud: number | null = null,
  longitud: number | null = null,
): Promise<void> {
  return solicitarApi<void>(`${rutaServicio(servicio)}/iniciar`, { metodo: 'POST', cuerpo: { latitud, longitud }, token })
}

/**
 * Consume POST .../servicios/{id}/finalizar. El backend rechaza la
 * finalización si hay pasajeros pendientes. `latitud`/`longitud` son la
 * ubicación real del conductor en ese momento (opcional: si el dispositivo no
 * la entrega, el servicio se finaliza igual).
 */
export function finalizarServicio(
  servicio: { empresaId: number; jornadaId: number; servicioId: number },
  token: string,
  latitud: number | null = null,
  longitud: number | null = null,
): Promise<void> {
  return solicitarApi<void>(`${rutaServicio(servicio)}/finalizar`, { metodo: 'POST', cuerpo: { latitud, longitud }, token })
}

/** Consume POST .../pasajeros/{id}/marcar-llegada. */
export function marcarLlegada(servicio: { empresaId: number; jornadaId: number; servicioId: number }, servicioPasajeroId: number, token: string): Promise<void> {
  return solicitarApi<void>(`${rutaServicio(servicio)}/pasajeros/${servicioPasajeroId}/marcar-llegada`, { metodo: 'POST', token })
}

/** Consume PUT .../pasajeros/{id}/estado (resultado de la recogida o avance dentro del vehículo). */
export function cambiarEstadoPasajero(servicio: { empresaId: number; jornadaId: number; servicioId: number }, servicioPasajeroId: number, nuevoEstado: number, token: string): Promise<void> {
  return solicitarApi<void>(`${rutaServicio(servicio)}/pasajeros/${servicioPasajeroId}/estado`, { metodo: 'PUT', cuerpo: { nuevoEstado }, token })
}

/** Consume GET .../pasajeros/{id}/mensajes (conversación individual). */
export function obtenerMensajes(servicio: { empresaId: number; jornadaId: number; servicioId: number }, servicioPasajeroId: number, token: string): Promise<Mensaje[]> {
  return solicitarApi<Mensaje[]>(`${rutaServicio(servicio)}/pasajeros/${servicioPasajeroId}/mensajes`, { token })
}

/** Consume POST .../pasajeros/{id}/mensajes. */
export function enviarMensaje(servicio: { empresaId: number; jornadaId: number; servicioId: number }, servicioPasajeroId: number, contenido: string, token: string): Promise<Mensaje> {
  return solicitarApi<Mensaje>(`${rutaServicio(servicio)}/pasajeros/${servicioPasajeroId}/mensajes`, { metodo: 'POST', cuerpo: { contenido }, token })
}

/** Consume GET /api/conductores/mis-unidades: las unidades de trabajo del conductor con los datos de su vehículo. */
export function obtenerMisUnidades(token: string): Promise<UnidadDeTrabajo[]> {
  return solicitarApi<UnidadDeTrabajo[]>('/api/conductores/mis-unidades', { token })
}

/** Ubicación de recogida usada antes por el empleado (refleja UbicacionAnteriorDto). */
export interface UbicacionAnterior {
  direccion: string
  barrio: string
  latitud: number | null
  longitud: number | null
  fechaRegistro: string
}

/** Incidencia de un pasajero (refleja IncidenciaDto, con lo necesario para adjuntar fotos). */
export interface IncidenciaPasajero {
  incidenciaId: number
  tipo: number
  descripcion: string
  fechaHora: string
  latitud: number | null
  longitud: number | null
  evidencias: { evidenciaId: number; referenciaArchivo: string }[]
}

type ReferenciaServicio = { empresaId: number; jornadaId: number; servicioId: number }

/** Consume PUT .../pasajeros/{id}/ubicacion-recogida: guarda el punto GPS exacto de la recogida. */
export function guardarUbicacionRecogida(servicio: ReferenciaServicio, servicioPasajeroId: number, latitud: number, longitud: number, token: string): Promise<void> {
  return solicitarApi<void>(`${rutaServicio(servicio)}/pasajeros/${servicioPasajeroId}/ubicacion-recogida`, { metodo: 'PUT', cuerpo: { latitud, longitud }, token })
}

/** Consume GET .../pasajeros/{id}/ubicaciones-anteriores. */
export function obtenerUbicacionesAnteriores(servicio: ReferenciaServicio, servicioPasajeroId: number, token: string): Promise<UbicacionAnterior[]> {
  return solicitarApi<UbicacionAnterior[]>(`${rutaServicio(servicio)}/pasajeros/${servicioPasajeroId}/ubicaciones-anteriores`, { token })
}

/** Consume DELETE .../pasajeros/{id}/ubicaciones-anteriores: olvida la ubicación guardada del empleado. */
export function eliminarUbicacionGuardada(servicio: ReferenciaServicio, servicioPasajeroId: number, token: string): Promise<void> {
  return solicitarApi<void>(`${rutaServicio(servicio)}/pasajeros/${servicioPasajeroId}/ubicaciones-anteriores`, { metodo: 'DELETE', token })
}

/** Consume POST .../pasajeros/{id}/incidencias. */
export function crearIncidencia(
  servicio: ReferenciaServicio,
  servicioPasajeroId: number,
  datos: { tipo: number; descripcion: string; latitud: number | null; longitud: number | null },
  token: string,
): Promise<IncidenciaPasajero> {
  return solicitarApi<IncidenciaPasajero>(`${rutaServicio(servicio)}/pasajeros/${servicioPasajeroId}/incidencias`, { metodo: 'POST', cuerpo: datos, token })
}

/** Consume POST .../incidencias/{incidenciaId}/evidencias/foto (multipart). */
export function subirFotoIncidencia(servicio: ReferenciaServicio, servicioPasajeroId: number, incidenciaId: number, foto: File, token: string): Promise<unknown> {
  const datos = new FormData()
  datos.append('archivo', foto)
  return solicitarApi<unknown>(`${rutaServicio(servicio)}/pasajeros/${servicioPasajeroId}/incidencias/${incidenciaId}/evidencias/foto`, { metodo: 'POST', cuerpo: datos, token })
}

/** Consume GET .../pasajeros/{id}/incidencias: las incidencias ya reportadas para este pasajero, con sus evidencias. */
export function obtenerIncidencias(servicio: ReferenciaServicio, servicioPasajeroId: number, token: string): Promise<IncidenciaPasajero[]> {
  return solicitarApi<IncidenciaPasajero[]>(`${rutaServicio(servicio)}/pasajeros/${servicioPasajeroId}/incidencias`, { token })
}

/**
 * Consume GET .../incidencias/{incidenciaId}/evidencias/{evidenciaId}/archivo: descarga la fotografía de
 * una evidencia (requiere el token en la cabecera, así que no sirve como URL directa de una etiqueta
 * <img>) y devuelve una URL local (`URL.createObjectURL`) para mostrarla. Quien la use debe liberarla
 * con `URL.revokeObjectURL` cuando ya no la necesite.
 */
export async function obtenerUrlFotoEvidencia(
  servicio: ReferenciaServicio, servicioPasajeroId: number, incidenciaId: number, evidenciaId: number, token: string,
): Promise<string> {
  const respuesta = await fetch(
    `${urlBaseApi}${rutaServicio(servicio)}/pasajeros/${servicioPasajeroId}/incidencias/${incidenciaId}/evidencias/${evidenciaId}/archivo`,
    { headers: { Authorization: `Bearer ${token}` } },
  )
  if (!respuesta.ok) {
    throw new ErrorApi(respuesta.status, 'No se pudo cargar la foto.')
  }
  const datos = await respuesta.blob()
  return URL.createObjectURL(datos)
}

/** Consume PUT .../pasajeros/{id}/orden: el conductor cambia el orden de recogida. */
export function reordenarPasajeroConductor(servicio: ReferenciaServicio, servicioPasajeroId: number, nuevoOrden: number, token: string): Promise<void> {
  return solicitarApi<void>(`${rutaServicio(servicio)}/pasajeros/${servicioPasajeroId}/orden`, { metodo: 'PUT', cuerpo: { nuevoOrden }, token })
}
