import { useState, type FormEvent } from 'react'
import { BotonPrimario } from '../componentes/BotonPrimario'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { TarjetaAutenticacion } from '../componentes/TarjetaAutenticacion'
import { ErrorApi } from '../servicios/clienteHttp'
import { solicitarRecuperacion } from '../servicios/servicioAutenticacion'
import './PaginaOlvideContrasena.css'

/**
 * Solicita el enlace para restablecer la contraseña. La API responde igual
 * exista o no la cuenta, por lo que el mensaje de éxito es siempre el mismo
 * (no revela qué cédulas o correos están registrados).
 */
export function PaginaOlvideContrasena() {
  const [identificador, setIdentificador] = useState('')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)
  const [solicitado, setSolicitado] = useState(false)

  async function alEnviar(evento: FormEvent) {
    evento.preventDefault()
    setMensajeError(null)
    setEnviando(true)

    try {
      await solicitarRecuperacion(identificador)
      setSolicitado(true)
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo enviar la solicitud.')
    } finally {
      setEnviando(false)
    }
  }

  return (
    <TarjetaAutenticacion titulo="Olvidé mi contraseña" subtitulo="Indica tu cédula o correo y te enviaremos un enlace para crear una contraseña nueva.">
      {solicitado ? (
        <MensajeAlerta tipo="exito">Si la cuenta existe, te enviamos un correo con el enlace. El enlace vence en 2 horas.</MensajeAlerta>
      ) : (
        <form className="pagina-olvide-contrasena__formulario" onSubmit={alEnviar}>
          <CampoFormulario id="identificadorRecuperacion" etiqueta="Cédula o correo electrónico" valor={identificador} alCambiar={setIdentificador} requerido />

          {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

          <BotonPrimario disabled={enviando}>{enviando ? 'Enviando…' : 'Enviar enlace'}</BotonPrimario>
        </form>
      )}
    </TarjetaAutenticacion>
  )
}
