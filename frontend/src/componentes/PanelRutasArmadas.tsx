import { useAutenticacion } from '../contexto/useAutenticacion'
import { useImportacionRutasArmadas } from '../hooks/useImportacionRutasArmadas'
import { hoyColombia } from '../modelos/fechaColombia'
import type { ModoSeparacionRutas } from '../modelos/importacion'
import type { Sede } from '../modelos/sede'
import { BotonPrimario } from './BotonPrimario'
import { CampoFormulario } from './CampoFormulario'
import { IndicadorCarga } from './IndicadorCarga'
import { MensajeAlerta } from './MensajeAlerta'
import { SelectorFormulario } from './SelectorFormulario'
import { VistaPreviaRutasArmadas } from './VistaPreviaRutasArmadas'
import { ZonaArrastreArchivo } from './ZonaArrastreArchivo'
import '../estilos/componentes/PanelRutasArmadas.css'

interface PropiedadesPanelRutasArmadas {
  empresaId: number
  modo: ModoSeparacionRutas
  sedes: Sede[]
  /** Fecha de las rutas que no la traen en el archivo; se elige en la parte general de «Adjuntar rutas». */
  fecha: string
  alCambiarFecha: (fecha: string) => void
  /** Avisa al crear las rutas, para que la pantalla recargue las tarjetas. */
  alTerminar: () => void
}

/**
 * Carga de un Excel cuyas rutas ya vienen armadas: la app las crea tal como están, sin repartir, reordenar
 * ni mover pasajeros, y no las publica. Lee el archivo al elegirlo, muestra las rutas que detectó y solo
 * las crea cuando el coordinador lo confirma.
 */
export function PanelRutasArmadas({ empresaId, modo, sedes, fecha, alCambiarFecha, alTerminar }: PropiedadesPanelRutasArmadas) {
  const { token } = useAutenticacion()
  const estado = useImportacionRutasArmadas(empresaId, token, modo, fecha, alCambiarFecha, alTerminar)
  const { vista, resultado, mensajeError, ocupado } = estado

  return (
    <div className="panel-rutas-armadas">
      <p className="panel-rutas-armadas__ayuda">
        {modo === 'COLUMNA_NUMERO_RUTA'
          ? 'Cada fila trae el número de su ruta en una columna (por ejemplo «No RUTA»): las filas con el mismo número forman una ruta, en el orden del archivo.'
          : 'Una línea de texto abre cada ruta y las filas que siguen son sus pasajeros, en ese orden. La línea puede incluir la hora, la placa y ENTRADA o SALIDA.'}
      </p>
      {estado.necesitaTexto && (
        <CampoFormulario id="texto-inicio-ruta" etiqueta="La línea de cada ruta empieza con el texto" valor={estado.textoInicio} alCambiar={estado.cambiarTexto} requerido />
      )}
      <ZonaArrastreArchivo archivo={estado.archivo} alElegir={estado.elegirArchivo} deshabilitada={ocupado} />
      {ocupado && !vista && <p className="contenedor-pagina__estado">Leyendo el archivo…</p>}
      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

      {vista && (
        <>
          {vista.errores.length > 0 && (
            <MensajeAlerta tipo="error">
              {vista.errores.slice(0, 5).map((error) => (
                <span key={error} className="panel-rutas-armadas__linea">
                  {error}
                </span>
              ))}
              {vista.errores.length > 5 && <span className="panel-rutas-armadas__linea">… y {vista.errores.length - 5} más.</span>}
            </MensajeAlerta>
          )}
          {vista.advertencias.length > 0 && (
            <details className="panel-rutas-armadas__avisos">
              <summary>⚠ {vista.advertencias.length} aviso{vista.advertencias.length === 1 ? '' : 's'} para revisar (clic para ver)</summary>
              {vista.advertencias.map((aviso) => (
                <p key={aviso}>{aviso}</p>
              ))}
            </details>
          )}
          <p className="panel-rutas-armadas__resumen">
            {vista.rutas.length} ruta{vista.rutas.length === 1 ? '' : 's'} · {vista.totalPasajeros} pasajeros. Se crean tal como vienen y quedan sin publicar.
          </p>
          <VistaPreviaRutasArmadas rutas={vista.rutas} />
          <div className="panel-rutas-armadas__campos">
            {vista.faltaSede && (
              <SelectorFormulario
                id="sede-rutas-armadas"
                etiqueta="Sede de las rutas que no la traen"
                valor={estado.sedeGeneral}
                opciones={sedes.filter((sede) => sede.activa).map((sede) => ({ valor: String(sede.sedeId), texto: sede.nombre }))}
                alCambiar={estado.setSedeGeneral}
                requerido
              />
            )}
          </div>
          {fecha && fecha < hoyColombia() && <MensajeAlerta tipo="error">Ojo: la fecha {fecha} ya pasó. Si las rutas son para otro día, cámbiala arriba antes de crearlas.</MensajeAlerta>}
          <BotonPrimario type="button" disabled={!estado.listoParaImportar} onClick={estado.importar}>
            {ocupado ? (
              <>
                <IndicadorCarga /> Creando rutas…
              </>
            ) : (
              'Crear las rutas tal como vienen'
            )}
          </BotonPrimario>
        </>
      )}

      {resultado && (
        <>
          <MensajeAlerta tipo="exito">
            Listo: {resultado.serviciosCreados} ruta{resultado.serviciosCreados === 1 ? '' : 's'} con {resultado.pasajerosAsignados} pasajeros. Revísalas abajo y publícalas cuando estén listas.
          </MensajeAlerta>
          {resultado.advertencias.length > 0 && (
            <details className="panel-rutas-armadas__avisos">
              <summary>⚠ {resultado.advertencias.length} aviso{resultado.advertencias.length === 1 ? '' : 's'} de la creación (clic para ver)</summary>
              {resultado.advertencias.map((aviso) => (
                <p key={aviso}>{aviso}</p>
              ))}
            </details>
          )}
        </>
      )}
    </div>
  )
}
