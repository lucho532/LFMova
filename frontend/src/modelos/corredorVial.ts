/** Refleja CorredorVialDto (LFMova.Application). */
export interface CorredorVial {
  corredorVialId: number
  empresaId: number
  nombre: string
  activo: boolean
}

/** Refleja CrearCorredorVialDto / ActualizarCorredorVialDto (LFMova.Application). */
export interface DatosCorredorVial {
  nombre: string
}
