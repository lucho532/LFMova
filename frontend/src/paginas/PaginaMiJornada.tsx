import { useEffect, useMemo, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { TarjetaUnidadTrabajo } from '../componentes/TarjetaUnidadTrabajo'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { useNotificaciones } from '../contexto/useNotificaciones'
import { EstadoServicio, NOMBRES_ESTADO_SERVICIO, NOMBRES_TIPO_SERVICIO, formatearHora, nombreDe } from '../modelos/enumeraciones'
import type { UnidadDeTrabajo } from '../modelos/conductor'
import type { Servicio } from '../modelos/operacion'
import { ErrorApi } from '../servicios/clienteHttp'
import { obtenerMisServiciosConductor, obtenerMisUnidades } from '../servicios/servicioConductorPropio'
import './PaginaMiJornada.css'

const ESTADOS_CERRADOS: number[] = [EstadoServicio.FINALIZADO, EstadoServicio.CANCELADO]

type Pestana = 'asignadas' | 'finalizadas' | 'vehiculo'

/** Las horas reales llegan en UTC; se muestran en la hora local del dispositivo. */
function horaLocal(iso: string | null): string {
  if (!iso) return '—'
  const conZona = /[zZ]|[+-]\d{2}:?\d{2}$/.test(iso) ? iso : `${iso}Z`
  return new Date(conZona).toLocaleTimeString('es', { hour: '2-digit', minute: '2-digit' })
}

/** Duración de la ruta entre el inicio y el fin reales, por ejemplo "1 h 13 min". */
function duracion(inicio: string | null, fin: string | null): string {
  if (!inicio || !fin) return ''
  const aUtc = (iso: string) => new Date(/[zZ]|[+-]\d{2}:?\d{2}$/.test(iso) ? iso : `${iso}Z`).getTime()
  const minutos = Math.max(0, Math.round((aUtc(fin) - aUtc(inicio)) / 60000))
  return minutos >= 60 ? `${Math.floor(minutos / 60)} h ${minutos % 60} min` : `${minutos} min`
}

function rutaServicio(servicio: Servicio) {
  return `/conductor/servicios/${servicio.empresaId}/${servicio.jornadaId}/${servicio.servicioId}`
}

/**
 * Pantalla principal del conductor (pensada para el celular), con tres
 * variantes según la ruta: "Rutas asignadas" (la que está en curso o la
 * próxima, y las que siguen), "Rutas finalizadas" y "Datos vehículo" (la
 * ficha de su unidad de trabajo). La navegación entre las tres vive en
 * `BarraLateral` (`/conductor`, `/conductor/finalizadas`,
 * `/conductor/vehiculo`); esta pantalla no repite esos controles. Los
 * servicios vienen de la API (`mis-servicios`); aquí solo se agrupan y
 * ordenan.
 */
export function PaginaMiJornada() {
  const { token } = useAutenticacion()
  const { version } = useNotificaciones()
  const location = useLocation()
  const [servicios, setServicios] = useState<Servicio[]>([])
  const [unidades, setUnidades] = useState<UnidadDeTrabajo[]>([])
  const [cargando, setCargando] = useState(true)
  const [actualizando, setActualizando] = useState(false)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const pestana: Pestana = location.pathname.endsWith('/finalizadas') ? 'finalizadas' : location.pathname.endsWith('/vehiculo') ? 'vehiculo' : 'asignadas'

  async function cargarDatos(tokenActual: string) {
    const [resultadoServicios, resultadoUnidades] = await Promise.allSettled([obtenerMisServiciosConductor(tokenActual), obtenerMisUnidades(tokenActual)])
    if (resultadoServicios.status === 'fulfilled') {
      setServicios(resultadoServicios.value)
      setMensajeError(null)
    } else {
      setMensajeError(resultadoServicios.reason instanceof ErrorApi ? resultadoServicios.reason.message : 'No se pudieron cargar tus servicios.')
    }
    setUnidades(resultadoUnidades.status === 'fulfilled' ? resultadoUnidades.value : [])
  }

  // Se recarga cuando llega una notificación nueva (ruta publicada, reasignación…), sin esperar a que la persona refresque a mano.
  useEffect(() => {
    if (!token) return
    setCargando(true)
    cargarDatos(token).finally(() => setCargando(false))
  }, [token, version])

  async function alRefrescar() {
    if (!token || actualizando) return
    setActualizando(true)
    await cargarDatos(token)
    setActualizando(false)
  }

  // Pide el permiso de ubicación apenas entra a su jornada, para que ya esté concedido cuando haga falta (llegada, finalizar ruta).
  useEffect(() => {
    if (navigator.geolocation) navigator.geolocation.getCurrentPosition(() => {}, () => {})
  }, [])

  const { proximo, siguientes, cerrados } = useMemo(() => {
    const clave = (s: Servicio) => s.fecha + s.horaProgramada
    const abiertos = servicios.filter((s) => !ESTADOS_CERRADOS.includes(s.estado)).sort((a, b) => clave(a).localeCompare(clave(b)))
    const enCurso = abiertos.find((s) => s.estado === EstadoServicio.EN_CURSO)
    const principal = enCurso ?? abiertos[0] ?? null
    return {
      proximo: principal,
      siguientes: abiertos.filter((s) => s !== principal),
      cerrados: servicios.filter((s) => ESTADOS_CERRADOS.includes(s.estado)).sort((a, b) => clave(b).localeCompare(clave(a))),
    }
  }, [servicios])

  return (
    <main className="pagina-mi-jornada">
      <div className="pagina-mi-jornada__encabezado">
        <h1>Mi jornada</h1>
        <button
          type="button"
          className={`pagina-mi-jornada__refrescar${actualizando ? ' pagina-mi-jornada__refrescar--girando' : ''}`}
          onClick={alRefrescar}
          disabled={actualizando}
          aria-label="Actualizar"
          title="Actualizar"
        >
          <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <path d="M21 12a9 9 0 1 1-3-6.7" />
            <polyline points="21 3 21 9 15 9" />
          </svg>
        </button>
      </div>

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      {cargando && <p>Cargando…</p>}

      {!cargando && !mensajeError && pestana === 'asignadas' && (
        <>
          {!proximo && siguientes.length === 0 && (
            <section className="pagina-mi-jornada__vacio">
              <h2>No tienes rutas asignadas</h2>
              <p>Cuando un coordinador te asigne un servicio, aparecerá aquí.</p>
            </section>
          )}

          {proximo && (
            <section className="pagina-mi-jornada__proximo">
              <span className="pagina-mi-jornada__etiqueta">{proximo.estado === EstadoServicio.EN_CURSO ? 'Servicio en curso' : 'Próximo servicio'}</span>
              <strong className="pagina-mi-jornada__hora">{formatearHora(proximo.horaProgramada)}</strong>
              <span className="pagina-mi-jornada__tipo">{nombreDe(NOMBRES_TIPO_SERVICIO, proximo.tipo)}</span>
              <span className="pagina-mi-jornada__detalle">
                {proximo.fecha} · {nombreDe(NOMBRES_ESTADO_SERVICIO, proximo.estado)}
                {proximo.estado === EstadoServicio.EN_CURSO && proximo.horaInicioReal ? ` · Inició ${horaLocal(proximo.horaInicioReal)}` : ''}
              </span>
              {proximo.nombreSede && <span className="pagina-mi-jornada__detalle">📍 {proximo.nombreSede}</span>}
              <Link to={rutaServicio(proximo)} className="pagina-mi-jornada__boton">
                Ver servicio
              </Link>
            </section>
          )}

          {siguientes.length > 0 && (
            <>
              <h2>Próximos</h2>
              <ul className="pagina-mi-jornada__lista">
                {siguientes.map((servicio) => (
                  <li key={servicio.servicioId}>
                    <Link to={rutaServicio(servicio)}>
                      <strong>{formatearHora(servicio.horaProgramada)}</strong> · {nombreDe(NOMBRES_TIPO_SERVICIO, servicio.tipo)}
                      <span>
                        {servicio.fecha} · {nombreDe(NOMBRES_ESTADO_SERVICIO, servicio.estado)}
                        {servicio.nombreSede && ` · 📍 ${servicio.nombreSede}`}
                      </span>
                    </Link>
                  </li>
                ))}
              </ul>
            </>
          )}
        </>
      )}

      {!cargando && !mensajeError && pestana === 'finalizadas' && (
        <>
          {cerrados.length === 0 ? (
            <p className="pagina-mi-jornada__vacio-pestana">Todavía no tienes rutas finalizadas.</p>
          ) : (
            <ul className="pagina-mi-jornada__lista pagina-mi-jornada__lista--cerrados">
              {cerrados.map((servicio) => (
                <li key={servicio.servicioId}>
                  <Link to={rutaServicio(servicio)}>
                    <strong>{formatearHora(servicio.horaProgramada)}</strong> · {nombreDe(NOMBRES_TIPO_SERVICIO, servicio.tipo)}
                    <span>
                      {servicio.fecha} · {nombreDe(NOMBRES_ESTADO_SERVICIO, servicio.estado)}
                      {servicio.nombreSede && ` · 📍 ${servicio.nombreSede}`}
                    </span>
                    <span className="pagina-mi-jornada__tiempos">
                      Inició <b>{horaLocal(servicio.horaInicioReal)}</b> · Finalizó <b>{horaLocal(servicio.horaFinReal)}</b>
                      {duracion(servicio.horaInicioReal, servicio.horaFinReal) && ` · Duración ${duracion(servicio.horaInicioReal, servicio.horaFinReal)}`}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </>
      )}

      {!cargando && !mensajeError && pestana === 'vehiculo' && (
        <>
          {unidades.length === 0 ? (
            <p className="pagina-mi-jornada__vacio-pestana">Todavía no tienes una unidad de trabajo registrada.</p>
          ) : (
            <div className="pagina-mi-jornada__unidades">
              {unidades.map((unidad) => (
                <TarjetaUnidadTrabajo key={unidad.unidadOperativaId} unidad={unidad} />
              ))}
            </div>
          )}
        </>
      )}
    </main>
  )
}
