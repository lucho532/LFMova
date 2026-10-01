import type { DatosSede, Sede } from '../modelos/sede'
import { solicitarApi } from './clienteHttp'

/** Consume GET /api/empresas/{empresaId}/sedes. */
export function obtenerSedes(empresaId: number, token: string): Promise<Sede[]> {
  return solicitarApi<Sede[]>(`/api/empresas/${empresaId}/sedes`, { token })
}

/** Consume GET /api/empresas/{empresaId}/sedes/{sedeId}. */
export function obtenerSede(empresaId: number, sedeId: number, token: string): Promise<Sede> {
  return solicitarApi<Sede>(`/api/empresas/${empresaId}/sedes/${sedeId}`, { token })
}

/** Consume POST /api/empresas/{empresaId}/sedes. Requiere rol COORDINADOR de esa empresa. */
export function crearSede(empresaId: number, datos: DatosSede, token: string): Promise<Sede> {
  return solicitarApi<Sede>(`/api/empresas/${empresaId}/sedes`, { metodo: 'POST', cuerpo: datos, token })
}

/** Consume PUT /api/empresas/{empresaId}/sedes/{sedeId}. Requiere rol COORDINADOR de esa empresa. */
export function actualizarSede(empresaId: number, sedeId: number, datos: DatosSede, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/sedes/${sedeId}`, { metodo: 'PUT', cuerpo: datos, token })
}

/** Consume POST /api/empresas/{empresaId}/sedes/{sedeId}/activar. Requiere rol COORDINADOR de esa empresa. */
export function activarSede(empresaId: number, sedeId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/sedes/${sedeId}/activar`, { metodo: 'POST', token })
}

/** Consume POST /api/empresas/{empresaId}/sedes/{sedeId}/desactivar. Requiere rol COORDINADOR de esa empresa. */
export function desactivarSede(empresaId: number, sedeId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/sedes/${sedeId}/desactivar`, { metodo: 'POST', token })
}
