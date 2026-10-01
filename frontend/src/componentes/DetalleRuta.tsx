import { useEffect, useRef, useState } from 'react'
import { EstadoPasajero, NOMBRES_ESTADO_PASAJERO, NOMBRES_ESTADO_SERVICIO, NOMBRES_TIPO_SERVICIO, formatearHora, nombreDe } from '../modelos/enumeraciones'
import type { Servicio, ServicioPasajero } from '../modelos/operacion'
import { obtenerIncidenciasRuta, obtenerPasajeros, obtenerServicio, obtenerUrlFotoEvidenciaRuta, type IncidenciaRuta } from '../servicios/servicioOperacion'
import { MensajeAlerta } from './MensajeAlerta'
import '../estilos/componentes/DetalleRuta.css'

/** Mismo orden que el enum TipoIncidencia del backend (ver HerramientasPasajero.tsx). */
const TIPOS_INCIDENCIA = [
  { valor: 0, texto: 'No contesta' },
  { valor: 1, texto: 'No se encuentra' },
  { valor: 2, texto: 'Dirección incorrecta' },
  { valor: 3, texto: 'No se pudo recoger' },
  { valor: 4, texto: 'Ubicación modificada' },
  { valor: 5, texto: 'Otra' },
]

function etiquetaTipoIncidencia(tipo: number): string {
  return TIPOS_INCIDENCIA.find((t) => t.valor === tipo)?.texto ?? 'Incidencia'
}

function enlaceMapa(latitud: number, longitud: number): string {
  return `https://www.google.com/maps/search/?api=1&query=${latitud},${longitud}`
}

/** Las horas reales llegan en UTC sin zona explícita; se muestran en la hora local del dispositivo. */
function horaLocal(iso: string | null): string | null {
  if (!iso) return null
  const conZona = /[zZ]|[+-]\d{2}:?\d{2}$/.test(iso) ? iso : `${iso}Z`
  return new Date(conZona).toLocaleTimeString('es', { hour: '2-digit', minute: '2-digit' })
}

/** Duración entre dos momentos ISO, por ejemplo "1 h 13 min". */
function duracion(inicio: string | null, fin: string | null): string | null {
  if (!inicio || !fin) return null
  const aUtc = (iso: string) => new Date(/[zZ]|[+-]\d{2}:?\d{2}$/.test(iso) ? iso : `${iso}Z`).getTime()
  const minutos = Math.max(0, Math.round((aUtc(fin) - aUtc(inicio)) / 60000))
  return minutos >= 60 ? `${Math.floor(minutos / 60)} h ${minutos % 60} min` : `${minutos} min`
}

/** Encabezado de la ruta a mostrar, con los datos que ya tenga disponibles quien abre el detalle. */
export interface EncabezadoDetalleRuta {
  fecha: string
  hora: string
  tipo: number
  sede: string
  conductor: string | null
  placa?: string | null
  estado?: number
}

interface PropiedadesDetalleRuta {
  abierto: boolean
  empresaId: number
  jornadaId: number | null
  servicioId: number | null
  encabezado: EncabezadoDetalleRuta | null
  /** Cédula a resaltar dentro de la lista (cuando se llega aquí desde la búsqueda de un empleado). */
  resaltarCedula?: string | null
  token: string
  alCerrar: () => void
}

/**
 * Detalle completo de una ruta para el coordinador: todos sus pasajeros con
 * su estado, y las incidencias que se hayan reportado sobre cada uno
 * (incluyendo sus fotos de evidencia y ubicación). Reutilizable tanto al
 * hacer clic en una ruta de la lista como al llegar desde la búsqueda de un
 * empleado por cédula.
 */
