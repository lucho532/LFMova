import { useEffect, useRef, useState, type FormEvent } from 'react'
import { createPortal } from 'react-dom'
import { armarPulsacionLarga } from '../hooks/pulsacionLarga'
import { formatearBarrioConCiudad, useBarriosVillamaria } from '../hooks/useBarriosVillamaria'
import { EstadoServicio } from '../modelos/enumeraciones'
import type { PasajeroPrevia, VistaPreviaImportacion } from '../modelos/importacion'
import type { Servicio } from '../modelos/operacion'
import { ErrorApi } from '../servicios/clienteHttp'
import type { OpcionUnidadOperativa } from '../servicios/servicioConductores'
import { BotonPrimario } from './BotonPrimario'
import { BotonSecundario } from './BotonSecundario'
import { CampoFormulario } from './CampoFormulario'
import { MensajeAlerta } from './MensajeAlerta'
import { ModalConfirmacion } from './ModalConfirmacion'
import { SelectorFormulario } from './SelectorFormulario'
import { TablaDatos } from './TablaDatos'
import '../estilos/componentes/PanelVistaPreviaImportacion.css'

interface PropiedadesPanelVistaPreviaImportacion {
  empresaId: number
  vista: VistaPreviaImportacion
  /** Servicios ya creados para la fecha elegida (si esta jornada ya se repartió antes). */
  serviciosReales: Servicio[]
  opcionesUnidad: OpcionUnidadOperativa[]
  alReasignar: (servicio: Servicio, unidadOperativaId: number) => Promise<void>
  /** El coordinador soltó a un pasajero sobre la tarjeta de otro servicio: lo mueve ahí. */
  alMoverPasajero: (servicioOrigen: Servicio, servicioPasajeroId: number, servicioDestinoId: number) => Promise<void>
  /**
   * El coordinador eligió, en el menú de mover, un conductor libre que todavía no tiene ruta en ese
   * horario: se le abre una ruta nueva (misma sede, fecha, hora y tipo que el origen) y se le mueve ahí
   * el pasajero. Sirve para dividir una ruta que quedó con demasiados barrios distintos.
   */
  alCrearRutaYMoverPasajero: (servicioOrigen: Servicio, servicioPasajeroId: number, unidadOperativaId: number) => Promise<void>
  /** El coordinador confirmó borrar por completo a un pasajero de la ruta (por ejemplo, se agregó por error al pegar filas). */
  alEliminarPasajero: (servicio: Servicio, servicioPasajeroId: number) => Promise<void>
  /**
   * El coordinador guardó la corrección de datos de un pasajero (nombre, celular, barrio y/o dirección
   * de esta ruta). Nombre/celular/barrio actualizan al Empleado; la dirección es solo de este servicio.
   */
  alEditarPasajero: (
    servicio: Servicio,
    servicioPasajeroId: number,
    empleadoId: number,
    datos: { nombreCompleto: string; celular: string; barrio: string; direccion: string },
  ) => Promise<void>
  /**
   * El coordinador confirmó "despublicar" una ruta ya enviada (vuelve a Asignado) para poder seguir
   * editándola: arrastrarle pasajeros desde otra ruta o asignárselos directo. Si tiene conductor, se le
   * notifica.
   */
  alDespublicar: (servicio: Servicio) => Promise<void>
}

const ESTADOS_SIN_CAMBIOS = new Set<number>([EstadoServicio.CANCELADO, EstadoServicio.EN_CURSO, EstadoServicio.FINALIZADO])

/** Un servicio en uno de estos estados ya no ocupa a su conductor (mismo criterio que usa el reparto automático en el backend). */
const ESTADOS_QUE_LIBERAN_UNIDAD = new Set<number>([EstadoServicio.FINALIZADO, EstadoServicio.CANCELADO])

/** Un servicio en uno de estos estados ya fue enviado al conductor (con "Enviar Rutas" o después). */
const ESTADOS_ENVIADOS = new Set<number>([EstadoServicio.PUBLICADO, EstadoServicio.EN_CURSO, EstadoServicio.FINALIZADO])

/** Datos del pasajero que se está arrastrando, guardados al iniciar el drag para saber en qué tarjetas se puede soltar y qué mostrar en la tarjeta que sigue al cursor. */
interface PasajeroArrastrado {
  servicioPasajeroId: number
  servicioOrigen: Servicio
  pasajero: PasajeroPrevia
}

