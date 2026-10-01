/** Refleja CuentaDto (LFMova.Application). */
export interface Cuenta {
  cedula: string
  nombreCompleto: string
  email: string | null
  telefono: string
  /** Nombres de las empresas a las que pertenece (vacío para el administrador de plataforma). */
  empresas: string[]
}

/** Refleja CambiarContrasenaDto (LFMova.Application). */
export interface CambiarContrasenaDatos {
  contrasenaActual: string
  nuevaContrasena: string
  confirmacionContrasena: string
}
