import type { InformeAsistenciaPeriodo } from '../modelos/informeAsistencia'
import { solicitarApi } from './clienteHttp'
import type { FiltroAsistencia } from './ordenRuta'

const urlBaseApi = import.meta.env.VITE_API_URL as string

/**
 * Consume GET /api/empresas/{empresaId}/informe-asistencia: un único Excel con los pasajeros que cumplen el
 * filtro en todas las rutas de la empresa entre las dos fechas (formato yyyy-MM-dd, ambas incluidas), con su
 * ruta, conductor y horarios. Devuelve el archivo y el nombre que le puso el servidor; requiere el token en
 * la cabecera, así que no sirve como enlace directo.
 */
export async function descargarInformeAsistenciaPeriodo(
  empresaId: number,
  desde: string,
  hasta: string,
  filtro: FiltroAsistencia,
  token: string,
): Promise<{ nombre: string; archivo: Blob }> {
  const parametros = new URLSearchParams({ desde, hasta, filtro })
  const respuesta = await fetch(`${urlBaseApi}/api/empresas/${empresaId}/informe-asistencia?${parametros}`, {
    headers: { Authorization: `Bearer ${token}` },
  })
  if (!respuesta.ok) {
    throw new Error('No se pudo descargar el informe.')
  }
  const nombre = /filename="?([^";]+)"?/.exec(respuesta.headers.get('Content-Disposition') ?? '')?.[1]
  return { nombre: nombre ?? 'informe-asistencia.xlsx', archivo: await respuesta.blob() }
}

/**
 * Consume GET /api/empresas/{empresaId}/informe-asistencia/vista-previa: los pasajeros que no asistieron a las
 * rutas de la empresa entre las dos fechas (yyyy-MM-dd, ambas incluidas), con su ruta, conductor y horarios.
 * Son los mismos datos que lleva el Excel, para mostrarlos antes de descargarlo.
 */
export function obtenerVistaPreviaInasistencias(empresaId: number, desde: string, hasta: string, token: string): Promise<InformeAsistenciaPeriodo> {
  const parametros = new URLSearchParams({ desde, hasta, filtro: 'NO_ASISTIERON' })
  return solicitarApi<InformeAsistenciaPeriodo>(`/api/empresas/${empresaId}/informe-asistencia/vista-previa?${parametros}`, { token })
}
