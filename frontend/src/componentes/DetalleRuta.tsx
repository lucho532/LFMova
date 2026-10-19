import { useEffect, useRef, useState } from 'react'
import { NOMBRES_ESTADO_SERVICIO, NOMBRES_TIPO_SERVICIO, formatearHora, nombreDe } from '../modelos/enumeraciones'
import type { Servicio, ServicioPasajero } from '../modelos/operacion'
import type { FiltroAsistencia } from '../servicios/ordenRuta'
import { obtenerIncidenciasRuta, obtenerPasajeros, obtenerServicio, type IncidenciaRuta } from '../servicios/servicioOperacion'
import { BarraAsistenciaRuta } from './BarraAsistenciaRuta'
import { DetallePasajeroRuta } from './DetallePasajeroRuta'
import { ListaPasajerosRuta } from './ListaPasajerosRuta'
import { MensajeAlerta } from './MensajeAlerta'
import { ResumenEjecucionRuta } from './ResumenEjecucionRuta'
import '../estilos/componentes/DetalleRuta.css'

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
  /** Cédula del trabajador a mostrar cuando se llega desde la búsqueda de un empleado; sin ella se muestra la ruta completa. */
  resaltarCedula?: string | null
  token: string
  alCerrar: () => void
}

/**
 * Detalle de una ruta para el coordinador, con dos vistas. La de la ruta muestra cuándo se inició y
 * finalizó (con sus ubicaciones) y el listado de pasajeros en el orden en que se atendieron. La del
 * trabajador muestra solo lo suyo (estado, espera, llamadas, incidencias con foto y mapa) y se abre al
 * llegar desde la búsqueda de un empleado o con «Ver detalle» en la lista de la ruta.
 */
export function DetalleRuta({ abierto, empresaId, jornadaId, servicioId, encabezado, resaltarCedula, token, alCerrar }: PropiedadesDetalleRuta) {
  const referencia = useRef<HTMLDialogElement>(null)
  const [servicioDetalle, setServicioDetalle] = useState<Servicio | null>(null)
  const [pasajeros, setPasajeros] = useState<ServicioPasajero[]>([])
  const [incidenciasPorPasajero, setIncidenciasPorPasajero] = useState<Map<number, IncidenciaRuta[]>>(new Map())
  const [cargando, setCargando] = useState(false)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  // Pasajero elegido con «Ver detalle» dentro de la ruta; no se usa si se llegó con una cédula.
  const [pasajeroElegidoId, setPasajeroElegidoId] = useState<number | null>(null)
  const [filtro, setFiltro] = useState<FiltroAsistencia>('TODOS')

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
    setPasajeroElegidoId(null)
    setFiltro('TODOS')
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
      })
      .catch(() => setMensajeError('No se pudo cargar el detalle de la ruta.'))
      .finally(() => setCargando(false))
  }, [abierto, empresaId, jornadaId, servicioId, token])

  const pasajeroActivo = pasajeros.find((p) => (resaltarCedula ? p.cedulaEmpleado === resaltarCedula : p.servicioPasajeroId === pasajeroElegidoId))
  const puedeVolver = pasajeroActivo !== undefined && !resaltarCedula

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

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

      {cargando ? (
        <p className="contenedor-pagina__estado">Cargando…</p>
      ) : pasajeroActivo && jornadaId !== null && servicioId !== null ? (
        <>
          {puedeVolver && (
            <button type="button" className="detalle-ruta__volver" onClick={() => setPasajeroElegidoId(null)}>
              ← Volver a la ruta
            </button>
          )}
          <DetallePasajeroRuta
            key={pasajeroActivo.servicioPasajeroId}
            pasajero={pasajeroActivo}
            incidencias={incidenciasPorPasajero.get(pasajeroActivo.servicioPasajeroId) ?? []}
            empresaId={empresaId}
            jornadaId={jornadaId}
            servicioId={servicioId}
            token={token}
          />
        </>
      ) : (
        <>
          {servicioDetalle && <ResumenEjecucionRuta servicio={servicioDetalle} />}
          {pasajeros.length === 0 || jornadaId === null || servicioId === null ? (
            <p className="contenedor-pagina__estado">Esta ruta no tiene pasajeros.</p>
          ) : (
            <>
              <BarraAsistenciaRuta
                pasajeros={pasajeros}
                filtro={filtro}
                alCambiarFiltro={setFiltro}
                empresaId={empresaId}
                jornadaId={jornadaId}
                servicioId={servicioId}
                token={token}
              />
              <ListaPasajerosRuta pasajeros={pasajeros} filtro={filtro} alVerDetalle={setPasajeroElegidoId} />
            </>
          )}
        </>
      )}

      <div className="detalle-ruta__acciones">
        <button type="button" className="detalle-ruta__cerrar" onClick={alCerrar}>
          Cerrar
        </button>
      </div>
    </dialog>
  )
}
