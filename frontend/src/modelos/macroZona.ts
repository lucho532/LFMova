/** Refleja MacroZonaDto (TransportApp.Application). */
export interface MacroZona {
  macroZonaId: number
  empresaId: number
  nombre: string
  activa: boolean
}

/** Refleja CrearMacroZonaDto / ActualizarMacroZonaDto (TransportApp.Application). */
export interface DatosMacroZona {
  nombre: string
}
