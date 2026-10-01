import { useEffect, useState, type MouseEvent } from 'react'
import { useParams } from 'react-router-dom'
import { BotonPrimario } from '../componentes/BotonPrimario'
import { BotonSecundario } from '../componentes/BotonSecundario'
import { ContenedorPagina } from '../componentes/ContenedorPagina'
import { EncabezadoPagina } from '../componentes/EncabezadoPagina'
import { IndicadorCarga } from '../componentes/IndicadorCarga'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { ModalConfirmacion } from '../componentes/ModalConfirmacion'
import { PanelBarriosSinZona } from '../componentes/PanelBarriosSinZona'
import { PanelCrearRutaPegado } from '../componentes/PanelCrearRutaPegado'
import { PanelVistaPreviaImportacion } from '../componentes/PanelVistaPreviaImportacion'
import { SelectorFormulario } from '../componentes/SelectorFormulario'
import { ZonaArrastreArchivo } from '../componentes/ZonaArrastreArchivo'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { EstadoServicio } from '../modelos/enumeraciones'
import { hoyColombia } from '../modelos/fechaColombia'
import type { ResultadoImportacion, ServicioPrevia, VistaPreviaImportacion } from '../modelos/importacion'
import type { Servicio } from '../modelos/operacion'
import type { Sede } from '../modelos/sede'
import { ErrorApi } from '../servicios/clienteHttp'
import { obtenerOpcionesDeUnidades, type OpcionUnidadOperativa } from '../servicios/servicioConductores'
import { actualizarEmpleado, obtenerEmpleado } from '../servicios/servicioEmpleados'
import { crearRutaVacia, importarExcel, validarImportacion } from '../servicios/servicioImportaciones'
import {
  asignarUnidad,
  deshacerReparto,
  despublicarServicio,
  editarDireccionPasajero,
  eliminarPasajero,
  eliminarRastro,
  eliminarServicio,
  moverPasajero,
  obtenerPasajeros,
  obtenerServiciosPendientes,
  publicarJornada,
} from '../servicios/servicioOperacion'
import { obtenerSedes } from '../servicios/servicioSedes'
import '../estilos/paginas/PaginaImportarExcel.css'

const MESES: Record<string, string> = {
  ENE: '01',
  FEB: '02',
  MAR: '03',
  ABR: '04',
  MAY: '05',
  JUN: '06',
  JUL: '07',
  AGO: '08',
  SEP: '09',
  SET: '09',
  OCT: '10',
  NOV: '11',
  DIC: '12',
}

/** Intenta leer el día de inicio de textos como "14-15 SEP" y lo devuelve como AAAA-MM-DD del año en curso (en Colombia). */
function fechaDesdeTexto(texto: string): string {
  const coincidencia = /(\d{1,2})\s*(?:[-–/]\s*\d{1,2})?\s*(?:DE\s+)?([A-Za-z]{3})/i.exec(texto)
  const mes = coincidencia ? MESES[coincidencia[2].toUpperCase()] : undefined
  if (!coincidencia || !mes) return ''
  return `${hoyColombia().slice(0, 4)}-${mes}-${coincidencia[1].padStart(2, '0')}`
}

/** Suma (o resta, con un número negativo) días a una fecha AAAA-MM-DD, en UTC. */
function sumarDias(fechaIso: string, dias: number): string {
  const fecha = new Date(`${fechaIso}T00:00:00Z`)
  fecha.setUTCDate(fecha.getUTCDate() + dias)
  return fecha.toISOString().slice(0, 10)
}

/** AAAA-MM-DD → DD/MM/AAAA. */
function fechaLegible(fechaIso: string): string {
  const [anio, mes, dia] = fechaIso.split('-')
  return `${dia}/${mes}/${anio}`
}

/** En el campo de fecha embebido en el título "Cargar Excel para el día …", un clic en cualquier parte despliega el selector nativo. */
function abrirSelectorFecha(evento: MouseEvent<HTMLInputElement>) {
  try {
    evento.currentTarget.showPicker()
  } catch {
    // Navegadores sin showPicker: queda el icono nativo del calendario.
  }
}

