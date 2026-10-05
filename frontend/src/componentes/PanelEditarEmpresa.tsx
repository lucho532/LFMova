import { useState, type FormEvent } from 'react'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { Empresa } from '../modelos/empresa'
import { ErrorApi } from '../servicios/clienteHttp'
import { actualizarEmpresa, eliminarEmpresa } from '../servicios/servicioEmpresas'
import { BotonPrimario } from './BotonPrimario'
import { CampoFormulario } from './CampoFormulario'
import { MensajeAlerta } from './MensajeAlerta'
import { ModalConfirmacion } from './ModalConfirmacion'
import '../estilos/componentes/PanelEditarEmpresa.css'

interface PropiedadesPanelEditarEmpresa {
  empresa: Empresa
  /** Avisa de que los datos se guardaron, con la empresa ya corregida. */
  alGuardar: (empresa: Empresa) => void
  /** Avisa de que la empresa se eliminó. */
  alEliminar: () => void
}

/**
 * Sección del administrador de plataforma para corregir los datos de una
 * empresa (nombre, CIF y dirección) o eliminarla por completo. Eliminar pide
 * confirmación porque borra todo su historial y no se puede deshacer. Las
 * validaciones de fondo las hace el backend.
 */
export function PanelEditarEmpresa({ empresa, alGuardar, alEliminar }: PropiedadesPanelEditarEmpresa) {
  const { token } = useAutenticacion()
  const [nombre, setNombre] = useState(empresa.nombre)
  const [cif, setCif] = useState(empresa.cif)
  const [direccion, setDireccion] = useState(empresa.direccion)
  const [ocupado, setOcupado] = useState(false)
  const [confirmando, setConfirmando] = useState(false)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [mensajeExito, setMensajeExito] = useState<string | null>(null)

  async function ejecutar(accion: () => Promise<void>, mensajeFallo: string) {
    setOcupado(true)
    setMensajeError(null)
    setMensajeExito(null)
    try {
      await accion()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : mensajeFallo)
    } finally {
      setOcupado(false)
    }
  }

  function alEnviar(evento: FormEvent) {
    evento.preventDefault()
    if (!token) return
    ejecutar(async () => {
      alGuardar(await actualizarEmpresa(empresa.empresaId, { nombre, cif, direccion }, token))
      setMensajeExito('Los datos de la empresa se guardaron.')
    }, 'No se pudieron guardar los datos.')
  }

  function alConfirmarEliminar() {
    setConfirmando(false)
    if (!token) return
    ejecutar(async () => {
      await eliminarEmpresa(empresa.empresaId, token)
      alEliminar()
    }, 'No se pudo eliminar la empresa.')
  }

  return (
    <section className="panel-editar-empresa">
      <h2>Datos de la empresa</h2>
      <form onSubmit={alEnviar}>
        <CampoFormulario id="empresaNombre" etiqueta="Nombre" valor={nombre} alCambiar={setNombre} requerido />
        <CampoFormulario id="empresaCif" etiqueta="CIF / NIT" valor={cif} alCambiar={setCif} requerido />
        <CampoFormulario id="empresaDireccion" etiqueta="Dirección" valor={direccion} alCambiar={setDireccion} requerido />
        {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
        {mensajeExito && <MensajeAlerta tipo="exito">{mensajeExito}</MensajeAlerta>}
        <BotonPrimario disabled={ocupado}>Guardar cambios</BotonPrimario>
      </form>

      <div className="panel-editar-empresa__peligro">
        <div>
          <strong>Eliminar empresa</strong>
          <p>
            Borra la empresa con todo su historial: sedes, zonas, rutas, pasajeros, incidencias y registros de facturación. Las personas conservan su
            cuenta, ya sin empresa. No se puede deshacer.
          </p>
        </div>
        <button type="button" className="panel-editar-empresa__eliminar" disabled={ocupado} onClick={() => setConfirmando(true)}>
          Eliminar empresa
        </button>
      </div>

      <ModalConfirmacion
        abierto={confirmando}
        titulo={`¿Eliminar ${empresa.nombre}?`}
        mensaje="Se borrará la empresa y todo su historial, incluidos los registros con los que se factura. No se puede deshacer. Si solo quieres que deje de operar, usa «Desactivar empresa»."
        textoConfirmar="Sí, eliminar"
        alConfirmar={alConfirmarEliminar}
        alCancelar={() => setConfirmando(false)}
      />
    </section>
  )
}
