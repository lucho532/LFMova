/** Refleja ProgramacionDto (TransportApp.Application). */
export interface Programacion {
  programacionTransporteId: number
  empresaId: number
  empleadoId: number
  sedeId: number
  fecha: string
  hora: string
  tipo: number
  direccionRecogida: string
  barrioRecogida: string
}

/** Refleja CrearProgramacionDto / ActualizarProgramacionDto (sin empleado al actualizar). */
export interface DatosProgramacion {
  sedeId: number
  fecha: string
  hora: string
  tipo: number
  direccionRecogida: string
  barrioRecogida: string
}

/** Refleja JornadaDto (TransportApp.Application). */
export interface Jornada {
  jornadaId: number
  empresaId: number
  fechaOperativa: string
}

/** Refleja DeshacerRepartoDto (TransportApp.Application). */
export interface DeshacerReparto {
  serviciosEliminados: number
  pasajerosLiberados: number
}

/** Refleja EliminarRastroDto (TransportApp.Application). */
export interface EliminarRastro {
  serviciosEliminados: number
  pasajerosEliminados: number
  programacionesEliminadas: number
  jornadaEliminada: boolean
}

/** Refleja ServicioDto (TransportApp.Application). */
export interface Servicio {
  servicioId: number
  empresaId: number
  jornadaId: number
  unidadOperativaId: number | null
  sedeId: number
  nombreSede: string
  fecha: string
  horaProgramada: string
  tipo: number
  estado: number
  horaInicioReal: string | null
  horaFinReal: string | null
  latitudInicio: number | null
  longitudInicio: number | null
  latitudFinalizacion: number | null
  longitudFinalizacion: number | null
  cantidadPasajeros: number
}

/** Refleja ServicioPasajeroDto (TransportApp.Application). */
export interface ServicioPasajero {
  servicioPasajeroId: number
  servicioId: number
  programacionTransporteId: number
  empleadoId: number
  estado: number
  orden: number
  horaLlegadaConductor: string | null
  horaProcesado: string | null
  direccionRecogida: string
  latitud: number | null
  longitud: number | null
  nombreCompletoEmpleado: string
  telefonoEmpleado: string
  cedulaEmpleado: string
  barrioEmpleado: string
}

/** Refleja PropuestaPlanificacionDto (TransportApp.Application). */
export interface PropuestaPlanificacion {
  servicioId: number
  ordenPropuesto: { servicioPasajeroId: number; empleadoId: number; orden: number; distanciaALaSedeKm: number | null }[]
  horaLimiteLlegadaSede: string | null
  horaSugeridaInicioRecogida: string | null
  distanciaDesdeServicioAnteriorKm: number | null
  distanciaHaciaServicioSiguienteKm: number | null
  advertencias: string[]
}
