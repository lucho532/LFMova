import { useState } from 'react'
import '../estilos/componentes/BotonRefrescar.css'

interface PropiedadesBotonRefrescar {
  /** Vuelve a cargar la pantalla actual con los datos más recientes del servidor. */
  alRefrescar: () => void
}

/**
 * Botón de la barra superior, igual para todos los roles, que recarga la
 * pantalla en la que está la persona para traer los datos más recientes
 * (sin cerrar la sesión ni cambiar de pantalla). El icono gira un instante
 * para confirmar que se pulsó. Qué se recarga lo decide quien lo usa.
 */
export function BotonRefrescar({ alRefrescar }: PropiedadesBotonRefrescar) {
  const [girando, setGirando] = useState(false)

  function alPulsar() {
    setGirando(true)
    alRefrescar()
    window.setTimeout(() => setGirando(false), 800)
  }

  return (
    <button
      type="button"
      className={`boton-refrescar${girando ? ' boton-refrescar--girando' : ''}`}
      onClick={alPulsar}
      aria-label="Actualizar la pantalla"
      title="Actualizar"
    >
      <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
        <path d="M21 12a9 9 0 1 1-3-6.7" />
        <polyline points="21 3 21 9 15 9" />
      </svg>
    </button>
  )
}
