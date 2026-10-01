/** Refleja CorredorVialDto (TransportApp.Application). */
export interface CorredorVial {
  corredorVialId: number
  empresaId: number
  nombre: string
  activo: boolean
}

/** Refleja CrearCorredorVialDto / ActualizarCorredorVialDto (TransportApp.Application). */
export interface DatosCorredorVial {
  nombre: string
}
