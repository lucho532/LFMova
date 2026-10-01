import { useEffect, useRef, useState, type PointerEvent as EventoPunteroReact } from 'react'

/** Píxeles que debe moverse el ratón con el botón presionado para que cuente como arrastre y no como clic. */
const UMBRAL_ARRASTRE = 6
/** Distancia al borde superior o inferior de la ventana desde la que la página empieza a desplazarse sola. */
const MARGEN_BORDE = 110
/** Píxeles que avanza la página por cuadro cuando el puntero está pegado al borde. */
const VELOCIDAD_MAXIMA = 22

/**
 * Arrastre propio con eventos de puntero (no el "drag and drop" nativo del
 * navegador, que bloquea la rueda del ratón mientras dura). Mientras se
 * arrastra: una etiqueta sigue al cursor, la rueda sigue desplazando la
 * página, y esta se desplaza sola si el puntero se acerca al borde. Al soltar
 * avisa sobre qué destino cayó: el elemento más cercano con el atributo
 * `atributoDestino` (por ejemplo `data-zona-id`), cuyo valor es su id.
 * Solo se activa con ratón; en pantallas táctiles no hace nada.
 */
export function useArrastrePuntero<T>(atributoDestino: string, alSoltar: (dato: T, destinoId: number) => void) {
  const [arrastrado, setArrastrado] = useState<T | null>(null)
  const [destinoId, setDestinoId] = useState<number | null>(null)
  const etiquetaRef = useRef<HTMLDivElement>(null)
  const alSoltarRef = useRef(alSoltar)
  const limpiarRef = useRef<(() => void) | null>(null)

  useEffect(() => {
    alSoltarRef.current = alSoltar
  })
  useEffect(() => () => limpiarRef.current?.(), [])

  function destinoBajo(x: number, y: number): number | null {
    const valor = document.elementFromPoint(x, y)?.closest(`[${atributoDestino}]`)?.getAttribute(atributoDestino)
    return valor ? Number(valor) : null
  }

  /** Se conecta al `onPointerDown` de lo que se puede arrastrar. */
  function alPresionar(evento: EventoPunteroReact, dato: T) {
    if (evento.pointerType !== 'mouse' || evento.button !== 0) return
    evento.stopPropagation()
    evento.preventDefault() // evita que el navegador seleccione texto mientras se arrastra

    const inicio = { x: evento.clientX, y: evento.clientY }
    let posicion = inicio
    let activo = false
    let cuadro = 0

    function ciclo() {
      const etiqueta = etiquetaRef.current
      if (etiqueta) etiqueta.style.transform = `translate(${posicion.x + 14}px, ${posicion.y + 14}px)`

      const desdeAbajo = window.innerHeight - posicion.y
      if (posicion.y < MARGEN_BORDE) window.scrollBy(0, -Math.ceil(VELOCIDAD_MAXIMA * (1 - posicion.y / MARGEN_BORDE)))
      else if (desdeAbajo < MARGEN_BORDE) window.scrollBy(0, Math.ceil(VELOCIDAD_MAXIMA * (1 - desdeAbajo / MARGEN_BORDE)))

      setDestinoId(destinoBajo(posicion.x, posicion.y))
      cuadro = requestAnimationFrame(ciclo)
    }

    function alMover(movimiento: PointerEvent) {
      posicion = { x: movimiento.clientX, y: movimiento.clientY }
      if (activo || Math.hypot(posicion.x - inicio.x, posicion.y - inicio.y) < UMBRAL_ARRASTRE) return
      activo = true
      setArrastrado(dato)
      cuadro = requestAnimationFrame(ciclo)
    }

    function limpiar() {
      document.removeEventListener('pointermove', alMover)
      document.removeEventListener('pointerup', alLevantar)
      cancelAnimationFrame(cuadro)
      limpiarRef.current = null
      setArrastrado(null)
      setDestinoId(null)
    }

    function alLevantar(final: PointerEvent) {
      const destino = activo ? destinoBajo(final.clientX, final.clientY) : null
      if (activo) {
        // El navegador dispara un "click" al soltar: se descarta para que no cuente como un toque sobre el destino.
        const descartar = (clic: MouseEvent) => {
          clic.stopPropagation()
          clic.preventDefault()
        }
        window.addEventListener('click', descartar, { capture: true, once: true })
        setTimeout(() => window.removeEventListener('click', descartar, { capture: true }), 0)
      }
      limpiar()
      if (destino !== null) alSoltarRef.current(dato, destino)
    }

    limpiarRef.current = limpiar
    document.addEventListener('pointermove', alMover)
    document.addEventListener('pointerup', alLevantar)
  }

  return { arrastrado, destinoId, etiquetaRef, alPresionar }
}
