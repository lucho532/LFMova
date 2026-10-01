import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ChatPasajero } from '../componentes/ChatPasajero'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { useNotificaciones } from '../contexto/useNotificaciones'
import { EstadoPasajero, NOMBRES_ESTADO_PASAJERO, nombreDe } from '../modelos/enumeraciones'
import type { ServicioPasajero } from '../modelos/operacion'
import { ErrorApi } from '../servicios/clienteHttp'
import { enviarMensaje, obtenerMensajes } from '../servicios/servicioConductorPropio'
import { obtenerPasajeros } from '../servicios/servicioOperacion'
import { obtenerUsuarioIdDelToken } from '../servicios/tokenJwt'
import './PaginaChat.css'

/**
 * Chat del conductor con un pasajero (a donde lleva una notificación): datos
 * del pasajero arriba, la conversación y, dentro del chat, el desplegable con
 * los mensajes predeterminados para no escribir mientras conduce.
 */
export function PaginaChatConductor() {
  const { empresaId, jornadaId, servicioId, servicioPasajeroId } = useParams<{ empresaId: string; jornadaId: string; servicioId: string; servicioPasajeroId: string }>()
  const { token } = useAutenticacion()
  const { version } = useNotificaciones()
  const referencia = { empresaId: Number(empresaId), jornadaId: Number(jornadaId), servicioId: Number(servicioId) }
  const idPasajero = Number(servicioPasajeroId)
  const [pasajero, setPasajero] = useState<ServicioPasajero | null>(null)
  const [mensajeError, setMensajeError] = useState<string | null>(null)

  // Se recarga cuando llega una notificación nueva (por ejemplo, que el pasajero confirmó o avisó que no
  // asistirá), para que el estado que se ve aquí arriba nunca quede desactualizado.
  useEffect(() => {
    if (!token) return
    obtenerPasajeros(referencia.empresaId, referencia.jornadaId, referencia.servicioId, token)
      .then((lista) => setPasajero(lista.find((p) => p.servicioPasajeroId === idPasajero) ?? null))
      .catch((error) => setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar el pasajero.'))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token, empresaId, jornadaId, servicioId, servicioPasajeroId, version])

  if (!token) return null

  return (
    <main className="pagina-chat">
      <Link to={`/conductor/servicios/${empresaId}/${jornadaId}/${servicioId}`} className="pagina-chat__volver">
        ← Volver al servicio
      </Link>

      <header className="pagina-chat__cabecera">
        <span className="pagina-chat__etiqueta">Chat con el pasajero</span>
        <h1>{pasajero?.nombreCompletoEmpleado ?? 'Pasajero'}</h1>
        {pasajero && (
          <p>
            {pasajero.direccionRecogida}
            {pasajero.barrioEmpleado ? ` · ${pasajero.barrioEmpleado}` : ''} · {nombreDe(NOMBRES_ESTADO_PASAJERO, pasajero.estado)}
          </p>
        )}
      </header>

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      {pasajero?.estado === EstadoPasajero.NO_ASISTIRA && <MensajeAlerta tipo="error">🚫 Este pasajero avisó que no asistirá.</MensajeAlerta>}
      {pasajero?.estado === EstadoPasajero.CONFIRMADO && <MensajeAlerta tipo="exito">✅ Este pasajero confirmó su asistencia.</MensajeAlerta>}

      <ChatPasajero
        miUsuarioId={obtenerUsuarioIdDelToken(token)}
        cargarMensajes={() => obtenerMensajes(referencia, idPasajero, token)}
        enviar={(contenido) => enviarMensaje(referencia, idPasajero, contenido, token)}
        conRespuestasRapidas
      />
    </main>
  )
}
