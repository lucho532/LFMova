/** Refleja ServicioDelEmpleadoDto (LFMova.Application). */
export interface ServicioDelEmpleado {
  servicioPasajeroId: number
  empresaId: number
  jornadaId: number
  servicioId: number
  fecha: string
  horaProgramada: string
  tipo: number
  estadoServicio: number
  estadoServicioPasajero: number
  sedeId: number
  direccionRecogida: string
  horaLlegadaConductor: string | null
  horaProcesado: string | null
  conductorNombre: string | null
  placa: string | null
  horaInicioReal: string | null
  horaFinReal: string | null
}
