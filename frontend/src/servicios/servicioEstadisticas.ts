import type { EstadisticaConductor, PasajeroEmpresa, ResumenEmpresa, RutaConductor, RutaEmpresa } from '../modelos/estadisticas'
import { solicitarApi } from './clienteHttp'

function rango(desde: string, hasta: string): string {
  const parametros = new URLSearchParams()
  if (desde) parametros.set('desde', desde)
  if (hasta) parametros.set('hasta', hasta)
  const texto = parametros.toString()
  return texto ? `?${texto}` : ''
}

/** Consume GET /api/empresas/{empresaId}/estadisticas/resumen. */
export function obtenerResumenEmpresa(empresaId: number, token: string): Promise<ResumenEmpresa> {
  return solicitarApi<ResumenEmpresa>(`/api/empresas/${empresaId}/estadisticas/resumen`, { token })
}

/** Consume GET /api/empresas/{empresaId}/estadisticas/conductores (rango de fechas opcional). */
export function obtenerEstadisticasConductores(empresaId: number, desde: string, hasta: string, token: string): Promise<EstadisticaConductor[]> {
  return solicitarApi<EstadisticaConductor[]>(`/api/empresas/${empresaId}/estadisticas/conductores${rango(desde, hasta)}`, { token })
}

/** Consume GET /api/empresas/{empresaId}/estadisticas/conductores/{conductorId}/rutas (rango opcional). */
export function obtenerRutasDeConductor(empresaId: number, conductorId: number, desde: string, hasta: string, token: string): Promise<RutaConductor[]> {
  return solicitarApi<RutaConductor[]>(`/api/empresas/${empresaId}/estadisticas/conductores/${conductorId}/rutas${rango(desde, hasta)}`, { token })
}

/** Consume GET /api/empresas/{empresaId}/estadisticas/rutas. `filtro`: programadas, realizadas, por-realizar, canceladas o vacío. */
export function obtenerRutasEmpresa(empresaId: number, filtro: string, desde: string, hasta: string, token: string): Promise<RutaEmpresa[]> {
  const parametros = new URLSearchParams()
  if (filtro) parametros.set('filtro', filtro)
  if (desde) parametros.set('desde', desde)
  if (hasta) parametros.set('hasta', hasta)
  const texto = parametros.toString()
  return solicitarApi<RutaEmpresa[]>(`/api/empresas/${empresaId}/estadisticas/rutas${texto ? `?${texto}` : ''}`, { token })
}

/** Consume GET /api/empresas/{empresaId}/estadisticas/pasajeros. */
export function obtenerPasajerosEmpresa(empresaId: number, soloTransportados: boolean, desde: string, hasta: string, token: string): Promise<PasajeroEmpresa[]> {
  const parametros = new URLSearchParams()
  if (soloTransportados) parametros.set('soloTransportados', 'true')
  if (desde) parametros.set('desde', desde)
  if (hasta) parametros.set('hasta', hasta)
  const texto = parametros.toString()
  return solicitarApi<PasajeroEmpresa[]>(`/api/empresas/${empresaId}/estadisticas/pasajeros${texto ? `?${texto}` : ''}`, { token })
}
