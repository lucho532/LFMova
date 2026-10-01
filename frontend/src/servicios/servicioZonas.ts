import type { DatosZona, Zona } from '../modelos/zona'
import { solicitarApi } from './clienteHttp'

/** Consume GET /api/empresas/{empresaId}/zonas. */
export function obtenerZonas(empresaId: number, token: string): Promise<Zona[]> {
  return solicitarApi<Zona[]>(`/api/empresas/${empresaId}/zonas`, { token })
}

/** Consume POST /api/empresas/{empresaId}/zonas. Requiere rol COORDINADOR de esa empresa. */
export function crearZona(empresaId: number, datos: DatosZona, token: string): Promise<Zona> {
  return solicitarApi<Zona>(`/api/empresas/${empresaId}/zonas`, { metodo: 'POST', cuerpo: datos, token })
}

/** Consume PUT /api/empresas/{empresaId}/zonas/{zonaId}. Requiere rol COORDINADOR de esa empresa. */
export function actualizarZona(empresaId: number, zonaId: number, datos: DatosZona, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/zonas/${zonaId}`, { metodo: 'PUT', cuerpo: datos, token })
}

/** Consume POST /api/empresas/{empresaId}/zonas/{zonaId}/barrios: agrega un barrio como alias adicional de una zona ya existente. Requiere rol COORDINADOR de esa empresa. */
export function agregarBarrioAZona(empresaId: number, zonaId: number, barrio: string, token: string): Promise<Zona> {
  return solicitarApi<Zona>(`/api/empresas/${empresaId}/zonas/${zonaId}/barrios`, { metodo: 'POST', cuerpo: { barrio }, token })
}

/** Consume POST /api/empresas/{empresaId}/zonas/{zonaId}/activar. Requiere rol COORDINADOR de esa empresa. */
export function activarZona(empresaId: number, zonaId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/zonas/${zonaId}/activar`, { metodo: 'POST', token })
}

/** Consume POST /api/empresas/{empresaId}/zonas/{zonaId}/desactivar. Requiere rol COORDINADOR de esa empresa. */
export function desactivarZona(empresaId: number, zonaId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/zonas/${zonaId}/desactivar`, { metodo: 'POST', token })
}

/**
 * Consume POST /api/empresas/{empresaId}/zonas/{zonaId}/mover-barrio: pasa un barrio de esa zona a otra.
 * Si la zona de origen se queda sin barrios, el backend la elimina.
 */
export function moverBarrioDeZona(empresaId: number, zonaId: number, barrio: string, zonaDestinoId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/zonas/${zonaId}/mover-barrio`, { metodo: 'POST', cuerpo: { barrio, zonaDestinoId }, token })
}

/** Consume POST /api/empresas/{empresaId}/zonas/{zonaId}/unir: la zona de destino recibe todos los barrios y esa zona se elimina. */
export function unirZonas(empresaId: number, zonaId: number, zonaDestinoId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/zonas/${zonaId}/unir`, { metodo: 'POST', cuerpo: { zonaDestinoId }, token })
}

/** Consume DELETE /api/empresas/{empresaId}/zonas/{zonaId}: elimina la zona; sus barrios quedan sin zona. */
export function eliminarZona(empresaId: number, zonaId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/zonas/${zonaId}`, { metodo: 'DELETE', token })
}
