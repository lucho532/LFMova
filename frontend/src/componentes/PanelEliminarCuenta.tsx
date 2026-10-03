import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { ErrorApi } from '../servicios/clienteHttp'
import { eliminarCuenta } from '../servicios/servicioCuenta'
import { CampoFormulario } from './CampoFormulario'
import { MensajeAlerta } from './MensajeAlerta'
import { ModalConfirmacion } from './ModalConfirmacion'
import '../estilos/componentes/PanelEliminarCuenta.css'

/**
 * Sección de «Mi cuenta» para que la persona elimine su propia cuenta:
 * explica qué se borra, pide la contraseña y una confirmación final. Si el
 * servidor la elimina, cierra la sesión (la app vuelve a la pantalla de
 * entrada). Las reglas (ruta en curso, único administrador) las aplica el
 * backend.
 */
export function PanelEliminarCuenta() {
  const { token, cerrarSesion } = useAutenticacion()
  const [contrasena, setContrasena] = useState('')
  const [confirmando, setConfirmando] = useState(false)
  const [ocupado, setOcupado] = useState(false)
  const [mensajeError, setMensajeError] = useState<string | null>(null)

  function alEnviar(evento: FormEvent) {
    evento.preventDefault()
    setMensajeError(null)
    setConfirmando(true)
  }

  async function alConfirmar() {
    setConfirmando(false)
    if (!token) return
    setOcupado(true)
    try {
      await eliminarCuenta(contrasena, token)
      cerrarSesion()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo eliminar la cuenta.')
      setOcupado(false)
    }
  }

  return (
    <section className="panel-eliminar-cuenta">
      <h2>Eliminar mi cuenta</h2>
      <p>
        Se borrará tu cuenta y todo tu rastro en LFMova: tus datos, las rutas en las que fuiste pasajero, tus mensajes, incidencias y, si eres
        conductor, tus vehículos. No se puede deshacer. <Link to="/eliminar-cuenta">Ver el detalle</Link>.
      </p>
      <form onSubmit={alEnviar}>
        <CampoFormulario
          id="eliminarCuentaContrasena"
          etiqueta="Escribe tu contraseña para confirmar"
          tipo="password"
          valor={contrasena}
          alCambiar={setContrasena}
          autoCompletar="current-password"
          requerido
        />
        {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
        <button type="submit" className="panel-eliminar-cuenta__boton" disabled={ocupado}>
          {ocupado ? 'Eliminando…' : 'Eliminar mi cuenta'}
        </button>
      </form>

      <ModalConfirmacion
        abierto={confirmando}
        titulo="¿Eliminar tu cuenta?"
        mensaje="Tu cuenta y todos tus datos se borrarán de forma definitiva. Si quieres volver a usar LFMova tendrás que registrarte de nuevo."
        textoConfirmar="Sí, eliminar"
        alConfirmar={alConfirmar}
        alCancelar={() => setConfirmando(false)}
      />
    </section>
  )
}
