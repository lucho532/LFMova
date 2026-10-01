import { useState, type FormEvent } from 'react'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { ErrorApi } from '../servicios/clienteHttp'
import { invitarPersona } from '../servicios/servicioInvitaciones'
import { BotonPrimario } from './BotonPrimario'
import { CampoFormulario } from './CampoFormulario'
import { MensajeAlerta } from './MensajeAlerta'
import './FormularioInvitarPersona.css'

interface PropiedadesFormularioInvitarPersona {
  empresaId: number
  /** Valores con los que arranca el formulario (por ejemplo, lo que se escribió en el buscador). */
  cedulaInicial?: string
  correoInicial?: string
  /** Avisa que se envió una invitación, para recargar la lista de invitaciones. */
  alInvitar: () => void
}

/**
 * Invita por correo a una persona que todavía no es parte de la empresa.
 * Nunca muestra datos de la persona ni si ya tiene cuenta: solo confirma que
 * la invitación se envió. Si ya tiene cuenta, el correo le llega a la de su
 * cuenta; si no, al correo escrito aquí, desde donde podrá registrarse.
 */
export function FormularioInvitarPersona({ empresaId, cedulaInicial = '', correoInicial = '', alInvitar }: PropiedadesFormularioInvitarPersona) {
  const { token } = useAutenticacion()
  const [cedula, setCedula] = useState(cedulaInicial)
  const [correo, setCorreo] = useState(correoInicial)
  const [enviando, setEnviando] = useState(false)
  const [mensaje, setMensaje] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(null)

  async function alEnviar(evento: FormEvent) {
    evento.preventDefault()
    if (!token) return
    setEnviando(true)
    setMensaje(null)
    try {
      await invitarPersona(empresaId, { cedula: cedula.trim(), correo: correo.trim() }, token)
      setMensaje({ tipo: 'exito', texto: 'Invitación enviada. Cuando la persona la acepte, aparecerá en la lista y podrás asignarle un rol.' })
      setCedula('')
      setCorreo('')
      alInvitar()
    } catch (error) {
      setMensaje({ tipo: 'error', texto: error instanceof ErrorApi ? error.message : 'No se pudo enviar la invitación.' })
    } finally {
      setEnviando(false)
    }
  }

  return (
    <form className="formulario-invitar-persona" onSubmit={alEnviar}>
      <p className="formulario-invitar-persona__ayuda">
        Le enviaremos un correo para que acepte unirse a la empresa. Si ya tiene cuenta, le llega al correo de su cuenta; si no, al que
        escribas aquí, y podrá registrarse desde ese mismo correo. No verás sus datos hasta que acepte.
      </p>
      <div className="formulario-invitar-persona__campos">
        <CampoFormulario id="invitarCedula" etiqueta="Cédula" valor={cedula} alCambiar={setCedula} requerido />
        <CampoFormulario id="invitarCorreo" etiqueta="Correo" tipo="email" valor={correo} alCambiar={setCorreo} requerido />
        <BotonPrimario disabled={enviando}>{enviando ? 'Enviando…' : 'Enviar invitación'}</BotonPrimario>
      </div>
      {mensaje && <MensajeAlerta tipo={mensaje.tipo}>{mensaje.texto}</MensajeAlerta>}
    </form>
  )
}
