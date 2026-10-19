import { useState } from 'react'
import '../estilos/componentes/RespuestasRapidas.css'

/** Mensajes predeterminados del conductor, para avisar al pasajero con un toque sin escribir mientras conduce. */
export const MENSAJES_RAPIDOS_CONDUCTOR = ['Estoy en camino', 'Voy un poco tarde', 'Estoy llegando', 'Puedes salir', 'He llegado', 'Gracias']

interface PropiedadesRespuestasRapidas {
  /** Envía el mensaje al pasajero por el chat de la app. */
  enviar: (contenido: string) => Promise<unknown>
}

/**
 * Desplegable (cerrado por defecto para no agrandar la tarjeta) con botones
 * grandes de mensajes ya escritos. Un toque envía el mensaje por el chat del
 * pasajero y confirma que salió; si falla, muestra el motivo.
 */
export function RespuestasRapidas({ enviar }: PropiedadesRespuestasRapidas) {
  const [abierto, setAbierto] = useState(false)
  const [enviando, setEnviando] = useState<string | null>(null)
  const [aviso, setAviso] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(null)

  async function alEnviar(mensaje: string) {
    setEnviando(mensaje)
    setAviso(null)
    try {
      await enviar(mensaje)
      setAviso({ tipo: 'exito', texto: `Enviado: «${mensaje}»` })
    } catch (error) {
      setAviso({ tipo: 'error', texto: error instanceof Error ? error.message : 'No se pudo enviar el mensaje.' })
    } finally {
      setEnviando(null)
    }
  }

  return (
    <div className="respuestas-rapidas">
      <button type="button" className="respuestas-rapidas__desplegar" onClick={() => setAbierto((valor) => !valor)} aria-expanded={abierto}>
        <span>💬 Avisar al pasajero</span>
        <span className={`respuestas-rapidas__flecha${abierto ? ' respuestas-rapidas__flecha--abierta' : ''}`} aria-hidden="true">
          ▾
        </span>
      </button>
      {abierto && (
        <div className="respuestas-rapidas__botones">
          {MENSAJES_RAPIDOS_CONDUCTOR.map((mensaje) => (
            <button key={mensaje} type="button" className="respuestas-rapidas__boton" disabled={enviando !== null} onClick={() => alEnviar(mensaje)}>
              {enviando === mensaje ? 'Enviando…' : mensaje}
            </button>
          ))}
        </div>
      )}
      {aviso && (
        <p className={`respuestas-rapidas__aviso respuestas-rapidas__aviso--${aviso.tipo}`} role={aviso.tipo === 'error' ? 'alert' : 'status'}>
          {aviso.texto}
        </p>
      )}
    </div>
  )
}
