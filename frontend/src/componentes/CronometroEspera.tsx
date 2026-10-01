import { useEffect, useState } from 'react'
import '../estilos/componentes/CronometroEspera.css'

/** Tiempo máximo de espera orientativo al pasajero (spec.md §25: aproximadamente 2 minutos). */
export const ESPERA_MAXIMA_SEGUNDOS = 120

/** Las horas reales llegan en UTC sin zona explícita; se completan antes de convertirlas. */
function aMilisegundos(iso: string): number {
  const conZona = /[zZ]|[+-]\d{2}:?\d{2}$/.test(iso) ? iso : `${iso}Z`
  return new Date(conZona).getTime()
}

function formatear(segundos: number): string {
  const minutos = Math.floor(segundos / 60)
  return `${String(minutos).padStart(2, '0')}:${String(segundos % 60).padStart(2, '0')}`
}

interface PropiedadesCronometroEspera {
  /** Momento (UTC) en que el conductor marcó "He llegado", tal como lo guardó el backend. */
  horaLlegadaConductor: string | null
  /** Verdadero mientras el conductor está en el punto de recogida esperando al pasajero. */
  activo: boolean
}

/**
 * Cronómetro de espera: arranca cuando el conductor marca "He llegado" y
 * cuenta hacia atrás los 2 minutos orientativos; al cumplirse, pasa a rojo y
 * muestra el tiempo excedido. Se calcula a partir de la hora que guardó el
 * backend (no del reloj de cada dispositivo), así que el conductor y el
 * empleado ven exactamente el mismo tiempo. Es solo una ayuda visual: el
 * backend no impone el límite.
 */
export function CronometroEspera({ horaLlegadaConductor, activo }: PropiedadesCronometroEspera) {
  const [ahora, setAhora] = useState(() => Date.now())

  useEffect(() => {
    if (!activo || !horaLlegadaConductor) return
    const temporizador = setInterval(() => setAhora(Date.now()), 1000)
    return () => clearInterval(temporizador)
  }, [activo, horaLlegadaConductor])

  const inicio = horaLlegadaConductor ? aMilisegundos(horaLlegadaConductor) : null
  const transcurridos = inicio === null ? 0 : Math.max(0, Math.floor((ahora - inicio) / 1000))
  const cumplido = transcurridos >= ESPERA_MAXIMA_SEGUNDOS

  useEffect(() => {
    if (cumplido && activo && typeof navigator.vibrate === 'function') navigator.vibrate([300, 150, 300])
  }, [cumplido, activo])

  if (!activo || inicio === null) return null

  return (
    <div className={`cronometro-espera${cumplido ? ' cronometro-espera--cumplido' : ''}`} role="timer" aria-live="off">
      <span className="cronometro-espera__etiqueta">{cumplido ? 'Tiempo de espera cumplido' : 'Esperando al pasajero'}</span>
      <strong className="cronometro-espera__tiempo">{cumplido ? `+${formatear(transcurridos - ESPERA_MAXIMA_SEGUNDOS)}` : formatear(ESPERA_MAXIMA_SEGUNDOS - transcurridos)}</strong>
    </div>
  )
}