export function DetalleRuta({ abierto, empresaId, jornadaId, servicioId, encabezado, resaltarCedula, token, alCerrar }: PropiedadesDetalleRuta) {
  const referencia = useRef<HTMLDialogElement>(null)
  const [servicioDetalle, setServicioDetalle] = useState<Servicio | null>(null)
  const [pasajeros, setPasajeros] = useState<ServicioPasajero[]>([])
  const [incidenciasPorPasajero, setIncidenciasPorPasajero] = useState<Map<number, IncidenciaRuta[]>>(new Map())
  const [cargando, setCargando] = useState(false)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [expandidos, setExpandidos] = useState<Set<number>>(new Set())
  const [fotos, setFotos] = useState<Record<number, string>>({})
  const [cargandoFotoId, setCargandoFotoId] = useState<number | null>(null)
  const fotosRef = useRef(fotos)
  fotosRef.current = fotos

  useEffect(() => {
    const dialogo = referencia.current
    if (!dialogo) return
    if (abierto && !dialogo.open) dialogo.showModal()
    if (!abierto && dialogo.open) dialogo.close()
  }, [abierto])

  useEffect(() => {
    if (!abierto || jornadaId === null || servicioId === null) return
    setCargando(true)
    setMensajeError(null)
    setExpandidos(new Set())
    setFotos({})
    Promise.all([
      obtenerPasajeros(empresaId, jornadaId, servicioId, token),
      obtenerIncidenciasRuta(empresaId, jornadaId, servicioId, token),
      obtenerServicio(empresaId, jornadaId, servicioId, token),
    ])
      .then(([listaPasajeros, incidencias, servicio]) => {
        setPasajeros(listaPasajeros)
        setServicioDetalle(servicio)
        const agrupadas = new Map<number, IncidenciaRuta[]>()
        for (const incidencia of incidencias) {
          const lista = agrupadas.get(incidencia.servicioPasajeroId) ?? []
          lista.push(incidencia)
          agrupadas.set(incidencia.servicioPasajeroId, lista)
        }
        setIncidenciasPorPasajero(agrupadas)
        if (resaltarCedula) {
          const pasajeroResaltado = listaPasajeros.find((p) => p.cedulaEmpleado === resaltarCedula)
          if (pasajeroResaltado && agrupadas.has(pasajeroResaltado.servicioPasajeroId)) {
            setExpandidos(new Set([pasajeroResaltado.servicioPasajeroId]))
          }
        }
      })
      .catch(() => setMensajeError('No se pudo cargar el detalle de la ruta.'))
      .finally(() => setCargando(false))
  }, [abierto, empresaId, jornadaId, servicioId, token, resaltarCedula])

  useEffect(() => {
    return () => {
      Object.values(fotosRef.current).forEach((url) => URL.revokeObjectURL(url))
    }
  }, [servicioId])

  function alternarExpandido(servicioPasajeroId: number) {
    setExpandidos((actual) => {
      const nuevo = new Set(actual)
      if (nuevo.has(servicioPasajeroId)) nuevo.delete(servicioPasajeroId)
      else nuevo.add(servicioPasajeroId)
      return nuevo
    })
  }

  async function verFoto(servicioPasajeroId: number, incidenciaId: number, evidenciaId: number) {
    if (jornadaId === null || servicioId === null || fotos[evidenciaId]) return
    setCargandoFotoId(evidenciaId)
    try {
      const url = await obtenerUrlFotoEvidenciaRuta(empresaId, jornadaId, servicioId, servicioPasajeroId, incidenciaId, evidenciaId, token)
      setFotos((actual) => ({ ...actual, [evidenciaId]: url }))
    } catch {
      setMensajeError('No se pudo cargar la foto.')
    } finally {
      setCargandoFotoId(null)
    }
  }

  return (
    <dialog ref={referencia} className="detalle-ruta" onCancel={alCerrar} onClose={alCerrar}>
      {encabezado && (
        <div className="detalle-ruta__encabezado">
          <h2>
            Ruta del {encabezado.fecha} · {formatearHora(encabezado.hora)} · {nombreDe(NOMBRES_TIPO_SERVICIO, encabezado.tipo)}
          </h2>
          <p className="detalle-ruta__sub">
            {encabezado.sede}
            {encabezado.conductor && ` · ${encabezado.conductor}`}
            {encabezado.placa && ` (${encabezado.placa})`}
            {encabezado.estado !== undefined && ` · ${nombreDe(NOMBRES_ESTADO_SERVICIO, encabezado.estado)}`}
          </p>
        </div>
      )}

      {servicioDetalle && (servicioDetalle.horaInicioReal || servicioDetalle.horaFinReal) && (
        <div className="detalle-ruta__ejecucion">
          {servicioDetalle.horaInicioReal && (
            <span>
              🚗 Inició {horaLocal(servicioDetalle.horaInicioReal)} ·{' '}
              {servicioDetalle.latitudInicio !== null && servicioDetalle.longitudInicio !== null ? (
                <a href={enlaceMapa(servicioDetalle.latitudInicio, servicioDetalle.longitudInicio)} target="_blank" rel="noreferrer">
                  📍 ubicación de inicio
                </a>
              ) : (
                <span className="detalle-ruta__sin-ubicacion">sin ubicación registrada</span>
              )}
            </span>
          )}
          {servicioDetalle.horaFinReal && (
            <span>
              🏁 Finalizó {horaLocal(servicioDetalle.horaFinReal)} ·{' '}
              {servicioDetalle.latitudFinalizacion !== null && servicioDetalle.longitudFinalizacion !== null ? (
                <a href={enlaceMapa(servicioDetalle.latitudFinalizacion, servicioDetalle.longitudFinalizacion)} target="_blank" rel="noreferrer">
                  📍 ubicación de llegada
                </a>
              ) : (
                <span className="detalle-ruta__sin-ubicacion">sin ubicación registrada</span>
              )}
            </span>
          )}
          {duracion(servicioDetalle.horaInicioReal, servicioDetalle.horaFinReal) && <span>⏱ Duración: {duracion(servicioDetalle.horaInicioReal, servicioDetalle.horaFinReal)}</span>}
        </div>
      )}

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

      {cargando ? (
        <p className="contenedor-pagina__estado">Cargando…</p>
      ) : pasajeros.length === 0 ? (
        <p className="contenedor-pagina__estado">Esta ruta no tiene pasajeros.</p>
      ) : (
        <ul className="detalle-ruta__lista">
          {pasajeros.map((pasajero) => {
            const incidencias = incidenciasPorPasajero.get(pasajero.servicioPasajeroId) ?? []
            const expandido = expandidos.has(pasajero.servicioPasajeroId)
            const resaltado = resaltarCedula === pasajero.cedulaEmpleado
            return (
              <li key={pasajero.servicioPasajeroId} className={`detalle-ruta__pasajero${resaltado ? ' detalle-ruta__pasajero--resaltado' : ''}`}>
                <div className="detalle-ruta__fila">
                  <div>
                    <strong>{pasajero.nombreCompletoEmpleado}</strong>
                    <div className="detalle-ruta__sub">
                      CC {pasajero.cedulaEmpleado} · {pasajero.direccionRecogida}
                    </div>
                  </div>
                  <div className="detalle-ruta__estado">
                    {nombreDe(NOMBRES_ESTADO_PASAJERO, pasajero.estado)}
                    {pasajero.estado === EstadoPasajero.RECOGIDO && horaLocal(pasajero.horaProcesado) && (
                      <span className="detalle-ruta__hora-estado"> · {horaLocal(pasajero.horaProcesado)}</span>
                    )}
                  </div>
                </div>

                {incidencias.length > 0 && (
                  <button type="button" className="detalle-ruta__boton-incidencias" onClick={() => alternarExpandido(pasajero.servicioPasajeroId)}>
                    ⚠ {incidencias.length} incidencia{incidencias.length > 1 ? 's' : ''} {expandido ? '▲' : '▼'}
                  </button>
                )}

                {expandido && (
                  <ul className="detalle-ruta__incidencias">
                    {incidencias.map((incidencia) => (
                      <li key={incidencia.incidenciaId} className="detalle-ruta__incidencia">
                        <div>
                          <strong>{etiquetaTipoIncidencia(incidencia.tipo)}</strong> · <small>{new Date(incidencia.fechaHora).toLocaleString()}</small>
                        </div>
                        {incidencia.descripcion && <p className="detalle-ruta__descripcion">{incidencia.descripcion}</p>}
                        {incidencia.latitud !== null && incidencia.longitud !== null && (
                          <a href={enlaceMapa(incidencia.latitud, incidencia.longitud)} target="_blank" rel="noreferrer">
                            📍 Ver ubicación
                          </a>
                        )}
                        {incidencia.evidencias.map((evidencia) => (
                          <div key={evidencia.evidenciaId} className="detalle-ruta__foto">
                            {fotos[evidencia.evidenciaId] ? (
                              <img src={fotos[evidencia.evidenciaId]} alt="Evidencia de la incidencia" />
                            ) : (
                              <button
                                type="button"
                                onClick={() => verFoto(pasajero.servicioPasajeroId, incidencia.incidenciaId, evidencia.evidenciaId)}
                                disabled={cargandoFotoId === evidencia.evidenciaId}
                              >
                                {cargandoFotoId === evidencia.evidenciaId ? 'Cargando…' : '📷 Ver foto'}
                              </button>
                            )}
                          </div>
                        ))}
                      </li>
                    ))}
                  </ul>
                )}
              </li>
            )
          })}
        </ul>
      )}

      <div className="detalle-ruta__acciones">
        <button type="button" className="detalle-ruta__cerrar" onClick={alCerrar}>
          Cerrar
        </button>
      </div>
    </dialog>
  )
}
