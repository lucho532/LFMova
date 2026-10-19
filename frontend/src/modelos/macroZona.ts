/** Refleja MacroZonaDto (LFMova.Application). */
export interface MacroZona {
  macroZonaId: number
  empresaId: number
  nombre: string
  activa: boolean
}

/** Refleja CrearMacroZonaDto / ActualizarMacroZonaDto (LFMova.Application). */
export interface DatosMacroZona {
  nombre: string
}