/**
 * Muestra lo que contiene el Excel y lo que ocurriría al importarlo:
 * servicios, pasajeros, advertencias y errores. Si cada persona es nueva, ya
 * tiene cuenta o cambia de empresa se resuelve internamente al importar; no
 * se le muestra al coordinador fila por fila (solo el total de "personas
 * nuevas" del resumen). Esta pantalla es el único lugar para gestionar la
 * programación: no existe una pantalla de "jornada" ni de detalle de un
 * servicio aparte, toda la gestión de pasajeros ocurre acá. Cuando una ruta
 * ya tiene un servicio real creado (por un reparto anterior), se muestra su
 * conductor junto al título, con opción de cambiarlo, y sus pasajeros se
 * pueden mover a otra ruta de la misma sede, fecha, hora y tipo (mismo
 * horario, otra zona o corredor) de dos formas: arrastrando la fila sobre la
 * tarjeta destino, o con la flechita al final de la fila, que despliega la
 * lista de conductores disponibles en ese mismo horario para elegir uno. Si
 * un movimiento deja una ruta sin pasajeros, el backend elimina esa ruta y
 * reubica su conductor automáticamente. El arrastre no usa el "drag and
 * drop" nativo del navegador (con eso, Chrome deja de repartir la rueda del
 * mouse y otros eventos normales de la página mientras se arrastra): se
 * sigue el puntero a mano con eventos de puntero, así la rueda y el
 * desplazamiento automático en los bordes de la pantalla funcionan siempre.
 */
