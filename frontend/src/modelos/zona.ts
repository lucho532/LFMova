/** Refleja ZonaDto (LFMova.Application). */
export interface Zona {
  zonaId: number
  empresaId: number
  nombre: string
  barrios: string[]
  macroZonaId: number | null
  macroZonaNombre: string | null
  corredorVialId: number | null
  corredorVialNombre: string | null
  activa: boolean
  orden: number
}

/** Refleja CrearZonaDto / ActualizarZonaDto (LFMova.Application). */
export interface DatosZona {
  nombre: string
  barrios: string[]
  macroZonaId?: number | null
  corredorVialId?: number | null
}
