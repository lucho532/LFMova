import { useEffect, useState, type FormEvent } from 'react'
import type { BarreraGeografica } from '../modelos/barreraGeografica'
import { ErrorApi } from '../servicios/clienteHttp'
import { crearBarreraGeografica, eliminarBarreraGeografica, obtenerBarrerasGeograficas } from '../servicios/servicioBarrerasGeograficas'
import { BotonPrimario } from './BotonPrimario'
import { BotonSecundario } from './BotonSecundario'
import { CampoFormulario } from './CampoFormulario'
import { MensajeAlerta } from './MensajeAlerta'
import '../estilos/componentes/SeccionBarrerasGeograficas.css'

interface PropiedadesSeccionBarrerasGeograficas {
  empresaId: number
  token: string
}

/**
 * Administra las barreras geográficas de la empresa: pares de barrios que
 * nunca deben combinarse en una misma zona (por ejemplo, separados por un
 * accidente topográfico sin vía de conexión útil). El backend impide crear
 * o actualizar una zona que junte un par declarado aquí.
 */
export function SeccionBarrerasGeograficas({ empresaId, token }: PropiedadesSeccionBarrerasGeograficas) {
  const [barreras, setBarreras] = useState<BarreraGeografica[]>([])
  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [enCurso, setEnCurso] = useState<number | null>(null)

  const [barrioA, setBarrioA] = useState('')
  const [barrioB, setBarrioB] = useState('')
  const [motivo, setMotivo] = useState('')
  const [guardando, setGuardando] = useState(false)

  async function cargar() {
    setCargando(true)
    setMensajeError(null)
    try {
      setBarreras(await obtenerBarrerasGeograficas(empresaId, token))
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudieron cargar las barreras geográficas.')
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
      await crearBarreraGeografica(empresaId, { barrioA, barrioB, motivo: motivo || undefined }, token)
      setBarrioA('')
      setBarrioB('')
      setMotivo('')
      await cargar()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo crear la barrera geográfica.')
    } finally {
      setGuardando(false)
    }
  }

  async function alEliminar(barrera: BarreraGeografica) {
    setEnCurso(barrera.barreraGeograficaId)
    setMensajeError(null)
    try {
      await eliminarBarreraGeografica(empresaId, barrera.barreraGeograficaId, token)
      await cargar()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo eliminar la barrera geográfica.')
    } finally {
      setEnCurso(null)
    }
  }

  return (
    <section className="seccion-barreras-geograficas">
      <h2>Barreras geográficas</h2>
      <p className="seccion-barreras-geograficas__ayuda">
        Declara pares de barrios que nunca deben quedar en la misma zona (por ejemplo, separados por una montaña sin vía directa).
      </p>

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

      <form className="seccion-barreras-geograficas__formulario" onSubmit={alCrear}>
        <CampoFormulario id="barrioA" etiqueta="Barrio A" valor={barrioA} alCambiar={setBarrioA} requerido />
        <CampoFormulario id="barrioB" etiqueta="Barrio B" valor={barrioB} alCambiar={setBarrioB} requerido />
        <CampoFormulario id="motivoBarrera" etiqueta="Motivo (opcional)" valor={motivo} alCambiar={setMotivo} />
        <BotonPrimario disabled={guardando}>{guardando ? 'Guardando…' : 'Agregar barrera'}</BotonPrimario>
      </form>

      {cargando ? (
        <p className="contenedor-pagina__estado">Cargando…</p>
      ) : barreras.length === 0 ? (
        <p className="contenedor-pagina__estado">Todavía no hay barreras geográficas registradas.</p>
      ) : (
        <ul className="seccion-barreras-geograficas__lista">
          {barreras.map((barrera) => (
            <li key={barrera.barreraGeograficaId}>
              <span>
                <strong>{barrera.barrioA}</strong> ❌ <strong>{barrera.barrioB}</strong>
                {barrera.motivo && <span className="seccion-barreras-geograficas__motivo"> — {barrera.motivo}</span>}
              </span>
              <BotonSecundario onClick={() => alEliminar(barrera)} disabled={enCurso === barrera.barreraGeograficaId}>
                Eliminar
              </BotonSecundario>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
