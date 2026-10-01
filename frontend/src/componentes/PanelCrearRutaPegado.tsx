import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { TipoServicio } from '../modelos/enumeraciones'
import { hoyColombia } from '../modelos/fechaColombia'
import type { CampoPasajeroPegado, FilaPasajeroPegado, PlantillaColumnasPegado } from '../modelos/importacion'
import type { Sede } from '../modelos/sede'
import { ErrorApi } from '../servicios/clienteHttp'
import type { OpcionUnidadOperativa } from '../servicios/servicioConductores'
import { crearRutaPegada, guardarPlantillaColumnasPegado, obtenerPlantillaColumnasPegado } from '../servicios/servicioImportaciones'
import { obtenerSedes } from '../servicios/servicioSedes'
import { BotonPrimario } from './BotonPrimario'
import { BotonSecundario } from './BotonSecundario'
import { CampoFormulario } from './CampoFormulario'
import { MensajeAlerta } from './MensajeAlerta'
import { SelectorFormulario } from './SelectorFormulario'
import { TarjetaFormulario } from './TarjetaFormulario'
import './PanelCrearRutaPegado.css'

interface PropiedadesPanelCrearRutaPegado {
  empresaId: number
  /** Conductores (con su unidad) disponibles para asignar de una vez, ya cargados por la página. */
  opcionesUnidad: OpcionUnidadOperativa[]
  /** Avisa al crear una ruta, para que la pantalla recargue las tarjetas de rutas. */
  alTerminar: () => void
}


/** Los cinco campos obligatorios de un pasajero; deben coincidir con los que exige GuardarPlantillaColumnasPegadoDto en el backend. */
const CAMPOS_OBLIGATORIOS: CampoPasajeroPegado[] = ['CEDULA', 'NOMBRE', 'CELULAR', 'DIRECCION', 'BARRIO']

/** Opciones que puede tomar cada columna al definir el esqueleto: los campos obligatorios, APELLIDOS (opcional) e IGNORAR (para una columna que sobra). */
const OPCIONES_COLUMNA: { clave: CampoPasajeroPegado; etiqueta: string }[] = [
  { clave: 'CEDULA', etiqueta: 'Cédula' },
  { clave: 'NOMBRE', etiqueta: 'Nombre (o nombre completo)' },
  { clave: 'APELLIDOS', etiqueta: 'Apellidos (si van en otra columna)' },
  { clave: 'CELULAR', etiqueta: 'Celular' },
  { clave: 'DIRECCION', etiqueta: 'Dirección' },
  { clave: 'BARRIO', etiqueta: 'Barrio' },
  { clave: 'IGNORAR', etiqueta: '(No usar esta columna)' },
]

function etiquetaDeColumna(clave: string): string {
  return OPCIONES_COLUMNA.find((c) => c.clave === clave)?.etiqueta ?? clave
}

/**
 * Un esqueleto es válido cuando no quedó ninguna columna sin elegir, cada
 * campo obligatorio aparece exactamente una vez y APELLIDOS aparece como
 * máximo una vez (el resto de columnas, si sobran, deben ser IGNORAR).
 */
function validarEsqueleto(columnas: (CampoPasajeroPegado | '')[]): string | null {
  if (columnas.some((c) => !c)) return 'Elige un dato para cada columna (o "No usar esta columna" si sobra).'
  const faltantes = CAMPOS_OBLIGATORIOS.filter((campo) => columnas.filter((c) => c === campo).length !== 1)
  if (faltantes.length > 0) {
    return `Estos datos son obligatorios y deben aparecer una sola vez cada uno: ${faltantes.map(etiquetaDeColumna).join(', ')}.`
  }
  if (columnas.filter((c) => c === 'APELLIDOS').length > 1) return 'Apellidos no puede repetirse.'
  return null
}

/** Separa el texto pegado en filas (una por línea) y columnas (separadas por tabulador, como copia Excel). */
function parsearPegado(texto: string, columnasEnOrden: string[]): FilaPasajeroPegado[] {
  return texto
    .split(/\r?\n/)
    .map((linea) => linea.trim())
    .filter((linea) => linea.length > 0)
    .map((linea) => {
      const valores = linea.split('\t')
      const obtener = (clave: CampoPasajeroPegado) => {
        const indice = columnasEnOrden.indexOf(clave)
        return indice >= 0 && indice < valores.length ? valores[indice].trim() : ''
      }
      // NOMBRE puede ser el nombre completo solo, o el nombre de pila cuando APELLIDOS ocupa otra columna.
      const nombreCompleto = [obtener('NOMBRE'), obtener('APELLIDOS')].filter(Boolean).join(' ')
      return {
        cedula: obtener('CEDULA'),
        nombreCompleto,
        celular: obtener('CELULAR'),
        direccion: obtener('DIRECCION'),
        barrio: obtener('BARRIO'),
      }
    })
}

