import type { CorredorVial, DatosCorredorVial } from '../modelos/corredorVial'
import { solicitarApi } from './clienteHttp'

/** Consume GET /api/empresas/{empresaId}/corredores-viales. */
export function obtenerCorredoresViales(empresaId: number, token: string): Promise<CorredorVial[]> {
  return solicitarApi<CorredorVial[]>(`/api/empresas/${empresaId}/corredores-viales`, { token })
}

/** Consume POST /api/empresas/{empresaId}/corredores-viales. Requiere rol COORDINADOR de esa empresa. */
export function crearCorredorVial(empresaId: number, datos: DatosCorredorVial, token: string): Promise<CorredorVial> {
  return solicitarApi<CorredorVial>(`/api/empresas/${empresaId}/corredores-viales`, { metodo: 'POST', cuerpo: datos, token })
}

/** Consume POST /api/empresas/{empresaId}/corredores-viales/{corredorVialId}/activar. Requiere rol COORDINADOR de esa empresa. */
export function activarCorredorVial(empresaId: number, corredorVialId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/corredores-viales/${corredorVialId}/activar`, { metodo: 'POST', token })
}

/** Consume POST /api/empresas/{empresaId}/corredores-viales/{corredorVialId}/desactivar. Requiere rol COORDINADOR de esa empresa. */
export function desactivarCorredorVial(empresaId: number, corredorVialId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/corredores-viales/${corredorVialId}/desactivar`, { metodo: 'POST', token })
}
