import { useState } from 'react'
import type { ServicioPasajero } from '../modelos/operacion'
import { marcarUbicacionCompartidaVista, ubicacionCompartidaVista } from '../servicios/ubicacionesVistas'
import { BotonNavegar } from './BotonNavegar'
import '../estilos/componentes/UbicacionesCompartidas.css'

/** Hora (de Colombia) de un instante guardado en UTC, por ejemplo "5:42 a. m.". */
function horaColombia(instante: string): string {
  return new Date(instante).toLocaleTimeString('es-CO', { timeZone: 'America/Bogota', hour: 'numeric', minute: '2-digit' })
}

/**
 * Tarjeta desplegable del conductor con la ubicación que el propio pasajero
 * compartió para esta ruta (no la que guarda el conductor). Al tocar la
 * ubicación aparece la opción de navegar hasta ella. El aviso con el número
 * solo se muestra mientras el conductor no la ha abierto. Solo muestra y
 * enlaza: no pide ni guarda ubicaciones.
 */
export function UbicacionesCompartidas({ pasajero }: { pasajero: ServicioPasajero }) {
  const [abierta, setAbierta] = useState(false)
  const [elegida, setElegida] = useState(false)
  const [vista, setVista] = useState(() => ubicacionCompartidaVista(pasajero.servicioPasajeroId, pasajero.fechaHoraUbicacionCompartida))
  const compartida =
    pasajero.latitudCompartida !== null && pasajero.longitudCompartida !== null
      ? { latitud: pasajero.latitudCompartida, longitud: pasajero.longitudCompartida }
      : null

  // Si el pasajero comparte una ubicación nueva, vuelve a contar como no vista.
  const [fechaVista, setFechaVista] = useState(pasajero.fechaHoraUbicacionCompartida)
  if (fechaVista !== pasajero.fechaHoraUbicacionCompartida) {
    setFechaVista(pasajero.fechaHoraUbicacionCompartida)
    setVista(ubicacionCompartidaVista(pasajero.servicioPasajeroId, pasajero.fechaHoraUbicacionCompartida))
  }

  function alAlternar() {
    if (!abierta && compartida) {
      marcarUbicacionCompartidaVista(pasajero.servicioPasajeroId, pasajero.fechaHoraUbicacionCompartida)
      setVista(true)
    }
    setAbierta((v) => !v)
  }

  return (
    <div className="ubicaciones-compartidas">
      <button type="button" className="ubicaciones-compartidas__titulo" onClick={alAlternar} aria-expanded={abierta}>
        <span>
          📡 Ubicaciones compartidas
          {compartida && !vista && <span className="ubicaciones-compartidas__insignia">1</span>}
        </span>
        <span className="ubicaciones-compartidas__flecha">{abierta ? '▲' : '▼'}</span>
      </button>

      {abierta && !compartida && <p className="ubicaciones-compartidas__vacio">El pasajero todavía no ha compartido su ubicación.</p>}

      {abierta && compartida && (
        <div className="ubicaciones-compartidas__contenido">
          <button type="button" className="ubicaciones-compartidas__ubicacion" onClick={() => setElegida((v) => !v)} aria-expanded={elegida}>
            <strong>📍 Ubicación de {pasajero.nombreCompletoEmpleado}</strong>
            <small>
              {pasajero.fechaHoraUbicacionCompartida && `Compartida a las ${horaColombia(pasajero.fechaHoraUbicacionCompartida)} · `}
              {compartida.latitud.toFixed(5)}, {compartida.longitud.toFixed(5)}
            </small>
          </button>
          {elegida && (
            <BotonNavegar destino={compartida} className="ubicaciones-compartidas__navegar">
              🧭 Navegar
            </BotonNavegar>
          )}
        </div>
      )}
    </div>
  )
}
