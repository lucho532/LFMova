/** Refleja PasajerosDiaDto (LFMova.Application). */
export interface PasajerosDia {
  fecha: string
  entradas: number
  salidas: number
}

/** Refleja PanoramaOperacionDto (LFMova.Application). */
export interface PanoramaOperacion {
  desde: string
  hasta: string
  serie: PasajerosDia[]
  empleadosTransportados: number
  variacionEmpleadosTransportados: number | null
  serviciosRealizados: number
  serviciosEntrada: number
  serviciosSalida: number
  conductoresEnOperacion: number
  promedioAsistencia: number
}

/** Refleja ResumenEmpresaDto (LFMova.Application). */
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
}

/** Refleja VehiculoResumenDto (LFMova.Application). */
export interface VehiculoResumen {
  placa: string
  marca: string
  modelo: string
  capacidad: number
  vigenciaSoat: string | null
  vigenciaTecnomecanica: string | null
}

/** Refleja EstadisticaConductorDto (LFMova.Application). */
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

/** Refleja RutaConductorDto (LFMova.Application). */
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

/** Refleja RutaEmpresaDto (LFMova.Application). */
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
  horaInicioReal: string | null
  horaFinReal: string | null
}

/** Refleja PasajeroEmpresaDto (LFMova.Application). */
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
