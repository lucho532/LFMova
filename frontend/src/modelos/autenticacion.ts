/** Refleja IniciarSesionDto (TransportApp.Application). */
export interface IniciarSesionDatos {
  identificador: string
  password: string
}

/** Refleja RespuestaAutenticacionDto (TransportApp.Application). */
export interface RespuestaAutenticacion {
  token: string
  expiraEnUtc: string
}

/** Refleja RegistrarUsuarioDto (TransportApp.Application). */
export interface RegistrarUsuarioDatos {
  correo: string
  nombreCompleto: string
  cedula: string
  telefono: string
  password: string
  /** Token del enlace de invitación a una empresa, si la persona se registra desde ese enlace. */
  tokenInvitacion?: string
}

/** Refleja ResultadoRegistroDto (TransportApp.Application). */
export interface ResultadoRegistro {
  /** `false` cuando se registró desde una invitación con el mismo correo: ya puede iniciar sesión. */
  requiereConfirmarCorreo: boolean
}

/** Refleja RestablecerContrasenaDto (TransportApp.Application). */
export interface RestablecerContrasenaDatos {
  token: string
  nuevaContrasena: string
  confirmacionContrasena: string
}