/**
 * Las rutas ya enviadas (publicadas o en curso) se siguen mostrando unos días después de su fecha; las que
 * falta enviar se muestran siempre, sin importar su fecha (por ejemplo, las de un Excel con la fecha de otro día).
 */
const DIAS_ATRAS_ENVIADAS = 3

/**
 * Programación: se arman las rutas (cargando un Excel o a mano), cada una
 * con su propia fecha, y cuando cada unidad ya tiene a sus empleados
 * asignados se publican todas de una vez con el botón de arriba, sin
 * importar si quedaron repartidas en jornadas de distintos días (por
 * ejemplo, la salida de la noche y las entradas de la madrugada del día
 * siguiente). No hace falta entrar a cada ruta ni ir cambiando estados una
 * por una, ni navegar día por día.
 */
export function PaginaImportarExcel() {
  const { empresaId } = useParams<{ empresaId: string }>()
  const { token } = useAutenticacion()
  const idEmpresa = Number(empresaId)

  const [fechaExcel, setFechaExcel] = useState(hoyColombia())
  const [publicando, setPublicando] = useState(false)
  const [mensajePublicar, setMensajePublicar] = useState<{
    tipo: 'exito' | 'error'
    texto: string
  } | null>(null)
  const [deshaciendo, setDeshaciendo] = useState(false)
  const [confirmarDeshacer, setConfirmarDeshacer] = useState(false)
  const [eliminandoRastro, setEliminandoRastro] = useState(false)
  const [confirmarEliminarRastro, setConfirmarEliminarRastro] = useState(false)
  const [menuAccionesAbierto, setMenuAccionesAbierto] = useState(false)

  const [archivo, setArchivo] = useState<File | null>(null)
  const [unidades, setUnidades] = useState<OpcionUnidadOperativa[]>([])
  const [sedes, setSedes] = useState<Sede[]>([])
  const [vista, setVista] = useState<VistaPreviaImportacion | null>(null)
  const [serviciosReales, setServiciosReales] = useState<Servicio[]>([])
  const [vistaReal, setVistaReal] = useState<VistaPreviaImportacion | null>(null)
  const [resultado, setResultado] = useState<ResultadoImportacion | null>(null)
  /** Sede elegida para los pasajeros del Excel que no traen sede (o cuya sede no se reconoce). */
  const [sedeGeneral, setSedeGeneral] = useState('')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [ocupado, setOcupado] = useState(false)

  useEffect(() => {
    if (!token || !empresaId) return
    obtenerOpcionesDeUnidades(idEmpresa, token)
      .then(setUnidades)
      .catch(() => setUnidades([]))
    obtenerSedes(idEmpresa, token)
      .then(setSedes)
      .catch(() => setSedes([]))
  }, [empresaId, idEmpresa, token])

  // Rutas pendientes de la empresa, de todas las jornadas (ver obtenerServiciosPendientes): permite
  // mostrar y cambiar el conductor de cada ruta directamente aquí, sin una pantalla de "jornada" aparte
  // ni tener que elegir una fecha para verlas, y no se pierden al salir y volver a la pantalla.
  async function cargarServiciosReales() {
    if (!token || !empresaId) {
      setServiciosReales([])
      return
    }
    try {
      setServiciosReales(await obtenerServiciosPendientes(idEmpresa, sumarDias(hoyColombia(), -DIAS_ATRAS_ENVIADAS), token))
    } catch {
      setServiciosReales([])
    }
  }

  useEffect(() => {
    cargarServiciosReales()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [empresaId, idEmpresa, token, resultado])

  // Jornadas con al menos una ruta activa (no finalizada) actualmente en pantalla: "Deshacer reparto" y
  // "Eliminar todo" son acciones por jornada, así que solo se habilitan cuando hay una sola en pantalla
  // (el caso normal); con rutas de varios días a la vez, el coordinador las gestiona ruta por ruta.
  const jornadasActivas = [...new Set(serviciosReales.filter((s) => s.estado !== EstadoServicio.FINALIZADO).map((s) => s.jornadaId))]
  const jornadaActivaUnica = jornadasActivas.length === 1 ? jornadasActivas[0] : null

  // Arma una vista igual a la del Excel pero con los datos ya guardados, para que las rutas de la fecha
  // elegida se sigan viendo al volver a esta pantalla sin tener que volver a cargar el archivo.
  useEffect(() => {
    if (!token || serviciosReales.length === 0) {
      setVistaReal(null)
      return
    }
    let cancelado = false
    Promise.all(
      // Una ruta finalizada ya no necesita acción del coordinador en esta pantalla: desaparece de aquí
      // (sigue disponible en "Rutas", que sí tiene un filtro de "Realizadas").
      serviciosReales
        .filter((s) => s.estado !== EstadoServicio.FINALIZADO)
        .map(async (s): Promise<ServicioPrevia> => {
          const pasajerosReales = await obtenerPasajeros(idEmpresa, s.jornadaId, s.servicioId, token)
          const sede = sedes.find((sd) => sd.sedeId === s.sedeId)
          return {
            tipo: s.tipo,
            sedeEnHoja: sede?.nombre ?? `Sede #${s.sedeId}`,
            sedeEncontrada: sede?.nombre ?? null,
            sedeId: s.sedeId,
            hora: s.horaProgramada,
            fecha: s.fecha,
            servicioId: s.servicioId,
            pasajeros: pasajerosReales.map((p) => ({
              cedula: p.cedulaEmpleado,
              nombreCompleto: p.nombreCompletoEmpleado,
              direccion: p.direccionRecogida,
              barrio: p.barrioEmpleado,
              celular: p.telefonoEmpleado,
              situacion: '',
              servicioPasajeroId: p.servicioPasajeroId,
              empleadoId: p.empleadoId,
            })),
          }
        }),
    )
      .then((servicios) => {
        if (cancelado) return
        // Una ruta sin ningún pasajero no debería quedar visible (es un rastro de una creación a medias,
        // por ejemplo si falló el paso de moverle el pasajero al crearla para dividir otra ruta).
        const conPasajeros = servicios.filter((s) => s.pasajeros.length > 0)
        // Ordena por fecha y luego por hora: las rutas visibles ya pueden abarcar varios días a la vez.
        conPasajeros.sort((a, b) => (a.fecha ?? '').localeCompare(b.fecha ?? '') || a.hora.localeCompare(b.hora))
        setVistaReal({
          transportador: '',
          fechaTexto: '',
          cruzaMedianoche: false,
          fechaOperativaSugerida: null,
          servicios: conPasajeros,
          totalPasajeros: conPasajeros.reduce((total, s) => total + s.pasajeros.length, 0),
          empleadosNuevos: 0,
          advertencias: [],
          errores: [],
          puedeImportar: true,
          barriosSinZona: resultado?.barriosSinZona ?? [],
        })
      })
      .catch(() => {
        if (!cancelado) setVistaReal(null)
      })
    return () => {
      cancelado = true
    }
  }, [serviciosReales, sedes, token, idEmpresa, resultado])

  async function alReasignarConductor(servicio: Servicio, unidadOperativaId: number) {
    if (!token) return
    await asignarUnidad(idEmpresa, servicio.jornadaId, servicio.servicioId, unidadOperativaId, token)
    await cargarServiciosReales()
  }

  /** El coordinador arrastró a un pasajero de una tarjeta de ruta a otra: lo mueve ahí. */
  async function alMoverPasajero(servicioOrigen: Servicio, servicioPasajeroId: number, servicioDestinoId: number) {
    if (!token) return
    await moverPasajero(idEmpresa, servicioOrigen.jornadaId, servicioOrigen.servicioId, servicioPasajeroId, servicioDestinoId, token)
    await cargarServiciosReales()
  }

  /** El coordinador borra por completo a un pasajero de la ruta (por ejemplo, se agregó por error al pegar filas). */
  async function alEliminarPasajero(servicio: Servicio, servicioPasajeroId: number) {
    if (!token) return
    await eliminarPasajero(idEmpresa, servicio.jornadaId, servicio.servicioId, servicioPasajeroId, token)
    await cargarServiciosReales()
  }

  /**
   * El coordinador "despublica" una ruta ya enviada (vuelve a Asignado) para poder seguir editándola:
   * arrastrarle pasajeros desde otra ruta, asignárselos directo, etc. Si tiene conductor, se le notifica.
   */
  async function alDespublicar(servicio: Servicio) {
    if (!token) return
    await despublicarServicio(idEmpresa, servicio.jornadaId, servicio.servicioId, token)
    await cargarServiciosReales()
  }

  /**
   * El coordinador corrige los datos de un pasajero ya creado (por ejemplo, un dato mal pegado al crear
   * la ruta). Nombre, celular y barrio viven en el Empleado (se actualizan ahí, sin tocar su dirección
   * habitual: se conserva la que ya tenía); la dirección de esta ruta puntual vive en el ServicioPasajero
   * y se corrige aparte, sin cambiar su estado ni notificar a nadie.
   */
  async function alEditarPasajero(
    servicio: Servicio,
    servicioPasajeroId: number,
    empleadoId: number,
    datos: {
      nombreCompleto: string
      celular: string
      barrio: string
      direccion: string
    },
  ) {
    if (!token) return
    const empleadoActual = await obtenerEmpleado(idEmpresa, empleadoId, token)
    await actualizarEmpleado(
      idEmpresa,
      empleadoId,
      {
        nombreCompleto: datos.nombreCompleto,
        telefono: datos.celular,
        direccion: empleadoActual.direccion,
        barrio: datos.barrio,
      },
      token,
    )
    await editarDireccionPasajero(idEmpresa, servicio.jornadaId, servicio.servicioId, servicioPasajeroId, datos.direccion, token)
    await cargarServiciosReales()
  }

  /**
   * El coordinador eligió, en el menú de mover, un conductor libre sin ruta en ese horario: le abre una
   * ruta nueva (misma sede, fecha, hora y tipo que el origen) y le mueve ahí el pasajero. Sirve para
   * dividir una ruta que quedó con demasiados barrios distintos (por ejemplo, una que absorbió todo lo
   * que había quedado sin conductor) entre varios conductores.
   */
  async function alCrearRutaYMoverPasajero(servicioOrigen: Servicio, servicioPasajeroId: number, unidadOperativaId: number) {
    if (!token) return
    const nuevaRuta = await crearRutaVacia(
      idEmpresa,
      {
        fecha: servicioOrigen.fecha,
        hora: servicioOrigen.horaProgramada,
        tipo: servicioOrigen.tipo,
        sedeId: servicioOrigen.sedeId,
        unidadOperativaId,
      },
      token,
    )
    if (nuevaRuta.servicioId) {
      try {
        await moverPasajero(idEmpresa, servicioOrigen.jornadaId, servicioOrigen.servicioId, servicioPasajeroId, nuevaRuta.servicioId, token)
      } catch (error) {
        // Si el pasajero no alcanzó a moverse, no dejar la ruta recién creada vacía: se borra de una vez
        // (recién se creó, sin pasajeros todavía, así que no hay nada más que perder).
        await eliminarServicio(idEmpresa, nuevaRuta.jornadaId, nuevaRuta.servicioId, token).catch(() => {})
        throw error
      }
    }
    await cargarServiciosReales()
  }

  /** El coordinador resolvió un barrio sin zona (lo agregó a una zona o creó una nueva): ya no hace falta mostrarlo. */
  function alResolverBarrioSinZona(barrio: string) {
    const quitarBarrio = (v: VistaPreviaImportacion) => ({
      ...v,
      barriosSinZona: v.barriosSinZona.filter((b) => b !== barrio),
    })
    setVista((anterior) => (anterior ? quitarBarrio(anterior) : anterior))
    setVistaReal((anterior) => (anterior ? quitarBarrio(anterior) : anterior))
  }

  /**
   * Publica de una vez todas las jornadas que tienen alguna ruta activa en pantalla (sin importar de
   * cuántos días distintos sean): cada una se publica por separado, así que si alguna todavía tiene
   * algún servicio sin resolver o sin unidad, esa falla pero las demás igual quedan publicadas.
   */
  async function alPublicar() {
    if (!token) return
    setPublicando(true)
    setMensajePublicar(null)
    if (jornadasActivas.length === 0) {
      setMensajePublicar({
        tipo: 'error',
        texto: 'Todavía no hay ninguna ruta armada para publicar.',
      })
      setPublicando(false)
      return
    }
    const resultados = await Promise.allSettled(jornadasActivas.map((jornadaId) => publicarJornada(idEmpresa, jornadaId, token)))
    const fallidos = resultados.filter((r): r is PromiseRejectedResult => r.status === 'rejected')
    if (fallidos.length === 0) {
      setMensajePublicar({
        tipo: 'exito',
        texto: 'Listo: todas las rutas quedaron publicadas. Ya las ven los conductores.',
      })
    } else {
      const primerError = fallidos[0].reason
      const detalle = primerError instanceof ErrorApi ? primerError.message : 'No se pudieron publicar algunas rutas.'
      setMensajePublicar({
        tipo: 'error',
        texto:
          jornadasActivas.length > 1
            ? `${detalle} (${jornadasActivas.length - fallidos.length} de ${jornadasActivas.length} jornadas sí quedaron publicadas.)`
            : detalle,
      })
    }
    await cargarServiciosReales()
    setPublicando(false)
  }

  async function alDeshacerReparto() {
    if (!token || jornadaActivaUnica === null) return
    setConfirmarDeshacer(false)
    setDeshaciendo(true)
    setMensajePublicar(null)
    try {
      await deshacerReparto(idEmpresa, jornadaActivaUnica, token)
      setMensajePublicar({
        tipo: 'exito',
        texto: 'Listo: ya puedes volver a cargar el Excel de esa fecha.',
      })
      setResultado(null)
      setVista(null)
      await cargarServiciosReales()
    } catch (error) {
      setMensajePublicar({
        tipo: 'error',
        texto: error instanceof ErrorApi ? error.message : 'No se pudo deshacer el reparto.',
      })
    } finally {
      setDeshaciendo(false)
    }
  }

  /**
   * Borra de raíz cualquier rastro de programación de esta jornada (rutas, pasajeros y las programaciones
   * de transporte que hayan quedado sin asignar, por ejemplo de una importación que falló a medias), para
   * poder volver a cargar el Excel como si esa fecha nunca se hubiera importado. No toca empleados ni el
   * historial de importaciones. A diferencia de "Deshacer reparto", no sirve si ya hay rutas publicadas.
   */
  async function alEliminarRastro() {
    if (!token || jornadaActivaUnica === null) return
    setConfirmarEliminarRastro(false)
    setEliminandoRastro(true)
    setMensajePublicar(null)
    try {
      await eliminarRastro(idEmpresa, jornadaActivaUnica, token)
      setMensajePublicar({
        tipo: 'exito',
        texto: 'Listo: esa fecha quedó como si nunca se hubiera importado nada. Ya puedes cargar el Excel desde cero.',
      })
      setResultado(null)
      setVista(null)
      setArchivo(null)
      await cargarServiciosReales()
    } catch (error) {
      setMensajePublicar({
        tipo: 'error',
        texto: error instanceof ErrorApi ? error.message : 'No se pudo eliminar la programación de esa fecha.',
      })
    } finally {
      setEliminandoRastro(false)
    }
  }

  async function alElegirArchivo(nuevo: File) {
    if (!token || !empresaId) return
    setArchivo(nuevo)
    setVista(null)
    setResultado(null)
    setMensajeError(null)
    setOcupado(true)
    try {
      const previa = await validarImportacion(idEmpresa, nuevo, token)
      setVista(previa)
      const fechaSugerida = previa.fechaOperativaSugerida ?? fechaDesdeTexto(previa.fechaTexto)
      if (fechaSugerida) setFechaExcel(fechaSugerida)
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo leer el archivo.')
    } finally {
      setOcupado(false)
    }
  }

  async function alImportar(repartir: boolean, unidadId: number | null) {
    if (!token || !empresaId || !archivo || !fechaExcel) return
    setOcupado(true)
    setMensajeError(null)
    try {
      setResultado(await importarExcel(idEmpresa, archivo, fechaExcel, unidadId, repartir, sedeGeneral ? Number(sedeGeneral) : null, token))
      // La vista previa (armada antes de importar) no sabe cómo quedó el reparto real por zona ni
      // si algún grupo quedó sin unidad por falta de cupo: se descarta para que se muestre "vistaReal",
      // construida a partir de los servicios ya guardados, uno por cada ruta real (incluida la de "sin asignar").
      setVista(null)
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo importar el archivo.')
    } finally {
      setOcupado(false)
    }
  }

  // Sigue habilitado después de importar (a propósito): si el reparto automático no quedó como
  // se esperaba, el coordinador puede volver a hacer clic para intentarlo de nuevo (por ejemplo,
  // después de ajustar las Zonas) sin tener que volver a elegir el archivo desde cero.
  const faltaSede = Boolean(vista?.servicios.some((servicio) => !servicio.sedeEncontrada && !servicio.sedeEnHoja.trim()))
  const listo = Boolean(archivo && fechaExcel) && !ocupado && (!faltaSede || sedeGeneral !== '')

  return (
    <ContenedorPagina ancho="amplio">
      <EncabezadoPagina
        titulo="Programación"
        subtitulo="Arma las rutas (con Excel o a mano), cada una con su propia fecha, y publícalas todas juntas cuando estén listas, sin importar el día de cada una."
      />
      <div className="pagina-importar-excel__enviar">
        <BotonPrimario className="pagina-importar-excel__boton-enviar" type="button" disabled={publicando} onClick={alPublicar}>
          {publicando ? (
            <>
              <IndicadorCarga /> Publicando…
            </>
          ) : (
            '📢 Enviar Rutas'
          )}
        </BotonPrimario>
        <span className="pagina-importar-excel__enviar-nota">Envía todas las rutas listas a sus conductores</span>
      </div>
      {mensajePublicar && <MensajeAlerta tipo={mensajePublicar.tipo}>{mensajePublicar.texto}</MensajeAlerta>}

      <ModalConfirmacion
        abierto={confirmarDeshacer}
        titulo="¿Deshacer el reparto automático de este día?"
        mensaje="Se borran todas las rutas de esta jornada que todavía no se publicaron (sus pasajeros quedan sin asignar, listos para repartirse de nuevo). Las rutas ya publicadas o en curso no se tocan."
        textoConfirmar="Deshacer reparto"
        alConfirmar={alDeshacerReparto}
        alCancelar={() => setConfirmarDeshacer(false)}
      />

      <ModalConfirmacion
        abierto={confirmarEliminarRastro}
        titulo="¿Eliminar toda la programación de este día?"
        mensaje="Se borran las rutas de esta jornada que todavía no se publicaron, sus pasajeros y también las programaciones de transporte que hayan quedado sin asignar (por ejemplo, de un Excel que se importó a medias). Queda como si nunca se hubiera importado nada para esa fecha: puedes cargar el Excel desde cero. No se toca a los empleados ni el historial de importaciones. No sirve si ya hay rutas publicadas o en curso."
        textoConfirmar="Eliminar todo"
        alConfirmar={alEliminarRastro}
        alCancelar={() => setConfirmarEliminarRastro(false)}
      />

      <div className="pagina-importar-excel__fuentes">
        <section className="pagina-importar-excel__fuente">
          <h2>
            Cargar Excel para el día{' '}
            <input
              className="pagina-importar-excel__fecha-excel-inline"
              type="date"
              value={fechaExcel}
              onChange={(evento) => setFechaExcel(evento.target.value)}
              onClick={abrirSelectorFecha}
              required
            />
          </h2>
          <ZonaArrastreArchivo archivo={archivo} alElegir={alElegirArchivo} deshabilitada={ocupado} />
          {ocupado && !vista && <p className="contenedor-pagina__estado">Leyendo el archivo…</p>}
          {archivo && fechaExcel < hoyColombia() && (
            <MensajeAlerta tipo="error">
              Ojo: la fecha es el {fechaLegible(fechaExcel)}, que ya pasó (se tomó del archivo). Si las rutas son para otro día, cámbiala arriba antes de
              repartir.
            </MensajeAlerta>
          )}
          {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
        </section>

        <PanelCrearRutaPegado empresaId={idEmpresa} opcionesUnidad={unidades} alTerminar={cargarServiciosReales} />
      </div>

      {/* "Repartir" necesita el Excel cargado; "Más acciones" (deshacer, eliminar) sirve siempre que haya rutas
          pendientes, también al volver a la pantalla sin el archivo. */}
      {(archivo || jornadasActivas.length > 0) && (
        <div className="programacion-acciones">
          {archivo && faltaSede && (
            <div className="programacion-acciones__fila">
              <SelectorFormulario
                id="sede-general"
                etiqueta="Sede para los pasajeros que no la traen en el Excel"
                valor={sedeGeneral}
                opciones={sedes.filter((sede) => sede.activa).map((sede) => ({ valor: String(sede.sedeId), texto: sede.nombre }))}
                alCambiar={setSedeGeneral}
                requerido
              />
            </div>
          )}
          {archivo && (
            <div className="programacion-acciones__fila">
              <BotonPrimario type="button" disabled={!listo || unidades.length === 0} onClick={() => alImportar(true, null)}>
                {ocupado ? (
                  <>
                    <IndicadorCarga /> Repartiendo…
                  </>
                ) : resultado ? (
                  'Repartir de nuevo'
                ) : (
                  'Repartir entre todas las unidades'
                )}
              </BotonPrimario>
              <span className="programacion-acciones__nota">
                {unidades.length === 0
                  ? 'Aún no tienes unidades: agrega conductores con su vehículo en Conductores.'
                  : `Usa ${unidades.length} unidades activas según su capacidad; ninguna se repite a la misma hora.`}
              </span>
            </div>
          )}

          {jornadasActivas.length > 0 && (
            <div className="programacion-acciones__menu">
              <BotonSecundario type="button" onClick={() => setMenuAccionesAbierto((abierto) => !abierto)}>
                Más acciones ▾
              </BotonSecundario>
              {menuAccionesAbierto && (
                <div className="programacion-acciones__menu-lista">
                  <button
                    type="button"
                    disabled={jornadaActivaUnica === null || deshaciendo}
                    title={
                      jornadasActivas.length > 1 ? 'No disponible con rutas de varios días a la vez: deshaz el reparto de cada una por separado.' : undefined
                    }
                    onClick={() => {
                      setMenuAccionesAbierto(false)
                      setConfirmarDeshacer(true)
                    }}
                  >
                    {deshaciendo ? 'Deshaciendo…' : '↺ Deshacer reparto automático'}
                  </button>
                  <button
                    type="button"
                    disabled={jornadaActivaUnica === null || eliminandoRastro}
                    title={jornadasActivas.length > 1 ? 'No disponible con rutas de varios días a la vez: elimina cada una por separado.' : undefined}
                    onClick={() => {
                      setMenuAccionesAbierto(false)
                      setConfirmarEliminarRastro(true)
                    }}
                  >
                    {eliminandoRastro ? 'Eliminando…' : '🗑 Eliminar todo de esta fecha'}
                  </button>
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {resultado && (
        <>
          <MensajeAlerta tipo="exito">Listo: rutas creadas para el {fechaLegible(fechaExcel)}. Ya puedes revisar cada ruta abajo.</MensajeAlerta>
          {resultado.advertencias.length > 0 && (
            <details className="programacion-acciones__avisos">
              <summary>
                ⚠ {resultado.advertencias.length} aviso
                {resultado.advertencias.length === 1 ? '' : 's'} del reparto (clic para ver)
              </summary>
              {resultado.advertencias.map((aviso) => (
                <p key={aviso} className="programacion-acciones__nota">
                  {aviso}
                </p>
              ))}
            </details>
          )}
        </>
      )}

      {(vista ?? vistaReal) && (vista ?? vistaReal)!.barriosSinZona.length > 0 && (
        <PanelBarriosSinZona empresaId={idEmpresa} barrios={(vista ?? vistaReal)!.barriosSinZona} alResolver={alResolverBarrioSinZona} />
      )}

      {(vista ?? vistaReal) && (
        <PanelVistaPreviaImportacion
          empresaId={idEmpresa}
          vista={(vista ?? vistaReal)!}
          serviciosReales={serviciosReales}
          opcionesUnidad={unidades}
          alReasignar={alReasignarConductor}
          alMoverPasajero={alMoverPasajero}
          alCrearRutaYMoverPasajero={alCrearRutaYMoverPasajero}
          alEliminarPasajero={alEliminarPasajero}
          alEditarPasajero={alEditarPasajero}
          alDespublicar={alDespublicar}
        />
      )}
    </ContenedorPagina>
  )
}
