import { useState } from 'react'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { CrearVehiculoDatos } from '../modelos/conductor'
import type { Persona } from '../modelos/persona'
import { ErrorApi } from '../servicios/clienteHttp'
import { crearConductor, vincularConductor } from '../servicios/servicioConductores'
import { asignarCoordinador } from '../servicios/servicioEmpresas'
import { BotonPrimario } from './BotonPrimario'
import { FormularioVehiculo } from './FormularioVehiculo'
import { MensajeAlerta } from './MensajeAlerta'
import './PanelCambiarRolPersona.css'

interface PropiedadesPanelCambiarRolPersona {
  persona: Persona
  empresaId: number
  /** Se invoca tras un cambio de rol exitoso, para recargar los datos y cerrar la fila. */
  alTerminar: (mensaje: string) => Promise<void>
}

/**
 * Acciones de un coordinador sobre un empleado ya encontrado por cédula:
 * asignarlo como coordinador de su propia empresa, o hacerlo conductor (con
 * los datos de su vehículo) y vincularlo a ella. La empresa es siempre la
 * del coordinador que la usa, a diferencia del panel equivalente del
 * administrador de plataforma, que elige la empresa.
 */
export function PanelCambiarRolPersona({ persona, empresaId, alTerminar }: PropiedadesPanelCambiarRolPersona) {
  const { token } = useAutenticacion()
  const [ocupado, setOcupado] = useState(false)
  const [mensajeError, setMensajeError] = useState<string | null>(null)

  const yaVinculadaComoConductor = persona.empresasConductor.some((e) => e.empresaId === empresaId)

  async function ejecutar(accion: () => Promise<unknown>, exito: string, fallo: string) {
    setOcupado(true)
    setMensajeError(null)
    try {
      await accion()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : fallo)
      setOcupado(false)
      return
    }
    setOcupado(false)
    await alTerminar(exito)
  }

  async function hacerConductor(vehiculo: CrearVehiculoDatos) {
    if (!token) return
    try {
      await crearConductor(empresaId, { cedula: persona.cedula, vehiculo }, token)
    } catch (error) {
      throw new Error(error instanceof ErrorApi ? error.message : 'No se pudo asignar el rol de conductor.')
    }
    await alTerminar('Listo: la persona ahora es conductor y su unidad operativa quedó creada.')
  }

  return (
    <div className="cambiar-rol-persona">
      <section>
        <h4>Hacer coordinador</h4>
        {persona.empresaCoordinada ? (
          <p>
            {persona.empresaCoordinada.empresaId === empresaId
              ? 'Ya es coordinador de esta empresa.'
              : `Ya es coordinador de ${persona.empresaCoordinada.nombre}; una persona solo puede coordinar una empresa.`}
          </p>
        ) : (
          <BotonPrimario
            type="button"
            disabled={ocupado}
            onClick={() =>
              ejecutar(
                () =>
                  asignarCoordinador(
                    empresaId,
                    { cedula: persona.cedula, nombreCoordinador: '', telefonoCoordinador: '', correoCoordinador: '' },
                    token!,
                  ),
                'Listo: la persona ahora es coordinador de esta empresa.',
                'No se pudo asignar el coordinador.',
              )
            }
          >
            Hacer coordinador de esta empresa
          </BotonPrimario>
        )}
      </section>

      <section>
        <h4>Hacer conductor</h4>
        {persona.esConductor ? (
          yaVinculadaComoConductor ? (
            <p>Ya es conductor de esta empresa.</p>
          ) : (
            <BotonPrimario
              type="button"
              disabled={ocupado}
              onClick={() =>
                ejecutar(
                  () => vincularConductor(empresaId, { cedula: persona.cedula }, token!),
                  'Listo: el conductor quedó vinculado a esta empresa.',
                  'No se pudo vincular al conductor.',
                )
              }
            >
              Vincular como conductor de esta empresa
            </BotonPrimario>
          )
        ) : (
          <>
            <p className="cambiar-rol-persona__nota">Datos del vehículo (forman su unidad de trabajo):</p>
            <FormularioVehiculo textoBoton="Hacer conductor y crear unidad" textoEnviando="Asignando…" alGuardar={hacerConductor} />
          </>
        )}
      </section>

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
    </div>
  )
}
