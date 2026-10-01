import type { ReactNode } from 'react'
import '../estilos/componentes/TablaDatos.css'

interface PropiedadesTablaDatos {
  columnas: string[]
  children: ReactNode
}

/**
 * Tabla administrativa con el estilo estándar de la aplicación (tarjeta con
 * borde, cabecera tenue y filas con resalte). Las filas (`<tr>`) las aporta
 * quien la usa.
 */
export function TablaDatos({ columnas, children }: PropiedadesTablaDatos) {
  return (
    <div className="tabla-datos">
      <table>
        <thead>
          <tr>
            {columnas.map((columna) => (
              <th key={columna}>{columna}</th>
            ))}
          </tr>
        </thead>
        <tbody>{children}</tbody>
      </table>
    </div>
  )
}
