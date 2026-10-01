import type { Persona } from '../modelos/persona'
import { solicitarApi } from './clienteHttp'

/** Consume GET /api/personas/buscar?cedula=. Solo administradores y coordinadores; no modifica nada. */
export function buscarPersona(cedula: string, token: string): Promise<Persona> {
  return solicitarApi<Persona>(`/api/personas/buscar?cedula=${encodeURIComponent(cedula.trim())}`, { token })
}
