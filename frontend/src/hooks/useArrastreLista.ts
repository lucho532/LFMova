import { useEffect, useRef, type RefObject } from 'react'

/** Tiempo que hay que mantener presionada una tarjeta para empezar a arrastrarla. */
const ESPERA_MS = 400
/** Si el dedo se mueve más que esto antes de ese tiempo, es un desplazamiento normal de la pantalla. */
const TOLERANCIA_PX = 10

interface Arrastre {
  desde: number
  inicioY: number
  elementos: HTMLElement[]
  centros: number[]
  paso: number
  hasta: number
}

/**
 * Permite reordenar los hijos directos de una lista manteniendo presionado
 * uno (dedo o ratón) y soltándolo en otra posición. Mientras se arrastra, el
 * elemento sigue al dedo y los demás se apartan para mostrar dónde quedará.
 * Solo mueve elementos en pantalla y avisa con `alMover(desde, hasta)`:
 * guardar el nuevo orden le toca a quien la usa. Con `alMover` vacío no hace
 * nada.
 */
export function useArrastreLista(lista: RefObject<HTMLElement | null>, alMover: ((desde: number, hasta: number) => void) | undefined) {
  const alMoverActual = useRef(alMover)
  useEffect(() => {
    alMoverActual.current = alMover
  })
  const habilitado = alMover !== undefined

  useEffect(() => {
    const contenedor = lista.current
    if (!contenedor || !habilitado) return

    let temporizador: number | null = null
    let pendiente: { indice: number; y: number } | null = null
    let arrastre: Arrastre | null = null

    const limpiarEstilos = () => {
      for (const elemento of arrastre?.elementos ?? []) {
        elemento.style.transform = ''
        elemento.style.transition = ''
        elemento.classList.remove('lista-arrastrable__elemento--arrastrando')
      }
      contenedor.classList.remove('lista-arrastrable--arrastrando')
    }

    const cancelarEspera = () => {
      if (temporizador !== null) window.clearTimeout(temporizador)
      temporizador = null
      pendiente = null
    }

    const activar = () => {
      if (!pendiente) return
      const elementos = Array.from(contenedor.children) as HTMLElement[]
      const rectangulos = elementos.map((e) => e.getBoundingClientRect())
      const propio = rectangulos[pendiente.indice]
      const siguiente = rectangulos[pendiente.indice + 1] ?? rectangulos[pendiente.indice - 1]
      const separacion = siguiente ? Math.abs(siguiente.top - propio.top) - propio.height : 12
      arrastre = {
        desde: pendiente.indice,
        inicioY: pendiente.y,
        elementos,
        centros: rectangulos.map((r) => r.top + r.height / 2),
        paso: propio.height + Math.max(0, separacion),
        hasta: pendiente.indice,
      }
      temporizador = null
      pendiente = null
      contenedor.classList.add('lista-arrastrable--arrastrando')
      elementos[arrastre.desde].classList.add('lista-arrastrable__elemento--arrastrando')
      elementos.forEach((e, i) => {
        if (i !== arrastre!.desde) e.style.transition = 'transform 150ms ease'
      })
      navigator.vibrate?.(30)
    }

    const iniciar = (objetivo: EventTarget | null, y: number) => {
      const elemento = (objetivo as Element | null)?.closest('input, textarea, select') ? null : (objetivo as Element | null)
      if (!elemento) return
      const indice = Array.from(contenedor.children).findIndex((hijo) => hijo.contains(elemento))
      if (indice < 0) return
      pendiente = { indice, y }
      temporizador = window.setTimeout(activar, ESPERA_MS)
    }

    const mover = (y: number) => {
      if (pendiente && Math.abs(y - pendiente.y) > TOLERANCIA_PX) cancelarEspera()
      if (!arrastre) return
      const desplazamiento = y - arrastre.inicioY
      const centro = arrastre.centros[arrastre.desde] + desplazamiento
      const hasta = arrastre.centros.filter((c, i) => i !== arrastre!.desde && c < centro).length
      arrastre.hasta = hasta
      arrastre.elementos.forEach((elemento, i) => {
        if (i === arrastre!.desde) {
          elemento.style.transform = `translateY(${desplazamiento}px)`
        } else if (arrastre!.desde < hasta && i > arrastre!.desde && i <= hasta) {
          elemento.style.transform = `translateY(${-arrastre!.paso}px)`
        } else if (hasta < arrastre!.desde && i >= hasta && i < arrastre!.desde) {
          elemento.style.transform = `translateY(${arrastre!.paso}px)`
        } else {
          elemento.style.transform = ''
        }
      })
    }

    const terminar = () => {
      cancelarEspera()
      if (!arrastre) return
      const { desde, hasta } = arrastre
      limpiarEstilos()
      arrastre = null
      // El clic que el navegador dispara al soltar no debe abrir ni cerrar la tarjeta que se acaba de
      // mover. Se ignora solo durante un instante, para no tragarse el siguiente toque real.
      const ignorarClic = (e: MouseEvent) => e.stopPropagation()
      window.addEventListener('click', ignorarClic, { capture: true })
      window.setTimeout(() => window.removeEventListener('click', ignorarClic, { capture: true }), 300)
      if (desde !== hasta) alMoverActual.current?.(desde, hasta)
    }

    // Con el dedo: mientras se arrastra se bloquea el desplazamiento de la pantalla.
    const alTocar = (e: TouchEvent) => iniciar(e.target, e.touches[0].clientY)
    const alMoverDedo = (e: TouchEvent) => {
      if (arrastre) e.preventDefault()
      mover(e.touches[0].clientY)
    }
    // Con el ratón (en el computador) funciona igual: mantener presionado y arrastrar.
    const alPresionar = (e: MouseEvent) => {
      if (e.button === 0) iniciar(e.target, e.clientY)
    }
    const alMoverRaton = (e: MouseEvent) => {
      if (pendiente || arrastre) mover(e.clientY)
    }
    // Evita el menú contextual o la selección de texto del Android al mantener presionado.
    const alMenuContextual = (e: Event) => {
      if (pendiente || arrastre) e.preventDefault()
    }

    contenedor.addEventListener('touchstart', alTocar, { passive: true })
    contenedor.addEventListener('touchmove', alMoverDedo, { passive: false })
    contenedor.addEventListener('touchend', terminar)
    contenedor.addEventListener('touchcancel', terminar)
    contenedor.addEventListener('mousedown', alPresionar)
    window.addEventListener('mousemove', alMoverRaton)
    window.addEventListener('mouseup', terminar)
    contenedor.addEventListener('contextmenu', alMenuContextual)

    return () => {
      cancelarEspera()
      limpiarEstilos()
      contenedor.removeEventListener('touchstart', alTocar)
      contenedor.removeEventListener('touchmove', alMoverDedo)
      contenedor.removeEventListener('touchend', terminar)
      contenedor.removeEventListener('touchcancel', terminar)
      contenedor.removeEventListener('mousedown', alPresionar)
      window.removeEventListener('mousemove', alMoverRaton)
      window.removeEventListener('mouseup', terminar)
      contenedor.removeEventListener('contextmenu', alMenuContextual)
    }
  }, [lista, habilitado])
}
