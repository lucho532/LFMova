import { useEffect, useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { BotonSecundario } from '../componentes/BotonSecundario'
import { BuscadorEmpleadoRuta } from '../componentes/BuscadorEmpleadoRuta'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { ContenedorPagina } from '../componentes/ContenedorPagina'
import { DetalleRuta, type EncabezadoDetalleRuta } from '../componentes/DetalleRuta'
import { EncabezadoPagina } from '../componentes/EncabezadoPagina'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { ModalConfirmacion } from '../componentes/ModalConfirmacion'
import { EstadoServicio, NOMBRES_ESTADO_SERVICIO, NOMBRES_TIPO_SERVICIO, formatearHora, nombreDe } from '../modelos/enumeraciones'
import type { PasajeroEmpresa, RutaEmpresa } from '../modelos/estadisticas'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { ErrorApi } from '../servicios/clienteHttp'
import { obtenerPasajerosEmpresa, obtenerRutasEmpresa } from '../servicios/servicioEstadisticas'
import { eliminarServicio } from '../servicios/servicioOperacion'
import '../estilos/paginas/PaginaRutasEmpresa.css'

/** Una ruta ya finalizada (ejecutada de verdad) no se puede eliminar: es operación real, no se puede deshacer. */
const ESTADOS_QUE_NO_SE_PUEDEN_ELIMINAR = new Set<number>([EstadoServicio.EN_CURSO, EstadoServicio.FINALIZADO])

const FILTROS = [
  { valor: 'programadas', texto: 'Programadas' },
  { valor: 'por-realizar', texto: 'Por realizar' },
  { valor: 'realizadas', texto: 'Realizadas' },
  { valor: 'canceladas', texto: 'Canceladas' },
  { valor: '', texto: 'Todas' },
]

/**
 * Detalle de las rutas de la empresa (lo que cuentan las tarjetas del
 * inicio): fecha, horario, sede, unidad y conductor asignados, pasajeros y
 * estado, con filtro por estado y por fechas. La gestión de pasajeros de
 * cada ruta se hace desde Programación, no desde acá.
 */
export function PaginaRutasEmpresa() {
  const { empresaId } = useParams<{ empresaId: string }>()
  const { token } = useAutenticacion()
  const [parametros, setParametros] = useSearchParams()
  const filtro = parametros.get('filtro') ?? 'programadas'
  const [desde, setDesde] = useState('')
  const [hasta, setHasta] = useState('')
  const [rutas, setRutas] = useState<RutaEmpresa[]>([])
  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [rutaAEliminar, setRutaAEliminar] = useState<RutaEmpresa | null>(null)
  const [eliminando, setEliminando] = useState(false)
  const [errorEliminar, setErrorEliminar] = useState<string | null>(null)
  const [pasajerosBusqueda, setPasajerosBusqueda] = useState<PasajeroEmpresa[]>([])
  const [detalle, setDetalle] = useState<{ jornadaId: number; servicioId: number; encabezado: EncabezadoDetalleRuta; resaltarCedula: string | null } | null>(null)

  function cargar() {
    if (!token || !empresaId) return
    setCargando(true)
    obtenerRutasEmpresa(Number(empresaId), filtro, desde, hasta, token)
      .then(setRutas)
      .catch((error) => setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudieron cargar las rutas.'))
      .finally(() => setCargando(false))
  }

  useEffect(cargar, [empresaId, filtro, desde, hasta, token])

  useEffect(() => {
    if (!token || !empresaId) return
    obtenerPasajerosEmpresa(Number(empresaId), false, desde, hasta, token)
      .then(setPasajerosBusqueda)
      .catch(() => setPasajerosBusqueda([]))
  }, [empresaId, desde, hasta, token])

  function abrirDetalleDeRuta(ruta: RutaEmpresa) {
    setDetalle({
      jornadaId: ruta.jornadaId,
      servicioId: ruta.servicioId,
      encabezado: { fecha: ruta.fecha, hora: ruta.hora, tipo: ruta.tipo, sede: ruta.sede, conductor: ruta.conductor, placa: ruta.placa, estado: ruta.estado },
      resaltarCedula: null,
    })
  }

  function abrirDetalleDesdeBusqueda(pasajero: PasajeroEmpresa) {
    setDetalle({
      jornadaId: pasajero.jornadaId,
      servicioId: pasajero.servicioId,
      encabezado: { fecha: pasajero.fecha, hora: pasajero.hora, tipo: pasajero.tipo, sede: pasajero.sede, conductor: pasajero.conductor },
      resaltarCedula: pasajero.cedula,
    })
  }

  async function confirmarEliminar() {
    if (!rutaAEliminar || !empresaId || !token) return
    setEliminando(true)
    setErrorEliminar(null)
    try {
      await eliminarServicio(Number(empresaId), rutaAEliminar.jornadaId, rutaAEliminar.servicioId, token)
      setRutaAEliminar(null)
      cargar()
    } catch (error) {
      setErrorEliminar(error instanceof ErrorApi ? error.message : 'No se pudo eliminar la ruta.')
    } finally {
      setEliminando(false)
    }
  }

  const totalPasajeros = rutas.reduce((suma, r) => suma + r.pasajeros, 0)

  return (
    <ContenedorPagina ancho="amplio" volverA={`/empresas/${empresaId}`} textoVolver="← Inicio">
      <EncabezadoPagina titulo="Rutas" subtitulo="Horarios, unidades asignadas y avance de cada ruta." />

      <BuscadorEmpleadoRuta pasajeros={pasajerosBusqueda} alSeleccionar={abrirDetalleDesdeBusqueda} />

      <div className="rutas-empresa__filtros">
        {FILTROS.map((f) => (
          <button
            key={f.valor}
            type="button"
            className={`rutas-empresa__pestana${filtro === f.valor ? ' rutas-empresa__pestana--activa' : ''}`}
            onClick={() => setParametros(f.valor ? { filtro: f.valor } : { filtro: '' })}
          >
            {f.texto}
          </button>
        ))}
      </div>

      <div className="rutas-empresa__fechas">
        <CampoFormulario id="rutasDesde" etiqueta="Desde" tipo="date" valor={desde} alCambiar={setDesde} />
        <CampoFormulario id="rutasHasta" etiqueta="Hasta" tipo="date" valor={hasta} alCambiar={setHasta} />
        {(desde || hasta) && (
          <BotonSecundario
            type="button"
            onClick={() => {
              setDesde('')
              setHasta('')
            }}
          >
            Quitar fechas
          </BotonSecundario>
        )}
        <p className="rutas-empresa__total">
          {rutas.length} rutas · {totalPasajeros} pasajeros
        </p>
      </div>

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      {errorEliminar && <MensajeAlerta tipo="error">{errorEliminar}</MensajeAlerta>}

      {cargando ? (
        <p className="contenedor-pagina__estado">Cargando…</p>
      ) : rutas.length === 0 ? (
        <p className="contenedor-pagina__estado">No hay rutas con este filtro.</p>
      ) : (
        <div className="tabla-datos">
          <table>
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Hora</th>
                <th>Tipo</th>
                <th>Sede</th>
                <th>Unidad</th>
                <th>Pasajeros</th>
                <th>Estado</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {rutas.map((ruta) => (
                <tr key={ruta.servicioId} className="rutas-empresa__fila" onClick={() => abrirDetalleDeRuta(ruta)}>
                  <td>{ruta.fecha}</td>
                  <td>{formatearHora(ruta.hora)}</td>
                  <td>{nombreDe(NOMBRES_TIPO_SERVICIO, ruta.tipo)}</td>
                  <td>{ruta.sede}</td>
                  <td>
                    {ruta.conductor ? (
                      <>
                        {ruta.conductor}
                        <div className="rutas-empresa__sub">{ruta.placa}</div>
                      </>
                    ) : (
                      <span className="rutas-empresa__sin-unidad">Sin unidad</span>
                    )}
                  </td>
                  <td>
                    {ruta.pasajerosTransportados}/{ruta.pasajeros}
                  </td>
                  <td>{nombreDe(NOMBRES_ESTADO_SERVICIO, ruta.estado)}</td>
                  <td className="rutas-empresa__celda-eliminar">
                    {!ESTADOS_QUE_NO_SE_PUEDEN_ELIMINAR.has(ruta.estado) && (
                      <button
                        type="button"
                        className="rutas-empresa__boton-eliminar"
                        title="Eliminar esta ruta"
                        onClick={(evento) => {
                          evento.stopPropagation()
                          setRutaAEliminar(ruta)
                        }}
                      >
                        🗑
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <ModalConfirmacion
        abierto={rutaAEliminar !== null}
        titulo="Eliminar ruta"
        mensaje={`¿Eliminar la ruta de las ${rutaAEliminar ? formatearHora(rutaAEliminar.hora) : ''} del ${rutaAEliminar?.fecha ?? ''}? Se borran también sus pasajeros asignados (sin dejar registro); si tenía conductor, se le notifica. No se puede deshacer.`}
        textoConfirmar={eliminando ? 'Eliminando…' : 'Eliminar'}
        alConfirmar={confirmarEliminar}
        alCancelar={() => setRutaAEliminar(null)}
      />
      <p className="rutas-empresa__ayuda">
        Haz clic en una ruta para ver sus pasajeros y detalles. <Link to={`/empresas/${empresaId}/programacion`}>Cargar más rutas</Link>
      </p>

      <DetalleRuta
        abierto={detalle !== null}
        empresaId={Number(empresaId)}
        jornadaId={detalle?.jornadaId ?? null}
        servicioId={detalle?.servicioId ?? null}
        encabezado={detalle?.encabezado ?? null}
        resaltarCedula={detalle?.resaltarCedula}
        token={token ?? ''}
        alCerrar={() => setDetalle(null)}
      />
    </ContenedorPagina>
  )
}
