import type { IncidenciaRuta } from '../servicios/servicioOperacion'
import type { ReferenciaPasajero } from '../servicios/servicioLlamadas'
import { HistorialLlamadas } from './HistorialLlamadas'
import { MapaUbicacion } from './MapaUbicacion'
import '../estilos/componentes/IncidenciaDetalleRuta.css'

/** Valor del enum TipoIncidencia del backend para «No contesta»: es la que muestra además el historial de llamadas. */
const TIPO_NO_CONTESTA = 0

interface PropiedadesIncidenciaDetalleRuta {
  incidencia: IncidenciaRuta
  /** Texto del tipo de incidencia ya traducido. */
  etiquetaTipo: string
  /** Fotos ya descargadas, por identificador de evidencia (URL local para un `<img>`). */
  fotos: Record<number, string>
  cargandoFotoId: number | null
  alVerFoto: (evidenciaId: number) => void
  pasajero: ReferenciaPasajero
}

/**
 * Una incidencia de un pasajero dentro del detalle de ruta: su descripción y, como evidencia, la foto
 * con el mapa del lugar donde se reportó, uno al lado del otro. No descarga nada por sí sola: la foto
 * se pide con `alVerFoto` para no cargar imágenes que el coordinador quizá no abra.
 */
export function IncidenciaDetalleRuta({ incidencia, etiquetaTipo, fotos, cargandoFotoId, alVerFoto, pasajero }: PropiedadesIncidenciaDetalleRuta) {
  const tieneUbicacion = incidencia.latitud !== null && incidencia.longitud !== null

  return (
    <li className="incidencia-ruta">
      <div>
        <strong>{etiquetaTipo}</strong> · <small>{new Date(incidencia.fechaHora).toLocaleString()}</small>
      </div>
      {incidencia.descripcion && <p className="incidencia-ruta__descripcion">{incidencia.descripcion}</p>}

      {(incidencia.evidencias.length > 0 || tieneUbicacion) && (
        <div className="incidencia-ruta__evidencia">
          {incidencia.evidencias.length > 0 && (
            <div className="incidencia-ruta__fotos">
              {incidencia.evidencias.map((evidencia) => (
                <div key={evidencia.evidenciaId} className="incidencia-ruta__foto">
                  {fotos[evidencia.evidenciaId] ? (
                    <img src={fotos[evidencia.evidenciaId]} alt="Evidencia de la incidencia" />
                  ) : (
                    <button type="button" onClick={() => alVerFoto(evidencia.evidenciaId)} disabled={cargandoFotoId === evidencia.evidenciaId}>
                      {cargandoFotoId === evidencia.evidenciaId ? 'Cargando…' : '📷 Ver foto'}
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}
          {tieneUbicacion && <MapaUbicacion latitud={incidencia.latitud!} longitud={incidencia.longitud!} titulo={`Ubicación donde se reportó: ${etiquetaTipo}`} />}
        </div>
      )}

      {incidencia.tipo === TIPO_NO_CONTESTA && <HistorialLlamadas pasajero={pasajero} />}
    </li>
  )
}
