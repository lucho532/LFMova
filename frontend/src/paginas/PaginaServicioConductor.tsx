import { useEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { ListaArrastrable } from '../componentes/ListaArrastrable'
import { ModalConfirmacion } from '../componentes/ModalConfirmacion'
import { TarjetaPasajeroConductor } from '../componentes/TarjetaPasajeroConductor'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { useNotificaciones } from '../contexto/useNotificaciones'
import { EstadoPasajero, EstadoServicio, NOMBRES_ESTADO_SERVICIO, NOMBRES_TIPO_SERVICIO, formatearHora, nombreDe } from '../modelos/enumeraciones'
import type { Servicio, ServicioPasajero } from '../modelos/operacion'
import { ErrorApi } from '../servicios/clienteHttp'
import {
  cambiarEstadoPasajero,
  enviarMensaje,
  crearIncidencia,
  eliminarUbicacionGuardada,
  finalizarServicio,
  guardarUbicacionRecogida,
  iniciarServicio,
  marcarLlegada,
  obtenerIncidencias,
  obtenerMensajes,
  obtenerMisServiciosConductor,
  obtenerUrlFotoEvidencia,
  reordenarPasajeroConductor,
  obtenerUbicacionesAnteriores,
  subirFotoIncidencia,
} from '../servicios/servicioConductorPropio'
import { obtenerPasajeros } from '../servicios/servicioOperacion'
import { obtenerUsuarioIdDelToken } from '../servicios/tokenJwt'
import '../estilos/paginas/PaginaServicioConductor.css'

/** Ya no requiere acción del conductor: fue recogido, no se pudo recoger, avisó que no asistirá o se canceló. */
const ESTADOS_GESTIONADOS: number[] = [EstadoPasajero.RECOGIDO, EstadoPasajero.NO_RECOGIDO, EstadoPasajero.NO_ASISTIRA, EstadoPasajero.CANCELADO]

/** Intenta obtener la posición actual; nunca rechaza más allá de lo necesario (la ubicación siempre es opcional). */
function obtenerPosicionOpcional(): Promise<{ latitud: number; longitud: number } | null> {
  return new Promise((resolver) => {
    if (!navigator.geolocation) {
      resolver(null)
      return
    }
    navigator.geolocation.getCurrentPosition(
      (posicion) => resolver({ latitud: posicion.coords.latitude, longitud: posicion.coords.longitude }),
      () => resolver(null),
      { enableHighAccuracy: true, timeout: 10000 },
    )
  })
}

/**
 * Pantalla operativa de un servicio para el conductor (mobile-first, botones
 * grandes): iniciar, gestionar cada pasajero (llegada, espera, resultado,
 * llamar, chat) y finalizar. Los pasajeros ya gestionados (recogidos, no
 * recogidos, no asistieron o cancelados) se ven en una pestaña aparte de los
 * que todavía faltan por recoger, para no mezclar ambas listas en una sola y
 * dar pie a confusiones. Solo se ofrecen las acciones del estado actual; si
 * el backend rechaza una (por ejemplo, finalizar con pasajeros pendientes),
 * se muestra su mensaje tal cual. El servicio se toma de la lista propia del
 * conductor porque la consulta de detalle es solo del coordinador.
 */
export function PaginaServicioConductor() {
  const { empresaId, jornadaId, servicioId } = useParams<{ empresaId: string; jornadaId: string; servicioId: string }>()
  const { token } = useAutenticacion()
  const { version } = useNotificaciones()
  const navegar = useNavigate()
  // Si se llegó desde una notificación (?chat=id), se abre el chat de ese pasajero.
  const chatDeNotificacion = Number(useSearchParams()[0].get('chat')) || null
  const referencia = { empresaId: Number(empresaId), jornadaId: Number(jornadaId), servicioId: Number(servicioId) }

  const [servicio, setServicio] = useState<Servicio | null>(null)
  const [pasajeros, setPasajeros] = useState<ServicioPasajero[]>([])
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [confirmandoFin, setConfirmandoFin] = useState(false)
  const [ocupado, setOcupado] = useState(false)
  // Separadas en pestañas (no en una sola lista corrida) para que no se preste a confusión ver a quién
  // ya se gestionó junto con quién todavía falta por recoger.
  const [pestana, setPestana] = useState<'porRecoger' | 'gestionados'>('porRecoger')
  const [avisoFlotante, setAvisoFlotante] = useState<string | null>(null)
  const temporizadorAviso = useRef<number | null>(null)

  /**
   * Aviso a nivel de página (no de la tarjeta del pasajero): algunas acciones, como reportar una
   * incidencia, recargan la lista y pueden mover al pasajero a la otra pestaña o incluso desaparecer de la
   * vista actual, lo que desmonta su tarjeta antes de que un aviso local ahí adentro llegue a mostrarse.
   */
  function mostrarAvisoFlotante(texto: string) {
    if (temporizadorAviso.current) clearTimeout(temporizadorAviso.current)
    setAvisoFlotante(texto)
    temporizadorAviso.current = window.setTimeout(() => setAvisoFlotante(null), 6000)
  }

  useEffect(() => () => {
    if (temporizadorAviso.current) clearTimeout(temporizadorAviso.current)
  }, [])

  async function cargar() {
    if (!token) return
    try {
      const [propios, lista] = await Promise.all([
        obtenerMisServiciosConductor(token),
        obtenerPasajeros(Number(empresaId), Number(jornadaId), Number(servicioId), token),
      ])
      setServicio(propios.find((s) => s.servicioId === Number(servicioId)) ?? null)
      setPasajeros(lista)
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar el servicio.')
    }
  }

  useEffect(() => {
    cargar()
    // Se refresca solo para que el conductor vea de inmediato si un pasajero confirma, avisa que no
    // asistirá o comparte su ubicación (por ejemplo, para que "Navegar" nunca use un punto desactualizado).
    const temporizador = setInterval(() => {
      if (!document.hidden) cargar()
    }, 15000)
    return () => clearInterval(temporizador)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token, empresaId, jornadaId, servicioId, version])

  // Pide el permiso de ubicación apenas se entra a ejecutar una ruta, para que ya esté concedido cuando haga falta al finalizarla.
  useEffect(() => {
    obtenerPosicionOpcional()
  }, [])

  async function ejecutar(accion: () => Promise<void>, mensajeFallo: string) {
    setMensajeError(null)
    setOcupado(true)
    try {
      await accion()
      await cargar()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : mensajeFallo)
    } finally {
      setOcupado(false)
    }
  }

  async function alIniciar() {
    await ejecutar(async () => {
      const posicion = await obtenerPosicionOpcional()
      await iniciarServicio(referencia, token!, posicion?.latitud ?? null, posicion?.longitud ?? null)
    }, 'No se pudo iniciar el servicio.')
  }

  async function alFinalizar() {
    setConfirmandoFin(false)
    setMensajeError(null)
    setOcupado(true)
    try {
      const posicion = await obtenerPosicionOpcional()
      await finalizarServicio(referencia, token!, posicion?.latitud ?? null, posicion?.longitud ?? null)
      // Al finalizar, se vuelve a la pantalla principal de la jornada, donde queda el servicio con su hora de inicio y fin.
      navegar('/conductor')
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo finalizar el servicio.')
    } finally {
      setOcupado(false)
    }
  }

  if (!token) return null

  if (!servicio) {
    return (
      <main className="pagina-servicio-conductor">
        <Link to="/conductor" className="pagina-servicio-conductor__volver">← Mi jornada</Link>
        {mensajeError ? <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta> : <p>Cargando…</p>}
      </main>
    )
  }

  const miUsuarioId = obtenerUsuarioIdDelToken(token)
  const enCurso = servicio.estado === EstadoServicio.EN_CURSO
  const finalizado = servicio.estado === EstadoServicio.FINALIZADO
  const puedeIniciar = servicio.estado === EstadoServicio.PUBLICADO
  const ordenados = [...pasajeros].sort((a, b) => a.orden - b.orden)
  const pendientes = ordenados.filter((p) => !ESTADOS_GESTIONADOS.includes(p.estado))
  const gestionados = ordenados.filter((p) => ESTADOS_GESTIONADOS.includes(p.estado))

  const puedeReordenar = servicio.estado === EstadoServicio.PUBLICADO || enCurso

  /**
   * Suelta una tarjeta de "Por recoger" en otra posición: se reordena en pantalla al instante y el
   * pasajero pasa a ocupar el lugar (orden) del que estaba ahí; el servidor renumera la ruta.
   */
  function alMoverPendiente(desde: number, hasta: number) {
    const movido = pendientes[desde]
    const destino = pendientes[hasta]
    const nuevos = [...pendientes]
    nuevos.splice(hasta, 0, ...nuevos.splice(desde, 1))
    const ordenes = pendientes.map((p) => p.orden)
    setPasajeros([...gestionados, ...nuevos.map((p, i) => ({ ...p, orden: ordenes[i] }))])
    ejecutar(() => reordenarPasajeroConductor(referencia, movido.servicioPasajeroId, destino.orden, token!), 'No se pudo cambiar el orden.')
  }

  const tarjetaDe = (pasajero: ServicioPasajero) => {
    return (
      <TarjetaPasajeroConductor
        key={pasajero.servicioPasajeroId}
        pasajero={pasajero}
        servicioEnCurso={enCurso}
        servicioFinalizado={finalizado}
        chatAbiertoInicial={chatDeNotificacion === pasajero.servicioPasajeroId}
        miUsuarioId={miUsuarioId}
        alMarcarLlegada={() => ejecutar(() => marcarLlegada(referencia, pasajero.servicioPasajeroId, token), 'No se pudo registrar la llegada.')}
        alCambiarEstado={(nuevoEstado) => ejecutar(() => cambiarEstadoPasajero(referencia, pasajero.servicioPasajeroId, nuevoEstado, token), 'No se pudo cambiar el estado del pasajero.')}
        cargarMensajes={() => obtenerMensajes(referencia, pasajero.servicioPasajeroId, token)}
        enviarMensaje={(contenido) => enviarMensaje(referencia, pasajero.servicioPasajeroId, contenido, token)}
        arrastrable={puedeReordenar}
        guardarUbicacion={async (latitud, longitud) => {
          await guardarUbicacionRecogida(referencia, pasajero.servicioPasajeroId, latitud, longitud, token)
          await cargar()
        }}
        cargarUbicacionesAnteriores={() => obtenerUbicacionesAnteriores(referencia, pasajero.servicioPasajeroId, token)}
        eliminarUbicacionGuardada={() => eliminarUbicacionGuardada(referencia, pasajero.servicioPasajeroId, token)}
        reportarIncidencia={async (tipo, descripcion, foto, latitud, longitud) => {
          const incidencia = await crearIncidencia(
            referencia,
            pasajero.servicioPasajeroId,
            { tipo, descripcion: descripcion.trim() || 'Evidencia fotográfica', latitud, longitud },
            token,
          )
          if (foto) await subirFotoIncidencia(referencia, pasajero.servicioPasajeroId, incidencia.incidenciaId, foto, token)
          // El aviso se muestra antes de recargar: recargar puede mover al pasajero a "Ya gestionados" (o
          // sacarlo de la lista visible) y desmontar su tarjeta antes de que un aviso local ahí llegue a verse.
          mostrarAvisoFlotante('✔ Incidencia registrada correctamente.')
          // La incidencia puede dejar al pasajero como no recogido: se recarga para reflejarlo.
          await cargar()
        }}
        cargarIncidencias={() => obtenerIncidencias(referencia, pasajero.servicioPasajeroId, token)}
        cargarFotoEvidencia={(incidenciaId, evidenciaId) => obtenerUrlFotoEvidencia(referencia, pasajero.servicioPasajeroId, incidenciaId, evidenciaId, token)}
      />
    )
  }

  return (
    <main className="pagina-servicio-conductor">
      <Link to="/conductor" className="pagina-servicio-conductor__volver">← Mi jornada</Link>

      <header className="pagina-servicio-conductor__cabecera">
        <span>{nombreDe(NOMBRES_TIPO_SERVICIO, servicio.tipo)}</span>
        <strong>{formatearHora(servicio.horaProgramada)}</strong>
        <span>
          {servicio.fecha} · {nombreDe(NOMBRES_ESTADO_SERVICIO, servicio.estado)}
        </span>
        {servicio.nombreSede && <span className="pagina-servicio-conductor__sede">📍 {servicio.nombreSede}</span>}
      </header>

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

      {puedeIniciar && (
        <button type="button" className="pagina-servicio-conductor__principal" disabled={ocupado} onClick={alIniciar}>
          Iniciar servicio
        </button>
      )}

      {ordenados.length === 0 ? (
        <>
          <h2>Pasajeros (0)</h2>
          <p className="pagina-servicio-conductor__vacio">Este servicio no tiene pasajeros asignados.</p>
        </>
      ) : (
        <>
          <div className="pagina-servicio-conductor__pestanas" role="tablist">
            <button
              type="button"
              role="tab"
              aria-selected={pestana === 'porRecoger'}
              className={`pagina-servicio-conductor__pestana${pestana === 'porRecoger' ? ' pagina-servicio-conductor__pestana--activa' : ''}`}
              onClick={() => setPestana('porRecoger')}
            >
              Por recoger ({pendientes.length})
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={pestana === 'gestionados'}
              className={`pagina-servicio-conductor__pestana${pestana === 'gestionados' ? ' pagina-servicio-conductor__pestana--activa' : ''}`}
              onClick={() => setPestana('gestionados')}
            >
              Ya gestionados ({gestionados.length})
            </button>
          </div>

          {pestana === 'porRecoger' ? (
            pendientes.length === 0 ? (
              <p className="pagina-servicio-conductor__vacio">Ya gestionaste a todos los pasajeros: solo falta finalizar la ruta.</p>
            ) : (
              <>
                {puedeReordenar && pendientes.length > 1 && (
                  <p className="pagina-servicio-conductor__ayuda">Mantén presionada una tarjeta y arrástrala para cambiar el orden de recogida.</p>
                )}
                <ListaArrastrable className="pagina-servicio-conductor__pasajeros" alMover={puedeReordenar ? alMoverPendiente : undefined}>
                  {pendientes.map(tarjetaDe)}
                </ListaArrastrable>
              </>
            )
          ) : gestionados.length === 0 ? (
            <p className="pagina-servicio-conductor__vacio">Todavía no has gestionado a ningún pasajero.</p>
          ) : (
            <ul className="pagina-servicio-conductor__pasajeros">{gestionados.map(tarjetaDe)}</ul>
          )}
        </>
      )}

      {enCurso && (
        <button type="button" className="pagina-servicio-conductor__principal pagina-servicio-conductor__principal--fin" disabled={ocupado} onClick={() => setConfirmandoFin(true)}>
          Finalizar servicio
        </button>
      )}

      <ModalConfirmacion
        abierto={confirmandoFin}
        titulo="¿Finalizar el servicio?"
        mensaje="El servicio pasará a Finalizado y se guardará tu ubicación actual. Si quedan pasajeros pendientes de gestionar, el sistema te lo indicará."
        textoConfirmar="Finalizar"
        alConfirmar={alFinalizar}
        alCancelar={() => setConfirmandoFin(false)}
      />

      {avisoFlotante &&
        createPortal(
          <p className="pagina-servicio-conductor__aviso-flotante" role="status">
            {avisoFlotante}
          </p>,
          document.body,
        )}
    </main>
  )
}
