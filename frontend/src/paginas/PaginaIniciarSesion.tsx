import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router-dom'
import { BotonPrimario } from '../componentes/BotonPrimario'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { ErrorApi } from '../servicios/clienteHttp'
import { iniciarSesion } from '../servicios/servicioAutenticacion'
import { obtenerRolesDelToken, rutaInicialSegunRoles } from '../servicios/tokenJwt'
import '../estilos/paginas/PaginaIniciarSesion.css'

/**
 * Inicio de sesión con cédula o correo y contraseña: un único cuadro sobre la
 * imagen de la aplicación como fondo de pantalla. Muestra el motivo real del
 * fallo (credenciales, correo sin confirmar, servidor caído…) que entrega el
 * cliente HTTP.
 */
export function PaginaIniciarSesion() {
  const [identificador, setIdentificador] = useState('')
  const [password, setPassword] = useState('')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  const { iniciarSesion: guardarToken, token } = useAutenticacion()
  const navegar = useNavigate()
  const [parametros] = useSearchParams()
  // Página a la que volver tras entrar (por ejemplo, la invitación que se estaba aceptando). Solo rutas
  // internas: una dirección absoluta o "//otro-sitio" permitiría redirigir a un sitio ajeno.
  const volverA = parametros.get('volverA')
  const destinoInterno = volverA && volverA.startsWith('/') && !volverA.startsWith('//') ? volverA : null

  if (token) {
    return <Navigate to={destinoInterno ?? rutaInicialSegunRoles(obtenerRolesDelToken(token))} replace />
  }

  async function alEnviar(evento: FormEvent) {
    evento.preventDefault()
    setMensajeError(null)
    setEnviando(true)

    try {
      const respuesta = await iniciarSesion({ identificador: identificador.trim(), password })
      guardarToken(respuesta.token)
      navegar(destinoInterno ?? rutaInicialSegunRoles(obtenerRolesDelToken(respuesta.token)))
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo iniciar sesión.')
    } finally {
      setEnviando(false)
    }
  }

  return (
    <main className="pagina-inicio-sesion">
      <section className="pagina-inicio-sesion__cuadro" aria-labelledby="titulo-inicio-sesion">
        <header className="pagina-inicio-sesion__cabecera">
          <h1 id="titulo-inicio-sesion" className="pagina-inicio-sesion__titulo">
            Iniciar sesión
          </h1>
          <p className="pagina-inicio-sesion__subtitulo">Ingresa con tu cédula o correo electrónico y tu contraseña.</p>
        </header>

        <form className="pagina-inicio-sesion__formulario" onSubmit={alEnviar}>
          <CampoFormulario id="identificador" etiqueta="Cédula o correo electrónico" valor={identificador} alCambiar={setIdentificador} autoCompletar="username" requerido />

          <CampoFormulario id="password" etiqueta="Contraseña" tipo="password" valor={password} alCambiar={setPassword} autoCompletar="current-password" requerido />

          {mensajeError && (
            <p className="pagina-inicio-sesion__error" role="alert">
              {mensajeError}
            </p>
          )}

          <BotonPrimario disabled={enviando}>{enviando ? 'Ingresando…' : 'Ingresar'}</BotonPrimario>
        </form>

        <div className="pagina-inicio-sesion__enlaces">
          <Link to="/crear-cuenta">Crear cuenta</Link>
          <Link to="/olvide-contrasena">Olvidé mi contraseña</Link>
        </div>
      </section>
    </main>
  )
}
