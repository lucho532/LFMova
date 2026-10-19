import { useState } from 'react'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { CrearVehiculoDatos } from '../modelos/conductor'
import type { Empresa } from '../modelos/empresa'
import type { Persona } from '../modelos/persona'
import { ErrorApi } from '../servicios/clienteHttp'
import { crearConductor, vincularConductor } from '../servicios/servicioConductores'
import { asignarCoordinador } from '../servicios/servicioEmpresas'
import { BotonPrimario } from './BotonPrimario'
import { FormularioVehiculo } from './FormularioVehiculo'
import { MensajeAlerta } from './MensajeAlerta'
import { SelectorFormulario } from './SelectorFormulario'
import '../estilos/componentes/PanelAccionesPersonaAdministrador.css'

interface PropiedadesPanelAccionesPersonaAdministrador {
  persona: Persona
  empresas: Empresa[]
  /** Se invoca tras un cambio de rol exitoso, para recargar los datos y reiniciar la búsqueda. */
  alTerminar: (mensaje: string) => Promise<void>
}

/**
 * Acciones del administrador sobre una persona encontrada: asignarla como
 * coordinador de una empresa, o hacerla conductor (con los datos de su
 * vehículo) y vincularla a una empresa.
 */
export function PanelAccionesPersonaAdministrador({ persona, empresas, alTerminar }: PropiedadesPanelAccionesPersonaAdministrador) {
  const { token } = useAutenticacion()
  const [empresaCoordinador, setEmpresaCoordinador] = useState('')
  const [empresaConductor, setEmpresaConductor] = useState('')
  const [ocupado, setOcupado] = useState(false)
  const [mensajeError, setMensajeError] = useState<string | null>(null)

  const opciones = [...empresas].sort((a, b) => a.nombre.localeCompare(b.nombre, 'es')).map((e) => ({ valor: String(e.empresaId), texto: e.nombre }))
  const yaVinculada = persona.empresasConductor.some((e) => String(e.empresaId) === empresaConductor)

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
      await crearConductor(Number(empresaConductor), { cedula: persona.cedula, vehiculo }, token)
    } catch (error) {
      throw new Error(error instanceof ErrorApi ? error.message : 'No se pudo asignar el rol de conductor.')
    }
    await alTerminar('Listo: la persona ahora es conductor de la empresa y su unidad operativa quedó creada.')
  }

  return (
    <div className="acciones-persona-admin">
      <section>
        <h4>Hacer coordinador</h4>
        {persona.empresaCoordinada ? (
          <p>Ya es coordinador de {persona.empresaCoordinada.nombre}; una persona solo puede coordinar una empresa.</p>
        ) : (
          <>
            <SelectorFormulario id="empresaCoordinador" etiqueta="Empresa" valor={empresaCoordinador} opciones={opciones} alCambiar={setEmpresaCoordinador} textoVacio="Elige la empresa" />
            <BotonPrimario
              type="button"
              disabled={ocupado || !empresaCoordinador}
              onClick={() =>
                ejecutar(
                  () => asignarCoordinador(Number(empresaCoordinador), { cedula: persona.cedula, nombreCoordinador: '', telefonoCoordinador: '', correoCoordinador: '' }, token!),
                  'Listo: la persona ahora es coordinador de la empresa.',
                  'No se pudo asignar el coordinador.',
                )
              }
            >
              Hacer coordinador de esa empresa
            </BotonPrimario>
          </>
        )}
      </section>

      <section>
        <h4>Hacer conductor</h4>
        <SelectorFormulario id="empresaConductor" etiqueta="Empresa" valor={empresaConductor} opciones={opciones} alCambiar={setEmpresaConductor} textoVacio="Elige la empresa" />
        {empresaConductor && persona.esConductor && (
          yaVinculada ? (
            <p>Ya es conductor de esa empresa.</p>
          ) : (
            <BotonPrimario
              type="button"
              disabled={ocupado}
              onClick={() => ejecutar(() => vincularConductor(Number(empresaConductor), { cedula: persona.cedula }, token!), 'Listo: el conductor quedó vinculado a la empresa.', 'No se pudo vincular al conductor.')}
            >
              Vincular como conductor de esa empresa
            </BotonPrimario>
          )
        )}
        {empresaConductor && !persona.esConductor && (
          <>
            <p className="acciones-persona-admin__nota">Datos del vehículo (forman su unidad de trabajo):</p>
            <FormularioVehiculo textoBoton="Hacer conductor y crear unidad" textoEnviando="Asignando…" alGuardar={hacerConductor} />
          </>
        )}
      </section>

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
    </div>
  )
}
