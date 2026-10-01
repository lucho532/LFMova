import { useEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { useNavigate } from 'react-router-dom'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { useNotificaciones } from '../contexto/useNotificaciones'
import type { Notificacion } from '../modelos/notificacion'
import './CampanaNotificaciones.css'

/**
 * Confirmaciones y llegada del conductor en verde; avisos de que no asistirá y de ruta no iniciada en rojo;
 * mensajes nuevos en azul; el resto usa el estilo normal (y no sale como letrero emergente).
 */
function claseTipo(tipo: string): string {
  if (tipo === 'PASAJERO_CONFIRMO' || tipo === 'CONDUCTOR_LLEGO') return 'campana-notificaciones__tipo--verde'
  if (tipo === 'PASAJERO_NO_ASISTIRA' || tipo === 'ALERTA_RUTA_NO_INICIADA') return 'campana-notificaciones__tipo--rojo'
  if (tipo === 'MENSAJE_NUEVO') return 'campana-notificaciones__tipo--azul'
  return ''
}

/** Icono del letrero emergente según el tipo de notificación. */
function iconoAviso(tipo: string): string {
  if (tipo === 'CONDUCTOR_LLEGO') return '🚗 '
  if (tipo === 'MENSAJE_NUEVO') return '💬 '
  if (tipo === 'ALERTA_RUTA_NO_INICIADA') return '⏰ '
  return tipo === 'PASAJERO_CONFIRMO' ? '✔ ' : '✖ '
}

/** Tiempo que un letrero emergente permanece visible antes de cerrarse solo. */
const DURACION_AVISO_MS = 6000

/**
 * Campana de la barra superior con las notificaciones del usuario (nuevo
 * servicio, cambios, alertas de ruta no iniciada, incidencias…). Las
 * notificaciones y su consulta periódica vienen de `ProveedorNotificaciones`
 * (compartido con el resto de la app); esta campana solo las muestra y, al
 * llegar una nueva de respuesta del pasajero, la anuncia como letrero
 * emergente.
 */
export function CampanaNotificaciones() {
  const { token } = useAutenticacion()
  const { notificaciones, marcarLeida } = useNotificaciones()
  const [abierta, setAbierta] = useState(false)
  const contenedor = useRef<HTMLDivElement>(null)
  const vistas = useRef<Set<number> | null>(null)
  const [avisos, setAvisos] = useState<Notificacion[]>([])
  const navegar = useNavigate()
  const temporizadoresAviso = useRef<number[]>([])

  // Tras la primera carga, lo que llega nuevo y es una respuesta del pasajero se muestra como letrero
  // emergente (verde o rojo), que se cierra solo pasados unos segundos (o antes, si la persona lo cierra a mano).
  useEffect(() => {
    if (vistas.current !== null) {
      const nuevas = notificaciones.filter((n) => !n.leida && !vistas.current!.has(n.notificacionId) && claseTipo(n.tipo) !== '')
      if (nuevas.length > 0) {
        setAvisos((actuales) => [...nuevas, ...actuales].slice(0, 3))
        nuevas.forEach((aviso) => {
          const id = window.setTimeout(() => cerrarAviso(aviso.notificacionId), DURACION_AVISO_MS)
          temporizadoresAviso.current.push(id)
        })
      }
    }
    vistas.current = new Set(notificaciones.map((n) => n.notificacionId))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [notificaciones])

  useEffect(() => () => temporizadoresAviso.current.forEach(clearTimeout), [])

  useEffect(() => {
    function alHacerClicFuera(evento: MouseEvent) {
      if (contenedor.current && !contenedor.current.contains(evento.target as Node)) setAbierta(false)
    }
    document.addEventListener('mousedown', alHacerClicFuera)
    return () => document.removeEventListener('mousedown', alHacerClicFuera)
  }, [])

  if (!token) return null

  const noLeidas = notificaciones.filter((n) => !n.leida)
  const sinLeer = noLeidas.length

  async function alMarcar(notificacion: Notificacion) {
    // Si la notificación trae enlace, se lleva directo a donde se puede responder (por ejemplo, el chat del remitente).
    if (notificacion.enlace) {
      setAbierta(false)
      navegar(notificacion.enlace)
    }

    if (notificacion.leida) return
    try {
      await marcarLeida(notificacion.notificacionId)
    } catch {
      // Se queda sin marcar; el usuario puede reintentar.
    }
  }

  function cerrarAviso(id: number) {
    setAvisos((actuales) => actuales.filter((a) => a.notificacionId !== id))
  }

  return (
    <div className="campana-notificaciones" ref={contenedor}>
      {avisos.length > 0 &&
        createPortal(
          // En un portal a document.body: así nunca queda atrapado detrás del contenido de la página por
          // culpa de algún ancestro con su propio z-index (por ejemplo, la barra superior).
          <div className="campana-notificaciones__avisos" aria-live="assertive">
            {avisos.map((aviso) => (
              <div key={aviso.notificacionId} className={`campana-notificaciones__aviso ${claseTipo(aviso.tipo)}`} role="alert">
                <button
                  type="button"
                  className="campana-notificaciones__aviso-cuerpo"
                  onClick={() => {
                    cerrarAviso(aviso.notificacionId)
                    alMarcar(aviso)
                  }}
                >
                  <strong>{iconoAviso(aviso.tipo)}{aviso.titulo}</strong>
                  <span>{aviso.mensaje}</span>
                </button>
                <button type="button" className="campana-notificaciones__aviso-cerrar" onClick={() => cerrarAviso(aviso.notificacionId)} aria-label="Cerrar aviso">
                  ×
                </button>
              </div>
            ))}
          </div>,
          document.body,
        )}
      <button type="button" className="campana-notificaciones__boton" onClick={() => setAbierta((valor) => !valor)} aria-label={`Notificaciones${sinLeer ? `, ${sinLeer} sin leer` : ''}`}>
        🔔
        {sinLeer > 0 && <span className="campana-notificaciones__contador">{sinLeer > 9 ? '9+' : sinLeer}</span>}
      </button>
      {abierta && (
        <div className="campana-notificaciones__panel" role="dialog" aria-label="Notificaciones">
          <h3>Notificaciones</h3>
          {noLeidas.length === 0 ? (
            <p className="campana-notificaciones__vacio">No tienes notificaciones pendientes.</p>
          ) : (
            <ul>
              {noLeidas.slice(0, 30).map((n) => (
                <li key={n.notificacionId} className={`campana-notificaciones__nueva ${claseTipo(n.tipo)}`}>
                  <button type="button" onClick={() => alMarcar(n)}>
                    <strong>{n.titulo}</strong>
                    <span>{n.mensaje}</span>
                    <small>
                      {new Date(n.fechaHora).toLocaleString()}
                      {n.enlace ? ' · Toca para responder' : ''}
                    </small>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  )
}
