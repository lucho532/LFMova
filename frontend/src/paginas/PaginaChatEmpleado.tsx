import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ChatPasajero } from '../componentes/ChatPasajero'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { ServicioDelEmpleado } from '../modelos/servicioEmpleado'
import { ErrorApi } from '../servicios/clienteHttp'
import { enviarMensajeAlConductor, obtenerMensajesConConductor, obtenerMisServicios } from '../servicios/servicioEmpleadoPropio'
import { obtenerUsuarioIdDelToken } from '../servicios/tokenJwt'
import '../estilos/paginas/PaginaChat.css'

/** Chat del empleado con su conductor (a donde lleva una notificación de mensaje). */
export function PaginaChatEmpleado() {
  const { servicioPasajeroId } = useParams<{ servicioPasajeroId: string }>()
  const { token } = useAutenticacion()
  const [servicio, setServicio] = useState<ServicioDelEmpleado | null>(null)
  const [mensajeError, setMensajeError] = useState<string | null>(null)

  useEffect(() => {
    if (!token) return
    obtenerMisServicios(token)
      .then((lista) => {
        const encontrado = lista.find((s) => s.servicioPasajeroId === Number(servicioPasajeroId)) ?? null
        setServicio(encontrado)
        if (!encontrado) setMensajeError('No se encontró ese servicio en tu transporte.')
      })
      .catch((error) => setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar tu servicio.'))
  }, [token, servicioPasajeroId])

  if (!token) return null

  return (
    <main className="pagina-chat">
      <Link to="/mi-transporte" className="pagina-chat__volver">
        ← Mi transporte
      </Link>

      <header className="pagina-chat__cabecera">
        <span className="pagina-chat__etiqueta">Chat con tu conductor</span>
        <h1>{servicio?.conductorNombre ?? 'Conductor'}</h1>
        {servicio && (
          <p>
            {servicio.fecha} · {servicio.horaProgramada.slice(0, 5)}
            {servicio.placa ? ` · Vehículo ${servicio.placa}` : ''}
          </p>
        )}
      </header>

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

      {servicio && (
        <ChatPasajero
          miUsuarioId={obtenerUsuarioIdDelToken(token)}
          cargarMensajes={() => obtenerMensajesConConductor(servicio, token)}
          enviar={(contenido) => enviarMensajeAlConductor(servicio, contenido, token)}
        />
      )}
    </main>
  )
}
