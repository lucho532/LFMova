/**
 * Registra el service worker que permite instalar la versión web de LFMova
 * como aplicación de escritorio (PWA) desde Chrome o Edge. Solo en la web
 * publicada: ni en desarrollo (para no interferir con la recarga de Vite)
 * ni dentro de la APK de Android, que ya es una app instalada.
 */
export function registrarInstalacionEscritorio(): void {
  const esAppNativa = (window as { Capacitor?: { isNativePlatform?: () => boolean } }).Capacitor?.isNativePlatform?.() === true
  if (!import.meta.env.PROD || esAppNativa || !('serviceWorker' in navigator)) return

  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/sw.js').catch(() => {
      // Sin service worker la web funciona igual; solo no se ofrece instalarla.
    })
  })
}
