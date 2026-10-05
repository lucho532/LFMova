import type { AsignarCoordinadorDatos, Coordinador, CrearEmpresaDatos, Empresa } from '../modelos/empresa'
import { solicitarApi } from './clienteHttp'

/** Consume GET /api/empresas. Requiere rol ADMINISTRADOR_PLATAFORMA. */
export function obtenerEmpresas(token: string): Promise<Empresa[]> {
  return solicitarApi<Empresa[]>('/api/empresas', { token })
}

/** Consume GET /api/empresas/{empresaId}. */
export function obtenerEmpresa(empresaId: number, token: string): Promise<Empresa> {
  return solicitarApi<Empresa>(`/api/empresas/${empresaId}`, { token })
}

/** Consume POST /api/empresas (crea la empresa junto con su primer coordinador). Requiere rol ADMINISTRADOR_PLATAFORMA. */
export function crearEmpresa(datos: CrearEmpresaDatos, token: string): Promise<Empresa> {
  return solicitarApi<Empresa>('/api/empresas', { metodo: 'POST', cuerpo: datos, token })
}

/** Consume POST /api/empresas/{empresaId}/activar. Requiere rol ADMINISTRADOR_PLATAFORMA. */
export function activarEmpresa(empresaId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/activar`, { metodo: 'POST', token })
}

/** Consume POST /api/empresas/{empresaId}/desactivar. Requiere rol ADMINISTRADOR_PLATAFORMA. */
export function desactivarEmpresa(empresaId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/desactivar`, { metodo: 'POST', token })
}

/** Consume GET /api/empresas/{empresaId}/coordinadores. */
export function obtenerCoordinadores(empresaId: number, token: string): Promise<Coordinador[]> {
  return solicitarApi<Coordinador[]>(`/api/empresas/${empresaId}/coordinadores`, { token })
}

/** Consume DELETE /api/empresas/{empresaId}/coordinadores/{usuarioRolId} (desactiva el rol). */
export function desactivarCoordinador(empresaId: number, usuarioRolId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/coordinadores/${usuarioRolId}`, { metodo: 'DELETE', token })
}

/** Consume POST /api/empresas/{empresaId}/coordinadores/{usuarioRolId}/reactivar. */
export function reactivarCoordinador(empresaId: number, usuarioRolId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/coordinadores/${usuarioRolId}/reactivar`, { metodo: 'POST', token })
}

/** Consume POST /api/empresas/{empresaId}/coordinadores. Asigna un coordinador; si la persona no tiene cuenta, se crea y se le invita por correo. */
export function asignarCoordinador(empresaId: number, datos: AsignarCoordinadorDatos, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/coordinadores`, { metodo: 'POST', cuerpo: datos, token })
}

/** Consume PUT /api/empresas/{empresaId}: el administrador corrige el nombre, el CIF y la dirección. */
export function actualizarEmpresa(empresaId: number, datos: { nombre: string; cif: string; direccion: string }, token: string): Promise<Empresa> {
  return solicitarApi<Empresa>(`/api/empresas/${empresaId}`, { metodo: 'PUT', cuerpo: datos, token })
}

/** Consume DELETE /api/empresas/{empresaId}: el administrador elimina la empresa con todo su historial. */
export function eliminarEmpresa(empresaId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}`, { metodo: 'DELETE', token })
}
