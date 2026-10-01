/** Refleja CuentaDto (TransportApp.Application). */
export interface Cuenta {
  cedula: string
  nombreCompleto: string
  email: string | null
  telefono: string
  /** Nombres de las empresas a las que pertenece (vacío para el administrador de plataforma). */
  empresas: string[]
}

/** Refleja CambiarContrasenaDto (TransportApp.Application). */
export interface CambiarContrasenaDatos {
  contrasenaActual: string
  nuevaContrasena: string
  confirmacionContrasena: string
}
