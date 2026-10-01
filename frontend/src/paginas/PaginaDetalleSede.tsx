import { useEffect, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { BotonPrimario } from '../componentes/BotonPrimario'
import { BotonSecundario } from '../componentes/BotonSecundario'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { EtiquetaEstado } from '../componentes/EtiquetaEstado'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { Sede } from '../modelos/sede'
import { ErrorApi } from '../servicios/clienteHttp'
import { activarSede, actualizarSede, desactivarSede, obtenerSede } from '../servicios/servicioSedes'
import './PaginaDetalleSede.css'

/**
 * Muestra y permite editar los datos de una sede, y activarla o
 * desactivarla. Requiere rol COORDINADOR de esa empresa en el backend.
 */
export function PaginaDetalleSede() {
  const { empresaId, sedeId } = useParams<{ empresaId: string; sedeId: string }>()
  const { token } = useAutenticacion()

  const [sede, setSede] = useState<Sede | null>(null)
  const [nombre, setNombre] = useState('')
  const [direccion, setDireccion] = useState('')
  const [ciudad, setCiudad] = useState('')
  const [barrio, setBarrio] = useState('')
  const [latitud, setLatitud] = useState('')
  const [longitud, setLongitud] = useState('')
  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [accionEnCurso, setAccionEnCurso] = useState(false)

  async function cargarSede() {
    if (!token || !empresaId || !sedeId) return

    setCargando(true)
    setMensajeError(null)

    try {
      const sedeCargada = await obtenerSede(Number(empresaId), Number(sedeId), token)
      setSede(sedeCargada)
      setNombre(sedeCargada.nombre)
      setDireccion(sedeCargada.direccion)
      setCiudad(sedeCargada.ciudad)
      setBarrio(sedeCargada.barrio)
      setLatitud(sedeCargada.latitud === null ? '' : String(sedeCargada.latitud))
      setLongitud(sedeCargada.longitud === null ? '' : String(sedeCargada.longitud))
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar la sede.')
    } finally {
      setCargando(false)
    }
  }

  useEffect(() => {
    cargarSede()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [empresaId, sedeId, token])

  async function alGuardar(evento: FormEvent) {
    evento.preventDefault()
    if (!token || !empresaId || !sedeId) return

    setAccionEnCurso(true)
    setMensajeError(null)

    try {
      await actualizarSede(
        Number(empresaId),
        Number(sedeId),
        {
          nombre,
          direccion,
          ciudad,
          barrio,
          latitud: latitud.trim() === '' ? null : Number(latitud),
          longitud: longitud.trim() === '' ? null : Number(longitud),
        },
        token,
      )
      await cargarSede()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudieron guardar los cambios.')
    } finally {
      setAccionEnCurso(false)
    }
  }

  async function alCambiarEstado() {
    if (!token || !empresaId || !sedeId || !sede) return

    setAccionEnCurso(true)
    setMensajeError(null)

    try {
      if (sede.activa) {
        await desactivarSede(Number(empresaId), Number(sedeId), token)
      } else {
        await activarSede(Number(empresaId), Number(sedeId), token)
      }
      await cargarSede()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cambiar el estado de la sede.')
    } finally {
      setAccionEnCurso(false)
    }
  }

  if (cargando) {
    return (
      <main className="pagina-detalle-sede">
        <p>Cargando…</p>
      </main>
    )
  }

  if (!sede) {
    return (
      <main className="pagina-detalle-sede">
        <Link to={`/empresas/${empresaId}/sedes`} className="pagina-detalle-sede__volver">
          ← Volver a sedes
        </Link>
        <p role="alert">{mensajeError ?? 'La sede no existe.'}</p>
      </main>
    )
  }

  return (
    <main className="pagina-detalle-sede">
      <Link to={`/empresas/${empresaId}/sedes`} className="pagina-detalle-sede__volver">
        ← Volver a sedes
      </Link>

      <header className="pagina-detalle-sede__encabezado">
        <h1>{sede.nombre}</h1>
        <div className="pagina-detalle-sede__estado">
          <EtiquetaEstado activo={sede.activa} />
          <BotonSecundario onClick={alCambiarEstado} disabled={accionEnCurso}>
            {sede.activa ? 'Desactivar' : 'Activar'}
          </BotonSecundario>
        </div>
      </header>

      {mensajeError && (
        <p className="pagina-detalle-sede__error" role="alert">
          {mensajeError}
        </p>
      )}

      <form className="pagina-detalle-sede__formulario" onSubmit={alGuardar}>
        <div className="pagina-detalle-sede__campos">
          <CampoFormulario id="nombreSede" etiqueta="Nombre" valor={nombre} alCambiar={setNombre} requerido />
          <CampoFormulario id="direccionSede" etiqueta="Dirección" valor={direccion} alCambiar={setDireccion} requerido />
          <CampoFormulario id="ciudadSede" etiqueta="Ciudad" valor={ciudad} alCambiar={setCiudad} requerido />
          <CampoFormulario id="barrioSede" etiqueta="Barrio" valor={barrio} alCambiar={setBarrio} requerido />
          <CampoFormulario id="latitudSede" etiqueta="Latitud (opcional)" tipo="number" valor={latitud} alCambiar={setLatitud} />
          <CampoFormulario id="longitudSede" etiqueta="Longitud (opcional)" tipo="number" valor={longitud} alCambiar={setLongitud} />
        </div>
        <BotonPrimario disabled={accionEnCurso}>Guardar cambios</BotonPrimario>
      </form>
    </main>
  )
}
