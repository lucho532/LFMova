import type {
  DatosProgramacion,
  DeshacerReparto,
  EliminarRastro,
  Jornada,
  Programacion,
  PropuestaPlanificacion,
  Servicio,
  ServicioPasajero,
} from '../modelos/operacion'
import { solicitarApi } from './clienteHttp'

const base = (empresaId: number) => `/api/empresas/${empresaId}`
const rutaServicio = (empresaId: number, jornadaId: number, servicioId: number) =>
  `${base(empresaId)}/jornadas/${jornadaId}/servicios/${servicioId}`

// --- Programaciones ---

/** Consume GET /api/empresas/{empresaId}/programaciones. */
export function obtenerProgramaciones(empresaId: number, token: string): Promise<Programacion[]> {
  return solicitarApi<Programacion[]>(`${base(empresaId)}/programaciones`, { token })
}

/** Consume GET /api/empresas/{empresaId}/programaciones/{id}. */
export function obtenerProgramacion(empresaId: number, id: number, token: string): Promise<Programacion> {
  return solicitarApi<Programacion>(`${base(empresaId)}/programaciones/${id}`, { token })
}

/** Consume POST /api/empresas/{empresaId}/programaciones. */
export function crearProgramacion(empresaId: number, datos: DatosProgramacion & { empleadoId: number }, token: string): Promise<Programacion> {
  return solicitarApi<Programacion>(`${base(empresaId)}/programaciones`, { metodo: 'POST', cuerpo: datos, token })
}

/** Consume PUT /api/empresas/{empresaId}/programaciones/{id}. */
export function actualizarProgramacion(empresaId: number, id: number, datos: DatosProgramacion, token: string): Promise<void> {
  return solicitarApi<void>(`${base(empresaId)}/programaciones/${id}`, { metodo: 'PUT', cuerpo: datos, token })
}

// --- Jornadas ---

/** Consume GET /api/empresas/{empresaId}/jornadas. */
export function obtenerJornadas(empresaId: number, token: string): Promise<Jornada[]> {
  return solicitarApi<Jornada[]>(`${base(empresaId)}/jornadas`, { token })
}

/** Consume POST /api/empresas/{empresaId}/jornadas/{jornadaId}/publicar. */
export function publicarJornada(empresaId: number, jornadaId: number, token: string): Promise<void> {
  return solicitarApi<void>(`${base(empresaId)}/jornadas/${jornadaId}/publicar`, { metodo: 'POST', token })
}

/**
 * Consume POST /api/empresas/{empresaId}/jornadas/{jornadaId}/deshacer-reparto: borra los servicios
 * de la jornada que todavía no se publicaron y deja sus pasajeros listos para repartirse de nuevo.
 */
export function deshacerReparto(empresaId: number, jornadaId: number, token: string): Promise<DeshacerReparto> {
  return solicitarApi<DeshacerReparto>(`${base(empresaId)}/jornadas/${jornadaId}/deshacer-reparto`, { metodo: 'POST', token })
}

/**
 * Consume POST /api/empresas/{empresaId}/jornadas/{jornadaId}/eliminar-rastro: además de lo que hace
 * deshacerReparto, borra las programaciones de esa fecha que hayan quedado sin asignar (por ejemplo,
 * de una importación fallida) y, si la jornada queda vacía, la jornada misma. Deja la fecha lista para
 * importar un Excel completamente desde cero.
 */
export function eliminarRastro(empresaId: number, jornadaId: number, token: string): Promise<EliminarRastro> {
  return solicitarApi<EliminarRastro>(`${base(empresaId)}/jornadas/${jornadaId}/eliminar-rastro`, { metodo: 'POST', token })
}

// --- Servicios ---

/**
 * Consume GET .../jornadas/servicios-pendientes?desde=: las rutas que muestra Programación, de todas las
 * jornadas: las que falta enviar (de cualquier fecha) y las publicadas o en curso desde `desde`.
 */
