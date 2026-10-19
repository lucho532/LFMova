import type { ServicioPasajero } from '../modelos/operacion'
import { cumpleFiltro, numerarRuta, type FiltroAsistencia } from '../servicios/ordenRuta'
import { horaLocal } from '../servicios/tiempoRuta'
import { EsperaRegistrada } from './EsperaRegistrada'
import { EstadoPasajeroTexto } from './EstadoPasajeroTexto'
import '../estilos/componentes/ListaPasajerosRuta.css'

interface PropiedadesListaPasajerosRuta {
  pasajeros: ServicioPasajero[]
  filtro: FiltroAsistencia
  alVerDetalle: (servicioPasajeroId: number) => void
}

/**
 * Tabla de los pasajeros de la ruta en el orden en que se atendieron: número de recogida, nombre, estado,
 * hora en que quedó registrado el resultado y tiempo de espera. Muestra solo los que cumplen el filtro,
 * pero conserva el número que cada uno tiene en la ruta completa. Es un resumen: el detalle de cada
 * pasajero (llamadas, incidencias, fotos) se abre con «Ver detalle». No modifica nada.
 */
export function ListaPasajerosRuta({ pasajeros, filtro, alVerDetalle }: PropiedadesListaPasajerosRuta) {
  const filas = numerarRuta(pasajeros).filter(({ pasajero }) => cumpleFiltro(pasajero.estado, filtro))

  if (filas.length === 0) return <p className="lista-pasajeros-ruta__vacio">Ningún pasajero cumple este filtro.</p>

  return (
    <div className="lista-pasajeros-ruta">
      <div className="lista-pasajeros-ruta__encabezado" aria-hidden="true">
        <span className="lista-pasajeros-ruta__numero">#</span>
        <span className="lista-pasajeros-ruta__datos">Pasajero</span>
        <span className="lista-pasajeros-ruta__estado">Estado</span>
        <span className="lista-pasajeros-ruta__hora">Hora de recogida</span>
        <span className="lista-pasajeros-ruta__espera">Espera</span>
        <span className="lista-pasajeros-ruta__accion" />
      </div>
      <ol className="lista-pasajeros-ruta__filas">
        {filas.map(({ pasajero, numero }) => (
          <li key={pasajero.servicioPasajeroId} className="lista-pasajeros-ruta__fila">
            <span className="lista-pasajeros-ruta__numero">{numero ?? '—'}</span>
            <div className="lista-pasajeros-ruta__datos">
              <strong>{pasajero.nombreCompletoEmpleado}</strong>
              <span className="lista-pasajeros-ruta__sub">CC {pasajero.cedulaEmpleado}</span>
            </div>
            <span className="lista-pasajeros-ruta__estado">
              <EstadoPasajeroTexto estado={pasajero.estado} />
            </span>
            <span className="lista-pasajeros-ruta__hora">{horaLocal(pasajero.horaProcesado) ?? '—'}</span>
            <span className="lista-pasajeros-ruta__espera">
              <EsperaRegistrada horaLlegadaConductor={pasajero.horaLlegadaConductor} horaProcesado={pasajero.horaProcesado} />
            </span>
            <span className="lista-pasajeros-ruta__accion">
              <button type="button" className="lista-pasajeros-ruta__detalle" onClick={() => alVerDetalle(pasajero.servicioPasajeroId)}>
                Ver detalle
              </button>
            </span>
          </li>
        ))}
      </ol>
    </div>
  )
}
