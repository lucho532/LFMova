import type { Notificacion } from '../modelos/notificacion'
import { solicitarApi } from './clienteHttp'

/** Consume GET /api/usuarios/{usuarioId}/notificaciones (más recientes primero). */
export function obtenerNotificaciones(usuarioId: number, token: string): Promise<Notificacion[]> {
  return solicitarApi<Notificacion[]>(`/api/usuarios/${usuarioId}/notificaciones`, { token })
}

/** Consume POST /api/usuarios/{usuarioId}/notificaciones/{notificacionId}/marcar-leida. */
export function marcarNotificacionLeida(usuarioId: number, notificacionId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/usuarios/${usuarioId}/notificaciones/${notificacionId}/marcar-leida`, { metodo: 'POST', token })
}
