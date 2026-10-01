import { Fragment, useEffect, useState, type FormEvent } from 'react'
import type { Mensaje } from '../modelos/mensaje'
import { ErrorApi } from '../servicios/clienteHttp'
import { BotonPrimario } from './BotonPrimario'
import { BotonSecundario } from './BotonSecundario'
import { RespuestasRapidas } from './RespuestasRapidas'
import { MensajeAlerta } from './MensajeAlerta'
import '../estilos/componentes/ChatPasajero.css'

interface PropiedadesChatPasajero {
  miUsuarioId: number | null
  cargarMensajes: () => Promise<Mensaje[]>
  enviar: (contenido: string) => Promise<Mensaje>
  /** El conductor ve, dentro del chat, el desplegable con mensajes predeterminados de un toque. */
  conRespuestasRapidas?: boolean
  /** La ruta ya finalizó: se puede seguir viendo la conversación, pero no enviar más mensajes. */
  soloLectura?: boolean
}

const MESES_CORTOS = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic']

/** Clave de día en hora local del dispositivo (los mensajes llegan en UTC). */
function claveDia(fecha: Date): string {
  return `${fecha.getFullYear()}-${fecha.getMonth()}-${fecha.getDate()}`
}

function etiquetaDia(fecha: Date): string {
  const hoy = new Date()
  const ayer = new Date()
  ayer.setDate(hoy.getDate() - 1)
  if (claveDia(fecha) === claveDia(hoy)) return 'Hoy'
  if (claveDia(fecha) === claveDia(ayer)) return 'Ayer'
  const anio = fecha.getFullYear() === hoy.getFullYear() ? '' : ` ${fecha.getFullYear()}`
  return `${fecha.getDate()} ${MESES_CORTOS[fecha.getMonth()]}${anio}`
}

/** Hora del mensaje; si es de un día distinto a hoy, se antepone la fecha. */
function etiquetaHora(fecha: Date): string {
  const hora = fecha.toLocaleTimeString('es', { hour: '2-digit', minute: '2-digit' })
  return claveDia(fecha) === claveDia(new Date()) ? hora : `${fecha.getDate()} ${MESES_CORTOS[fecha.getMonth()]} · ${hora}`
}

/**
 * Conversación individual (conductor ↔ empleado) de un pasajero. No es un chat
 * grupal ni en tiempo real: se carga al abrir y se puede refrescar a mano.
 * Las horas llegan en UTC y se muestran en la hora local del dispositivo.
 */
export function ChatPasajero({ miUsuarioId, cargarMensajes, enviar, conRespuestasRapidas = false, soloLectura = false }: PropiedadesChatPasajero) {
  const [mensajes, setMensajes] = useState<Mensaje[]>([])
  const [texto, setTexto] = useState('')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [ocupado, setOcupado] = useState(false)

  async function refrescar() {
    try {
      setMensajes(await cargarMensajes())
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar la conversación.')
    }
  }

  useEffect(() => {
    refrescar()
    // El chat no es en tiempo real: mientras está abierto se consulta cada pocos segundos para ver los mensajes nuevos.
    const temporizador = setInterval(() => {
      if (!document.hidden) refrescar()
    }, 5000)
    return () => clearInterval(temporizador)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  async function alEnviar(evento: FormEvent) {
    evento.preventDefault()
    if (!texto.trim()) return
    setOcupado(true)
    setMensajeError(null)
    try {
      await enviar(texto.trim())
      setTexto('')
      await refrescar()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo enviar el mensaje.')
    } finally {
      setOcupado(false)
    }
  }

  return (
    <div className="chat-pasajero">
      <ul className="chat-pasajero__mensajes">
        {mensajes.length === 0 && <li className="chat-pasajero__vacio">Todavía no hay mensajes.</li>}
        {mensajes.map((mensaje, indice) => {
          const fecha = new Date(mensaje.fechaHora)
          const anterior = indice > 0 ? new Date(mensajes[indice - 1].fechaHora) : null
          const cambiaDia = anterior === null || claveDia(anterior) !== claveDia(fecha)
          return (
            <Fragment key={mensaje.mensajeId}>
              {cambiaDia && <li className="chat-pasajero__dia">{etiquetaDia(fecha)}</li>}
              <li className={mensaje.usuarioId === miUsuarioId ? 'chat-pasajero__propio' : 'chat-pasajero__ajeno'}>
                <span>{mensaje.contenido}</span>
                <time dateTime={mensaje.fechaHora}>{etiquetaHora(fecha)}</time>
              </li>
            </Fragment>
          )
        })}
      </ul>
      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      {soloLectura ? (
        <p className="chat-pasajero__solo-lectura">Esta ruta ya finalizó: el chat quedó cerrado, solo puedes ver los mensajes.</p>
      ) : (
        <>
          {conRespuestasRapidas && (
            <RespuestasRapidas
              enviar={async (mensaje) => {
                await enviar(mensaje)
                await refrescar()
              }}
            />
          )}
          <form className="chat-pasajero__formulario" onSubmit={alEnviar}>
            <input value={texto} onChange={(evento) => setTexto(evento.target.value)} placeholder="Escribe un mensaje…" aria-label="Mensaje" />
            <BotonPrimario disabled={ocupado}>Enviar</BotonPrimario>
            <BotonSecundario onClick={refrescar}>Actualizar</BotonSecundario>
          </form>
        </>
      )}
    </div>
  )
}
