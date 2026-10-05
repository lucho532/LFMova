import type { DetalleFacturacion, ResumenFacturacionEmpresa } from '../modelos/facturacion'
import { solicitarApi } from './clienteHttp'

const urlBaseApi = import.meta.env.VITE_API_URL as string

const consulta = (anio: number, mes: number) => `anio=${anio}&mes=${mes}`

/** Consume GET /api/facturacion: uso de todas las empresas en el mes (solo administrador de plataforma). */
export function obtenerResumenFacturacion(anio: number, mes: number, token: string): Promise<ResumenFacturacionEmpresa[]> {
  return solicitarApi<ResumenFacturacionEmpresa[]>(`/api/facturacion?${consulta(anio, mes)}`, { token })
}

/** Consume GET /api/facturacion/empresas/{empresaId}: conductores que finalizaron rutas en el mes. */
export function obtenerDetalleFacturacion(empresaId: number, anio: number, mes: number, token: string): Promise<DetalleFacturacion> {
  return solicitarApi<DetalleFacturacion>(`/api/facturacion/empresas/${empresaId}?${consulta(anio, mes)}`, { token })
}

/** Consume POST /api/facturacion/empresas/{empresaId}/cierres: deja fijos los totales del mes (solo un mes ya terminado). */
export function cerrarMesFacturacion(empresaId: number, anio: number, mes: number, token: string): Promise<DetalleFacturacion> {
  return solicitarApi<DetalleFacturacion>(`/api/facturacion/empresas/${empresaId}/cierres?${consulta(anio, mes)}`, { metodo: 'POST', token })
}

/**
 * Consume GET /api/facturacion/empresas/{empresaId}/excel: el soporte del mes en Excel. Devuelve el
 * archivo y el nombre que le puso el servidor; requiere el token, así que no sirve como enlace directo.
 */
export async function descargarExcelFacturacion(empresaId: number, anio: number, mes: number, token: string): Promise<{ nombre: string; archivo: Blob }> {
  const respuesta = await fetch(`${urlBaseApi}/api/facturacion/empresas/${empresaId}/excel?${consulta(anio, mes)}`, {
    headers: { Authorization: `Bearer ${token}` },
  })
  if (!respuesta.ok) {
    throw new Error('No se pudo descargar el soporte de facturación.')
  }
  const nombre = /filename="?([^";]+)"?/.exec(respuesta.headers.get('Content-Disposition') ?? '')?.[1]
  return { nombre: nombre ?? `facturacion-${anio}-${mes}.xlsx`, archivo: await respuesta.blob() }
}
