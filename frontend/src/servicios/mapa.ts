/** Enlace que abre un punto en Google Maps (pestaña o app de mapas del dispositivo). */
export function enlaceMapa(latitud: number, longitud: number): string {
  return `https://www.google.com/maps/search/?api=1&query=${latitud},${longitud}`
}

/** Dirección del mapa incrustado de Google (sin clave de API) centrado en un punto con su marcador. */
export function urlMapaIncrustado(latitud: number, longitud: number): string {
  return `https://maps.google.com/maps?q=${latitud},${longitud}&z=16&output=embed`
}
