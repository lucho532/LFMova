import { useEffect, useRef, useState } from 'react'
import { useNotificaciones } from '../contexto/useNotificaciones'
import { EstadoPasajero, EstadoServicio, NOMBRES_ESTADO_PASAJERO, NOMBRES_ESTADO_SERVICIO, NOMBRES_TIPO_SERVICIO, formatearHora, nombreDe } from '../modelos/enumeraciones'
import type { Mensaje } from '../modelos/mensaje'
import type { ServicioDelEmpleado } from '../modelos/servicioEmpleado'
import type { DatosConfirmacion } from '../servicios/servicioEmpleadoPropio'
import { VentanaChat } from './VentanaChat'
import { CronometroEspera } from './CronometroEspera'
import { MensajeAlerta } from './MensajeAlerta'
import './TarjetaServicioEmpleado.css'

/** Las horas reales llegan en UTC; se muestran en la hora local del dispositivo. */
function horaLocal(iso: string | null): string {
  if (!iso) return '—'
  const conZona = /[zZ]|[+-]\d{2}:?\d{2}$/.test(iso) ? iso : `${iso}Z`
  return new Date(conZona).toLocaleTimeString('es', { hour: '2-digit', minute: '2-digit' })
}

/** El enlace de una notificación de chat termina en el servicioPasajeroId de esa conversación. */
function esMensajeDelPasajero(enlace: string | null, servicioPasajeroId: number): boolean {
  if (!enlace) return false
  const partes = enlace.split('/')
  return Number(partes[partes.length - 1]) === servicioPasajeroId
}

interface PropiedadesTarjetaServicioEmpleado {
  servicio: ServicioDelEmpleado
  miUsuarioId: number | null
  confirmar: (datos: DatosConfirmacion) => Promise<void>
  noAsistire: () => Promise<void>
  compartirUbicacion: (latitud: number, longitud: number) => Promise<void>
  cargarMensajes: () => Promise<Mensaje[]>
  enviarMensaje: (contenido: string) => Promise<Mensaje>
  /** Abre el chat de entrada (por ejemplo, al llegar desde una notificación). */
  chatAbiertoInicial?: boolean
}

/**
 * Servicio de transporte del empleado: qué le toca, en qué estado va y lo
 * que puede hacer (confirmar asistencia, avisar que no asistirá, compartir su
 * ubicación y escribirle al conductor). Muestra quién lo va a recoger (sin su teléfono: la comunicación es solo por el chat). El backend valida cada acción.
 */
