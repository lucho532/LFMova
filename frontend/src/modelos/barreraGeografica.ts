/** Refleja BarreraGeograficaDto (LFMova.Application). */
export interface BarreraGeografica {
  barreraGeograficaId: number
  empresaId: number
  barrioA: string
  barrioB: string
  motivo: string | null
}

/** Refleja CrearBarreraGeograficaDto (LFMova.Application). */
export interface DatosBarreraGeografica {
  barrioA: string
  barrioB: string
  motivo?: string
}
