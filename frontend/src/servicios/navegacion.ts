/** Aplicaciones de mapas con las que el conductor puede navegar hasta un punto. */
export type AppNavegacion = 'GOOGLE_MAPS' | 'WAZE'

/** Aplicaciones disponibles, en el orden en que se ofrecen, con su nombre visible. */
export const APLICACIONES_NAVEGACION: { valor: AppNavegacion; nombre: string }[] = [
  { valor: 'GOOGLE_MAPS', nombre: 'Google Maps' },
  { valor: 'WAZE', nombre: 'Waze' },
]

const CLAVE_ALMACENAMIENTO = 'lfmova.appNavegacion'

/** Nombre visible de una aplicación de navegación. */
export function nombreAppNavegacion(app: AppNavegacion): string {
  return APLICACIONES_NAVEGACION.find((a) => a.valor === app)?.nombre ?? app
}

/** Enlace para navegar en carro a un punto por sus coordenadas con la aplicación indicada (Google Maps si no se indica). */
export function enlaceNavegacion(destino: { latitud: number; longitud: number }, app: AppNavegacion = 'GOOGLE_MAPS'): string {
  if (app === 'WAZE') {
    return `https://waze.com/ul?ll=${destino.latitud},${destino.longitud}&navigate=yes`
  }
  return `https://www.google.com/maps/dir/?api=1&destination=${destino.latitud},${destino.longitud}&travelmode=driving`
}

/**
 * Aplicación que el conductor dejó como predeterminada en este dispositivo,
 * o `null` si prefiere que se le pregunte cada vez. Es una preferencia local
 * (como el tema), no un dato de la cuenta.
 */
export function obtenerAppPredeterminada(): AppNavegacion | null {
  try {
    const guardada = window.localStorage.getItem(CLAVE_ALMACENAMIENTO)
    return APLICACIONES_NAVEGACION.some((a) => a.valor === guardada) ? (guardada as AppNavegacion) : null
  } catch {
    // Almacenamiento no disponible (navegación privada, etc.): se pregunta cada vez.
    return null
  }
}

/** Guarda la aplicación predeterminada de este dispositivo; con `null` vuelve a preguntar cada vez. */
export function guardarAppPredeterminada(app: AppNavegacion | null): void {
  try {
    if (app) window.localStorage.setItem(CLAVE_ALMACENAMIENTO, app)
    else window.localStorage.removeItem(CLAVE_ALMACENAMIENTO)
  } catch {
    // Sin almacenamiento la preferencia no se recuerda; no impide navegar.
  }
}
