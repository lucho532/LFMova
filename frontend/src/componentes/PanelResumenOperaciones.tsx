import type { PanoramaOperacion } from '../modelos/estadisticas'
import '../estilos/componentes/PanelResumenOperaciones.css'

interface PropiedadesPanelResumenOperaciones {
  panorama: PanoramaOperacion
}

/** Formatea una fecha ISO (AAAA-MM-DD) como día/mes/año sin depender de la zona horaria del navegador. */
function fechaCorta(fecha: string): string {
  const [anio, mes, dia] = fecha.split('-')
  return `${dia}/${mes}/${anio}`
}

const formatoEntero = new Intl.NumberFormat('es-CO')
const formatoPorcentaje = new Intl.NumberFormat('es-CO', { maximumFractionDigits: 1 })

/** Variación frente al periodo anterior: flecha, signo y color según suba o baje; vacío si no hay con qué comparar. */
function TextoVariacion({ variacion }: { variacion: number | null }) {
  if (variacion === null) return <span className="panel-resumen__nota">Sin periodo anterior para comparar</span>
  const clase = variacion > 0 ? 'sube' : variacion < 0 ? 'baja' : 'igual'
  const flecha = variacion > 0 ? '↑' : variacion < 0 ? '↓' : '='
  return (
    <span className={`panel-resumen__variacion panel-resumen__variacion--${clase}`}>
      {flecha} {formatoPorcentaje.format(Math.abs(variacion))}% vs. periodo anterior
    </span>
  )
}

/**
 * Cuatro indicadores del periodo elegido: empleados transportados, servicios realizados, conductores en
 * operación y promedio de asistencia. Solo presenta los datos recibidos; no los calcula.
 */
export function PanelResumenOperaciones({ panorama }: PropiedadesPanelResumenOperaciones) {
  return (
    <section className="panel-resumen">
      <h3>Resumen de operaciones</h3>
      <p className="panel-resumen__periodo">
        {fechaCorta(panorama.desde)} – {fechaCorta(panorama.hasta)}
      </p>
      <div className="panel-resumen__cuadricula">
        <div className="panel-resumen__indicador">
          <span className="panel-resumen__titulo">Empleados transportados</span>
          <strong className="panel-resumen__valor panel-resumen__valor--destacado">{formatoEntero.format(panorama.empleadosTransportados)}</strong>
          <TextoVariacion variacion={panorama.variacionEmpleadosTransportados} />
        </div>
        <div className="panel-resumen__indicador">
          <span className="panel-resumen__titulo">Servicios realizados</span>
          <strong className="panel-resumen__valor">{formatoEntero.format(panorama.serviciosRealizados)}</strong>
          <span className="panel-resumen__nota">
            {panorama.serviciosEntrada} entradas · {panorama.serviciosSalida} salidas
          </span>
        </div>
        <div className="panel-resumen__indicador">
          <span className="panel-resumen__titulo">Conductores en operación</span>
          <strong className="panel-resumen__valor">{formatoEntero.format(panorama.conductoresEnOperacion)}</strong>
          <span className="panel-resumen__nota">Con rutas en el periodo</span>
        </div>
        <div className="panel-resumen__indicador">
          <span className="panel-resumen__titulo">Promedio de asistencia</span>
          <strong className="panel-resumen__valor">{formatoPorcentaje.format(panorama.promedioAsistencia)}%</strong>
          <div className="panel-resumen__barra" role="progressbar" aria-valuemin={0} aria-valuemax={100} aria-valuenow={panorama.promedioAsistencia}>
            <div className="panel-resumen__relleno" style={{ width: `${panorama.promedioAsistencia}%` }} />
          </div>
        </div>
      </div>
    </section>
  )
}
