import type { ReactNode } from 'react'
import '../estilos/componentes/FilaTarjetasResumen.css'

/** Fila adaptable que reparte varias TarjetaResumen en columnas iguales. */
export function FilaTarjetasResumen({ children }: { children: ReactNode }) {
  return <div className="fila-tarjetas-resumen">{children}</div>
}
