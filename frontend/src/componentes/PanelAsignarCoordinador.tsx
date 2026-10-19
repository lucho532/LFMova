import { useState, type FormEvent } from 'react'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { ErrorApi } from '../servicios/clienteHttp'
import { asignarCoordinador } from '../servicios/servicioEmpresas'
import { BotonPrimario } from './BotonPrimario'
import { BuscadorPersona } from './BuscadorPersona'
import { CampoFormulario } from './CampoFormulario'
import { MensajeAlerta } from './MensajeAlerta'
import '../estilos/componentes/PanelAsignarCoordinador.css'

interface PropiedadesPanelAsignarCoordinador {
  empresaId: number
  alAsignado: (mensaje: string) => Promise<void>
}

/**
 * Asignación de un coordinador a una empresa en dos pasos: primero se busca a
 * la persona por cédula y se confirman sus datos; solo entonces se le asigna
 * el rol. Si nadie tiene esa cédula, se ofrece invitarla creando su cuenta
 * (recibirá un correo para establecer su contraseña).
 */
export function PanelAsignarCoordinador({ empresaId, alAsignado }: PropiedadesPanelAsignarCoordinador) {
  const { token } = useAutenticacion()
  const [nombre, setNombre] = useState('')
  const [telefono, setTelefono] = useState('')
  const [correo, setCorreo] = useState('')
  const [ocupado, setOcupado] = useState(false)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [reinicio, setReinicio] = useState(0)

  async function asignar(cedula: string, datosNuevaCuenta: boolean) {
    if (!token) return
    setOcupado(true)
    setMensajeError(null)
    try {
      await asignarCoordinador(
        empresaId,
        { cedula, nombreCoordinador: datosNuevaCuenta ? nombre : '', telefonoCoordinador: datosNuevaCuenta ? telefono : '', correoCoordinador: datosNuevaCuenta ? correo : '' },
        token,
      )
      setNombre('')
      setTelefono('')
      setCorreo('')
      setReinicio((valor) => valor + 1)
      await alAsignado(
        datosNuevaCuenta
          ? 'Cuenta creada y coordinador asignado. Le enviamos un correo para que establezca su contraseña.'
          : 'Coordinador asignado.',
      )
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo asignar el coordinador.')
    } finally {
      setOcupado(false)
    }
  }

  return (
    <div className="panel-asignar-coordinador">
      <BuscadorPersona
        key={reinicio}
        ayuda="Busca a la persona por su cédula (Enter) y verifica sus datos antes de asignarla como coordinador."
        acciones={(persona) => (
          <BotonPrimario type="button" disabled={ocupado || persona.esCoordinador} onClick={() => asignar(persona.cedula, false)}>
            {persona.esCoordinador ? 'Ya es coordinador' : ocupado ? 'Asignando…' : 'Hacer coordinador'}
          </BotonPrimario>
        )}
        cuandoNoExiste={(cedula) => (
          <form
            className="panel-asignar-coordinador__invitacion"
            onSubmit={(evento: FormEvent) => {
              evento.preventDefault()
              asignar(cedula, true)
            }}
          >
            <p>Puedes crear su cuenta ahora: recibirá un correo para elegir su contraseña.</p>
            <CampoFormulario id="nombreInvitado" etiqueta="Nombre completo" valor={nombre} alCambiar={setNombre} requerido />
            <CampoFormulario id="telefonoInvitado" etiqueta="Teléfono" tipo="tel" valor={telefono} alCambiar={setTelefono} requerido />
            <CampoFormulario id="correoInvitado" etiqueta="Correo electrónico" tipo="email" valor={correo} alCambiar={setCorreo} requerido />
            <BotonPrimario disabled={ocupado}>{ocupado ? 'Creando…' : 'Crear cuenta y hacer coordinador'}</BotonPrimario>
          </form>
        )}
      />
      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
    </div>
  )
}
