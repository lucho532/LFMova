/** Refleja ConductorDto (LFMova.Application). */
export interface Conductor {
  conductorId: number
  usuarioId: number
  cedula: string
  nombreCompleto: string
  telefono: string
  activo: boolean
  empresaIdsVinculadosActivos: number[]
}

/**
 * Refleja CrearConductorDto (LFMova.Application): asigna el rol de
 * conductor a una persona que ya se registró; su nombre y teléfono se toman
 * de su propia cuenta.
 */
export interface CrearConductorDatos {
  cedula: string
  vehiculo: CrearVehiculoDatos
}

/** Refleja VincularConductorDto (LFMova.Application). */
export interface VincularConductorDatos {
  cedula: string
}

/** Refleja VehiculoDto (LFMova.Application). */
export interface Vehiculo {
  vehiculoId: number
  conductorId: number
  placa: string
  marca: string
  modelo: string
  capacidad: number
  vigenciaSoat: string | null
  vigenciaTecnomecanica: string | null
  activo: boolean
}

/** Refleja CrearVehiculoDto (LFMova.Application). */
export interface CrearVehiculoDatos {
  placa: string
  marca: string
  modelo: string
  capacidad: number
  vigenciaSoat: string
  vigenciaTecnomecanica: string
}

/** Refleja UnidadOperativaDto (LFMova.Application). */
export interface UnidadOperativa {
  unidadOperativaId: number
  conductorId: number
  vehiculoId: number
  activa: boolean
}

/** Refleja UnidadDeTrabajoDto (LFMova.Application): unidad operativa con los datos de su vehículo. */
export interface UnidadDeTrabajo {
  unidadOperativaId: number
  activa: boolean
  vehiculo: Vehiculo
}
