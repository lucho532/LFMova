import type { Servicio } from '../modelos/operacion'
import { enlaceMapa } from '../servicios/mapa'
import { duracion, horaLocal } from '../servicios/tiempoRuta'
import '../estilos/componentes/ResumenEjecucionRuta.css'

interface PropiedadesUbicacionRuta {
  latitud: number | null
  longitud: number | null
  texto: string
}

/** Enlace al punto registrado, o aviso de que no hay ubicación (rutas anteriores a que fuera obligatoria). */
function UbicacionRuta({ latitud, longitud, texto }: PropiedadesUbicacionRuta) {
  if (latitud === null || longitud === null) return <span className="resumen-ejecucion-ruta__sin-ubicacion">sin ubicación registrada</span>
  return (
    <a href={enlaceMapa(latitud, longitud)} target="_blank" rel="noreferrer">
      📍 {texto}
    </a>
  )
}

/**
 * Resumen de la ejecución de una ruta: a qué hora la inició y la finalizó el conductor, con la
 * ubicación de cada momento, y cuánto duró. Solo muestra datos ya registrados; no se muestra si la
 * ruta no se ha iniciado.
 */
export function ResumenEjecucionRuta({ servicio }: { servicio: Servicio }) {
  if (!servicio.horaInicioReal && !servicio.horaFinReal) return null
  const duracionRuta = duracion(servicio.horaInicioReal, servicio.horaFinReal)

  return (
    <div className="resumen-ejecucion-ruta">
      {servicio.horaInicioReal && (
        <span>
          🚗 Inició {horaLocal(servicio.horaInicioReal)} · <UbicacionRuta latitud={servicio.latitudInicio} longitud={servicio.longitudInicio} texto="ubicación de inicio" />
        </span>
      )}
      {servicio.horaFinReal && (
        <span>
          🏁 Finalizó {horaLocal(servicio.horaFinReal)} · <UbicacionRuta latitud={servicio.latitudFinalizacion} longitud={servicio.longitudFinalizacion} texto="ubicación de llegada" />
        </span>
      )}
      {duracionRuta && <span>⏱ Duración: {duracionRuta}</span>}
    </div>
  )
}
