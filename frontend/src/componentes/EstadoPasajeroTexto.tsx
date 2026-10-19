import { EstadoPasajero, NOMBRES_ESTADO_PASAJERO, nombreDe } from '../modelos/enumeraciones'
import '../estilos/componentes/EstadoPasajeroTexto.css'

interface PropiedadesEstadoPasajeroTexto {
  estado: number
  /** Texto a mostrar en lugar del nombre del estado (por ejemplo, «Te recogieron»); conserva el color del estado. */
  texto?: string
}

/**
 * Nombre del estado de un pasajero con su color en toda la app: «Recogido» en verde y «No recogido» en
 * rojo. Los demás estados se muestran sin color. Solo presenta el texto; no cambia ni valida el estado.
 */
export function EstadoPasajeroTexto({ estado, texto }: PropiedadesEstadoPasajeroTexto) {
  const modificador =
    estado === EstadoPasajero.RECOGIDO ? ' estado-pasajero--recogido' : estado === EstadoPasajero.NO_RECOGIDO ? ' estado-pasajero--no-recogido' : ''
  return <span className={`estado-pasajero${modificador}`}>{texto ?? nombreDe(NOMBRES_ESTADO_PASAJERO, estado)}</span>
}
