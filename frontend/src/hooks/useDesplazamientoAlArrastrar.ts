import { useEffect } from 'react'

/** Distancia al borde superior o inferior de la ventana, en píxeles, desde la que empieza el desplazamiento. */
const MARGEN_BORDE = 130
/** Píxeles que avanza la página por cuadro cuando el puntero está pegado al borde. */
const VELOCIDAD_MAXIMA = 22

/**
 * Mientras `activo` sea verdadero (hay algo arrastrándose), desplaza la página
 * hacia arriba o hacia abajo cuando el puntero se acerca al borde de la
 * ventana, más rápido cuanto más cerca. Sin esto no se puede soltar sobre un
 * elemento que quedó fuera de la vista.
 */
export function useDesplazamientoAlArrastrar(activo: boolean) {
  useEffect(() => {
    if (!activo) return

    let velocidad = 0
    let cuadro = 0

    function alArrastrar(evento: DragEvent) {
      const desdeAbajo = window.innerHeight - evento.clientY
      if (evento.clientY < MARGEN_BORDE) velocidad = -VELOCIDAD_MAXIMA * (1 - evento.clientY / MARGEN_BORDE)
      else if (desdeAbajo < MARGEN_BORDE) velocidad = VELOCIDAD_MAXIMA * (1 - desdeAbajo / MARGEN_BORDE)
      else velocidad = 0
    }

    function avanzar() {
      if (velocidad !== 0) window.scrollBy(0, velocidad)
      cuadro = requestAnimationFrame(avanzar)
    }

    window.addEventListener('dragover', alArrastrar)
    cuadro = requestAnimationFrame(avanzar)
    return () => {
      window.removeEventListener('dragover', alArrastrar)
      cancelAnimationFrame(cuadro)
    }
  }, [activo])
}
