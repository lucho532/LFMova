import { useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { BotonSecundario } from '../componentes/BotonSecundario'
import { EtiquetaEstado } from '../componentes/EtiquetaEstado'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { Sede } from '../modelos/sede'
import { ErrorApi } from '../servicios/clienteHttp'
import { activarSede, desactivarSede, obtenerSedes } from '../servicios/servicioSedes'
import '../estilos/paginas/PaginaSedes.css'

/**
 * Lista las sedes de la empresa y permite activar o desactivar cada una.
 * Requiere rol COORDINADOR de esa empresa en el backend.
 */
export function PaginaSedes() {
  const { empresaId } = useParams<{ empresaId: string }>()
  const { token } = useAutenticacion()
  const [sedes, setSedes] = useState<Sede[]>([])
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [cargando, setCargando] = useState(true)
  const [sedeIdEnCurso, setSedeIdEnCurso] = useState<number | null>(null)

  async function cargarSedes() {
    if (!token || !empresaId) return

    setCargando(true)
    setMensajeError(null)

    try {
      setSedes(await obtenerSedes(Number(empresaId), token))
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudieron cargar las sedes.')
    } finally {
      setCargando(false)
    }
  }

  useEffect(() => {
    cargarSedes()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [empresaId, token])

  const sedesOrdenadas = useMemo(() => [...sedes].sort((a, b) => a.nombre.localeCompare(b.nombre, 'es')), [sedes])

  async function alCambiarEstado(sede: Sede) {
    if (!token || !empresaId) return

    setSedeIdEnCurso(sede.sedeId)
    setMensajeError(null)

    try {
      if (sede.activa) {
        await desactivarSede(Number(empresaId), sede.sedeId, token)
      } else {
        await activarSede(Number(empresaId), sede.sedeId, token)
      }
      await cargarSedes()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cambiar el estado de la sede.')
    } finally {
      setSedeIdEnCurso(null)
    }
  }

  return (
    <main className="pagina-sedes">
      <header className="pagina-sedes__encabezado">
        <div>
          <h1>Sedes</h1>
          <p className="pagina-sedes__subtitulo">Sedes registradas para esta empresa.</p>
        </div>
        <Link to={`/empresas/${empresaId}/sedes/nueva`} className="boton-primario-enlace">
          + Nueva sede
        </Link>
      </header>

      {mensajeError && (
        <p className="pagina-sedes__error" role="alert">
          {mensajeError}
        </p>
      )}

      {cargando ? (
        <p className="pagina-sedes__estado">Cargando…</p>
      ) : sedesOrdenadas.length === 0 ? (
        <p className="pagina-sedes__estado">Todavía no hay sedes registradas.</p>
      ) : (
        <div className="pagina-sedes__tabla-envoltorio">
          <table className="pagina-sedes__tabla">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Dirección</th>
                <th>Ciudad</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {sedesOrdenadas.map((sede) => (
                <tr key={sede.sedeId}>
                  <td>
                    <Link to={`/empresas/${empresaId}/sedes/${sede.sedeId}`} className="pagina-sedes__enlace">
                      {sede.nombre}
                    </Link>
                  </td>
                  <td>{sede.direccion}</td>
                  <td>{sede.ciudad}</td>
                  <td>
                    <EtiquetaEstado activo={sede.activa} />
                  </td>
                  <td className="pagina-sedes__celda-accion">
                    <BotonSecundario onClick={() => alCambiarEstado(sede)} disabled={sedeIdEnCurso === sede.sedeId}>
                      {sede.activa ? 'Desactivar' : 'Activar'}
                    </BotonSecundario>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </main>
  )
}
