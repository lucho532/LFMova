/** Refleja ProgramacionDto (LFMova.Application). */
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

/** Refleja DeshacerRepartoDto (LFMova.Application). */
export interface DeshacerReparto {
  serviciosEliminados: number
  pasajerosLiberados: number
}

/** Refleja EliminarRastroDto (LFMova.Application). */
export interface EliminarRastro {
  serviciosEliminados: number
  pasajerosEliminados: number
  programacionesEliminadas: number
  jornadaEliminada: boolean
}

/** Refleja ServicioDto (LFMova.Application). */
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

/** Refleja ServicioPasajeroDto (LFMova.Application). */
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
  /** Punto que el propio empleado compartió para este servicio (no el que guarda el conductor). */
  latitudCompartida: number | null
  longitudCompartida: number | null
  /** Instante (UTC) en que el empleado compartió su ubicación por última vez. */
  fechaHoraUbicacionCompartida: string | null
  nombreCompletoEmpleado: string
  telefonoEmpleado: string
  cedulaEmpleado: string
  barrioEmpleado: string
}

