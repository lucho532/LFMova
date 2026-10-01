import type { ReactNode } from 'react'
import '../estilos/componentes/EncabezadoPagina.css'

interface PropiedadesEncabezadoPagina {
  titulo: ReactNode
  subtitulo?: string
  acciones?: ReactNode
}

/** Título de pantalla con subtítulo opcional y zona de acciones a la derecha. */
export function EncabezadoPagina({ titulo, subtitulo, acciones }: PropiedadesEncabezadoPagina) {
  return (
    <header className="encabezado-pagina">
      <div>
        <h1>{titulo}</h1>
        {subtitulo && <p>{subtitulo}</p>}
      </div>
      {acciones && <div className="encabezado-pagina__acciones">{acciones}</div>}
    </header>
  )
}
