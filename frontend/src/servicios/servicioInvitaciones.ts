import type { DatosInvitacion, DetalleInvitacion, InvitacionEmpresa } from '../modelos/invitacion'
import { solicitarApi } from './clienteHttp'

/** Consume POST /api/empresas/{empresaId}/invitaciones: el coordinador invita una cédula por correo. */
export function invitarPersona(empresaId: number, datos: DatosInvitacion, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/invitaciones`, { metodo: 'POST', cuerpo: datos, token })
}

/** Consume GET /api/empresas/{empresaId}/invitaciones: invitaciones enviadas por la empresa. */
export function obtenerInvitaciones(empresaId: number, token: string): Promise<InvitacionEmpresa[]> {
  return solicitarApi<InvitacionEmpresa[]>(`/api/empresas/${empresaId}/invitaciones`, { token })
}

/** Consume GET /api/invitaciones/detalle?token=: detalle para quien abre el enlace del correo (sin sesión). */
export function obtenerDetalleInvitacion(tokenInvitacion: string): Promise<DetalleInvitacion> {
  return solicitarApi<DetalleInvitacion>(`/api/invitaciones/detalle?token=${encodeURIComponent(tokenInvitacion)}`)
}

/** Consume POST /api/invitaciones/aceptar: la persona con sesión iniciada acepta la invitación. */
export function aceptarInvitacion(tokenInvitacion: string, token: string): Promise<void> {
  return solicitarApi<void>('/api/invitaciones/aceptar', { metodo: 'POST', cuerpo: { token: tokenInvitacion }, token })
}

/** Consume DELETE /api/empresas/{empresaId}/invitaciones/{invitacionEmpresaId}: quita una invitación de la lista. */
export function eliminarInvitacion(empresaId: number, invitacionEmpresaId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/invitaciones/${invitacionEmpresaId}`, { metodo: 'DELETE', token })
}
