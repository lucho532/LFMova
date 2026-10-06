import { useEffect, useRef, useState } from 'react'
import { useNotificaciones } from '../contexto/useNotificaciones'
import { EstadoPasajero, NOMBRES_ESTADO_PASAJERO, nombreDe } from '../modelos/enumeraciones'
import type { ServicioPasajero } from '../modelos/operacion'
import { VentanaChat } from './VentanaChat'
import type { Mensaje } from '../modelos/mensaje'
import type { IncidenciaPasajero, UbicacionAnterior } from '../servicios/servicioConductorPropio'
import { BotonLlamar } from './BotonLlamar'
import { CronometroEspera } from './CronometroEspera'
import { HerramientasPasajero } from './HerramientasPasajero'
import { ModalConfirmacion } from './ModalConfirmacion'
import '../estilos/componentes/TarjetaPasajeroConductor.css'

/** El enlace de una notificación de chat termina en el servicioPasajeroId de esa conversación. */
function esMensajeDelPasajero(enlace: string | null, servicioPasajeroId: number): boolean {
  if (!enlace) return false
  const partes = enlace.split('/')
  return Number(partes[partes.length - 1]) === servicioPasajeroId
}

interface AccionPasajero {
  texto: string
  ejecutar: () => Promise<void>
  secundaria?: boolean
  /** Si tiene texto, antes de ejecutar la acción se pide confirmar con ese mensaje (para evitar un toque accidental). */
  confirmar?: string
}

interface PropiedadesTarjetaPasajeroConductor {
  pasajero: ServicioPasajero
  /** Empresa y jornada del servicio (el pasajero solo trae el identificador del servicio). */
  referenciaServicio: { empresaId: number; jornadaId: number }
  /** Solo se ofrecen acciones mientras el servicio está en curso. */
  servicioEnCurso: boolean
  /** La ruta ya finalizó: el chat queda solo de lectura, sin poder enviar más mensajes. */
  servicioFinalizado?: boolean
  miUsuarioId: number | null
  alMarcarLlegada: () => Promise<void>
  alCambiarEstado: (nuevoEstado: number) => Promise<void>
  cargarMensajes: () => Promise<Mensaje[]>
  enviarMensaje: (contenido: string) => Promise<Mensaje>
  /** Abre el chat de entrada (por ejemplo, al llegar desde una notificación). */
  chatAbiertoInicial?: boolean
  /** Indica que la tarjeta se puede arrastrar para cambiar el orden de recogida (lo gestiona la lista). */
  arrastrable?: boolean
  guardarUbicacion: (latitud: number, longitud: number) => Promise<void>
  cargarUbicacionesAnteriores: () => Promise<UbicacionAnterior[]>
  eliminarUbicacionGuardada: () => Promise<void>
  reportarIncidencia: (tipo: number, descripcion: string, foto: File | null, latitud: number | null, longitud: number | null) => Promise<void>
  cargarIncidencias: () => Promise<IncidenciaPasajero[]>
  cargarFotoEvidencia: (incidenciaId: number, evidenciaId: number) => Promise<string>
}

/**
 * Tarjeta compacta de un pasajero para el conductor: datos de contacto y
 * recogida, estado y solo dos pasos: registrar la llegada ("He llegado", que arranca el cronómetro de espera de 2 minutos) y el resultado
 * final: "Recogido", o una incidencia (foto / incidencia) si no se pudo recoger, que lo deja como no recogido. Al finalizar el servicio, los
 * recogidos cuentan como transportados. Los
 * botones se muestran según el estado actual del pasajero; el backend
 * sigue siendo quien valida cada transición.
 */
