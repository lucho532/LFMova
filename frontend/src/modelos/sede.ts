/** Refleja SedeDto (LFMova.Application). */
export interface Sede {
  sedeId: number
  empresaId: number
  nombre: string
  direccion: string
  ciudad: string
  barrio: string
  latitud: number | null
  longitud: number | null
  activa: boolean
}

/** Refleja CrearSedeDto / ActualizarSedeDto (LFMova.Application). */
export interface DatosSede {
  nombre: string
  direccion: string
  ciudad: string
  barrio: string
  latitud: number | null
  longitud: number | null
}