/**
 * Crea una ruta pegando filas de pasajeros copiadas de un Excel externo, en
 * vez de escribirlas una por una: el coordinador elige fecha, hora, tipo,
 * sede y, si quiere, el conductor que hará la ruta, y pega el bloque de
 * filas. Cada ruta lleva su propia fecha (no depende de una fecha elegida en
 * otra parte de la pantalla), para poder armar de una vez rutas de varios
 * días seguidos (por ejemplo, la salida de la noche y las entradas de la
 * madrugada siguiente) sin ir cambiando de pantalla. Si no elige conductor,
 * la ruta queda sin asignar para asignárselo después desde su tarjeta, igual
 * que las que arma la importación de Excel. La primera vez (o cuando el
 * formato de su Excel cambia) el coordinador define un "esqueleto": en qué
 * columna viene cada dato; ese orden se guarda por empresa y se reutiliza en
 * los pegados siguientes.
 */
export function PanelCrearRutaPegado({ empresaId, opcionesUnidad, alTerminar }: PropiedadesPanelCrearRutaPegado) {
  const { token } = useAutenticacion()
  const [abierto, setAbierto] = useState(false)
  const [fecha, setFecha] = useState(hoyColombia())
  const [hora, setHora] = useState('06:00')
  const [tipo, setTipo] = useState<number>(TipoServicio.ENTRADA)
  const [sedes, setSedes] = useState<Sede[]>([])
  const [sedeId, setSedeId] = useState('')
  const [unidadOperativaId, setUnidadOperativaId] = useState('')

  const [plantilla, setPlantilla] = useState<PlantillaColumnasPegado | null>(null)
  const [cargandoPlantilla, setCargandoPlantilla] = useState(true)
  const [editandoEsqueleto, setEditandoEsqueleto] = useState(false)
  const [columnasEsqueleto, setColumnasEsqueleto] = useState<(CampoPasajeroPegado | '')[]>(['', '', '', '', ''])
  const [guardandoEsqueleto, setGuardandoEsqueleto] = useState(false)
  const [errorEsqueleto, setErrorEsqueleto] = useState<string | null>(null)

  const [textoPegado, setTextoPegado] = useState('')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [mensajeExito, setMensajeExito] = useState<{ texto: string; advertencias: string[] } | null>(null)
  const [enviando, setEnviando] = useState(false)

  useEffect(() => {
    if (!token) return
    obtenerSedes(empresaId, token)
      .then((lista) => setSedes(lista.filter((s) => s.activa)))
      .catch(() => setSedes([]))

    setCargandoPlantilla(true)
    obtenerPlantillaColumnasPegado(empresaId, token)
      .then((p) => setPlantilla(p))
      .catch((error) => {
        if (error instanceof ErrorApi && error.status === 404) {
          setPlantilla(null)
          setEditandoEsqueleto(true) // primera vez: se pide el esqueleto de una vez, no hace falta un clic aparte
        }
      })
      .finally(() => setCargandoPlantilla(false))
  }, [empresaId, token])

  async function alGuardarEsqueleto(evento: FormEvent) {
    evento.preventDefault()
    if (!token) return
    const errorValidacion = validarEsqueleto(columnasEsqueleto)
    if (errorValidacion) {
      setErrorEsqueleto(errorValidacion)
      return
    }
    setGuardandoEsqueleto(true)
    setErrorEsqueleto(null)
    try {
      const guardada = await guardarPlantillaColumnasPegado(empresaId, columnasEsqueleto as string[], token)
      setPlantilla(guardada)
      setEditandoEsqueleto(false)
    } catch (error) {
      setErrorEsqueleto(error instanceof ErrorApi ? error.message : 'No se pudo guardar el esqueleto de columnas.')
    } finally {
      setGuardandoEsqueleto(false)
    }
  }

  function alEmpezarEsqueletoNuevo() {
    setColumnasEsqueleto(plantilla?.columnasEnOrden ?? ['', '', '', '', ''])
    setErrorEsqueleto(null)
    setEditandoEsqueleto(true)
  }

  const filasPegadas = useMemo(
    () => (plantilla ? parsearPegado(textoPegado, plantilla.columnasEnOrden) : []),
    [textoPegado, plantilla],
  )

  async function alCrearRuta(evento: FormEvent) {
    evento.preventDefault()
    if (!token || !plantilla || !sedeId || !fecha) return
    if (filasPegadas.length === 0) {
      setMensajeError('Pega al menos una fila con los datos de un pasajero.')
      return
    }

    setEnviando(true)
    setMensajeError(null)
    setMensajeExito(null)
    try {
      const resultado = await crearRutaPegada(
        empresaId,
        { fecha, hora: `${hora}:00`, tipo, sedeId: Number(sedeId), unidadOperativaId: unidadOperativaId ? Number(unidadOperativaId) : null, pasajeros: filasPegadas },
        token,
      )
      const textoConductor = unidadOperativaId
        ? `y conductor asignado: ${opcionesUnidad.find((o) => o.unidadOperativaId === Number(unidadOperativaId))?.texto ?? 'listo'}.`
        : 'sin conductor todavía: asígnaselo abajo en su tarjeta.'
      setMensajeExito({
        texto: `Ruta creada con ${resultado.pasajerosAsignados} pasajero${resultado.pasajerosAsignados === 1 ? '' : 's'}, ${textoConductor}`,
        advertencias: resultado.advertencias,
      })
      setTextoPegado('')
      setUnidadOperativaId('')
      alTerminar()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo crear la ruta.')
    } finally {
      setEnviando(false)
    }
  }

  return (
    <section className="panel-crear-ruta-pegado">
      <h2>Crear ruta pegando filas de Excel</h2>
      <div className="panel-crear-ruta-pegado__caja">
        <BotonSecundario type="button" onClick={() => setAbierto((valor) => !valor)}>
          {abierto ? 'Cerrar' : '📋 Crear ruta (pegar filas)'}
        </BotonSecundario>

        {abierto && cargandoPlantilla && <p className="contenedor-pagina__estado">Cargando…</p>}

        {abierto && !cargandoPlantilla && editandoEsqueleto && (
          <>
            <p className="contenedor-pagina__estado">
              {plantilla
                ? 'Cambia el orden si el Excel de donde copias cambió de formato: se guarda para los próximos pegados.'
                : 'Primera vez: dinos en qué columna viene cada dato en el Excel del que vas a copiar. Se guarda para no volver a preguntarlo.'}
            </p>
            <TarjetaFormulario alEnviar={alGuardarEsqueleto}>
              {columnasEsqueleto.map((valor, indice) => (
                <div key={indice} className="panel-crear-ruta-pegado__columna-esqueleto">
                  <SelectorFormulario
                    id={`esqueletoColumna${indice}`}
                    etiqueta={`Columna ${indice + 1}`}
                    valor={valor}
                    alCambiar={(nuevoValor) =>
                      setColumnasEsqueleto((anterior) => anterior.map((c, i) => (i === indice ? (nuevoValor as CampoPasajeroPegado) : c)))
                    }
                    opciones={OPCIONES_COLUMNA.map((c) => ({ valor: c.clave, texto: c.etiqueta }))}
                    textoVacio="Elige el dato"
                    requerido
                  />
                  {columnasEsqueleto.length > 1 && (
                    <button
                      type="button"
                      className="panel-crear-ruta-pegado__quitar-columna"
                      title="Quitar esta columna"
                      onClick={() => setColumnasEsqueleto((anterior) => anterior.filter((_, i) => i !== indice))}
                    >
                      ✕
                    </button>
                  )}
                </div>
              ))}
              <div className="panel-crear-ruta-pegado__agregar-columna">
                <BotonSecundario type="button" onClick={() => setColumnasEsqueleto((anterior) => [...anterior, ''])}>
                  + Agregar columna
                </BotonSecundario>
              </div>
              {errorEsqueleto && <MensajeAlerta tipo="error">{errorEsqueleto}</MensajeAlerta>}
              <div className="panel-crear-ruta-pegado__acciones-esqueleto">
                <BotonPrimario disabled={guardandoEsqueleto}>{guardandoEsqueleto ? 'Guardando…' : 'Guardar esqueleto'}</BotonPrimario>
                {plantilla && (
                  <BotonSecundario type="button" onClick={() => setEditandoEsqueleto(false)}>
                    Cancelar
                  </BotonSecundario>
                )}
              </div>
            </TarjetaFormulario>
          </>
        )}

        {abierto && !cargandoPlantilla && plantilla && !editandoEsqueleto && (
          <>
            <p className="panel-crear-ruta-pegado__esqueleto-actual">
              Orden de columnas: {plantilla.columnasEnOrden.map(etiquetaDeColumna).join(' · ')}{' '}
              <button type="button" className="panel-crear-ruta-pegado__cambiar-esqueleto" onClick={alEmpezarEsqueletoNuevo}>
                Cambiar esqueleto
              </button>
            </p>

            <TarjetaFormulario alEnviar={alCrearRuta} columnas={2}>
              <CampoFormulario id="rutaPegadaFecha" etiqueta="Fecha" tipo="date" valor={fecha} alCambiar={setFecha} requerido />
              <CampoFormulario id="rutaPegadaHora" etiqueta="Hora" tipo="time" valor={hora} alCambiar={setHora} requerido />

              <div className="panel-crear-ruta-pegado__tipo">
                <span className="panel-crear-ruta-pegado__tipo-etiqueta">Tipo</span>
                <div className="panel-crear-ruta-pegado__tipo-opciones">
                  <label>
                    <input type="radio" name="tipoRutaPegada" checked={tipo === TipoServicio.ENTRADA} onChange={() => setTipo(TipoServicio.ENTRADA)} /> Entrada
                  </label>
                  <label>
                    <input type="radio" name="tipoRutaPegada" checked={tipo === TipoServicio.SALIDA} onChange={() => setTipo(TipoServicio.SALIDA)} /> Salida
                  </label>
                </div>
              </div>

              <SelectorFormulario
                id="rutaPegadaSede"
                etiqueta="Sede"
                valor={sedeId}
                opciones={sedes.map((s) => ({ valor: String(s.sedeId), texto: s.nombre }))}
                alCambiar={setSedeId}
                textoVacio={sedes.length === 0 ? 'No hay sedes activas' : 'Elige la sede'}
                requerido
              />

              <SelectorFormulario
                id="rutaPegadaConductor"
                etiqueta="Conductor (opcional)"
                valor={unidadOperativaId}
                opciones={opcionesUnidad.map((o) => ({ valor: String(o.unidadOperativaId), texto: o.texto }))}
                alCambiar={setUnidadOperativaId}
                textoVacio="Sin asignar (lo eliges después)"
              />

              <div className="panel-crear-ruta-pegado__pegado">
                <label htmlFor="rutaPegadaTexto">Pega aquí las filas copiadas del Excel</label>
                <textarea
                  id="rutaPegadaTexto"
                  wrap="off"
                  value={textoPegado}
                  onChange={(evento) => setTextoPegado(evento.target.value)}
                  placeholder="Copia las filas en tu Excel (Ctrl+C) y pégalas aquí (Ctrl+V). Una persona por línea."
                />
                {filasPegadas.length > 0 && (
                  <p className="panel-crear-ruta-pegado__conteo">
                    {filasPegadas.length} pasajero{filasPegadas.length === 1 ? '' : 's'} detectado{filasPegadas.length === 1 ? '' : 's'}
                  </p>
                )}
              </div>

              {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
              {mensajeExito && (
                <div className="panel-crear-ruta-pegado__resultado">
                  <MensajeAlerta tipo="exito">{mensajeExito.texto}</MensajeAlerta>
                  {mensajeExito.advertencias.length > 0 && (
                    <details>
                      <summary>
                        ⚠ {mensajeExito.advertencias.length} aviso{mensajeExito.advertencias.length === 1 ? '' : 's'} (clic para ver)
                      </summary>
                      {mensajeExito.advertencias.map((aviso) => (
                        <p key={aviso}>⚠ {aviso}</p>
                      ))}
                    </details>
                  )}
                </div>
              )}
              <BotonPrimario disabled={enviando || !sedeId || !fecha}>{enviando ? 'Creando…' : 'Crear ruta'}</BotonPrimario>
            </TarjetaFormulario>
          </>
        )}
      </div>
    </section>
  )
}
