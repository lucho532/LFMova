import { Camera, CameraResultType, CameraSource } from '@capacitor/camera'
import { useEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { EstadoPasajero } from '../modelos/enumeraciones'
import type { ServicioPasajero } from '../modelos/operacion'
import type { IncidenciaPasajero, UbicacionAnterior } from '../servicios/servicioConductorPropio'
import { ModalConfirmacion } from './ModalConfirmacion'
import '../estilos/componentes/HerramientasPasajero.css'

/** Tipos de incidencia que el conductor puede reportar (mismo orden que el enum TipoIncidencia del backend). */
const TIPOS_INCIDENCIA = [
  { valor: 0, texto: 'No contesta' },
  { valor: 1, texto: 'No se encuentra' },
  { valor: 2, texto: 'Dirección incorrecta' },
  { valor: 3, texto: 'No se pudo recoger' },
  { valor: 4, texto: 'Ubicación modificada' },
  { valor: 5, texto: 'Otra' },
]

/** Cuál sección desplegable está abierta (solo una a la vez, para no ocupar toda la pantalla). */
type Seccion = 'anteriores' | 'incidencias' | 'nuevaIncidencia'

interface PropiedadesHerramientasPasajero {
  pasajero: ServicioPasajero
  /** Solo mientras el servicio está en curso se puede reportar una incidencia (antes no hay nada que reportar). */
  servicioEnCurso: boolean
  guardarUbicacion: (latitud: number, longitud: number) => Promise<void>
  cargarUbicacionesAnteriores: () => Promise<UbicacionAnterior[]>
  eliminarUbicacionGuardada: () => Promise<void>
  reportarIncidencia: (tipo: number, descripcion: string, foto: File | null, latitud: number | null, longitud: number | null) => Promise<void>
  /** Incidencias ya reportadas para este pasajero (para poder revisarlas después), con sus evidencias. */
  cargarIncidencias: () => Promise<IncidenciaPasajero[]>
  /** Trae la foto de una evidencia como una URL local lista para mostrarla en un <img>. */
  cargarFotoEvidencia: (incidenciaId: number, evidenciaId: number) => Promise<string>
}

function etiquetaTipoIncidencia(tipo: number): string {
  return TIPOS_INCIDENCIA.find((t) => t.valor === tipo)?.texto ?? 'Incidencia'
}

/** Enlace de Google Maps para navegar a un punto por sus coordenadas. */
function enlaceNavegacion(destino: { latitud: number; longitud: number }): string {
  return `https://www.google.com/maps/dir/?api=1&destination=${destino.latitud},${destino.longitud}&travelmode=driving`
}

function obtenerPosicion(): Promise<GeolocationPosition> {
  return new Promise((resolver, rechazar) => {
    if (!navigator.geolocation) {
      rechazar(new Error('Este dispositivo no permite obtener la ubicación.'))
      return
    }
    navigator.geolocation.getCurrentPosition(resolver, () => rechazar(new Error('No se pudo obtener tu ubicación. Revisa el permiso de ubicación.')), {
      enableHighAccuracy: true,
      timeout: 15000,
    })
  })
}

/**
 * Herramientas de campo de un pasajero: navegar, guardar el punto GPS exacto
 * de la recogida, ver/olvidar la ubicación guardada del empleado y reportar
 * una incidencia con foto. Cada una es una fila propia; las que tienen
 * contenido (ubicaciones anteriores, incidencias reportadas, nueva
 * incidencia) se despliegan una a la vez, para no llenar la pantalla con
 * todo abierto de una. "Navegar" solo usa una ubicación conocida (la que
 * compartió el empleado para esta ruta, o la última guardada de una recogida
 * anterior); si no hay ninguna, avisa en vez de intentar con la dirección
 * escrita. El backend valida quién puede hacer cada acción.
 */
export function HerramientasPasajero({
  pasajero,
  servicioEnCurso,
  guardarUbicacion,
  cargarUbicacionesAnteriores,
  eliminarUbicacionGuardada,
  reportarIncidencia,
  cargarIncidencias,
  cargarFotoEvidencia,
}: PropiedadesHerramientasPasajero) {
  const [aviso, setAviso] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(null)
  const [ocupado, setOcupado] = useState(false)
  const [seccionAbierta, setSeccionAbierta] = useState<Seccion | null>(null)
  const [anteriores, setAnteriores] = useState<UbicacionAnterior[] | null>(null)
  const [ubicacionGuardada, setUbicacionGuardada] = useState<UbicacionAnterior | null>(null)
  const [tipo, setTipo] = useState(0)
  const [descripcion, setDescripcion] = useState('')
  const [foto, setFoto] = useState<File | null>(null)
  const [tomandoFoto, setTomandoFoto] = useState(false)
  // Los cuatro primeros tipos significan que no se pudo recoger al pasajero: el sistema lo deja como no recogido.
  const impideRecogida = tipo <= 3

  const [confirmandoIncidencia, setConfirmandoIncidencia] = useState(false)
  const [incidencias, setIncidencias] = useState<IncidenciaPasajero[] | null>(null)
  const [fotos, setFotos] = useState<Record<number, string>>({})
  const [cargandoFoto, setCargandoFoto] = useState<number | null>(null)
  const temporizadorAviso = useRef<number | null>(null)

  // Las URL de las fotos (URL.createObjectURL) viven en memoria del navegador: se liberan al salir de la tarjeta.
  useEffect(() => {
    return () => {
      Object.values(fotos).forEach((url) => URL.revokeObjectURL(url))
      if (temporizadorAviso.current) clearTimeout(temporizadorAviso.current)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  /** Muestra un aviso que se cierra solo a los pocos segundos, para no dejar un letrero pegado en la tarjeta. */
  function mostrarAviso(nuevoAviso: { tipo: 'exito' | 'error'; texto: string }) {
    if (temporizadorAviso.current) clearTimeout(temporizadorAviso.current)
    setAviso(nuevoAviso)
    temporizadorAviso.current = window.setTimeout(() => setAviso(null), 6000)
  }

  // Se consulta apenas se muestra la tarjeta: así el botón "Navegar" ya sabe, sin esperar un clic, si hay una ubicación guardada de una recogida anterior.
  useEffect(() => {
    let cancelado = false
    cargarUbicacionesAnteriores()
      .then((lista) => {
        if (!cancelado) setUbicacionGuardada(lista.find((u) => u.latitud !== null && u.longitud !== null) ?? null)
      })
      .catch(() => {
        // Sin ubicación guardada previa no pasa nada: el botón de navegar simplemente avisará que no hay ninguna.
      })
    return () => {
      cancelado = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pasajero.servicioPasajeroId])

  async function ejecutar(accion: () => Promise<void>, exito: string | null, fallo: string) {
    setOcupado(true)
    setAviso(null)
    try {
      await accion()
      if (exito) mostrarAviso({ tipo: 'exito', texto: exito })
    } catch (error) {
      mostrarAviso({ tipo: 'error', texto: error instanceof Error ? error.message : fallo })
    } finally {
      setOcupado(false)
    }
  }

  async function alGuardarUbicacion() {
    await ejecutar(
      async () => {
        const posicion = await obtenerPosicion()
        await guardarUbicacion(posicion.coords.latitude, posicion.coords.longitude)
        setUbicacionGuardada({ direccion: pasajero.direccionRecogida, barrio: pasajero.barrioEmpleado, latitud: posicion.coords.latitude, longitud: posicion.coords.longitude, fechaRegistro: new Date().toISOString() })
      },
      'Ubicación de recogida guardada.',
      'No se pudo guardar la ubicación.',
    )
  }

  async function alEliminarUbicacionGuardada() {
    await ejecutar(
      async () => {
        await eliminarUbicacionGuardada()
        setUbicacionGuardada(null)
        setAnteriores(null)
      },
      'Se olvidó la ubicación guardada: la próxima recogida guardará una nueva.',
      'No se pudo eliminar la ubicación guardada.',
    )
  }

  async function alAlternarAnteriores() {
    if (seccionAbierta === 'anteriores') {
      setSeccionAbierta(null)
      setAnteriores(null)
      return
    }
    setSeccionAbierta('anteriores')
    await ejecutar(async () => setAnteriores(await cargarUbicacionesAnteriores()), null, 'No se pudieron cargar las ubicaciones anteriores.')
  }

  async function alAlternarIncidencias() {
    if (seccionAbierta === 'incidencias') {
      setSeccionAbierta(null)
      return
    }
    setSeccionAbierta('incidencias')
    if (incidencias === null) {
      await ejecutar(async () => setIncidencias(await cargarIncidencias()), null, 'No se pudieron cargar las incidencias.')
    }
  }

  function alAlternarNuevaIncidencia() {
    setSeccionAbierta((actual) => (actual === 'nuevaIncidencia' ? null : 'nuevaIncidencia'))
  }

  async function alVerFoto(incidenciaId: number, evidenciaId: number) {
    if (fotos[evidenciaId]) return
    setCargandoFoto(evidenciaId)
    try {
      const url = await cargarFotoEvidencia(incidenciaId, evidenciaId)
      setFotos((anteriores) => ({ ...anteriores, [evidenciaId]: url }))
    } catch {
      mostrarAviso({ tipo: 'error', texto: 'No se pudo cargar la foto.' })
    } finally {
      setCargandoFoto(null)
    }
  }

  /**
   * Abre la cámara nativa directamente (nunca la galería ni ningún otro
   * origen): la evidencia de una incidencia debe ser una foto tomada en el
   * momento, no una imagen elegida de antes.
   */
  async function alTomarFoto() {
    setTomandoFoto(true)
    try {
      const capturada = await Camera.getPhoto({
        source: CameraSource.Camera,
        resultType: CameraResultType.Uri,
        quality: 80,
        allowEditing: false,
        saveToGallery: false,
      })
      if (!capturada.webPath) return
      const respuesta = await fetch(capturada.webPath)
      const datos = await respuesta.blob()
      setFoto(new File([datos], `incidencia-${Date.now()}.jpg`, { type: datos.type || 'image/jpeg' }))
    } catch (error) {
      // Cancelar la cámara no es un error que deba mostrarse.
      if (!(error instanceof Error) || !/cancel/i.test(error.message)) {
        mostrarAviso({ tipo: 'error', texto: 'No se pudo abrir la cámara. Revisa el permiso de cámara.' })
      }
    } finally {
      setTomandoFoto(false)
    }
  }

  async function alReportar() {
    await ejecutar(
      async () => {
        let posicion: GeolocationPosition | null = null
        try {
          posicion = await obtenerPosicion()
        } catch {
          // La ubicación es opcional en una incidencia.
        }
        await reportarIncidencia(tipo, descripcion.trim() || (TIPOS_INCIDENCIA.find((t) => t.valor === tipo)?.texto ?? ''), foto, posicion?.coords.latitude ?? null, posicion?.coords.longitude ?? null)
        setDescripcion('')
        setFoto(null)
        setSeccionAbierta(null)
      },
      // Sin aviso de éxito aquí: reportarIncidencia ya muestra su propia confirmación a nivel de página
      // (recargar la lista puede mover o esconder esta tarjeta antes de que un aviso local se llegue a ver).
      null,
      'No se pudo registrar la incidencia.',
    )
  }

  // "Navegar" solo usa una ubicación conocida: la compartida por el empleado para esta ruta, o si no, la última guardada.
  const destino =
    pasajero.latitud !== null && pasajero.longitud !== null
      ? { latitud: pasajero.latitud, longitud: pasajero.longitud }
      : ubicacionGuardada
        ? { latitud: ubicacionGuardada.latitud!, longitud: ubicacionGuardada.longitud! }
        : null

  return (
    <div className="herramientas-pasajero">
      <div className="herramientas-pasajero__fila">
        {destino ? (
          <a className="herramientas-pasajero__fila-boton" href={enlaceNavegacion(destino)} target="_blank" rel="noreferrer">
            <span>🧭 Navegar</span>
          </a>
        ) : (
          <button
            type="button"
            className="herramientas-pasajero__fila-boton"
            onClick={() => mostrarAviso({ tipo: 'error', texto: 'No hay ninguna ubicación guardada para este pasajero.' })}
          >
            <span>🧭 Navegar</span>
          </button>
        )}
      </div>

      {aviso &&
        createPortal(
          // En un portal a document.body, fijo en pantalla: dentro del panel podía quedar fuera de vista
          // (por ejemplo, arriba de la sección de incidencia que se acaba de usar, sin hacer scroll).
          <p className={`herramientas-pasajero__aviso-flotante herramientas-pasajero__aviso-flotante--${aviso.tipo}`} role={aviso.tipo === 'error' ? 'alert' : 'status'}>
            {aviso.texto}
          </p>,
          document.body,
        )}

      <div className="herramientas-pasajero__fila">
        <button type="button" className="herramientas-pasajero__fila-boton" disabled={ocupado} onClick={alGuardarUbicacion}>
          <span>📍 Guardar ubicación</span>
        </button>
      </div>

      <div className="herramientas-pasajero__fila">
        <button
          type="button"
          className="herramientas-pasajero__fila-boton"
          disabled={ocupado}
          onClick={alAlternarAnteriores}
          aria-expanded={seccionAbierta === 'anteriores'}
        >
          <span>🕘 Ubicaciones anteriores</span>
          <span className="herramientas-pasajero__flecha">{seccionAbierta === 'anteriores' ? '▲' : '▼'}</span>
        </button>
        {seccionAbierta === 'anteriores' && anteriores && (
          <ul className="herramientas-pasajero__lista">
            {anteriores.length === 0 && <li>Este empleado no tiene ubicaciones anteriores guardadas.</li>}
            {anteriores.map((u, indice) => (
              <li key={`${u.fechaRegistro}-${indice}`}>
                <span>
                  {u.direccion}
                  {u.barrio ? ` · ${u.barrio}` : ''} <small>({u.fechaRegistro.slice(0, 10)})</small>
                </span>
                {u.latitud !== null && u.longitud !== null && (
                  <a href={enlaceNavegacion({ latitud: u.latitud, longitud: u.longitud })} target="_blank" rel="noreferrer">
                    Navegar
                  </a>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>

      {ubicacionGuardada && (
        <div className="herramientas-pasajero__fila">
          <button type="button" className="herramientas-pasajero__fila-boton" disabled={ocupado} onClick={alEliminarUbicacionGuardada}>
            <span>🗑 Olvidar ubicación guardada</span>
          </button>
        </div>
      )}

      {servicioEnCurso && pasajero.estado === EstadoPasajero.CONDUCTOR_LLEGO && (
        <div className="herramientas-pasajero__fila">
          <button
            type="button"
            className="herramientas-pasajero__fila-boton"
            onClick={alAlternarNuevaIncidencia}
            aria-expanded={seccionAbierta === 'nuevaIncidencia'}
          >
            <span>📷 Foto / incidencia</span>
            <span className="herramientas-pasajero__flecha">{seccionAbierta === 'nuevaIncidencia' ? '▲' : '▼'}</span>
          </button>
          {seccionAbierta === 'nuevaIncidencia' && (
            <div className="herramientas-pasajero__incidencia">
              <label>
                Tipo
                <select value={tipo} onChange={(evento) => setTipo(Number(evento.target.value))}>
                  {TIPOS_INCIDENCIA.map((t) => (
                    <option key={t.valor} value={t.valor}>
                      {t.texto}
                    </option>
                  ))}
                </select>
              </label>
              {impideRecogida && <p className="herramientas-pasajero__nota">Al registrarla, el pasajero queda como «no recogido» y ya no bloquea finalizar el servicio.</p>}
              <label>
                Descripción
                <textarea value={descripcion} onChange={(evento) => setDescripcion(evento.target.value)} rows={2} placeholder="Qué pasó (opcional)" />
              </label>
              <button type="button" className="herramientas-pasajero__boton" disabled={tomandoFoto} onClick={alTomarFoto}>
                📷 {tomandoFoto ? 'Abriendo cámara…' : foto ? 'Foto tomada · tomar otra' : 'Tomar foto'}
              </button>
              <button
                type="button"
                className="herramientas-pasajero__boton herramientas-pasajero__boton--principal"
                disabled={ocupado || (!impideRecogida && !descripcion.trim() && !foto)}
                onClick={() => setConfirmandoIncidencia(true)}
              >
                Registrar incidencia
              </button>
            </div>
          )}
        </div>
      )}

      <div className="herramientas-pasajero__fila">
        <button
          type="button"
          className="herramientas-pasajero__fila-boton"
          disabled={ocupado}
          onClick={alAlternarIncidencias}
          aria-expanded={seccionAbierta === 'incidencias'}
        >
          <span>📋 Incidencias reportadas</span>
          <span className="herramientas-pasajero__flecha">{seccionAbierta === 'incidencias' ? '▲' : '▼'}</span>
        </button>
        {seccionAbierta === 'incidencias' && incidencias && (
          <ul className="herramientas-pasajero__lista">
            {incidencias.length === 0 && <li>Todavía no se ha reportado ninguna incidencia de este pasajero.</li>}
            {incidencias.map((i) => (
              <li key={i.incidenciaId} className="herramientas-pasajero__incidencia-item">
                <span>
                  <strong>{etiquetaTipoIncidencia(i.tipo)}</strong> · <small>{new Date(i.fechaHora).toLocaleString()}</small>
                  {i.descripcion && <><br />{i.descripcion}</>}
                  {i.latitud !== null && i.longitud !== null && (
                    <>
                      <br />
                      <a href={enlaceNavegacion({ latitud: i.latitud, longitud: i.longitud })} target="_blank" rel="noreferrer">
                        📍 Ver ubicación
                      </a>
                    </>
                  )}
                </span>
                {i.evidencias.map((e) => (
                  <div key={e.evidenciaId} className="herramientas-pasajero__foto">
                    {fotos[e.evidenciaId] ? (
                      <img src={fotos[e.evidenciaId]} alt="Evidencia de la incidencia" />
                    ) : (
                      <button type="button" onClick={() => alVerFoto(i.incidenciaId, e.evidenciaId)} disabled={cargandoFoto === e.evidenciaId}>
                        {cargandoFoto === e.evidenciaId ? 'Cargando…' : '📷 Ver foto'}
                      </button>
                    )}
                  </div>
                ))}
              </li>
            ))}
          </ul>
        )}
      </div>

      <ModalConfirmacion
        abierto={confirmandoIncidencia}
        titulo="Registrar incidencia"
        mensaje={`¿Confirmas que quieres registrar esta incidencia (${etiquetaTipoIncidencia(tipo)})? Esta acción no se puede deshacer desde la app.`}
        textoConfirmar="Sí, registrar"
        alConfirmar={() => {
          setConfirmandoIncidencia(false)
          alReportar()
        }}
        alCancelar={() => setConfirmandoIncidencia(false)}
      />
    </div>
  )
}
