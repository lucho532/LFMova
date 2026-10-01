/** Refleja BarreraGeograficaDto (TransportApp.Application). */
export interface BarreraGeografica {
  barreraGeograficaId: number
  empresaId: number
  barrioA: string
  barrioB: string
  motivo: string | null
}

/** Refleja CrearBarreraGeograficaDto (TransportApp.Application). */
export interface DatosBarreraGeografica {
  barrioA: string
  barrioB: string
  motivo?: string
}
