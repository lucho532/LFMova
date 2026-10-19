import { useEffect, useRef, useState } from 'react'
import { EstadoPasajero } from '../modelos/enumeraciones'
import type { ServicioPasajero } from '../modelos/operacion'
import { obtenerUrlFotoEvidenciaRuta, type IncidenciaRuta } from '../servicios/servicioOperacion'
import { horaLocal } from '../servicios/tiempoRuta'
import { EsperaRegistrada } from './EsperaRegistrada'
import { EstadoPasajeroTexto } from './EstadoPasajeroTexto'
import { HistorialLlamadas } from './HistorialLlamadas'
import { IncidenciaDetalleRuta } from './IncidenciaDetalleRuta'
import { MensajeAlerta } from './MensajeAlerta'
import '../estilos/componentes/DetallePasajeroRuta.css'

/** Mismo orden que el enum TipoIncidencia del backend (ver HerramientasPasajero.tsx). */
const TIPOS_INCIDENCIA = [
  { valor: 0, texto: 'No contesta' },
  { valor: 1, texto: 'No se encuentra' },
  { valor: 2, texto: 'Dirección incorrecta' },
  { valor: 3, texto: 'No se pudo recoger' },
  { valor: 4, texto: 'Ubicación modificada' },
  { valor: 5, texto: 'Otra' },
]

const etiquetaTipoIncidencia = (tipo: number): string => TIPOS_INCIDENCIA.find((t) => t.valor === tipo)?.texto ?? 'Incidencia'

interface PropiedadesDetallePasajeroRuta {
  pasajero: ServicioPasajero
  incidencias: IncidenciaRuta[]
  empresaId: number
  jornadaId: number
  servicioId: number
  token: string
}

/**
 * Tarjeta con todo lo registrado de un pasajero en una ruta: su estado, la hora y el tiempo de espera,
 * las llamadas del conductor y sus incidencias con la foto y el mapa de cada una. No muestra datos de la
 * ruta en general (inicio, fin, duración) ni de otros pasajeros.
 */
export function DetallePasajeroRuta({ pasajero, incidencias, empresaId, jornadaId, servicioId, token }: PropiedadesDetallePasajeroRuta) {
  const [incidenciasAbiertas, setIncidenciasAbiertas] = useState(true)
  const [fotos, setFotos] = useState<Record<number, string>>({})
  const [cargandoFotoId, setCargandoFotoId] = useState<number | null>(null)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const fotosRef = useRef(fotos)
  fotosRef.current = fotos
  const referenciaPasajero = { empresaId, jornadaId, servicioId, servicioPasajeroId: pasajero.servicioPasajeroId }

  // Las fotos son URL locales en memoria del navegador: se liberan al salir de la tarjeta.
  useEffect(() => {
    return () => {
      Object.values(fotosRef.current).forEach((url) => URL.revokeObjectURL(url))
    }
  }, [])

  async function verFoto(incidenciaId: number, evidenciaId: number) {
    if (fotos[evidenciaId]) return
    setCargandoFotoId(evidenciaId)
    try {
      const url = await obtenerUrlFotoEvidenciaRuta(empresaId, jornadaId, servicioId, pasajero.servicioPasajeroId, incidenciaId, evidenciaId, token)
      setFotos((actual) => ({ ...actual, [evidenciaId]: url }))
    } catch {
      setMensajeError('No se pudo cargar la foto.')
    } finally {
      setCargandoFotoId(null)
    }
  }

  return (
    <div className="detalle-pasajero-ruta">
      <div className="detalle-pasajero-ruta__fila">
        <div>
          <strong>{pasajero.nombreCompletoEmpleado}</strong>
          <div className="detalle-pasajero-ruta__sub">
            CC {pasajero.cedulaEmpleado} · {pasajero.direccionRecogida}
          </div>
          <EsperaRegistrada horaLlegadaConductor={pasajero.horaLlegadaConductor} horaProcesado={pasajero.horaProcesado} etiqueta="Tiempo de espera:" />
        </div>
        <div className="detalle-pasajero-ruta__estado">
          <EstadoPasajeroTexto estado={pasajero.estado} />
          {pasajero.estado === EstadoPasajero.RECOGIDO && horaLocal(pasajero.horaProcesado) && (
            <span className="detalle-pasajero-ruta__hora-estado"> · {horaLocal(pasajero.horaProcesado)}</span>
          )}
        </div>
      </div>

      <HistorialLlamadas pasajero={referenciaPasajero} plegable />

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

      {incidencias.length > 0 && (
        <button type="button" className="detalle-pasajero-ruta__boton-incidencias" onClick={() => setIncidenciasAbiertas((abierto) => !abierto)}>
          ⚠ {incidencias.length} incidencia{incidencias.length > 1 ? 's' : ''} {incidenciasAbiertas ? '▲' : '▼'}
        </button>
      )}

      {incidenciasAbiertas && incidencias.length > 0 && (
        <ul className="detalle-pasajero-ruta__incidencias">
          {incidencias.map((incidencia) => (
            <IncidenciaDetalleRuta
              key={incidencia.incidenciaId}
              incidencia={incidencia}
              etiquetaTipo={etiquetaTipoIncidencia(incidencia.tipo)}
              fotos={fotos}
              cargandoFotoId={cargandoFotoId}
              alVerFoto={(evidenciaId) => verFoto(incidencia.incidenciaId, evidenciaId)}
              pasajero={referenciaPasajero}
            />
          ))}
        </ul>
      )}
    </div>
  )
}
