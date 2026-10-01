/** Estado derivado de una invitación: se puede aceptar, ya se aceptó o venció. */
export type EstadoInvitacion = 'PENDIENTE' | 'ACEPTADA' | 'VENCIDA'

/** Refleja InvitacionEmpresaDto (TransportApp.Application): lo que ve el coordinador; nunca incluye el correo de destino. */
export interface InvitacionEmpresa {
  invitacionEmpresaId: number
  cedula: string
  /** Solo se conoce cuando la persona ya aceptó. */
  nombreCompleto: string | null
  estado: EstadoInvitacion
  fechaCreacion: string
  fechaExpiracion: string
  fechaAceptacion: string | null
}

/** Refleja CrearInvitacionEmpresaDto (TransportApp.Application). */
export interface DatosInvitacion {
  cedula: string
  correo: string
}

/** Refleja DetalleInvitacionDto (TransportApp.Application): lo que ve la persona invitada al abrir el enlace. */
export interface DetalleInvitacion {
  nombreEmpresa: string
  nombreInvitador: string
  cedula: string
  correo: string
  requiereRegistro: boolean
  estado: EstadoInvitacion
  fechaExpiracion: string
}
