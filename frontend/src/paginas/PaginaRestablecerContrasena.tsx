import { useState, type FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { BotonPrimario } from '../componentes/BotonPrimario'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { TarjetaAutenticacion } from '../componentes/TarjetaAutenticacion'
import { ErrorApi } from '../servicios/clienteHttp'
import { restablecerContrasena } from '../servicios/servicioAutenticacion'
import '../estilos/paginas/PaginaRestablecerContrasena.css'

/**
 * Destino del enlace enviado por correo para establecer la contraseña: sirve
 * tanto para recuperarla como para que una persona invitada (por ejemplo, el
 * primer coordinador de una empresa) cree su contraseña inicial.
 */
export function PaginaRestablecerContrasena() {
  const [parametros] = useSearchParams()
  const token = parametros.get('token')
  const [nuevaContrasena, setNuevaContrasena] = useState('')
  const [confirmacionContrasena, setConfirmacionContrasena] = useState('')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)
  const [completado, setCompletado] = useState(false)

  async function alEnviar(evento: FormEvent) {
    evento.preventDefault()
    if (!token) return

    setMensajeError(null)

    if (nuevaContrasena !== confirmacionContrasena) {
      setMensajeError('Las contraseñas no coinciden.')
      return
    }

    setEnviando(true)

    try {
      await restablecerContrasena({ token, nuevaContrasena, confirmacionContrasena })
      setCompletado(true)
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo establecer la contraseña.')
    } finally {
      setEnviando(false)
    }
  }

  if (!token) {
    return (
      <TarjetaAutenticacion titulo="Establecer contraseña">
        <MensajeAlerta tipo="error">El enlace no es válido.</MensajeAlerta>
      </TarjetaAutenticacion>
    )
  }

  if (completado) {
    return (
      <TarjetaAutenticacion titulo="Contraseña actualizada">
        <MensajeAlerta tipo="exito">
          Tu contraseña quedó establecida. Ya puedes <Link to="/iniciar-sesion">iniciar sesión</Link>.
        </MensajeAlerta>
      </TarjetaAutenticacion>
    )
  }

  return (
    <TarjetaAutenticacion titulo="Establecer contraseña" subtitulo="Elige la contraseña con la que vas a ingresar a LFMova.">
      <form className="pagina-restablecer-contrasena__formulario" onSubmit={alEnviar}>
        <CampoFormulario
          id="nuevaContrasena"
          etiqueta="Nueva contraseña"
          tipo="password"
          valor={nuevaContrasena}
          alCambiar={setNuevaContrasena}
          autoCompletar="new-password"
          requerido
        />
        <CampoFormulario
          id="confirmacionContrasena"
          etiqueta="Confirmar contraseña"
          tipo="password"
          valor={confirmacionContrasena}
          alCambiar={setConfirmacionContrasena}
          autoCompletar="new-password"
          requerido
        />

        {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

        <BotonPrimario disabled={enviando}>{enviando ? 'Guardando…' : 'Guardar contraseña'}</BotonPrimario>
      </form>
    </TarjetaAutenticacion>
  )
}