export function obtenerServiciosPendientes(empresaId: number, desde: string, token: string): Promise<Servicio[]> {
  return solicitarApi<Servicio[]>(`${base(empresaId)}/jornadas/servicios-pendientes?desde=${desde}`, { token })
}

/** Consume GET .../jornadas/{jornadaId}/servicios. */
export function obtenerServicios(empresaId: number, jornadaId: number, token: string): Promise<Servicio[]> {
  return solicitarApi<Servicio[]>(`${base(empresaId)}/jornadas/${jornadaId}/servicios`, { token })
}

/** Consume GET .../jornadas/{jornadaId}/servicios/{servicioId}: un servicio concreto, con sus horas y ubicación reales. */
export function obtenerServicio(empresaId: number, jornadaId: number, servicioId: number, token: string): Promise<Servicio> {
  return solicitarApi<Servicio>(rutaServicio(empresaId, jornadaId, servicioId), { token })
}

/** Consume POST .../servicios/{servicioId}/cambiar-estado. */
export function cambiarEstadoServicio(empresaId: number, jornadaId: number, servicioId: number, nuevoEstado: number, token: string): Promise<void> {
  return solicitarApi<void>(`${rutaServicio(empresaId, jornadaId, servicioId)}/cambiar-estado`, { metodo: 'POST', cuerpo: { nuevoEstado }, token })
}

/** Consume POST .../servicios/{servicioId}/asignar-unidad. */
export function asignarUnidad(empresaId: number, jornadaId: number, servicioId: number, unidadOperativaId: number, token: string): Promise<void> {
  return solicitarApi<void>(`${rutaServicio(empresaId, jornadaId, servicioId)}/asignar-unidad`, { metodo: 'POST', cuerpo: { unidadOperativaId }, token })
}

/** Consume POST .../servicios/{servicioId}/retirar-unidad. */
export function retirarUnidad(empresaId: number, jornadaId: number, servicioId: number, token: string): Promise<void> {
  return solicitarApi<void>(`${rutaServicio(empresaId, jornadaId, servicioId)}/retirar-unidad`, { metodo: 'POST', token })
}

/**
 * Consume POST .../servicios/{servicioId}/despublicar: "despublica" una ruta ya enviada (vuelve a
 * Asignado) para poder seguir editándola. Conserva el conductor; si tiene, se le notifica.
 */
export function despublicarServicio(empresaId: number, jornadaId: number, servicioId: number, token: string): Promise<void> {
  return solicitarApi<void>(`${rutaServicio(empresaId, jornadaId, servicioId)}/despublicar`, { metodo: 'POST', token })
}

/**
 * Consume DELETE .../servicios/{servicioId}: elimina por completo la ruta (borrado físico). Falla si
 * está en curso o ya finalizada. Si tenía conductor, se le notifica.
 */
export function eliminarServicio(empresaId: number, jornadaId: number, servicioId: number, token: string): Promise<void> {
  return solicitarApi<void>(rutaServicio(empresaId, jornadaId, servicioId), { metodo: 'DELETE', token })
}

/** Consume GET .../servicios/{servicioId}/propuesta-planificacion: solo una recomendación de lectura. */
export function obtenerPropuestaPlanificacion(empresaId: number, jornadaId: number, servicioId: number, token: string): Promise<PropuestaPlanificacion> {
  return solicitarApi<PropuestaPlanificacion>(`${rutaServicio(empresaId, jornadaId, servicioId)}/propuesta-planificacion`, { token })
}

// --- Pasajeros del servicio ---

/** Consume GET .../servicios/{servicioId}/pasajeros. */
export function obtenerPasajeros(empresaId: number, jornadaId: number, servicioId: number, token: string): Promise<ServicioPasajero[]> {
  return solicitarApi<ServicioPasajero[]>(`${rutaServicio(empresaId, jornadaId, servicioId)}/pasajeros`, { token })
}

