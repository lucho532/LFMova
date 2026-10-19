import { NOMBRES_TIPO_SERVICIO, formatearHora, nombreDe } from '../modelos/enumeraciones'
import type { FilaInformeAsistencia } from '../modelos/informeAsistencia'
import { TiempoEspera } from './EsperaRegistrada'
import { EstadoPasajeroTexto } from './EstadoPasajeroTexto'
import { FilaTablaSeleccionable } from './FilaTablaSeleccionable'
import '../estilos/componentes/TablaInasistencias.css'

/** «2026-10-15» → «15/10/2026». */
const formatearFecha = (fecha: string): string => fecha.split('-').reverse().join('/')

/** Hora de «2026-10-15T06:02:44» (ya en hora de Colombia, sin zona) como «06:02». */
const horaDeRegistro = (horaRegistro: string | null): string => (horaRegistro ? horaRegistro.slice(11, 16) : '—')

interface PropiedadesTablaInasistencias {
  filas: FilaInformeAsistencia[]
  /** Se invoca al elegir un pasajero para ver su ficha completa. */
  alSeleccionar: (fila: FilaInformeAsistencia) => void
}

/**
 * Tabla de los pasajeros que no asistieron, con la ruta y el conductor que tenían asignados, el estado, la
 * hora en que quedó registrado y la espera. Es la vista previa de lo que lleva el Excel (que además trae
 * teléfono, dirección y barrio). Cada fila se puede abrir para ver la ficha completa del pasajero. Solo
 * muestra: no modifica nada.
 */
export function TablaInasistencias({ filas, alSeleccionar }: PropiedadesTablaInasistencias) {
  return (
    <div className="tabla-inasistencias">
      <table>
        <thead>
          <tr>
            <th>Ruta</th>
            <th>Conductor</th>
            <th>Pasajero</th>
            <th>Estado</th>
            <th>Hora</th>
            <th>Espera</th>
          </tr>
        </thead>
        <tbody>
          {filas.map((fila, indice) => (
            <FilaTablaSeleccionable key={`${fila.servicioId}-${fila.cedula}-${indice}`} alSeleccionar={() => alSeleccionar(fila)}>
              <td>
                <strong>
                  {formatearFecha(fila.fecha)} · {formatearHora(fila.horaRuta)}
                </strong>
                <div className="tabla-inasistencias__sub">
                  {nombreDe(NOMBRES_TIPO_SERVICIO, fila.tipo)} · {fila.sede}
                </div>
              </td>
              <td>
                {fila.conductor ?? <span className="tabla-inasistencias__sin-conductor">Sin conductor</span>}
                {fila.placa && <div className="tabla-inasistencias__sub">{fila.placa}</div>}
              </td>
              <td>
                <strong>{fila.nombreCompleto}</strong>
                <div className="tabla-inasistencias__sub">
                  CC {fila.cedula}
                  {fila.telefono && ` · ${fila.telefono}`}
                </div>
                {fila.direccion && <div className="tabla-inasistencias__sub">{fila.direccion}</div>}
              </td>
              <td>
                <EstadoPasajeroTexto estado={fila.estado} />
              </td>
              <td className="tabla-inasistencias__hora">{horaDeRegistro(fila.horaRegistro)}</td>
              <td>{fila.esperaSegundos === null ? '—' : <TiempoEspera segundos={fila.esperaSegundos} />}</td>
            </FilaTablaSeleccionable>
          ))}
        </tbody>
      </table>
    </div>
  )
}
