import { useEffect, useState, type FormEvent } from 'react'
import type { MacroZona } from '../modelos/macroZona'
import { ErrorApi } from '../servicios/clienteHttp'
import { activarMacroZona, crearMacroZona, desactivarMacroZona, obtenerMacroZonas } from '../servicios/servicioMacroZonas'
import { BotonPrimario } from './BotonPrimario'
import { BotonSecundario } from './BotonSecundario'
import { CampoFormulario } from './CampoFormulario'
import { MensajeAlerta } from './MensajeAlerta'
import './SeccionMacroZonas.css'

interface PropiedadesSeccionMacroZonas {
  empresaId: number
  token: string
  /** Avisa al padre cada vez que cambia la lista, para llenar el selector de macrozona del formulario de zona. */
  alCambiarMacroZonas: (macroZonas: MacroZona[]) => void
}

/**
 * Administra las macrozonas de la empresa (agrupación organizativa de zonas
 * por comuna, por ejemplo "Atardeceres"). No participa en el reparto de
 * rutas: solo organiza las zonas para que el coordinador las encuentre más
 * fácil.
 */
export function SeccionMacroZonas({ empresaId, token, alCambiarMacroZonas }: PropiedadesSeccionMacroZonas) {
  const [macroZonas, setMacroZonas] = useState<MacroZona[]>([])
  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [enCurso, setEnCurso] = useState<number | null>(null)
  const [nombreNuevo, setNombreNuevo] = useState('')
  const [guardando, setGuardando] = useState(false)

  async function cargar() {
    setCargando(true)
    setMensajeError(null)
    try {
      const lista = await obtenerMacroZonas(empresaId, token)
      setMacroZonas(lista)
      alCambiarMacroZonas(lista)
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudieron cargar las macrozonas.')
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
      await crearMacroZona(empresaId, { nombre: nombreNuevo }, token)
      setNombreNuevo('')
      await cargar()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo crear la macrozona.')
    } finally {
      setGuardando(false)
    }
  }

  async function alCambiarEstado(macroZona: MacroZona) {
    setEnCurso(macroZona.macroZonaId)
    setMensajeError(null)
    try {
      if (macroZona.activa) {
        await desactivarMacroZona(empresaId, macroZona.macroZonaId, token)
      } else {
        await activarMacroZona(empresaId, macroZona.macroZonaId, token)
      }
      await cargar()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cambiar el estado de la macrozona.')
    } finally {
      setEnCurso(null)
    }
  }

  return (
    <section className="seccion-macro-zonas">
      <h2>Macrozonas</h2>
      <p className="seccion-macro-zonas__ayuda">Agrupan zonas por comuna solo para organizarlas mejor; no cambian cómo se reparten las rutas.</p>

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

      <form className="seccion-macro-zonas__formulario" onSubmit={alCrear}>
        <CampoFormulario id="nombreMacroZona" etiqueta="Nueva macrozona" valor={nombreNuevo} alCambiar={setNombreNuevo} requerido />
        <BotonPrimario disabled={guardando}>{guardando ? 'Guardando…' : 'Agregar'}</BotonPrimario>
      </form>

      {cargando ? (
        <p className="contenedor-pagina__estado">Cargando…</p>
      ) : macroZonas.length === 0 ? (
        <p className="contenedor-pagina__estado">Todavía no hay macrozonas registradas.</p>
      ) : (
        <ul className="seccion-macro-zonas__lista">
          {macroZonas.map((macroZona) => (
            <li key={macroZona.macroZonaId}>
              <span className={macroZona.activa ? '' : 'seccion-macro-zonas__inactiva'}>{macroZona.nombre}</span>
              <BotonSecundario onClick={() => alCambiarEstado(macroZona)} disabled={enCurso === macroZona.macroZonaId}>
                {macroZona.activa ? 'Desactivar' : 'Activar'}
              </BotonSecundario>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
