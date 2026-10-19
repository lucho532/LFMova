import { useEffect, useState } from 'react'
import '../estilos/componentes/AvisoActualizacion.css'

/** Cada cuánto se consulta si hay una versión nueva mientras la app está abierta. */
const INTERVALO_MS = 5 * 60 * 1000

/** Nombre (con huella) del archivo principal que cargó esta ventana; cambia en cada despliegue. */
function archivoEnUso(): string | null {
  return document.querySelector<HTMLScriptElement>('script[type="module"][src*="/assets/"]')?.getAttribute('src') ?? null
}

/** Consulta la página publicada, sin caché, y devuelve el archivo principal que anuncia ahora. */
async function archivoPublicado(): Promise<string | null> {
  const respuesta = await fetch('/index.html', { cache: 'no-store' })
  if (!respuesta.ok) return null
  return (await respuesta.text()).match(/src="(\/assets\/[^"]+\.js)"/)?.[1] ?? null
}

/**
 * Aviso de que se publicó una versión nueva de la web mientras la app (en el
 * navegador o instalada en el computador) seguía abierta con la anterior.
 * Lo comprueba al volver a la ventana y cada pocos minutos; no recarga sola
 * para no interrumpir lo que la persona esté haciendo. No aplica a la APK de
 * Android (que trae sus archivos dentro) ni al entorno de desarrollo.
 */
export function AvisoActualizacion() {
  const [hayVersionNueva, setHayVersionNueva] = useState(false)

  useEffect(() => {
    const enUso = archivoEnUso()
    const esAppNativa = (window as { Capacitor?: { isNativePlatform?: () => boolean } }).Capacitor?.isNativePlatform?.() === true
    if (!import.meta.env.PROD || esAppNativa || !enUso) return

    const comprobar = () => {
      if (document.visibilityState !== 'visible') return
      archivoPublicado()
        .then((publicado) => {
          if (publicado && publicado !== enUso) setHayVersionNueva(true)
        })
        .catch(() => {
          // Sin conexión no se puede comprobar; se reintenta en la siguiente ocasión.
        })
    }

    comprobar()
    document.addEventListener('visibilitychange', comprobar)
    const intervalo = window.setInterval(comprobar, INTERVALO_MS)
    return () => {
      document.removeEventListener('visibilitychange', comprobar)
      window.clearInterval(intervalo)
    }
  }, [])

  if (!hayVersionNueva) return null

  return (
    <div className="aviso-actualizacion" role="status">
      <span>Hay una versión nueva de LFMova.</span>
      <button type="button" onClick={() => window.location.reload()}>
        Actualizar
      </button>
    </div>
  )
}