export function TarjetaPasajeroConductor({
  pasajero,
  referenciaServicio,
  servicioEnCurso,
  servicioFinalizado = false,
  miUsuarioId,
  alMarcarLlegada,
  alCambiarEstado,
  cargarMensajes,
  enviarMensaje,
  chatAbiertoInicial = false,
  arrastrable = false,
  guardarUbicacion,
  cargarUbicacionesAnteriores,
  eliminarUbicacionGuardada,
  reportarIncidencia,
  cargarIncidencias,
  cargarFotoEvidencia,
}: PropiedadesTarjetaPasajeroConductor) {
  const { notificaciones, marcarLeida } = useNotificaciones()
  const [chatAbierto, setChatAbierto] = useState(chatAbiertoInicial)
  // Colapsada por defecto (solo nombre y dirección) para no ocupar toda la pantalla con cada pasajero;
  // se abre sola si ya está esperando al pasajero (cronómetro en marcha) o si se llega desde una notificación de chat.
  const [expandido, setExpandido] = useState(chatAbiertoInicial || pasajero.estado === EstadoPasajero.CONDUCTOR_LLEGO)
  const tarjeta = useRef<HTMLLIElement>(null)

  useEffect(() => {
    if (chatAbiertoInicial) {
      setChatAbierto(true)
      setExpandido(true)
      tarjeta.current?.scrollIntoView({ behavior: 'smooth', block: 'center' })
    }
  }, [chatAbiertoInicial])
  const [ocupado, setOcupado] = useState(false)

  const estado = pasajero.estado
  const referenciaPasajero = { ...referenciaServicio, servicioId: pasajero.servicioId, servicioPasajeroId: pasajero.servicioPasajeroId }
  const acciones: AccionPasajero[] = []
  const mensajesSinLeer = notificaciones.filter(
    (n) => !n.leida && n.tipo === 'MENSAJE_NUEVO' && esMensajeDelPasajero(n.enlace, pasajero.servicioPasajeroId),
  )

  function alAbrirChat() {
    mensajesSinLeer.forEach((n) => marcarLeida(n.notificacionId).catch(() => {}))
    setChatAbierto(true)
  }

  if (servicioEnCurso) {
    if (estado === EstadoPasajero.PROGRAMADO || estado === EstadoPasajero.CONFIRMADO) {
      // El cronómetro de espera arranca con la hora que guarde el backend al aceptar la llegada.
      acciones.push({ texto: 'He llegado', ejecutar: alMarcarLlegada })
    }
    if (estado === EstadoPasajero.CONDUCTOR_LLEGO) {
      acciones.push({
        texto: 'Recogido',
        ejecutar: () => alCambiarEstado(EstadoPasajero.RECOGIDO),
        confirmar: `¿Confirmas que ya recogiste a ${pasajero.nombreCompletoEmpleado}? Esta acción no se puede deshacer desde la app.`,
      })
    }
  }

  const [accionAConfirmar, setAccionAConfirmar] = useState<AccionPasajero | null>(null)

  async function ejecutar(accion: () => Promise<void>) {
    setOcupado(true)
    try {
      await accion()
    } finally {
      setOcupado(false)
    }
  }

  function alTocarAccion(accion: AccionPasajero) {
    if (accion.confirmar) setAccionAConfirmar(accion)
    else ejecutar(accion.ejecutar)
  }

  function alConfirmarAccion() {
    const accion = accionAConfirmar
    setAccionAConfirmar(null)
    if (accion) ejecutar(accion.ejecutar)
  }

  return (
    <li className={`tarjeta-pasajero-conductor${estado === EstadoPasajero.CONFIRMADO ? ' tarjeta-pasajero-conductor--confirmado' : ''}`} ref={tarjeta}>
      <div className="tarjeta-pasajero-conductor__cabecera">
        <button type="button" className="tarjeta-pasajero-conductor__resumen" onClick={() => setExpandido((v) => !v)} aria-expanded={expandido}>
          <strong>{pasajero.nombreCompletoEmpleado}</strong>
          <span className="tarjeta-pasajero-conductor__resumen-direccion">
            {pasajero.direccionRecogida}
            {pasajero.barrioEmpleado ? ` · ${pasajero.barrioEmpleado}` : ''}
          </span>
        </button>
        <span className="tarjeta-pasajero-conductor__orden">
          {arrastrable && (
            <span className="tarjeta-pasajero-conductor__agarre" aria-hidden="true" title="Mantén presionada la tarjeta para moverla">
              ⠿
            </span>
          )}
          #{pasajero.orden}
        </span>
      </div>

      {expandido && (
        <>
          <dl className="tarjeta-pasajero-conductor__datos">
            <div>
              <dt>Cédula</dt>
              <dd>{pasajero.cedulaEmpleado || '—'}</dd>
            </div>
            <div>
              <dt>Teléfono</dt>
              <dd>{pasajero.telefonoEmpleado || '—'}</dd>
            </div>
            <div>
              <dt>Dirección</dt>
              <dd>{pasajero.direccionRecogida}</dd>
            </div>
            <div>
              <dt>Barrio</dt>
              <dd>{pasajero.barrioEmpleado || '—'}</dd>
            </div>
          </dl>
          {/* Confirmado ya no lleva letrero propio: toda la tarjeta se pinta de verde (ver className del <li>). */}
          {estado === EstadoPasajero.NO_ASISTIRA && <p className="tarjeta-pasajero-conductor__letrero tarjeta-pasajero-conductor__letrero--rojo">✖ El pasajero avisó que no asistirá</p>}
          {estado !== EstadoPasajero.CONFIRMADO && estado !== EstadoPasajero.NO_ASISTIRA && (
            <p className="tarjeta-pasajero-conductor__estado">● {nombreDe(NOMBRES_ESTADO_PASAJERO, estado)}</p>
          )}

          <div className="tarjeta-pasajero-conductor__contacto">
            <BotonLlamar pasajero={referenciaPasajero} nombre={pasajero.nombreCompletoEmpleado} telefono={pasajero.telefonoEmpleado} className="tarjeta-pasajero-conductor__boton" />
            <button type="button" className="tarjeta-pasajero-conductor__boton tarjeta-pasajero-conductor__boton-chat" onClick={alAbrirChat}>
              Chat
              {mensajesSinLeer.length > 0 && <span className="tarjeta-pasajero-conductor__insignia">{mensajesSinLeer.length > 9 ? '9+' : mensajesSinLeer.length}</span>}
            </button>
          </div>
          <CronometroEspera horaLlegadaConductor={pasajero.horaLlegadaConductor} activo={servicioEnCurso && estado === EstadoPasajero.CONDUCTOR_LLEGO} />

          <HerramientasPasajero
            pasajero={pasajero}
            referenciaPasajero={referenciaPasajero}
            servicioEnCurso={servicioEnCurso}
            guardarUbicacion={guardarUbicacion}
            cargarUbicacionesAnteriores={cargarUbicacionesAnteriores}
            eliminarUbicacionGuardada={eliminarUbicacionGuardada}
            reportarIncidencia={reportarIncidencia}
            cargarIncidencias={cargarIncidencias}
            cargarFotoEvidencia={cargarFotoEvidencia}
          />

          {acciones.length > 0 && (
            <div className="tarjeta-pasajero-conductor__acciones">
              {acciones.map((accion) => (
                <button
                  key={accion.texto}
                  type="button"
                  disabled={ocupado}
                  className={`tarjeta-pasajero-conductor__accion${accion.secundaria ? ' tarjeta-pasajero-conductor__accion--secundaria' : ''}`}
                  onClick={() => alTocarAccion(accion)}
                >
                  {accion.texto}
                </button>
              ))}
            </div>
          )}

          <VentanaChat
            abierto={chatAbierto}
            miUsuarioId={miUsuarioId}
            cargarMensajes={cargarMensajes}
            enviar={enviarMensaje}
            conRespuestasRapidas
            soloLectura={servicioFinalizado}
            alCerrar={() => setChatAbierto(false)}
          />

          <ModalConfirmacion
            abierto={accionAConfirmar !== null}
            titulo="Confirmar"
            mensaje={accionAConfirmar?.confirmar ?? ''}
            textoConfirmar="Sí, confirmar"
            alConfirmar={alConfirmarAccion}
            alCancelar={() => setAccionAConfirmar(null)}
          />
        </>
      )}
    </li>
  )
}
