import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { TarjetaAutenticacion } from '../componentes/TarjetaAutenticacion'
import { ErrorApi } from '../servicios/clienteHttp'
import { confirmarCorreo } from '../servicios/servicioAutenticacion'

type EstadoConfirmacion = 'confirmando' | 'confirmado' | 'error'

/**
 * Destino del enlace enviado por correo al registrarse: toma el token de la
 * URL (`?token=`), lo envía a la API y muestra el resultado.
 */
export function PaginaConfirmarCorreo() {
  const [parametros] = useSearchParams()
  const token = parametros.get('token')
  const [estado, setEstado] = useState<EstadoConfirmacion>(token ? 'confirmando' : 'error')
  const [mensajeError, setMensajeError] = useState<string | null>(token ? null : 'El enlace no es válido.')

  useEffect(() => {
    if (!token) return

    let cancelado = false

    confirmarCorreo(token)
      .then(() => {
        if (!cancelado) setEstado('confirmado')
      })
      .catch((error) => {
        if (cancelado) return
        setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo confirmar el correo.')
        setEstado('error')
      })

    return () => {
      cancelado = true
    }
  }, [token])

  return (
    <TarjetaAutenticacion titulo="Confirmar correo">
      {estado === 'confirmando' && <p>Confirmando tu correo…</p>}
      {estado === 'confirmado' && (
        <MensajeAlerta tipo="exito">
          Tu correo quedó confirmado. Ya puedes <Link to="/iniciar-sesion">iniciar sesión</Link>.
        </MensajeAlerta>
      )}
      {estado === 'error' && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
    </TarjetaAutenticacion>
  )
}
