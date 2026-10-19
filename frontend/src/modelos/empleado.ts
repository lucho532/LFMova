/** Refleja EmpleadoDto (LFMova.Application). */
export interface Empleado {
  empleadoId: number
  usuarioId: number
  empresaId: number
  cedula: string
  /** Correo de su cuenta, o `null` si todavía no la ha activado. */
  email: string | null
  nombreCompleto: string
  telefono: string
  direccion: string
  barrio: string
  activo: boolean
}

/** Refleja ActualizarEmpleadoDto (LFMova.Application): datos actuales que un coordinador puede cambiar (no incluye cédula ni empresa). */
export interface DatosActualizarEmpleado {
  nombreCompleto: string
  telefono: string
  direccion: string
  barrio: string
}
