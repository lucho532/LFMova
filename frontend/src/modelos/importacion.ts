/** Refleja PasajeroPreviaDto (TransportApp.Application). */
export interface PasajeroPrevia {
  cedula: string
  nombreCompleto: string
  direccion: string
  barrio: string
  celular: string
  /** NUEVO, CUENTA_REGISTRADA, EXISTENTE o CAMBIA_DE_EMPRESA. */
  situacion: string
  /** Solo se llena en la vista después de importar (ver ServicioPrevia.servicioId): identifica al pasajero real para poder arrastrarlo a otra ruta. */
  servicioPasajeroId?: number
  /** Solo se llena en la vista después de importar: identifica al empleado real para poder editar sus datos (nombre, celular, barrio). */
  empleadoId?: number
}

/** Refleja ServicioPreviaDto (TransportApp.Application). */
export interface ServicioPrevia {
  tipo: number
  sedeEnHoja: string
  sedeEncontrada: string | null
  sedeId: number | null
  hora: string
  fecha: string | null
  pasajeros: PasajeroPrevia[]
  /**
   * Solo se llena cuando esta tarjeta representa un servicio real ya creado (vista después de importar):
   * identifica exactamente cuál, porque una misma sede+hora+tipo puede tener varios servicios reales
   * (uno por zona o corredor vial) y no alcanza con esos datos para saber a cuál corresponde.
   */
  servicioId?: number
}

/** Refleja VistaPreviaImportacionDto (TransportApp.Application). */
export interface VistaPreviaImportacion {
  transportador: string
  fechaTexto: string
  cruzaMedianoche: boolean
  fechaOperativaSugerida: string | null
  servicios: ServicioPrevia[]
  totalPasajeros: number
  empleadosNuevos: number
  advertencias: string[]
  errores: string[]
  puedeImportar: boolean
  /** Barrios del archivo que no coinciden con el de ninguna zona activa: esas personas quedan sin repartir por zona. */
  barriosSinZona: string[]
}

/** Refleja ResultadoImportacionDto (TransportApp.Application). */
export interface ResultadoImportacion {
  jornadaId: number
  /** Ruta (servicio) donde quedó el pasajero, cuando la operación afecta a uno solo (alta manual). */
  servicioId: number | null
  serviciosCreados: number
  pasajerosAsignados: number
  empleadosCreados: number
  empleadosVinculados: number
  programacionesCreadas: number
  filasOmitidas: number
  advertencias: string[]
  barriosSinZona: string[]
}

/** Datos para crear una ruta vacía a mano; refleja CrearRutaVaciaDto (TransportApp.Application). */
export interface DatosRutaVacia {
  fecha: string
  hora: string
  tipo: number
  sedeId: number
  unidadOperativaId: number
}

/** Refleja AgregarEmpleadoManualDto (TransportApp.Application): agrega a mano un pasajero de última hora a una ruta ya creada. */
export interface DatosAgregarEmpleadoManual {
  servicioId: number
  cedula: string
  nombreCompleto: string
  celular: string
  direccion: string
  barrio: string
}

/** Un pasajero de una fila pegada, ya separado en sus campos; refleja FilaPasajeroPegadoDto (TransportApp.Application). */
export interface FilaPasajeroPegado {
  cedula: string
  nombreCompleto: string
  celular: string
  direccion: string
  barrio: string
}

/** Datos para crear una ruta a partir de pasajeros pegados; refleja CrearRutaPegadaDto (TransportApp.Application). */
export interface DatosRutaPegada {
  fecha: string
  hora: string
  tipo: number
  sedeId: number
  /** Unidad operativa a asignar de una vez, o `null` para dejar la ruta sin conductor (se asigna después). */
  unidadOperativaId: number | null
  pasajeros: FilaPasajeroPegado[]
}

/**
 * Campo del pasajero que puede ocupar una columna del pegado: mismos valores
 * que espera GuardarPlantillaColumnasPegadoDto en el backend. NOMBRE puede
 * ser el nombre completo, o solo el nombre si APELLIDOS ocupa otra columna
 * aparte (opcional). IGNORAR marca una columna del Excel que no es ninguno
 * de los datos del pasajero.
 */
export type CampoPasajeroPegado = 'CEDULA' | 'NOMBRE' | 'APELLIDOS' | 'CELULAR' | 'DIRECCION' | 'BARRIO' | 'IGNORAR'

/** Refleja PlantillaColumnasPegadoDto (TransportApp.Application). */
export interface PlantillaColumnasPegado {
  plantillaColumnasPegadoId: number
  columnasEnOrden: CampoPasajeroPegado[]
}
