import { useEffect, useState, type FormEvent } from 'react'
import type { CorredorVial } from '../modelos/corredorVial'
import { ErrorApi } from '../servicios/clienteHttp'
import { activarCorredorVial, crearCorredorVial, desactivarCorredorVial, obtenerCorredoresViales } from '../servicios/servicioCorredoresViales'
import { BotonPrimario } from './BotonPrimario'
import { BotonSecundario } from './BotonSecundario'
import { CampoFormulario } from './CampoFormulario'
import { MensajeAlerta } from './MensajeAlerta'
import '../estilos/componentes/SeccionCorredoresViales.css'

interface PropiedadesSeccionCorredoresViales {
  empresaId: number
  token: string
  /** Avisa al padre cada vez que cambia la lista, para llenar el selector de corredor del formulario de zona. */
  alCambiarCorredores: (corredores: CorredorVial[]) => void
}

/**
 * Administra los corredores viales de la empresa: agrupaciones de Zonas que
 * están sobre el mismo camino real (por ejemplo, para llegar a Morrogacho
 * hay que pasar por La Francia). A diferencia de las Macrozonas, esto SÍ
 * cambia el reparto: las zonas de un mismo corredor se reparten juntas y
 * pueden terminar compartiendo un solo vehículo en vez de exigir uno cada
 * una.
 */
export function SeccionCorredoresViales({ empresaId, token, alCambiarCorredores }: PropiedadesSeccionCorredoresViales) {
  const [corredores, setCorredores] = useState<CorredorVial[]>([])
  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [enCurso, setEnCurso] = useState<number | null>(null)
  const [nombreNuevo, setNombreNuevo] = useState('')
  const [guardando, setGuardando] = useState(false)

  async function cargar() {
    setCargando(true)
    setMensajeError(null)
    try {
      const lista = await obtenerCorredoresViales(empresaId, token)
      setCorredores(lista)
      alCambiarCorredores(lista)
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudieron cargar los corredores viales.')
    } finally {
      setCargando(false)
    }
  }

  useEffect(() => {
    cargar()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [empresaId, token])

  async function alCrear(evento: FormEvent) {
    evento.preventDefault()
    setGuardando(true)
    setMensajeError(null)
    try {
      await crearCorredorVial(empresaId, { nombre: nombreNuevo }, token)
      setNombreNuevo('')
      await cargar()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo crear el corredor vial.')
    } finally {
      setGuardando(false)
    }
  }

  async function alCambiarEstado(corredor: CorredorVial) {
    setEnCurso(corredor.corredorVialId)
    setMensajeError(null)
    try {
      if (corredor.activo) {
        await desactivarCorredorVial(empresaId, corredor.corredorVialId, token)
      } else {
        await activarCorredorVial(empresaId, corredor.corredorVialId, token)
      }
      await cargar()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cambiar el estado del corredor vial.')
    } finally {
      setEnCurso(null)
    }
  }

  return (
    <section className="seccion-corredores-viales">
      <h2>Corredores viales</h2>
      <p className="seccion-corredores-viales__ayuda">
        Agrupan zonas que están sobre el mismo camino real: el reparto las trata como una sola ruta y pueden compartir vehículo, en vez de exigir un conductor por zona.
      </p>

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

      <form className="seccion-corredores-viales__formulario" onSubmit={alCrear}>
        <CampoFormulario id="nombreCorredorVial" etiqueta="Nuevo corredor vial" valor={nombreNuevo} alCambiar={setNombreNuevo} requerido />
        <BotonPrimario disabled={guardando}>{guardando ? 'Guardando…' : 'Agregar'}</BotonPrimario>
      </form>

      {cargando ? (
        <p className="contenedor-pagina__estado">Cargando…</p>
      ) : corredores.length === 0 ? (
        <p className="contenedor-pagina__estado">Todavía no hay corredores viales registrados.</p>
      ) : (
        <ul className="seccion-corredores-viales__lista">
          {corredores.map((corredor) => (
            <li key={corredor.corredorVialId}>
              <span className={corredor.activo ? '' : 'seccion-corredores-viales__inactivo'}>{corredor.nombre}</span>
              <BotonSecundario onClick={() => alCambiarEstado(corredor)} disabled={enCurso === corredor.corredorVialId}>
                {corredor.activo ? 'Desactivar' : 'Activar'}
              </BotonSecundario>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
