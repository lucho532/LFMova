import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { useNotificaciones } from '../contexto/useNotificaciones'
import { TarjetaServicioEmpleado } from '../componentes/TarjetaServicioEmpleado'
import type { ServicioDelEmpleado } from '../modelos/servicioEmpleado'
import { ErrorApi } from '../servicios/clienteHttp'
import {
  compartirMiUbicacion,
  confirmarAsistencia,
  enviarMensajeAlConductor,
  marcarNoAsistire,
  obtenerMensajesConConductor,
  obtenerMisServicios,
} from '../servicios/servicioEmpleadoPropio'
import { obtenerUsuarioIdDelToken } from '../servicios/tokenJwt'
import { EstadoServicio } from '../modelos/enumeraciones'
import '../estilos/paginas/PaginaMiTransporte.css'

/**
 * Pantalla inicial de cualquier persona que no es administradora ni
 * coordinadora. Una cuenta recién registrada no tiene datos de transporte
 * hasta que su cédula aparezca en una importación de Excel: mientras tanto se
 * muestra un estado vacío explicativo en lugar de un error.
 */
export function PaginaMiTransporte() {
  const { token } = useAutenticacion()
  const { version } = useNotificaciones()
  const [servicios, setServicios] = useState<ServicioDelEmpleado[]>([])
  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [pestana, setPestana] = useState<'programados' | 'finalizados'>('programados')
  const miUsuarioId = token ? obtenerUsuarioIdDelToken(token) : null
  const clave = (s: ServicioDelEmpleado) => s.fecha + s.horaProgramada
  const programados = servicios.filter((s) => s.estadoServicio !== EstadoServicio.FINALIZADO).sort((a, b) => clave(a).localeCompare(clave(b)))
  const finalizados = servicios.filter((s) => s.estadoServicio === EstadoServicio.FINALIZADO).sort((a, b) => clave(b).localeCompare(clave(a)))
  const visibles = pestana === 'programados' ? programados : finalizados
  // Si se llegó desde una notificación (?chat=id), se abre el chat de ese servicio.
  const chatDeNotificacion = Number(useSearchParams()[0].get('chat')) || null

  function cargar() {
    if (!token) return Promise.resolve()
    return obtenerMisServicios(token)
      .then((datos) => setServicios(datos))
      .catch((error) => {
        // 404: la cuenta todavía no tiene perfil de empleado (su cédula aún no aparece en ninguna importación).
        if (!(error instanceof ErrorApi && error.status === 404)) {
          setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar tu transporte.')
        }
      })
      .finally(() => setCargando(false))
  }

  // Se recarga al llegar una notificación nueva (nueva ruta, cambio de conductor…) y, además, cada
  // pocos segundos mientras la ruta está en curso: así el cronómetro de espera y el estado del pasajero
  // (por ejemplo, si el conductor lo deja como "no recogido" al reportar una incidencia) no se quedan
  // desactualizados esperando una notificación que puede no llegar.
  useEffect(() => {
    cargar()
    const temporizador = setInterval(() => {
      if (!document.hidden) cargar()
    }, 15000)
    return () => clearInterval(temporizador)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token, version])

  return (
    <main className="pagina-mi-transporte">
      <header className="pagina-mi-transporte__encabezado">
        <h1>Mi transporte</h1>
      </header>

      {cargando && <p>Cargando…</p>}

      {mensajeError && (
        <p className="pagina-mi-transporte__error" role="alert">
          {mensajeError}
        </p>
      )}

      {!cargando && !mensajeError && servicios.length === 0 && (
        <section className="pagina-mi-transporte__vacio">
          <h2>Todavía no tienes servicios</h2>
          <p>
            Tu cuenta está activa. Cuando tu empresa cargue su programación y tu cédula aparezca en ella, tus servicios de transporte se
            mostrarán aquí.
          </p>
        </section>
      )}

      {servicios.length > 0 && (
        <div className="pagina-mi-transporte__pestanas" role="tablist">
          <button type="button" role="tab" aria-selected={pestana === 'programados'} className={`pagina-mi-transporte__pestana${pestana === 'programados' ? ' pagina-mi-transporte__pestana--activa' : ''}`} onClick={() => setPestana('programados')}>
            Programados ({programados.length})
          </button>
          <button type="button" role="tab" aria-selected={pestana === 'finalizados'} className={`pagina-mi-transporte__pestana${pestana === 'finalizados' ? ' pagina-mi-transporte__pestana--activa' : ''}`} onClick={() => setPestana('finalizados')}>
            Finalizados ({finalizados.length})
          </button>
        </div>
      )}

      {servicios.length > 0 && visibles.length === 0 && (
        <p className="pagina-mi-transporte__vacio-pestana">{pestana === 'programados' ? 'No tienes servicios programados pendientes.' : 'Todavía no tienes servicios finalizados.'}</p>
      )}

      {visibles.length > 0 && (
        <ul className="pagina-mi-transporte__lista">
          {visibles.map((servicio) => {
            const referencia = { empresaId: servicio.empresaId, jornadaId: servicio.jornadaId, servicioId: servicio.servicioId, servicioPasajeroId: servicio.servicioPasajeroId }
            return (
              <TarjetaServicioEmpleado
                key={servicio.servicioPasajeroId}
                servicio={servicio}
                chatAbiertoInicial={chatDeNotificacion === servicio.servicioPasajeroId}
                miUsuarioId={miUsuarioId}
                confirmar={async (datos) => {
                  await confirmarAsistencia(referencia, datos, token!)
                  setServicios(await obtenerMisServicios(token!))
                }}
                noAsistire={async () => {
                  await marcarNoAsistire(referencia, token!)
                  setServicios(await obtenerMisServicios(token!))
                }}
                compartirUbicacion={(latitud, longitud) => compartirMiUbicacion(referencia, latitud, longitud, token!)}
                cargarMensajes={() => obtenerMensajesConConductor(referencia, token!)}
                enviarMensaje={(contenido) => enviarMensajeAlConductor(referencia, contenido, token!)}
              />
            )
          })}
        </ul>
      )}
    </main>
  )
}
