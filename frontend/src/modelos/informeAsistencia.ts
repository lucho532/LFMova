/**
 * Pasajero de la vista previa del informe de asistencia (GET .../informe-asistencia/vista-previa). Los enums
 * llegan como números. `horaRegistro` es hora de Colombia sin zona («2026-10-15T06:02:44»), no un instante UTC.
 */
export interface FilaInformeAsistencia {
  servicioId: number
  jornadaId: number
  fecha: string
  horaRuta: string
  tipo: number
  sede: string
  conductor: string | null
  placa: string | null
  cedula: string
  nombreCompleto: string
  telefono: string
  direccion: string
  barrio: string
  estado: number
  horaRegistro: string | null
  esperaSegundos: number | null
}

/** Informe de asistencia de una jornada o rango de días: lo mismo que lleva el Excel. */
export interface InformeAsistenciaPeriodo {
  nombreEmpresa: string
  desde: string
  hasta: string
  filas: FilaInformeAsistencia[]
}
