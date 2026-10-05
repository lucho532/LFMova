/** Lo que una empresa usó la plataforma en un mes (fila del resumen del administrador). */
export interface ResumenFacturacionEmpresa {
  empresaId: number
  nombreEmpresa: string
  /** Conductores vinculados hoy a la empresa: dato informativo, no se cobra por él. */
  conductoresVinculados: number
  /** Conductores distintos que finalizaron al menos una ruta en el mes: la base del cobro. */
  conductoresActivos: number
  rutasFinalizadas: number
  pasajerosTransportados: number
  /** Indica si el mes ya está cerrado (totales fijos). */
  cerrado: boolean
  fechaCierre: string | null
}

/** Un conductor que finalizó rutas para la empresa en el mes. */
export interface ConductorFacturable {
  cedula: string
  nombreConductor: string
  placas: string[]
  rutasFinalizadas: number
  pasajerosTransportados: number
  primeraRuta: string
  ultimaRuta: string
  /** Su cuenta de conductor ya no existe (se eliminó después de hacer las rutas). */
  eliminado: boolean
}

/** Detalle de una empresa en un mes: sus totales y los conductores que los explican. */
export interface DetalleFacturacion {
  resumen: ResumenFacturacionEmpresa
  anio: number
  mes: number
  conductores: ConductorFacturable[]
}
