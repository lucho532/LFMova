import { useEffect, useRef, useState } from 'react'
import { hoyColombia } from '../modelos/fechaColombia'
import type { FilaInformeAsistencia, InformeAsistenciaPeriodo } from '../modelos/informeAsistencia'
import { guardarArchivo } from '../servicios/archivos'
import { descargarInformeAsistenciaPeriodo, obtenerVistaPreviaInasistencias } from '../servicios/servicioInformeAsistencia'
import { CampoFormulario } from './CampoFormulario'
import { DetalleRuta } from './DetalleRuta'
import { MensajeAlerta } from './MensajeAlerta'
import { TablaInasistencias } from './TablaInasistencias'
import '../estilos/componentes/VistaPreviaInasistencias.css'

interface PropiedadesVistaPreviaInasistencias {
  abierto: boolean
  empresaId: number
  token: string
  alCerrar: () => void
}

/**
 * Vista preliminar de los pasajeros que no asistieron a las rutas de la empresa, con la opción de descargarlos
 * en un único Excel. Al abrirse muestra la fecha de hoy (hora de Colombia); se puede cambiar a otro día o a un
 * rango. Lo que se ve es exactamente lo que lleva el archivo. Al elegir un pasajero se abre su ficha completa
 * (la misma del detalle de ruta) sin salir de esta vista. Solo consulta y descarga: no cambia nada.
 */
export function VistaPreviaInasistencias({ abierto, empresaId, token, alCerrar }: PropiedadesVistaPreviaInasistencias) {
  const referencia = useRef<HTMLDialogElement>(null)
  const [desde, setDesde] = useState(hoyColombia)
  const [hasta, setHasta] = useState(hoyColombia)
  const [informe, setInforme] = useState<InformeAsistenciaPeriodo | null>(null)
  const [cargando, setCargando] = useState(false)
  const [descargando, setDescargando] = useState(false)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  // Pasajero elegido en la tabla: abre su ficha completa (incidencia, foto, ubicación y mapa) encima de esta vista.
  const [elegido, setElegido] = useState<FilaInformeAsistencia | null>(null)
  const rangoInvalido = Boolean(desde && hasta && hasta < desde)

  useEffect(() => {
    const dialogo = referencia.current
    if (!dialogo) return
    if (abierto && !dialogo.open) dialogo.showModal()
    if (!abierto && dialogo.open) dialogo.close()
  }, [abierto])

  // Cada vez que se abre vuelve a hoy: así siempre empieza con la jornada actual.
  useEffect(() => {
    if (!abierto) return
    const hoy = hoyColombia()
    setDesde(hoy)
    setHasta(hoy)
  }, [abierto])

  useEffect(() => {
    if (!abierto || !desde || !hasta || rangoInvalido) return
    let cancelado = false
    setCargando(true)
    setMensajeError(null)
    obtenerVistaPreviaInasistencias(empresaId, desde, hasta, token)
      .then((datos) => !cancelado && setInforme(datos))
      .catch(() => !cancelado && setMensajeError('No se pudo cargar la vista previa.'))
      .finally(() => !cancelado && setCargando(false))
    return () => {
      cancelado = true
    }
  }, [abierto, empresaId, desde, hasta, rangoInvalido, token])

  async function alDescargar() {
    setDescargando(true)
    setMensajeError(null)
    try {
      const { nombre, archivo } = await descargarInformeAsistenciaPeriodo(empresaId, desde, hasta, 'NO_ASISTIERON', token)
      guardarArchivo(nombre, archivo)
    } catch (causa) {
      setMensajeError(causa instanceof Error ? causa.message : 'No se pudo descargar el informe.')
    } finally {
      setDescargando(false)
    }
  }

  const filas = informe?.filas ?? []
  const sinFiltro = !desde || !hasta || rangoInvalido

  return (
    <>
      <dialog ref={referencia} className="vista-previa-inasistencias" onCancel={alCerrar} onClose={alCerrar}>
        <h2>Pasajeros que no asistieron</h2>
        <p className="vista-previa-inasistencias__sub">
          Vista previa de lo que lleva el Excel. Haz clic en un pasajero para ver su ficha completa: incidencia, foto, ubicación y mapa.
        </p>

        <div className="vista-previa-inasistencias__fechas">
          <CampoFormulario id="inasistenciasDesde" etiqueta="Desde" tipo="date" valor={desde} alCambiar={setDesde} />
          <CampoFormulario id="inasistenciasHasta" etiqueta="Hasta" tipo="date" valor={hasta} alCambiar={setHasta} />
          <p className="vista-previa-inasistencias__total">{sinFiltro || cargando ? '' : `${filas.length} pasajeros`}</p>
        </div>

        {rangoInvalido && <MensajeAlerta tipo="error">La fecha final no puede ser anterior a la inicial.</MensajeAlerta>}
        {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

        <div className="vista-previa-inasistencias__contenido">
          {sinFiltro ? (
            <p className="contenedor-pagina__estado">Elige las fechas para ver el informe.</p>
          ) : cargando ? (
            <p className="contenedor-pagina__estado">Cargando…</p>
          ) : filas.length === 0 ? (
            <p className="contenedor-pagina__estado">Nadie dejó de asistir en las rutas de este período.</p>
          ) : (
            <TablaInasistencias filas={filas} alSeleccionar={setElegido} />
          )}
        </div>

        <div className="vista-previa-inasistencias__acciones">
          <button type="button" className="vista-previa-inasistencias__cerrar" onClick={alCerrar}>
            Cerrar
          </button>
          <button
            type="button"
            className="vista-previa-inasistencias__descargar"
            disabled={descargando || cargando || sinFiltro || filas.length === 0}
            title={filas.length === 0 ? 'No hay pasajeros para descargar en este período.' : undefined}
            onClick={alDescargar}
          >
            {descargando ? 'Descargando…' : '⬇ Descargar Excel'}
          </button>
        </div>
      </dialog>
      <DetalleRuta
        abierto={elegido !== null}
        empresaId={empresaId}
        jornadaId={elegido?.jornadaId ?? null}
        servicioId={elegido?.servicioId ?? null}
        encabezado={
          elegido && {
            fecha: elegido.fecha,
            hora: elegido.horaRuta,
            tipo: elegido.tipo,
            sede: elegido.sede,
            conductor: elegido.conductor,
            placa: elegido.placa,
          }
        }
        resaltarCedula={elegido?.cedula}
        token={token}
        alCerrar={() => setElegido(null)}
      />
    </>
  )
}
