import { formatearCronometro, segundosEntre } from '../servicios/tiempoRuta'
import { ESPERA_MAXIMA_SEGUNDOS } from './CronometroEspera'
import '../estilos/componentes/EsperaRegistrada.css'

interface PropiedadesEsperaRegistrada {
  /** Instante (UTC) en que el conductor marcó "He llegado" al pasajero. */
  horaLlegadaConductor: string | null
  /** Instante (UTC) en que quedó registrado el resultado del pasajero (recogido o no). */
  horaProcesado: string | null
  /** Texto orientativo que va antes del tiempo (por ejemplo, «Tiempo de espera»); sin él se muestra solo el tiempo. */
  etiqueta?: string
}

/**
 * Tiempo de espera que quedó registrado con un pasajero ya procesado, desde que el conductor marcó la
 * llegada hasta que registró el resultado, haya salido o no. Se muestra solo el tiempo (mm:ss): en verde
 * si fue de 2 minutos o menos y en rojo si pasó de ese límite. No se muestra si falta alguno de los dos
 * instantes (por ejemplo, un pasajero que avisó que no asistiría). Si se indica `etiqueta`, la antepone en
 * gris para que se entienda qué mide el número.
 */
export function EsperaRegistrada({ horaLlegadaConductor, horaProcesado, etiqueta }: PropiedadesEsperaRegistrada) {
  const segundos = segundosEntre(horaLlegadaConductor, horaProcesado)
  if (segundos === null) return null
  return <TiempoEspera segundos={segundos} etiqueta={etiqueta} />
}

/**
 * Un tiempo de espera ya calculado, en segundos, con el mismo formato y color que `EsperaRegistrada`: verde
 * si fue de 2 minutos o menos y rojo si pasó de ese límite. Sirve cuando los segundos vienen del servidor.
 */
export function TiempoEspera({ segundos, etiqueta }: { segundos: number; etiqueta?: string }) {
  const excedida = segundos > ESPERA_MAXIMA_SEGUNDOS

  const tiempo = (
    <span
      className={`espera-registrada espera-registrada--${excedida ? 'excedida' : 'a-tiempo'}`}
      title="Tiempo de espera desde que el conductor llegó hasta que registró el resultado"
    >
      {formatearCronometro(segundos)}
    </span>
  )
  if (!etiqueta) return tiempo

  return (
    <span className="espera-registrada__con-etiqueta">
      <span className="espera-registrada__etiqueta">{etiqueta}</span> {tiempo}
    </span>
  )
}
