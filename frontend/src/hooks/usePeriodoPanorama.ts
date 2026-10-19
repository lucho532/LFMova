import { useState } from 'react'
import { hoyColombia } from '../modelos/fechaColombia'

export type OpcionPeriodo = '7' | '30' | 'personalizado'

/** Resta días a una fecha AAAA-MM-DD sin depender de la zona horaria del dispositivo. */
function restarDias(fecha: string, dias: number): string {
  const [anio, mes, dia] = fecha.split('-').map(Number)
  return new Date(Date.UTC(anio, mes - 1, dia - dias)).toISOString().slice(0, 10)
}

/**
 * Estado del periodo del panorama de operación: últimos 7 días, últimos 30 o un rango propio. Los
 * periodos fijos terminan hoy (hora de Colombia) e incluyen el día de hoy.
 */
export function usePeriodoPanorama() {
  const [opcion, setOpcion] = useState<OpcionPeriodo>('7')
  const [desdePropio, setDesdePropio] = useState(() => restarDias(hoyColombia(), 6))
  const [hastaPropio, setHastaPropio] = useState(hoyColombia)

  const hoy = hoyColombia()
  const desde = opcion === 'personalizado' ? desdePropio : restarDias(hoy, Number(opcion) - 1)
  const hasta = opcion === 'personalizado' ? hastaPropio : hoy
  const valido = Boolean(desde && hasta && hasta >= desde)

  return { opcion, setOpcion, desde, hasta, setDesdePropio, setHastaPropio, valido }
}
