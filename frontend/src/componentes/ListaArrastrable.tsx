import { useRef, type ReactNode } from 'react'
import { useArrastreLista } from '../hooks/useArrastreLista'
import '../estilos/componentes/ListaArrastrable.css'

interface PropiedadesListaArrastrable {
  className?: string
  /** Cada hijo directo es un elemento `<li>` de la lista. */
  children: ReactNode
  /** Recibe la posición de origen y la de destino al soltar; sin esta función la lista no se puede reordenar. */
  alMover?: (desde: number, hasta: number) => void
}

/**
 * Lista `<ul>` cuyos elementos se reordenan manteniendo presionado uno y
 * arrastrándolo a otra posición (ver {@link useArrastreLista}). Solo dibuja
 * la lista y el efecto visual: no guarda el orden.
 */
export function ListaArrastrable({ className, children, alMover }: PropiedadesListaArrastrable) {
  const lista = useRef<HTMLUListElement>(null)
  useArrastreLista(lista, alMover)

  const clases = ['lista-arrastrable', alMover ? 'lista-arrastrable--habilitada' : '', className ?? ''].filter(Boolean).join(' ')
  return (
    <ul ref={lista} className={clases}>
      {children}
    </ul>
  )
}
