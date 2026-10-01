/** Refleja ConductorDto (TransportApp.Application). */
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
 * Refleja CrearConductorDto (TransportApp.Application): asigna el rol de
 * conductor a una persona que ya se registró; su nombre y teléfono se toman
 * de su propia cuenta.
 */
export interface CrearConductorDatos {
  cedula: string
  vehiculo: CrearVehiculoDatos
}

/** Refleja VincularConductorDto (TransportApp.Application). */
export interface VincularConductorDatos {
  cedula: string
}

/** Refleja VehiculoDto (TransportApp.Application). */
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

/** Refleja CrearVehiculoDto (TransportApp.Application). */
export interface CrearVehiculoDatos {
  placa: string
  marca: string
  modelo: string
  capacidad: number
  vigenciaSoat: string
  vigenciaTecnomecanica: string
}

/** Refleja UnidadOperativaDto (TransportApp.Application). */
export interface UnidadOperativa {
  unidadOperativaId: number
  conductorId: number
  vehiculoId: number
  activa: boolean
}

/** Refleja UnidadDeTrabajoDto (TransportApp.Application): unidad operativa con los datos de su vehículo. */
export interface UnidadDeTrabajo {
  unidadOperativaId: number
  activa: boolean
  vehiculo: Vehiculo
}
