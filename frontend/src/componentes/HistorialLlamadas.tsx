import { useEffect, useState } from 'react'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { enlaceNavegacion } from '../servicios/navegacion'
import { obtenerLlamadas, type ReferenciaPasajero, type RegistroLlamada } from '../servicios/servicioLlamadas'
import '../estilos/componentes/HistorialLlamadas.css'

interface PropiedadesHistorialLlamadas {
  pasajero: ReferenciaPasajero
  /** Si es plegable, muestra un botón y solo consulta las llamadas al abrirlo. */
  plegable?: boolean
}

/** El instante llega en UTC sin zona explícita; se muestra en la hora local del dispositivo. */
function fechaHoraLocal(iso: string): string {
  const conZona = /[zZ]|[+-]\d{2}:?\d{2}$/.test(iso) ? iso : `${iso}Z`
  return new Date(conZona).toLocaleString('es', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

function textoDuracion(segundos: number | null): string {
  if (segundos === null) return 'duración sin registrar'
  return segundos >= 60 ? `unos ${Math.floor(segundos / 60)} min ${segundos % 60} s` : `unos ${segundos} s`
}

/**
 * Historial de las llamadas que el conductor hizo a un pasajero: cuándo
 * pulsó "Llamar", cuánto duró aproximadamente y desde dónde. Lo ven el
 * conductor, el propio pasajero y el coordinador. Solo consulta: quién puede
 * verlo lo decide el backend.
 */
export function HistorialLlamadas({ pasajero, plegable = false }: PropiedadesHistorialLlamadas) {
  const { token } = useAutenticacion()
  const [abierto, setAbierto] = useState(!plegable)
  const [llamadas, setLlamadas] = useState<RegistroLlamada[] | null>(null)
  const [fallo, setFallo] = useState(false)
  const { empresaId, jornadaId, servicioId, servicioPasajeroId } = pasajero

  useEffect(() => {
    if (!abierto || !token) return
    let cancelado = false
    setFallo(false)
    obtenerLlamadas({ empresaId, jornadaId, servicioId, servicioPasajeroId }, token)
      .then((lista) => {
        if (!cancelado) setLlamadas(lista)
      })
      .catch(() => {
        if (!cancelado) setFallo(true)
      })
    return () => {
      cancelado = true
    }
  }, [abierto, token, empresaId, jornadaId, servicioId, servicioPasajeroId])

  return (
    <div className="historial-llamadas">
      {plegable && (
        <button type="button" className="historial-llamadas__alternar" onClick={() => setAbierto((v) => !v)} aria-expanded={abierto}>
          📞 Llamadas del conductor {abierto ? '▲' : '▼'}
        </button>
      )}
      {abierto && (
        <div className="historial-llamadas__lista" role="list">
          {fallo && <p className="historial-llamadas__vacio">No se pudo cargar el historial de llamadas.</p>}
          {!fallo && llamadas === null && <p className="historial-llamadas__vacio">Cargando…</p>}
          {llamadas?.length === 0 && <p className="historial-llamadas__vacio">No hay llamadas registradas del conductor a este pasajero.</p>}
          {llamadas?.map((llamada) => (
            <div key={llamada.registroLlamadaId} className="historial-llamadas__llamada" role="listitem">
              <span>
                📞 <strong>{fechaHoraLocal(llamada.fechaHora)}</strong> · {textoDuracion(llamada.duracionAproximadaSegundos)}
              </span>
              {llamada.latitud !== null && llamada.longitud !== null && (
                <a href={enlaceNavegacion({ latitud: llamada.latitud, longitud: llamada.longitud })} target="_blank" rel="noreferrer">
                  📍 Dónde estaba
                </a>
              )}
            </div>
          ))}
          {llamadas && llamadas.length > 0 && (
            <p className="historial-llamadas__nota">La duración es aproximada e incluye el tiempo que timbró; no indica si el pasajero contestó.</p>
          )}
        </div>
      )}
    </div>
  )
}
