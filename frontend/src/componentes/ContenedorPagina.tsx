import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import './ContenedorPagina.css'

interface PropiedadesContenedorPagina {
  children: ReactNode
  volverA?: string
  textoVolver?: string
  ancho?: 'normal' | 'estrecho' | 'amplio' | 'total'
}

/** Contenedor común de las pantallas internas: ancho máximo, márgenes y enlace opcional para volver. */
export function ContenedorPagina({ children, volverA, textoVolver = '← Volver', ancho = 'normal' }: PropiedadesContenedorPagina) {
  return (
    <main className={`contenedor-pagina contenedor-pagina--${ancho}`}>
      {volverA && (
        <Link to={volverA} className="contenedor-pagina__volver">
          {textoVolver}
        </Link>
      )}
      {children}
    </main>
  )
}
