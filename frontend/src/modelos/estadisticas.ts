/** Refleja RutasDiaDto (TransportApp.Application). */
export interface RutasDia {
  fecha: string
  rutas: number
  pasajerosTransportados: number
}

/** Refleja RutasSedeDto (TransportApp.Application). */
export interface RutasSede {
  sede: string
  rutas: number
  pasajerosTransportados: number
}

/** Refleja ResumenEmpresaDto (TransportApp.Application). */
export interface ResumenEmpresa {
  rutasProgramadas: number
  rutasRealizadas: number
  rutasCanceladas: number
  rutasPorRealizar: number
  pasajerosProgramados: number
  pasajerosTransportados: number
  conductores: number
  empleados: number
  sedes: number
  ultimosDias: RutasDia[]
  porSede: RutasSede[]
}

/** Refleja VehiculoResumenDto (TransportApp.Application). */
export interface VehiculoResumen {
  placa: string
  marca: string
  modelo: string
  capacidad: number
  vigenciaSoat: string | null
  vigenciaTecnomecanica: string | null
}

/** Refleja EstadisticaConductorDto (TransportApp.Application). */
export interface EstadisticaConductor {
  conductorId: number
  cedula: string
  nombreCompleto: string
  telefono: string
  vehiculos: VehiculoResumen[]
  rutasProgramadas: number
  rutasRealizadas: number
  pasajerosTransportados: number
}

/** Refleja RutaConductorDto (TransportApp.Application). */
export interface RutaConductor {
  servicioId: number
  jornadaId: number
  fecha: string
  hora: string
  tipo: number
  estado: number
  sede: string
  pasajeros: number
  pasajerosTransportados: number
}

/** Refleja RutaEmpresaDto (TransportApp.Application). */
export interface RutaEmpresa {
  servicioId: number
  jornadaId: number
  fecha: string
  hora: string
  tipo: number
  estado: number
  sede: string
  unidadOperativaId: number | null
  conductor: string | null
  placa: string | null
  pasajeros: number
  pasajerosTransportados: number
}

/** Refleja PasajeroEmpresaDto (TransportApp.Application). */
export interface PasajeroEmpresa {
  cedula: string
  nombreCompleto: string
  telefono: string
  servicioId: number
  jornadaId: number
  fecha: string
  hora: string
  tipo: number
  sede: string
  estado: number
  conductor: string | null
}
