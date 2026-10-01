/** Refleja PersonaDto (LFMova.Application). */
export interface Persona {
  cedula: string
  nombreCompleto: string
  email: string | null
  telefono: string
  esConductor: boolean
  esCoordinador: boolean
  empresaCoordinada: EmpresaResumen | null
  empresasConductor: EmpresaResumen[]
  placas: string[]
  empresaEmpleado: EmpresaResumen | null
}

/** Refleja EmpresaResumenDto (LFMova.Application). */
export interface EmpresaResumen {
  empresaId: number
  nombre: string
}