export function PanelVistaPreviaImportacion({
  empresaId,
  vista,
  serviciosReales,
  opcionesUnidad,
  alReasignar,
  alMoverPasajero,
  alCrearRutaYMoverPasajero,
  alEliminarPasajero,
  alEditarPasajero,
  alDespublicar,
}: PropiedadesPanelVistaPreviaImportacion) {
  const [desplegado, setDesplegado] = useState<number | null>(null)
  const [unidadElegida, setUnidadElegida] = useState('')
  const [guardando, setGuardando] = useState(false)
  const [errorReasignar, setErrorReasignar] = useState<string | null>(null)
  /** Índice del servicio cuyo conductor se acaba de reasignar con éxito, para mostrarle el aviso; se limpia solo a los pocos segundos. */
  const [servicioReasignado, setServicioReasignado] = useState<number | null>(null)
  /** Pasajero que el coordinador pidió borrar, pendiente de confirmar (borrado físico, no se puede deshacer). */
  const [pasajeroAEliminar, setPasajeroAEliminar] = useState<{ servicio: Servicio; servicioPasajeroId: number; nombre: string } | null>(null)
  const [eliminandoPasajero, setEliminandoPasajero] = useState(false)
  const [errorEliminarPasajero, setErrorEliminarPasajero] = useState<string | null>(null)

  /** Ruta ya enviada que el coordinador pidió "despublicar" para editarla, pendiente de confirmar. */
  const [servicioADespublicar, setServicioADespublicar] = useState<Servicio | null>(null)
  const [despublicando, setDespublicando] = useState(false)
  const [errorDespublicar, setErrorDespublicar] = useState<string | null>(null)

  /** Pasajero que el coordinador está corrigiendo (nombre, celular, barrio y/o dirección de esta ruta). */
  const [pasajeroAEditar, setPasajeroAEditar] = useState<{ servicio: Servicio; servicioPasajeroId: number; empleadoId: number } | null>(null)
  const [nombreEditado, setNombreEditado] = useState('')
  const [celularEditado, setCelularEditado] = useState('')
  const [barrioEditado, setBarrioEditado] = useState('')
  const [direccionEditada, setDireccionEditada] = useState('')
  const [guardandoEdicion, setGuardandoEdicion] = useState(false)
  const [errorEditar, setErrorEditar] = useState<string | null>(null)
  const referenciaEdicion = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialogo = referenciaEdicion.current
    if (!dialogo) return
    if (pasajeroAEditar && !dialogo.open) dialogo.showModal()
    if (!pasajeroAEditar && dialogo.open) dialogo.close()
  }, [pasajeroAEditar])

  const [arrastrado, setArrastrado] = useState<PasajeroArrastrado | null>(null)
  const arrastradoRef = useRef<PasajeroArrastrado | null>(null)
  const etiquetaArrastreRef = useRef<HTMLTableElement>(null)
  const [errorMover, setErrorMover] = useState<string | null>(null)
  // Posición fija (viewport) del menú de mover: se renderiza en un portal fuera de la tarjeta de la ruta
  // (ver más abajo) para que nunca quede recortado ni obligue a hacer scroll dentro de la ruta para verlo
  // completo, aunque eso signifique que se superponga a otras rutas o al resto de la página.
  const [menuMoverAbierto, setMenuMoverAbierto] = useState<{ id: number; right: number; top: number | null; bottom: number | null } | null>(null)
  const barriosVillamaria = useBarriosVillamaria(empresaId)

  // Cierra el menú de mover al hacer clic en cualquier parte fuera de él (no solo en la flechita que lo
  // abrió): como ahora vive en un portal, se busca tanto el botón que lo abre como el menú mismo.
  useEffect(() => {
    if (!menuMoverAbierto) return
    function alHacerClicFuera(evento: PointerEvent) {
      if (evento.target instanceof Element && evento.target.closest('.vista-previa-importacion__mover, .vista-previa-importacion__menu-mover')) return
      setMenuMoverAbierto(null)
    }
    document.addEventListener('pointerdown', alHacerClicFuera)
    return () => document.removeEventListener('pointerdown', alHacerClicFuera)
  }, [menuMoverAbierto])

  /** Abre el menú de mover pegado al botón (hacia abajo, o hacia arriba si no hay espacio debajo). */
  function alternarMenuMover(servicioPasajeroId: number, boton: HTMLButtonElement) {
    if (menuMoverAbierto?.id === servicioPasajeroId) {
      setMenuMoverAbierto(null)
      return
    }
    const ALTURA_MENU_ESTIMADA = 260
    const rect = boton.getBoundingClientRect()
    const espacioAbajo = window.innerHeight - rect.bottom
    const haciaArriba = espacioAbajo < ALTURA_MENU_ESTIMADA && rect.top > espacioAbajo
    const right = window.innerWidth - rect.right
    setMenuMoverAbierto({
      id: servicioPasajeroId,
      right,
      top: haciaArriba ? null : rect.bottom + 4,
      bottom: haciaArriba ? window.innerHeight - rect.top + 4 : null,
    })
  }

  /** Misma sede, fecha, hora y tipo, y ninguno cancelado/en curso/finalizado: se puede mover un pasajero de uno a otro. */
  function sonRutasCompatibles(origen: Servicio, destino: Servicio): boolean {
    if (destino.servicioId === origen.servicioId) return false
    if (ESTADOS_SIN_CAMBIOS.has(destino.estado) || ESTADOS_SIN_CAMBIOS.has(origen.estado)) return false
    return origen.sedeId === destino.sedeId && origen.tipo === destino.tipo && origen.fecha === destino.fecha && origen.horaProgramada === destino.horaProgramada
  }

  function esDestinoValido(real: Servicio | undefined): real is Servicio {
    return Boolean(real && arrastrado && sonRutasCompatibles(arrastrado.servicioOrigen, real))
  }

  async function moverA(origen: Servicio, servicioPasajeroId: number, destino: Servicio) {
    setErrorMover(null)
    try {
      await alMoverPasajero(origen, servicioPasajeroId, destino.servicioId)
    } catch {
      setErrorMover('No se pudo mover al pasajero a esa ruta.')
    }
  }

  async function crearRutaYMoverA(origen: Servicio, servicioPasajeroId: number, unidadOperativaId: number) {
    setErrorMover(null)
    try {
      await alCrearRutaYMoverPasajero(origen, servicioPasajeroId, unidadOperativaId)
    } catch {
      setErrorMover('No se pudo crear la ruta nueva para mover al pasajero.')
    }
  }

  /** Posiciona la etiqueta que sigue al cursor directamente en el DOM (sin pasar por React) para que siga al mouse sin tirones. */
  function ubicarEtiqueta(x: number, y: number) {
    const etiqueta = etiquetaArrastreRef.current
    if (!etiqueta) return
    // Se mantiene dentro de la ventana: en pantallas angostas quedaría cortada a la derecha del dedo.
    const maximoX = Math.max(8, window.innerWidth - etiqueta.offsetWidth - 8)
    const maximoY = Math.max(8, window.innerHeight - etiqueta.offsetHeight - 8)
    etiqueta.style.transform = `translate(${Math.min(x + 14, maximoX)}px, ${Math.min(y + 14, maximoY)}px)`
  }

  function iniciarArrastre(pasajero: PasajeroArrastrado, x: number, y: number) {
    arrastradoRef.current = pasajero
    setArrastrado(pasajero)
    ubicarEtiqueta(x, y)
  }

  // Mientras se arrastra un pasajero (arrastre propio con eventos de puntero, no el "drag and drop"
  // nativo del navegador): se sigue el cursor con una etiqueta propia (para ver a quién se está
  // moviendo, ya que sin el arrastre nativo no aparece la miniatura que pone el navegador solo) y, si
  // el mouse queda pegado arriba o abajo de la pantalla, la página se desplaza sola (sin esto, no
  // habría forma de soltarlo sobre una tarjeta fuera de vista). La rueda del mouse no necesita manejo
  // aparte: al no ser un arrastre nativo, el navegador la sigue repartiendo normal.
  useEffect(() => {
    if (!arrastrado) return

    const ZONA_BORDE_PX = 90
    const VELOCIDAD_MAXIMA_PX = 22
    let posicion: { x: number; y: number } | null = null
    let idCuadro = 0

    function alMoverElPuntero(evento: PointerEvent) {
      posicion = { x: evento.clientX, y: evento.clientY }
    }

    function alSoltarElPuntero(evento: PointerEvent) {
      const origen = arrastradoRef.current
      arrastradoRef.current = null
      setArrastrado(null)
      // "pointercancel": el sistema interrumpió el toque; el arrastre se abandona sin mover a nadie.
      if (!origen || evento.type === 'pointercancel') return

      const elemento = document.elementFromPoint(evento.clientX, evento.clientY)
      const tarjeta = elemento?.closest<HTMLElement>('[data-servicio-id]')
      const servicioId = tarjeta?.dataset.servicioId ? Number(tarjeta.dataset.servicioId) : null
      const destino = servicioId === null ? undefined : serviciosReales.find((s) => s.servicioId === servicioId)
      if (destino && sonRutasCompatibles(origen.servicioOrigen, destino)) {
        void moverA(origen.servicioOrigen, origen.servicioPasajeroId, destino)
      }
    }

    function ciclo() {
      if (posicion !== null) {
        ubicarEtiqueta(posicion.x, posicion.y)

        const alto = window.innerHeight
        if (posicion.y < ZONA_BORDE_PX) {
          const intensidad = (ZONA_BORDE_PX - posicion.y) / ZONA_BORDE_PX
          window.scrollBy(0, -Math.ceil(VELOCIDAD_MAXIMA_PX * intensidad))
        } else if (posicion.y > alto - ZONA_BORDE_PX) {
          const intensidad = (posicion.y - (alto - ZONA_BORDE_PX)) / ZONA_BORDE_PX
          window.scrollBy(0, Math.ceil(VELOCIDAD_MAXIMA_PX * intensidad))
        }
      }
      idCuadro = requestAnimationFrame(ciclo)
    }

    document.addEventListener('pointermove', alMoverElPuntero)
    document.addEventListener('pointerup', alSoltarElPuntero)
    document.addEventListener('pointercancel', alSoltarElPuntero)
    idCuadro = requestAnimationFrame(ciclo)

    return () => {
      document.removeEventListener('pointermove', alMoverElPuntero)
      document.removeEventListener('pointerup', alSoltarElPuntero)
      document.removeEventListener('pointercancel', alSoltarElPuntero)
      cancelAnimationFrame(idCuadro)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [arrastrado])

  function alDesplegar(indice: number, real: Servicio) {
    if (desplegado === indice) {
      setDesplegado(null)
      return
    }
    setDesplegado(indice)
    setUnidadElegida(real.unidadOperativaId ? String(real.unidadOperativaId) : '')
    setErrorReasignar(null)
  }

  async function guardar(real: Servicio, indice: number) {
    if (!unidadElegida) return
    setGuardando(true)
    setErrorReasignar(null)
    try {
      await alReasignar(real, Number(unidadElegida))
      setDesplegado(null)
      setServicioReasignado(indice)
      setTimeout(() => setServicioReasignado((actual) => (actual === indice ? null : actual)), 4000)
    } catch {
      setErrorReasignar('No se pudo cambiar el conductor de esta ruta.')
    } finally {
      setGuardando(false)
    }
  }

  async function confirmarEliminarPasajero() {
    if (!pasajeroAEliminar) return
    setEliminandoPasajero(true)
    setErrorEliminarPasajero(null)
    try {
      await alEliminarPasajero(pasajeroAEliminar.servicio, pasajeroAEliminar.servicioPasajeroId)
      setPasajeroAEliminar(null)
    } catch {
      setErrorEliminarPasajero('No se pudo eliminar al pasajero.')
    } finally {
      setEliminandoPasajero(false)
    }
  }

  async function confirmarDespublicar() {
    if (!servicioADespublicar) return
    setDespublicando(true)
    setErrorDespublicar(null)
    try {
      await alDespublicar(servicioADespublicar)
      setServicioADespublicar(null)
    } catch (error) {
      setErrorDespublicar(error instanceof ErrorApi ? error.message : 'No se pudo volver a editar esta ruta.')
    } finally {
      setDespublicando(false)
    }
  }

  function abrirEdicion(servicio: Servicio, pasajero: PasajeroPrevia) {
    setPasajeroAEditar({ servicio, servicioPasajeroId: pasajero.servicioPasajeroId!, empleadoId: pasajero.empleadoId! })
    setNombreEditado(pasajero.nombreCompleto)
    setCelularEditado(pasajero.celular)
    setBarrioEditado(pasajero.barrio)
    setDireccionEditada(pasajero.direccion)
    setErrorEditar(null)
  }

  async function guardarEdicion(evento: FormEvent) {
    evento.preventDefault()
    if (!pasajeroAEditar) return
    setGuardandoEdicion(true)
    setErrorEditar(null)
    try {
      await alEditarPasajero(pasajeroAEditar.servicio, pasajeroAEditar.servicioPasajeroId, pasajeroAEditar.empleadoId, {
        nombreCompleto: nombreEditado,
        celular: celularEditado,
        barrio: barrioEditado,
        direccion: direccionEditada,
      })
      setPasajeroAEditar(null)
    } catch {
      setErrorEditar('No se pudieron guardar los cambios.')
    } finally {
      setGuardandoEdicion(false)
    }
  }

  return (
    <section className="vista-previa-importacion">
      {arrastrado && (
        <table ref={etiquetaArrastreRef} className="vista-previa-importacion__tarjeta-arrastre">
          <tbody>
            <tr>
              <td>{arrastrado.pasajero.cedula}</td>
              <td>{arrastrado.pasajero.nombreCompleto}</td>
              <td>{arrastrado.pasajero.direccion}</td>
              <td>{formatearBarrioConCiudad(arrastrado.pasajero.barrio, barriosVillamaria, arrastrado.pasajero.direccion)}</td>
              <td>{arrastrado.pasajero.celular}</td>
            </tr>
          </tbody>
        </table>
      )}
      <p className="vista-previa-importacion__resumen">
        <strong>{vista.transportador}</strong>Fecha en la hoja: <strong>{vista.fechaTexto || '—'}</strong> ·{' '}
        {vista.servicios.length} servicios · {vista.totalPasajeros} pasajeros · {vista.empleadosNuevos} personas nuevas
      </p>
      {vista.cruzaMedianoche && (
        <MensajeAlerta tipo="exito">
          La hoja cubre dos días: los servicios de madrugada (antes de las 12:00) se programan al día siguiente de la fecha operativa.
        </MensajeAlerta>
      )}
      {vista.errores.map((error) => (
        <MensajeAlerta key={error} tipo="error">
          {error}
        </MensajeAlerta>
      ))}
      {vista.advertencias.length > 0 && (
        <details className="vista-previa-importacion__avisos">
          <summary>⚠ {vista.advertencias.length} aviso{vista.advertencias.length === 1 ? '' : 's'} del reparto (clic para ver)</summary>
          {vista.advertencias.map((aviso) => (
            <p key={aviso} className="vista-previa-importacion__aviso">
              ⚠ {aviso}
            </p>
          ))}
        </details>
      )}
      {errorMover && <MensajeAlerta tipo="error">{errorMover}</MensajeAlerta>}
      {errorEliminarPasajero && <MensajeAlerta tipo="error">{errorEliminarPasajero}</MensajeAlerta>}
      {errorDespublicar && <MensajeAlerta tipo="error">{errorDespublicar}</MensajeAlerta>}
      {vista.servicios.map((servicio, indice) => {
        // Si la tarjeta ya sabe a qué servicio real corresponde (vista después de importar), se busca por
        // ese id exacto: sede+hora+tipo ya no identifica un único servicio, porque una misma ruta puede
        // repartirse en varios servicios reales (uno por zona o corredor vial).
        const real = servicio.servicioId
          ? serviciosReales.find((s) => s.servicioId === servicio.servicioId)
          : serviciosReales.find((s) =>
              s.tipo === servicio.tipo && s.sedeId === servicio.sedeId && s.horaProgramada === servicio.hora && (!servicio.fecha || s.fecha === servicio.fecha),
            )
        const opcionActual = real?.unidadOperativaId ? opcionesUnidad.find((o) => o.unidadOperativaId === real.unidadOperativaId) : undefined
        // Unidades ya comprometidas en otro servicio a esta misma fecha y hora (mismo criterio que usa el
        // reparto automático en el backend): se excluye el propio servicio que se está reasignando, para
        // que su conductor actual no aparezca como "ocupado" por sí mismo. Una entrada y una salida de la
        // misma sede a la misma hora no se cruzan: el conductor puede hacer las dos.
        const unidadesOcupadas = real
          ? new Set(
              serviciosReales
                .filter(
                  (s) =>
                    s.servicioId !== real.servicioId &&
                    s.fecha === real.fecha &&
                    s.horaProgramada === real.horaProgramada &&
                    !(s.tipo !== real.tipo && s.sedeId === real.sedeId) &&
                    !ESTADOS_QUE_LIBERAN_UNIDAD.has(s.estado),
                )
                .map((s) => s.unidadOperativaId)
                .filter((id): id is number => id !== null),
            )
          : new Set<number>()
        const abierto = desplegado === indice
        const sinAsignar = Boolean(real && !real.unidadOperativaId)
        const esDestino = esDestinoValido(real)

        let clase = sinAsignar ? 'vista-previa-importacion__servicio vista-previa-importacion__servicio--sin-asignar' : 'vista-previa-importacion__servicio'
        if (real?.estado === EstadoServicio.EN_CURSO) clase += ' vista-previa-importacion__servicio--en-curso'
        else if (real && ESTADOS_ENVIADOS.has(real.estado)) clase += ' vista-previa-importacion__servicio--enviado'
        if (arrastrado) clase += esDestino ? ' vista-previa-importacion__servicio--destino-valido' : ' vista-previa-importacion__servicio--destino-invalido'

        return (
          <div
            key={`${servicio.sedeEnHoja}-${servicio.hora}-${indice}`}
            className={clase}
            data-servicio-id={real ? real.servicioId : undefined}
          >
            <div className="vista-previa-importacion__cabecera">
              <h3>
                {servicio.tipo === 0 ? 'Entrada' : 'Salida'} · {servicio.sedeEnHoja} · {servicio.fecha ? `${servicio.fecha} · ` : ''}{servicio.hora.slice(0, 5)}
                {!servicio.sedeEncontrada && <span className="vista-previa-importacion__sin-sede"> (sede no encontrada)</span>}
                {real?.estado === EstadoServicio.EN_CURSO && <span className="vista-previa-importacion__etiqueta-en-curso"> 🟠 En curso</span>}
              </h3>
              {real && (
                <div className="vista-previa-importacion__conductor">
                  <span className={sinAsignar ? 'vista-previa-importacion__sin-asignar' : undefined}>
                    {sinAsignar && '⚠ '}
                    {opcionActual?.texto ?? (real.unidadOperativaId ? `Unidad #${real.unidadOperativaId}` : 'Sin asignar: no alcanzaron las unidades, reasigna a mano')}
                  </span>
                  {real.estado === EstadoServicio.PUBLICADO && (
                    <BotonSecundario type="button" onClick={() => setServicioADespublicar(real)} title="Vuelve a Asignado para poder arrastrarle o asignarle más pasajeros">
                      Volver a editable
                    </BotonSecundario>
                  )}
                  <BotonSecundario type="button" onClick={() => alDesplegar(indice, real)}>
                    {abierto ? 'Cerrar' : 'Cambiar conductor'}
                  </BotonSecundario>
                </div>
              )}
            </div>

            {real && abierto && (
              <div className="vista-previa-importacion__reasignar">
                <SelectorFormulario
                  id={`unidadPreviaServicio${indice}`}
                  etiqueta="Elegir conductor y vehículo"
                  valor={unidadElegida}
                  alCambiar={setUnidadElegida}
                  opciones={opcionesUnidad.map((o) => ({
                    valor: String(o.unidadOperativaId),
                    texto: o.texto,
                    disponible: !unidadesOcupadas.has(o.unidadOperativaId),
                  }))}
                  textoVacio={opcionesUnidad.length === 0 ? 'No hay unidades activas' : 'Elige la unidad'}
                />
                <BotonPrimario type="button" disabled={guardando || !unidadElegida} onClick={() => guardar(real, indice)}>
                  {guardando ? 'Guardando…' : 'Guardar'}
                </BotonPrimario>
                {errorReasignar && <MensajeAlerta tipo="error">{errorReasignar}</MensajeAlerta>}
              </div>
            )}

            {servicioReasignado === indice && <MensajeAlerta tipo="exito">Conductor reasignado correctamente.</MensajeAlerta>}

            <TablaDatos columnas={['#', 'Cédula', 'Nombre', 'Dirección', 'Barrio', 'Celular', '']}>
              {servicio.pasajeros.map((pasajero, indice) => {
                const sePuedeArrastrar = Boolean(real && pasajero.servicioPasajeroId && !ESTADOS_SIN_CAMBIOS.has(real.estado))
                const otrosDestinos = real && pasajero.servicioPasajeroId ? serviciosReales.filter((s) => sonRutasCompatibles(real, s)) : []
                // Conductores sin ruta en este mismo horario: se les puede abrir una ruta nueva para
                // mover ahí al pasajero (por ejemplo, para dividir una ruta con demasiados barrios).
                const conductoresLibres =
                  real && pasajero.servicioPasajeroId && !ESTADOS_SIN_CAMBIOS.has(real.estado)
                    ? opcionesUnidad.filter((o) => o.unidadOperativaId !== real.unidadOperativaId && !unidadesOcupadas.has(o.unidadOperativaId))
                    : []
                const menuAbierto = menuMoverAbierto?.id === pasajero.servicioPasajeroId

                return (
                  <tr
                    key={pasajero.cedula}
                    className={sePuedeArrastrar ? 'vista-previa-importacion__fila-arrastrable' : undefined}
                    onPointerDown={(evento) => {
                      if (!sePuedeArrastrar || !real || !pasajero.servicioPasajeroId || evento.button !== 0) return
                      // Si la presión empezó sobre la flechita de mover (otra acción en la misma fila), no se inicia un arrastre.
                      if (evento.target instanceof Element && evento.target.closest('.vista-previa-importacion__celda-mover')) return
                      const arrastrable = { servicioPasajeroId: pasajero.servicioPasajeroId, servicioOrigen: real, pasajero }
                      if (evento.pointerType !== 'mouse') {
                        // En pantallas táctiles deslizar el dedo desplaza la página: el arrastre solo empieza al dejar el dedo presionado.
                        armarPulsacionLarga(evento, (x, y) => iniciarArrastre(arrastrable, x, y), () => arrastradoRef.current !== null)
                        return
                      }
                      evento.preventDefault() // evita que el navegador intente seleccionar el texto de la fila mientras se arrastra
                      iniciarArrastre(arrastrable, evento.clientX, evento.clientY)
                    }}
                  >
                    <td>{indice + 1}</td>
                    <td>{pasajero.cedula}</td>
                    <td>{pasajero.nombreCompleto}</td>
                    <td>{pasajero.direccion}</td>
                    <td>{formatearBarrioConCiudad(pasajero.barrio, barriosVillamaria, pasajero.direccion)}</td>
                    <td>{pasajero.celular}</td>
                    <td className="vista-previa-importacion__celda-mover">
                      {(otrosDestinos.length > 0 || conductoresLibres.length > 0) && pasajero.servicioPasajeroId && (
                        <div className="vista-previa-importacion__mover">
                          <button
                            type="button"
                            className="vista-previa-importacion__boton-mover"
                            title="Mover a otro conductor de este mismo horario y sede"
                            onClick={(evento) => alternarMenuMover(pasajero.servicioPasajeroId!, evento.currentTarget)}
                          >
                            ▾
                          </button>
                          {menuAbierto &&
                            menuMoverAbierto &&
                            createPortal(
                              <div
                                className="vista-previa-importacion__menu-mover"
                                style={{ right: menuMoverAbierto.right, top: menuMoverAbierto.top ?? undefined, bottom: menuMoverAbierto.bottom ?? undefined }}
                              >
                                {otrosDestinos.map((destino) => {
                                  const etiqueta = opcionesUnidad.find((o) => o.unidadOperativaId === destino.unidadOperativaId)?.texto
                                    ?? (destino.unidadOperativaId ? `Unidad #${destino.unidadOperativaId}` : 'Sin asignar')
                                  return (
                                    <button
                                      key={destino.servicioId}
                                      type="button"
                                      onClick={async () => {
                                        setMenuMoverAbierto(null)
                                        await moverA(real!, pasajero.servicioPasajeroId!, destino)
                                      }}
                                    >
                                      {etiqueta} ({destino.cantidadPasajeros} pasajero{destino.cantidadPasajeros === 1 ? '' : 's'})
                                    </button>
                                  )
                                })}
                                {otrosDestinos.length > 0 && conductoresLibres.length > 0 && (
                                  <div className="vista-previa-importacion__menu-mover-separador" />
                                )}
                                {conductoresLibres.map((conductor) => (
                                  <button
                                    key={conductor.unidadOperativaId}
                                    type="button"
                                    onClick={async () => {
                                      setMenuMoverAbierto(null)
                                      await crearRutaYMoverA(real!, pasajero.servicioPasajeroId!, conductor.unidadOperativaId)
                                    }}
                                  >
                                    + Nueva ruta con {conductor.texto}
                                  </button>
                                ))}
                              </div>,
                              document.body,
                            )}
                        </div>
                      )}
                      {real && pasajero.servicioPasajeroId && pasajero.empleadoId && !ESTADOS_SIN_CAMBIOS.has(real.estado) && (
                        <button
                          type="button"
                          className="vista-previa-importacion__boton-editar"
                          title="Editar los datos de este pasajero"
                          onClick={() => abrirEdicion(real, pasajero)}
                        >
                          ✎
                        </button>
                      )}
                      {real && pasajero.servicioPasajeroId && !ESTADOS_SIN_CAMBIOS.has(real.estado) && (
                        <button
                          type="button"
                          className="vista-previa-importacion__boton-eliminar"
                          title="Eliminar a este pasajero de la ruta"
                          onClick={() =>
                            setPasajeroAEliminar({ servicio: real, servicioPasajeroId: pasajero.servicioPasajeroId!, nombre: pasajero.nombreCompleto })
                          }
                        >
                          🗑
                        </button>
                      )}
                    </td>
                  </tr>
                )
              })}
            </TablaDatos>
          </div>
        )
      })}

      <ModalConfirmacion
        abierto={pasajeroAEliminar !== null}
        titulo="Eliminar pasajero"
        mensaje={`¿Eliminar a ${pasajeroAEliminar?.nombre ?? 'este pasajero'} de la ruta? No queda registro de que estuvo asignado; si fue un error, tendrás que volver a agregarlo.`}
        textoConfirmar={eliminandoPasajero ? 'Eliminando…' : 'Eliminar'}
        alConfirmar={confirmarEliminarPasajero}
        alCancelar={() => setPasajeroAEliminar(null)}
      />

      <ModalConfirmacion
        abierto={servicioADespublicar !== null}
        titulo="Volver esta ruta a editable"
        mensaje="La ruta deja de estar publicada (vuelve a Asignado) para poder arrastrarle o asignarle más pasajeros. El conductor conserva la ruta y se le avisa que volvió a edición."
        textoConfirmar={despublicando ? 'Guardando…' : 'Volver a editable'}
        alConfirmar={confirmarDespublicar}
        alCancelar={() => setServicioADespublicar(null)}
      />

      <dialog ref={referenciaEdicion} className="vista-previa-importacion__modal-editar" onCancel={() => setPasajeroAEditar(null)}>
        <h2>Editar pasajero</h2>
        <p className="vista-previa-importacion__modal-nota">
          Nombre, celular y barrio son del empleado (se corrigen para todas sus rutas). La dirección es solo de esta ruta.
        </p>
        <form onSubmit={guardarEdicion} className="vista-previa-importacion__form-editar">
          <CampoFormulario id="editarNombre" etiqueta="Nombre" valor={nombreEditado} alCambiar={setNombreEditado} requerido />
          <CampoFormulario id="editarCelular" etiqueta="Celular" valor={celularEditado} alCambiar={setCelularEditado} requerido />
          <CampoFormulario id="editarBarrio" etiqueta="Barrio" valor={barrioEditado} alCambiar={setBarrioEditado} requerido />
          <CampoFormulario id="editarDireccion" etiqueta="Dirección (esta ruta)" valor={direccionEditada} alCambiar={setDireccionEditada} requerido />
          {errorEditar && <MensajeAlerta tipo="error">{errorEditar}</MensajeAlerta>}
          <div className="vista-previa-importacion__modal-acciones">
            <BotonSecundario type="button" onClick={() => setPasajeroAEditar(null)}>
              Cancelar
            </BotonSecundario>
            <BotonPrimario disabled={guardandoEdicion}>{guardandoEdicion ? 'Guardando…' : 'Guardar'}</BotonPrimario>
          </div>
        </form>
      </dialog>
    </section>
  )
}
