import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { BotonPrimario } from '../componentes/BotonPrimario'
import { BuscadorPersona } from '../componentes/BuscadorPersona'
import { FormularioVehiculo } from '../componentes/FormularioVehiculo'
import { ActividadConductores } from '../componentes/ActividadConductores'
import { ContenedorPagina } from '../componentes/ContenedorPagina'
import { EncabezadoPagina } from '../componentes/EncabezadoPagina'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { CrearVehiculoDatos } from '../modelos/conductor'
import { ErrorApi } from '../servicios/clienteHttp'
import './PaginaConductores.css'
import {
  crearConductor,
  vincularConductor,
} from '../servicios/servicioConductores'

/**
 * Lista los conductores de la empresa y permite asignar el rol de conductor a
 * una persona ya registrada, buscándola por cédula. Si esa persona ya es
 * conductor de otra empresa, solo se le vincula a esta.
 */
export function PaginaConductores() {
  const { empresaId } = useParams<{ empresaId: string }>()
  const { token } = useAutenticacion()
  const idEmpresa = Number(empresaId)

  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [mensajeExito, setMensajeExito] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)
  const [buscadorKey, setBuscadorKey] = useState(0)
  const [agregando, setAgregando] = useState(false)
  const [version, setVersion] = useState(0)

  async function alTerminar(mensaje: string) {
    setBuscadorKey((k) => k + 1)
    setMensajeExito(mensaje)
    setVersion((v) => v + 1)
  }

  /** La persona ya es conductor (de otra empresa): solo se vincula a esta, sin pedir vehículo. */
  async function vincular(cedula: string) {
    if (!token) return
    setEnviando(true)
    setMensajeError(null)
    setMensajeExito(null)
    try {
      await vincularConductor(idEmpresa, { cedula }, token)
      await alTerminar('Listo: el conductor quedó vinculado a esta empresa.')
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo vincular al conductor.')
    } finally {
      setEnviando(false)
    }
  }

  /** Asigna el rol de conductor y registra su vehículo; el backend forma su unidad operativa. */
  async function hacerConductor(cedula: string, vehiculo: CrearVehiculoDatos) {
    if (!token) return
    setMensajeError(null)
    setMensajeExito(null)
    try {
      await crearConductor(idEmpresa, { cedula, vehiculo }, token)
    } catch (error) {
      throw new Error(error instanceof ErrorApi ? error.message : 'No se pudo asignar el rol de conductor.')
    }
    await alTerminar('Listo: la persona ahora es conductor y su unidad operativa quedó creada.')
  }

  return (
    <ContenedorPagina ancho="amplio">
      <EncabezadoPagina
        titulo="Conductores"
        subtitulo="Actividad de cada conductor: rutas realizadas y pasajeros transportados."
        acciones={<BotonPrimario type="button" onClick={() => setAgregando((valor) => !valor)}>{agregando ? 'Cerrar' : 'Agregar conductor'}</BotonPrimario>}
      />

      {agregando && (
        <section className="pagina-conductores__agregar">
          <h2>Asignar rol de conductor</h2>
          <BuscadorPersona
            key={buscadorKey}
            ayuda="Busca por cédula (Enter) a una persona que ya sea parte de tu empresa; verifica sus datos y luego registra su vehículo para crear su unidad de trabajo."
            cuandoNoExiste={() => (
              <p>
                Si la persona se registró en la app pero todavía no es parte de tu empresa, invítala por correo desde{' '}
                <Link to={`/empresas/${idEmpresa}/empleados`}>Empleados</Link>: cuando acepte, podrás hacerla conductor.
              </p>
            )}
            acciones={(persona) =>
              persona.esConductor ? (
                <BotonPrimario type="button" disabled={enviando} onClick={() => vincular(persona.cedula)}>
                  {enviando ? 'Vinculando…' : 'Vincular como conductor de esta empresa'}
                </BotonPrimario>
              ) : (
                <div style={{ width: '100%' }}>
                  <p style={{ margin: '0 0 4px', fontWeight: 600 }}>Vehículo del conductor</p>
                  <FormularioVehiculo textoBoton="Hacer conductor y crear unidad" textoEnviando="Asignando…" alGuardar={(vehiculo) => hacerConductor(persona.cedula, vehiculo)} />
                </div>
              )
            }
          />
        </section>
      )}
      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      {mensajeExito && <MensajeAlerta tipo="exito">{mensajeExito}</MensajeAlerta>}

      <ActividadConductores empresaId={idEmpresa} version={version} />
    </ContenedorPagina>
  )
}
