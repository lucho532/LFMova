import { useState } from 'react'
import type { ServicioPasajero } from '../modelos/operacion'
import { guardarArchivo } from '../servicios/archivos'
import { cumpleFiltro, type FiltroAsistencia } from '../servicios/ordenRuta'
import { descargarInformeAsistencia } from '../servicios/servicioOperacion'
import { MensajeAlerta } from './MensajeAlerta'
import '../estilos/componentes/BarraAsistenciaRuta.css'

const OPCIONES: { valor: FiltroAsistencia; texto: string }[] = [
  { valor: 'TODOS', texto: 'Todos' },
  { valor: 'ASISTIERON', texto: 'Asistieron' },
  { valor: 'NO_ASISTIERON', texto: 'No asistieron' },
]

interface PropiedadesBarraAsistenciaRuta {
  pasajeros: ServicioPasajero[]
  filtro: FiltroAsistencia
  alCambiarFiltro: (filtro: FiltroAsistencia) => void
  empresaId: number
  jornadaId: number
  servicioId: number
  token: string
}

/**
 * Filtro de la lista de pasajeros de una ruta (todos, los que asistieron o los que no) con su cantidad, y
 * el botón que descarga en Excel el informe de lo que muestra el filtro, para enviarlo a la empresa. Solo
 * filtra y descarga: no cambia el estado de ningún pasajero.
 */
export function BarraAsistenciaRuta({ pasajeros, filtro, alCambiarFiltro, empresaId, jornadaId, servicioId, token }: PropiedadesBarraAsistenciaRuta) {
  const [descargando, setDescargando] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function alDescargar() {
    setDescargando(true)
    setError(null)
    try {
      const { nombre, archivo } = await descargarInformeAsistencia(empresaId, jornadaId, servicioId, filtro, token)
      guardarArchivo(nombre, archivo)
    } catch (causa) {
      setError(causa instanceof Error ? causa.message : 'No se pudo descargar el informe.')
    } finally {
      setDescargando(false)
    }
  }

  return (
    <div className="barra-asistencia-ruta">
      <div className="barra-asistencia-ruta__filtros" role="group" aria-label="Filtrar pasajeros">
        {OPCIONES.map((opcion) => (
          <button
            key={opcion.valor}
            type="button"
            aria-pressed={filtro === opcion.valor}
            className={`barra-asistencia-ruta__filtro${filtro === opcion.valor ? ' barra-asistencia-ruta__filtro--activo' : ''}`}
            onClick={() => alCambiarFiltro(opcion.valor)}
          >
            {opcion.texto} ({pasajeros.filter((p) => cumpleFiltro(p.estado, opcion.valor)).length})
          </button>
        ))}
      </div>
      <button type="button" className="barra-asistencia-ruta__descargar" disabled={descargando} onClick={alDescargar}>
        {descargando ? 'Descargando…' : '⬇ Descargar Excel'}
      </button>
      {error && <MensajeAlerta tipo="error">{error}</MensajeAlerta>}
    </div>
  )
}