/** Consume DELETE .../pasajeros/{servicioPasajeroId}: borra por completo al pasajero de la ruta (para cuando se agregó por error). */
export function eliminarPasajero(empresaId: number, jornadaId: number, servicioId: number, servicioPasajeroId: number, token: string): Promise<void> {
  return solicitarApi<void>(`${rutaServicio(empresaId, jornadaId, servicioId)}/pasajeros/${servicioPasajeroId}`, { metodo: 'DELETE', token })
}

/** Consume PUT .../pasajeros/{servicioPasajeroId}/direccion: corrige la dirección de recogida de este servicio puntual (no toca el estado del pasajero). */
export function editarDireccionPasajero(
  empresaId: number, jornadaId: number, servicioId: number, servicioPasajeroId: number, direccion: string, token: string,
): Promise<void> {
  return solicitarApi<void>(`${rutaServicio(empresaId, jornadaId, servicioId)}/pasajeros/${servicioPasajeroId}/direccion`, {
    metodo: 'PUT',
    cuerpo: { direccion },
    token,
  })
}

/** Consume PUT .../pasajeros/{servicioPasajeroId}/mover: arrastra al pasajero a otra ruta ya existente de la misma sede, fecha, hora y tipo. */
export function moverPasajero(
  empresaId: number, jornadaId: number, servicioId: number, servicioPasajeroId: number, servicioDestinoId: number, token: string,
): Promise<void> {
  return solicitarApi<void>(`${rutaServicio(empresaId, jornadaId, servicioId)}/pasajeros/${servicioPasajeroId}/mover`, {
    metodo: 'PUT',
    cuerpo: { servicioDestinoId },
    token,
  })
}

// --- Incidencias de la ruta (vista del coordinador) ---

/** Incidencia de un pasajero de la ruta, con lo necesario para el resumen del coordinador (refleja IncidenciaDto). */
export interface IncidenciaRuta {
  incidenciaId: number
  servicioPasajeroId: number
  tipo: number
  descripcion: string
  fechaHora: string
  latitud: number | null
  longitud: number | null
  evidencias: { evidenciaId: number; referenciaArchivo: string }[]
}

/** Consume GET .../servicios/{servicioId}/incidencias: las incidencias de todos los pasajeros de la ruta. Solo coordinador. */
export function obtenerIncidenciasRuta(empresaId: number, jornadaId: number, servicioId: number, token: string): Promise<IncidenciaRuta[]> {
  return solicitarApi<IncidenciaRuta[]>(`${rutaServicio(empresaId, jornadaId, servicioId)}/incidencias`, { token })
}

const urlBaseApi = import.meta.env.VITE_API_URL as string

/**
 * Consume GET .../pasajeros/{servicioPasajeroId}/incidencias/{incidenciaId}/evidencias/{evidenciaId}/archivo
 * (requiere el token en la cabecera, así que no sirve como URL directa de una etiqueta <img>) y devuelve
 * una URL local (`URL.createObjectURL`) para mostrarla. Quien la use debe liberarla con
 * `URL.revokeObjectURL` cuando ya no la necesite.
 */
export async function obtenerUrlFotoEvidenciaRuta(
  empresaId: number, jornadaId: number, servicioId: number, servicioPasajeroId: number, incidenciaId: number, evidenciaId: number, token: string,
): Promise<string> {
  const respuesta = await fetch(
    `${urlBaseApi}${rutaServicio(empresaId, jornadaId, servicioId)}/pasajeros/${servicioPasajeroId}/incidencias/${incidenciaId}/evidencias/${evidenciaId}/archivo`,
    { headers: { Authorization: `Bearer ${token}` } },
  )
  if (!respuesta.ok) {
    throw new Error('No se pudo cargar la foto.')
  }
  const datos = await respuesta.blob()
  return URL.createObjectURL(datos)
}
