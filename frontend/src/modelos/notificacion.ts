/** Refleja NotificacionDto (TransportApp.Application). */
export interface Notificacion {
  notificacionId: number
  tipo: string
  titulo: string
  mensaje: string
  /** Ruta de la aplicación a la que lleva al abrirla (por ejemplo, el chat del pasajero). */
  enlace: string | null
  leida: boolean
  fechaHora: string
}