export function TarjetaServicioEmpleado({ servicio, miUsuarioId, confirmar, noAsistire, compartirUbicacion, cargarMensajes, enviarMensaje, chatAbiertoInicial = false }: PropiedadesTarjetaServicioEmpleado) {
  const { notificaciones, marcarLeida } = useNotificaciones()
  const [aviso, setAviso] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(null)
  const [ocupado, setOcupado] = useState(false)
  const [chatAbierto, setChatAbierto] = useState(chatAbiertoInicial)
  const tarjeta = useRef<HTMLLIElement>(null)
  const mensajesSinLeer = notificaciones.filter(
    (n) => !n.leida && n.tipo === 'MENSAJE_NUEVO' && esMensajeDelPasajero(n.enlace, servicio.servicioPasajeroId),
  )

  function alAbrirChat() {
    mensajesSinLeer.forEach((n) => marcarLeida(n.notificacionId).catch(() => {}))
    setChatAbierto(true)
  }

  useEffect(() => {
    if (chatAbiertoInicial) {
      setChatAbierto(true)
      tarjeta.current?.scrollIntoView({ behavior: 'smooth', block: 'center' })
    }
  }, [chatAbiertoInicial])

  const estado = servicio.estadoServicioPasajero
  const finalizado = servicio.estadoServicio === EstadoServicio.FINALIZADO
  // El conductor ya resolvió a este empleado (lo recogió o no lo pudo recoger): no hace falta esperar a
  // que finalice el resto de la ruta para que su tarjeta se vea como terminada.
  const procesado = estado === EstadoPasajero.RECOGIDO || estado === EstadoPasajero.NO_RECOGIDO
  const puedeResponder = !finalizado && (estado === EstadoPasajero.PROGRAMADO || estado === EstadoPasajero.CONFIRMADO || estado === EstadoPasajero.NO_ASISTIRA)

  async function ejecutar(accion: () => Promise<void>, exito: string, fallo: string) {
    setOcupado(true)
    setAviso(null)
    try {
      await accion()
      setAviso({ tipo: 'exito', texto: exito })
    } catch (error) {
      setAviso({ tipo: 'error', texto: error instanceof Error ? error.message : fallo })
    } finally {
      setOcupado(false)
    }
  }

  async function alConfirmar() {
    setOcupado(true)
    setAviso(null)
    try {
      await confirmar({})
      // Sin aviso de éxito aquí: la tarjeta entera se pinta de verde cuando el estado queda Confirmado, así que sería redundante.
    } catch (error) {
      setAviso({ tipo: 'error', texto: error instanceof Error ? error.message : 'No se pudo confirmar.' })
    } finally {
      setOcupado(false)
    }
  }

  function alCompartirUbicacion() {
    ejecutar(
      () =>
        new Promise<void>((resolver, rechazar) => {
          if (!navigator.geolocation) {
            rechazar(new Error('Este dispositivo no permite obtener la ubicación.'))
            return
          }
          navigator.geolocation.getCurrentPosition(
            (posicion) => compartirUbicacion(posicion.coords.latitude, posicion.coords.longitude).then(resolver, rechazar),
            () => rechazar(new Error('No se pudo obtener tu ubicación. Revisa el permiso de ubicación.')),
            { enableHighAccuracy: true, timeout: 15000 },
          )
        }),
      'Ubicación compartida con tu conductor.',
      'No se pudo compartir la ubicación.',
    )
  }

  return (
    <li className={`tarjeta-servicio-empleado${estado === EstadoPasajero.CONFIRMADO ? ' tarjeta-servicio-empleado--confirmado' : ''}`} ref={tarjeta}>
      <div className="tarjeta-servicio-empleado__cabecera">
        <strong>
          {servicio.fecha} · {formatearHora(servicio.horaProgramada)}
        </strong>
        <span>{nombreDe(NOMBRES_TIPO_SERVICIO, servicio.tipo)}</span>
      </div>
      <p>📍 {servicio.direccionRecogida}</p>
      <div className="tarjeta-servicio-empleado__conductor">
        <span className="tarjeta-servicio-empleado__conductor-etiqueta">Te recoge</span>
        {servicio.conductorNombre ? (
          <>
            <strong>{servicio.conductorNombre}</strong>
            {servicio.placa && <span>Vehículo {servicio.placa}</span>}
            <span className="tarjeta-servicio-empleado__aviso-chat">Para comunicarte con tu conductor usa el chat de la app.</span>
          </>
        ) : (
          <span>Aún no tiene conductor asignado</span>
        )}
      </div>
      <p className="tarjeta-servicio-empleado__estado">
        Servicio: {nombreDe(NOMBRES_ESTADO_SERVICIO, servicio.estadoServicio)} · Tú: <strong>{nombreDe(NOMBRES_ESTADO_PASAJERO, estado)}</strong>
      </p>
      <CronometroEspera
        horaLlegadaConductor={servicio.horaLlegadaConductor}
        activo={servicio.estadoServicio === EstadoServicio.EN_CURSO && estado === EstadoPasajero.CONDUCTOR_LLEGO}
      />
      {finalizado ? (
        <p className="tarjeta-servicio-empleado__tiempos">
          Ruta iniciada a las <b>{horaLocal(servicio.horaInicioReal)}</b> · finalizada a las <b>{horaLocal(servicio.horaFinReal)}</b>
        </p>
      ) : (
        procesado && (
          <p className="tarjeta-servicio-empleado__tiempos">
            {estado === EstadoPasajero.RECOGIDO ? 'Te recogieron' : 'Quedaste como no recogido'} a las <b>{horaLocal(servicio.horaProcesado)}</b>
          </p>
        )
      )}

      {puedeResponder && (
        <>
          <div className="tarjeta-servicio-empleado__botones">
            <button
              type="button"
              className={`tarjeta-servicio-empleado__boton${estado === EstadoPasajero.CONFIRMADO ? ' tarjeta-servicio-empleado__boton--confirmado' : ''}`}
              disabled={ocupado}
              onClick={alConfirmar}
            >
              ✅ Confirmar asistencia
            </button>
            <button
              type="button"
              className={`tarjeta-servicio-empleado__boton${estado === EstadoPasajero.NO_ASISTIRA ? ' tarjeta-servicio-empleado__boton--no-asistira' : ''}`}
              disabled={ocupado}
              onClick={() => ejecutar(noAsistire, 'Le avisamos a tu conductor que no asistirás.', 'No se pudo registrar.')}
            >
              🚫 No asistiré
            </button>
          </div>
        </>
      )}

      <div className="tarjeta-servicio-empleado__botones">
        {!finalizado && !procesado && (
          <button type="button" className="tarjeta-servicio-empleado__boton" disabled={ocupado} onClick={alCompartirUbicacion}>
            📡 Compartir mi ubicación
          </button>
        )}
        <button type="button" className="tarjeta-servicio-empleado__boton tarjeta-servicio-empleado__boton-chat" onClick={alAbrirChat}>
          💬 Chat con el conductor
          {mensajesSinLeer.length > 0 && <span className="tarjeta-servicio-empleado__insignia">{mensajesSinLeer.length > 9 ? '9+' : mensajesSinLeer.length}</span>}
        </button>
      </div>

      {aviso && <MensajeAlerta tipo={aviso.tipo}>{aviso.texto}</MensajeAlerta>}
      <VentanaChat abierto={chatAbierto} miUsuarioId={miUsuarioId} cargarMensajes={cargarMensajes} enviar={enviarMensaje} soloLectura={finalizado} alCerrar={() => setChatAbierto(false)} />
    </li>
  )
}
