import type { BarreraGeografica, DatosBarreraGeografica } from '../modelos/barreraGeografica'
import { solicitarApi } from './clienteHttp'

/** Consume GET /api/empresas/{empresaId}/barreras-geograficas. */
export function obtenerBarrerasGeograficas(empresaId: number, token: string): Promise<BarreraGeografica[]> {
  return solicitarApi<BarreraGeografica[]>(`/api/empresas/${empresaId}/barreras-geograficas`, { token })
}

/** Consume POST /api/empresas/{empresaId}/barreras-geograficas. Requiere rol COORDINADOR de esa empresa. */
export function crearBarreraGeografica(empresaId: number, datos: DatosBarreraGeografica, token: string): Promise<BarreraGeografica> {
  return solicitarApi<BarreraGeografica>(`/api/empresas/${empresaId}/barreras-geograficas`, { metodo: 'POST', cuerpo: datos, token })
}

/** Consume DELETE /api/empresas/{empresaId}/barreras-geograficas/{barreraGeograficaId}. Requiere rol COORDINADOR de esa empresa. */
export function eliminarBarreraGeografica(empresaId: number, barreraGeograficaId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/barreras-geograficas/${barreraGeograficaId}`, { metodo: 'DELETE', token })
}
