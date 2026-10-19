import { Fragment, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { NOMBRES_ESTADO_SERVICIO, NOMBRES_TIPO_SERVICIO, formatearHora, nombreDe } from '../modelos/enumeraciones'
import type { Conductor } from '../modelos/conductor'
import type { EstadisticaConductor, RutaConductor } from '../modelos/estadisticas'
import { ErrorApi } from '../servicios/clienteHttp'
import { activarVinculacion, desactivarVinculacion, obtenerConductores } from '../servicios/servicioConductores'
import { obtenerEstadisticasConductores, obtenerRutasDeConductor } from '../servicios/servicioEstadisticas'
import { BotonSecundario } from './BotonSecundario'
import { MensajeAlerta } from './MensajeAlerta'
import { ModalConfirmacion } from './ModalConfirmacion'
import { VigenciaDocumento } from './VigenciaDocumento'
import '../estilos/componentes/ActividadConductores.css'

interface PropiedadesActividadConductores {
  empresaId: number
  /** Cambia cuando hay que volver a consultar (por ejemplo, tras agregar un conductor). */
  version: number
}

/**
 * Directorio de todos los conductores de la empresa: sus datos, los de su
 * vehículo (con la vigencia del SOAT y de la técnico-mecánica) y el número
 * de rutas que ha realizado. Cada fila se puede desplegar para ver el
 * detalle de esas rutas.
 */
export function ActividadConductores({ empresaId, version }: PropiedadesActividadConductores) {
  const { token } = useAutenticacion()
  const [conductores, setConductores] = useState<EstadisticaConductor[]>([])
  const [vinculaciones, setVinculaciones] = useState<Conductor[]>([])
  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [abierto, setAbierto] = useState<number | null>(null)
  const [rutas, setRutas] = useState<RutaConductor[]>([])
  const [conductorAEliminar, setConductorAEliminar] = useState<EstadisticaConductor | null>(null)
  const [procesandoAccion, setProcesandoAccion] = useState(false)
  const [errorAccion, setErrorAccion] = useState<string | null>(null)
  const [refresco, setRefresco] = useState(0)

  useEffect(() => {
    if (!token) return
    setCargando(true)
    setAbierto(null)
    Promise.all([obtenerEstadisticasConductores(empresaId, '', '', token), obtenerConductores(empresaId, token)])
      .then(([estadisticas, detalle]) => {
        setConductores(estadisticas)
        setVinculaciones(detalle)
      })
      .catch((error) => setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar el directorio.'))
      .finally(() => setCargando(false))
  }, [empresaId, token, version, refresco])

  /** Si no se pudo cargar todavía el detalle de vinculación, se asume activo para no mostrar el botón equivocado antes de tiempo. */
  function vinculacionActiva(conductorId: number): boolean {
    return vinculaciones.find((c) => c.conductorId === conductorId)?.empresaIdsVinculadosActivos.includes(empresaId) ?? true
  }

  async function confirmarEliminar() {
    if (!conductorAEliminar || !token) return
    setProcesandoAccion(true)
    setErrorAccion(null)
    try {
      await desactivarVinculacion(empresaId, conductorAEliminar.conductorId, token)
      setConductorAEliminar(null)
      setRefresco((r) => r + 1)
    } catch (error) {
      setErrorAccion(error instanceof ErrorApi ? error.message : 'No se pudo quitar al conductor de la empresa.')
    } finally {
      setProcesandoAccion(false)
    }
  }

  async function reactivar(conductorId: number) {
    if (!token) return
    setErrorAccion(null)
    try {
      await activarVinculacion(empresaId, conductorId, token)
      setRefresco((r) => r + 1)
    } catch (error) {
      setErrorAccion(error instanceof ErrorApi ? error.message : 'No se pudo reactivar al conductor.')
    }
  }

  async function alAlternar(conductorId: number) {
    if (!token) return
    if (abierto === conductorId) {
      setAbierto(null)
      return
    }
    try {
      setRutas(await obtenerRutasDeConductor(empresaId, conductorId, '', '', token))
      setAbierto(conductorId)
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudieron cargar las rutas.')
    }
  }

  return (
    <section className="actividad-conductores">
      <p className="actividad-conductores__totales">{conductores.length} conductores</p>

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      {errorAccion && <MensajeAlerta tipo="error">{errorAccion}</MensajeAlerta>}

      {cargando ? (
        <p className="contenedor-pagina__estado">Cargando…</p>
      ) : conductores.length === 0 ? (
        <p className="contenedor-pagina__estado">Todavía no hay conductores en esta empresa.</p>
      ) : (
        <div className="tabla-datos">
          <table>
            <thead>
              <tr>
                <th>Conductor</th>
                <th>Vehículo</th>
                <th>SOAT</th>
                <th>Técnico-mecánica</th>
                <th className="actividad-conductores__numero">Rutas realizadas</th>
                <th />
                <th />
              </tr>
            </thead>
            <tbody>
              {conductores.map((conductor) => {
                const activo = vinculacionActiva(conductor.conductorId)
                return (
                <Fragment key={conductor.conductorId}>
                  <tr className={activo ? undefined : 'actividad-conductores__fila-inactiva'}>
                    <td>
                      <Link to={`/empresas/${empresaId}/conductores/${conductor.conductorId}`}>{conductor.nombreCompleto}</Link>
                      <div className="actividad-conductores__sub">
                        CC {conductor.cedula} · {conductor.telefono}
                        {!activo && <span className="actividad-conductores__etiqueta-inactivo"> · Fuera de la empresa</span>}
                      </div>
                    </td>
                    <td>
                      {conductor.vehiculos.length === 0
                        ? '—'
                        : conductor.vehiculos.map((v) => (
                            <div key={v.placa}>
                              {v.placa} · {v.marca} {v.modelo} · {v.capacidad} puestos
                            </div>
                          ))}
                    </td>
                    <td>
                      {conductor.vehiculos.map((v) => (
                        <VigenciaDocumento key={v.placa} fecha={v.vigenciaSoat} />
                      ))}
                    </td>
                    <td>
                      {conductor.vehiculos.map((v) => (
                        <VigenciaDocumento key={v.placa} fecha={v.vigenciaTecnomecanica} />
                      ))}
                    </td>
                    <td className="actividad-conductores__numero">{conductor.rutasRealizadas}</td>
                    <td>
                      <BotonSecundario type="button" onClick={() => alAlternar(conductor.conductorId)}>
                        {abierto === conductor.conductorId ? 'Ocultar rutas' : 'Ver rutas'}
                      </BotonSecundario>
                    </td>
                    <td className="actividad-conductores__celda-eliminar">
                      {activo ? (
                        <button
                          type="button"
                          className="actividad-conductores__boton-eliminar"
                          title="Quitar de la empresa"
                          onClick={() => setConductorAEliminar(conductor)}
                        >
                          🗑
                        </button>
                      ) : (
                        <button type="button" className="actividad-conductores__boton-reactivar" onClick={() => reactivar(conductor.conductorId)}>
                          Reactivar
                        </button>
                      )}
                    </td>
                  </tr>
                  {abierto === conductor.conductorId && (
                    <tr className="actividad-conductores__detalle">
                      <td colSpan={7}>
                        {rutas.length === 0 ? (
                          <span>Este conductor todavía no tiene rutas.</span>
                        ) : (
                          <ul>
                            {rutas.map((ruta) => (
                              <li key={ruta.servicioId}>
                                <strong>{ruta.fecha}</strong> · {formatearHora(ruta.hora)} · {nombreDe(NOMBRES_TIPO_SERVICIO, ruta.tipo)} {ruta.sede} ·{' '}
                                {ruta.pasajerosTransportados}/{ruta.pasajeros} pasajeros · {nombreDe(NOMBRES_ESTADO_SERVICIO, ruta.estado)}
                              </li>
                            ))}
                          </ul>
                        )}
                      </td>
                    </tr>
                  )}
                </Fragment>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      <ModalConfirmacion
        abierto={conductorAEliminar !== null}
        titulo="Eliminar conductor"
        mensaje={`¿Quitar a ${conductorAEliminar?.nombreCompleto ?? ''} de esta empresa? Deja de estar disponible para nuevas rutas y no se le podrán asignar más; su historial de rutas ya realizadas se conserva. Si trabaja con otra empresa, no se ve afectado allí. Puedes volver a vincularlo cuando quieras.`}
        textoConfirmar={procesandoAccion ? 'Eliminando…' : 'Eliminar'}
        alConfirmar={confirmarEliminar}
        alCancelar={() => setConductorAEliminar(null)}
      />
    </section>
  )
}
