import { TipoServicio } from '../modelos/enumeraciones'
import type { RutaEmpresa } from '../modelos/estadisticas'
import { horaLocal, llegoTemprano } from '../servicios/tiempoRuta'
import '../estilos/componentes/HorarioRealRuta.css'

/**
 * Hora real de inicio y fin de una ruta ya realizada, para la fila del resumen del coordinador.
 * Solo la hora de fin de las rutas de entrada se colorea: verde si llegó a más tardar 15 minutos
 * antes de la hora programada y rojo si llegó después. No decide nada de negocio ni consulta la API.
 */
export function HorarioRealRuta({ ruta }: { ruta: RutaEmpresa }) {
  if (!ruta.horaInicioReal && !ruta.horaFinReal) return null

  let claseFin = ''
  if (ruta.tipo === TipoServicio.ENTRADA && ruta.horaFinReal) {
    claseFin = llegoTemprano(ruta.fecha, ruta.hora, ruta.horaFinReal) ? ' horario-real-ruta__fin--temprano' : ' horario-real-ruta__fin--tarde'
  }

  return (
    <div className="horario-real-ruta">
      {ruta.horaInicioReal && <span>Inicio {horaLocal(ruta.horaInicioReal)}</span>}
      {ruta.horaFinReal && <span className={`horario-real-ruta__fin${claseFin}`}>Fin {horaLocal(ruta.horaFinReal)}</span>}
    </div>
  )
}
