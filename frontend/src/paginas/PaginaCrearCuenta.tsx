import { useEffect, useState, type FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { BotonPrimario } from '../componentes/BotonPrimario'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { TarjetaAutenticacion } from '../componentes/TarjetaAutenticacion'
import type { DetalleInvitacion } from '../modelos/invitacion'
import { ErrorApi } from '../servicios/clienteHttp'
import { registrarse } from '../servicios/servicioAutenticacion'
import { obtenerDetalleInvitacion } from '../servicios/servicioInvitaciones'
import '../estilos/paginas/PaginaCrearCuenta.css'

/**
 * Permite que cualquier persona cree su cuenta (correo, nombre, cédula,
 * teléfono y contraseña). La cuenta nace con rol de empleado y sin datos: solo
 * tendrá información de transporte cuando su cédula aparezca en una
 * importación de Excel de una empresa. Antes de entrar debe confirmar su
 * correo con el enlace que recibe. Si llega desde una invitación
 * (`?invitacion=`), la cédula y el correo vienen de la invitación, la cuenta
 * queda unida a esa empresa y, con el mismo correo de la invitación, no hace
 * falta confirmarlo.
 */
export function PaginaCrearCuenta() {
  const [parametros] = useSearchParams()
  const tokenInvitacion = parametros.get('invitacion')
  const [invitacion, setInvitacion] = useState<DetalleInvitacion | null>(null)

  const [correo, setCorreo] = useState('')
  const [nombre, setNombre] = useState('')
  const [apellidos, setApellidos] = useState('')
  const [cedula, setCedula] = useState('')
  const [telefono, setTelefono] = useState('')
  const [password, setPassword] = useState('')
  const [confirmacion, setConfirmacion] = useState('')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)
  const [correoEnviadoA, setCorreoEnviadoA] = useState<string | null>(null)
  const [cuentaLista, setCuentaLista] = useState(false)

  useEffect(() => {
    if (!tokenInvitacion) return
    obtenerDetalleInvitacion(tokenInvitacion)
      .then((detalle) => {
        setInvitacion(detalle)
        setCedula(detalle.cedula)
        setCorreo(detalle.correo)
      })
      .catch((error) => setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar la invitación.'))
  }, [tokenInvitacion])

  async function alEnviar(evento: FormEvent) {
    evento.preventDefault()
    setMensajeError(null)

    if (password !== confirmacion) {
      setMensajeError('Las contraseñas no coinciden.')
      return
    }

    setEnviando(true)

    try {
      const nombreCompleto = `${nombre.trim()} ${apellidos.trim()}`.trim()
      const resultado = await registrarse({ correo, nombreCompleto, cedula, telefono, password, tokenInvitacion: tokenInvitacion ?? undefined })
      if (resultado.requiereConfirmarCorreo) {
        setCorreoEnviadoA(correo)
      } else {
        setCuentaLista(true)
      }
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo crear la cuenta.')
    } finally {
      setEnviando(false)
    }
  }

  if (cuentaLista) {
    return (
      <TarjetaAutenticacion titulo="Cuenta creada">
        <MensajeAlerta tipo="exito">
          Tu cuenta quedó lista y ya formas parte de {invitacion?.nombreEmpresa ?? 'la empresa'}. Ya puedes{' '}
          <Link to="/iniciar-sesion">iniciar sesión</Link>.
        </MensajeAlerta>
      </TarjetaAutenticacion>
    )
  }

  if (correoEnviadoA) {
    return (
      <TarjetaAutenticacion titulo="Revisa tu correo">
        <MensajeAlerta tipo="exito">
          Te enviamos un enlace de confirmación a <strong>{correoEnviadoA}</strong>. Ábrelo para activar tu cuenta y luego inicia sesión.
        </MensajeAlerta>
      </TarjetaAutenticacion>
    )
  }

  return (
    <TarjetaAutenticacion titulo="Crear cuenta" subtitulo="Regístrate para consultar tu transporte. Confirmaremos tu identidad por correo electrónico.">
      <form className="pagina-crear-cuenta__formulario" onSubmit={alEnviar}>
        {invitacion && (
          <MensajeAlerta tipo="exito">
            {invitacion.nombreInvitador} te invita a unirte a <strong>{invitacion.nombreEmpresa}</strong>. Al crear tu cuenta quedarás unido a la
            empresa.
          </MensajeAlerta>
        )}
        <CampoFormulario id="nombreRegistro" etiqueta="Nombre" valor={nombre} alCambiar={setNombre} autoCompletar="given-name" requerido />
        <CampoFormulario id="apellidosRegistro" etiqueta="Apellidos" valor={apellidos} alCambiar={setApellidos} autoCompletar="family-name" requerido />
        <CampoFormulario id="cedulaRegistro" etiqueta="Cédula" valor={cedula} alCambiar={setCedula} requerido deshabilitado={Boolean(invitacion)} />
        <CampoFormulario id="correoRegistro" etiqueta="Correo electrónico" tipo="email" valor={correo} alCambiar={setCorreo} autoCompletar="email" requerido />
        <CampoFormulario id="telefonoRegistro" etiqueta="Teléfono" tipo="tel" valor={telefono} alCambiar={setTelefono} autoCompletar="tel" requerido />
        <CampoFormulario id="passwordRegistro" etiqueta="Contraseña" tipo="password" valor={password} alCambiar={setPassword} autoCompletar="new-password" requerido />
        <CampoFormulario id="confirmacionRegistro" etiqueta="Confirmar contraseña" tipo="password" valor={confirmacion} alCambiar={setConfirmacion} autoCompletar="new-password" requerido />

        {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

        <p className="pagina-crear-cuenta__privacidad">
          Al crear tu cuenta autorizas el tratamiento de tus datos según la <Link to="/privacidad">Política de privacidad</Link>.
        </p>
        <BotonPrimario disabled={enviando}>{enviando ? 'Creando cuenta…' : 'Crear cuenta'}</BotonPrimario>
      </form>
    </TarjetaAutenticacion>
  )
}
