import { useEffect, useRef } from 'react'
import type { Mensaje } from '../modelos/mensaje'
import { ChatPasajero } from './ChatPasajero'
import './VentanaChat.css'

interface PropiedadesVentanaChat {
  abierto: boolean
  miUsuarioId: number | null
  cargarMensajes: () => Promise<Mensaje[]>
  enviar: (contenido: string) => Promise<Mensaje>
  /** El conductor ve, dentro del chat, el desplegable con mensajes predeterminados de un toque. */
  conRespuestasRapidas?: boolean
  /** La ruta ya finalizó: se puede seguir viendo la conversación, pero no enviar más mensajes. */
  soloLectura?: boolean
  alCerrar: () => void
}

/**
 * Ventana emergente con la conversación de un pasajero: se abre encima de la
 * tarjeta (no empuja el resto del contenido hacia abajo) y se cierra al
 * tocar fuera de ella, con la tecla Escape, o con el botón ×. Usa el
 * elemento nativo `<dialog>`, igual que el resto de ventanas de la app.
 */
export function VentanaChat({ abierto, miUsuarioId, cargarMensajes, enviar, conRespuestasRapidas, soloLectura, alCerrar }: PropiedadesVentanaChat) {
  const referencia = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialogo = referencia.current
    if (!dialogo) return
    if (abierto && !dialogo.open) dialogo.showModal()
    if (!abierto && dialogo.open) dialogo.close()
  }, [abierto])

  return (
    <dialog
      ref={referencia}
      className="ventana-chat"
      onCancel={alCerrar}
      onClose={alCerrar}
      onClick={(evento) => {
        // Un clic directo sobre el <dialog> (no sobre su contenido) es un clic en el fondo: se cierra.
        if (evento.target === evento.currentTarget) alCerrar()
      }}
    >
      <div className="ventana-chat__cabecera">
        <strong>Chat</strong>
        <button type="button" className="ventana-chat__cerrar" onClick={alCerrar} aria-label="Cerrar chat">
          ×
        </button>
      </div>
      {abierto && (
        <ChatPasajero miUsuarioId={miUsuarioId} cargarMensajes={cargarMensajes} enviar={enviar} conRespuestasRapidas={conRespuestasRapidas} soloLectura={soloLectura} />
      )}
    </dialog>
  )
}
