import { enlaceMapa, urlMapaIncrustado } from '../servicios/mapa'
import '../estilos/componentes/MapaUbicacion.css'

interface PropiedadesMapaUbicacion {
  latitud: number
  longitud: number
  /** Texto accesible del mapa (qué ubicación muestra). */
  titulo: string
}

/**
 * Vista de mapa de un punto registrado (por ejemplo, donde se reportó una incidencia), con un enlace
 * para abrirlo completo. Solo muestra el punto: no lo edita ni lo guarda. Requiere conexión a internet.
 */
export function MapaUbicacion({ latitud, longitud, titulo }: PropiedadesMapaUbicacion) {
  return (
    <figure className="mapa-ubicacion">
      <iframe className="mapa-ubicacion__marco" title={titulo} src={urlMapaIncrustado(latitud, longitud)} loading="lazy" referrerPolicy="no-referrer" />
      <figcaption>
        <a href={enlaceMapa(latitud, longitud)} target="_blank" rel="noreferrer">
          📍 Abrir en Google Maps
        </a>
      </figcaption>
    </figure>
  )
}
