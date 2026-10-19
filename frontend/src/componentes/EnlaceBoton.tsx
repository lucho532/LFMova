import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import '../estilos/componentes/EnlaceBoton.css'

/** Enlace de navegación con aspecto de botón principal (p. ej. "+ Nueva sede"). */
export function EnlaceBoton({ a, children }: { a: string; children: ReactNode }) {
  return (
    <Link to={a} className="enlace-boton">
      {children}
    </Link>
  )
}
