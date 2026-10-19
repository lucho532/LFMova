import type { PointerEvent as EventoPunteroReact } from 'react'

/** Milisegundos que debe quedarse el dedo quieto sobre algo para que cuente como "mantener presionado". */
const ESPERA_MS = 350
/** Píxeles que puede moverse el dedo durante la espera sin que se interprete como un deslizamiento. */
const TOLERANCIA_PX = 10

/**
 * Detecta, en pantallas táctiles, que el dedo se quedó presionado sobre un
 * elemento: si se desliza antes de tiempo es un desplazamiento normal de la
 * página y no pasa nada; si se queda quieto, llama a `alActivar` con la
 * posición del dedo. Desde ese momento, y mientras `estaActivo` devuelva
 * verdadero, impide que el navegador desplace la página con el dedo, para que
 * el movimiento sirva para arrastrar. No sabe qué se arrastra ni dónde se
 * suelta: eso lo decide quien la usa. Se conecta al `onPointerDown`.
 */
export function armarPulsacionLarga(evento: EventoPunteroReact, alActivar: (x: number, y: number) => void, estaActivo: () => boolean) {
  if (evento.pointerType === 'mouse') return

  const inicio = { x: evento.clientX, y: evento.clientY }
  let posicion = inicio

  const temporizador = window.setTimeout(() => {
    navigator.vibrate?.(30)
    alActivar(posicion.x, posicion.y)
  }, ESPERA_MS)

  function alMover(movimiento: PointerEvent) {
    posicion = { x: movimiento.clientX, y: movimiento.clientY }
    if (!estaActivo() && Math.hypot(posicion.x - inicio.x, posicion.y - inicio.y) > TOLERANCIA_PX) terminar()
  }

  function evitarDesplazamiento(toque: TouchEvent) {
    if (estaActivo() && toque.cancelable) toque.preventDefault()
  }

  /** El menú de "mantener presionado" del navegador no debe aparecer encima del arrastre. */
  function evitarMenu(menu: Event) {
    menu.preventDefault()
  }

  function terminar() {
    clearTimeout(temporizador)
    document.removeEventListener('pointermove', alMover)
    document.removeEventListener('pointerup', terminar)
    document.removeEventListener('pointercancel', terminar)
    document.removeEventListener('touchmove', evitarDesplazamiento)
    document.removeEventListener('contextmenu', evitarMenu)
  }

  document.addEventListener('pointermove', alMover)
  document.addEventListener('pointerup', terminar)
  document.addEventListener('pointercancel', terminar)
  document.addEventListener('touchmove', evitarDesplazamiento, { passive: false })
  document.addEventListener('contextmenu', evitarMenu)
}
