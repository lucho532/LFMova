import { useEffect, useState } from 'react'
import { usePeriodoPanorama } from '../hooks/usePeriodoPanorama'
import type { PanoramaOperacion } from '../modelos/estadisticas'
import { ErrorApi } from '../servicios/clienteHttp'
import { obtenerPanoramaOperacion } from '../servicios/servicioEstadisticas'
import { GraficaLineas } from './GraficaLineas'
import { MensajeAlerta } from './MensajeAlerta'
import { PanelResumenOperaciones } from './PanelResumenOperaciones'
import { SelectorPeriodo } from './SelectorPeriodo'
import '../estilos/componentes/PanoramaOperacionInicio.css'

interface PropiedadesPanoramaOperacionInicio {
  empresaId: number
  token: string
}

/** Formatea una fecha ISO (AAAA-MM-DD) como día/mes sin depender de la zona horaria del navegador. */
function diaMes(fecha: string): string {
  const [, mes, dia] = fecha.split('-')
  return `${dia}/${mes}`
}

/**
 * Bloque del inicio del coordinador que muestra, para un mismo periodo elegido, la gráfica de pasajeros
 * transportados (entradas y salidas por día) y el resumen de indicadores de la operación.
 */
export function PanoramaOperacionInicio({ empresaId, token }: PropiedadesPanoramaOperacionInicio) {
  const { opcion, setOpcion, desde, hasta, setDesdePropio, setHastaPropio, valido } = usePeriodoPanorama()
  const [panorama, setPanorama] = useState<PanoramaOperacion | null>(null)
  const [mensajeError, setMensajeError] = useState<string | null>(null)

  useEffect(() => {
    if (!valido) return
    let cancelado = false
    setMensajeError(null)
    obtenerPanoramaOperacion(empresaId, desde, hasta, token)
      .then((datos) => !cancelado && setPanorama(datos))
      .catch((error) => !cancelado && setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar el panorama.'))
    return () => {
      cancelado = true
    }
  }, [empresaId, token, desde, hasta, valido])

  return (
    <div className="panorama-inicio">
      <section className="panorama-inicio__grafica">
        <div className="panorama-inicio__cabecera">
          <h3>Pasajeros transportados por día</h3>
          <SelectorPeriodo opcion={opcion} desde={desde} hasta={hasta} alElegirOpcion={setOpcion} alCambiarDesde={setDesdePropio} alCambiarHasta={setHastaPropio} />
        </div>
        {!valido && <MensajeAlerta tipo="error">La fecha final debe ser igual o posterior a la inicial.</MensajeAlerta>}
        {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
        {panorama && <GraficaLineas puntos={panorama.serie.map((d) => ({ etiqueta: diaMes(d.fecha), entradas: d.entradas, salidas: d.salidas }))} textoVacio="No hay pasajeros transportados en este periodo." />}
      </section>
      {panorama && <PanelResumenOperaciones panorama={panorama} />}
    </div>
  )
}
