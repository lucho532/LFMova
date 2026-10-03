import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import '../estilos/componentes/DocumentoLegal.css'

interface PropiedadesDocumentoLegal {
  titulo: string
  /** Fecha de la última actualización del texto, ya escrita para mostrar. */
  actualizado: string
  children: ReactNode
}

/**
 * Marco de lectura de las páginas públicas de texto legal o informativo
 * (política de privacidad, eliminación de cuenta): título, fecha y el
 * contenido en una columna cómoda de leer. Solo presenta el texto.
 */
export function DocumentoLegal({ titulo, actualizado, children }: PropiedadesDocumentoLegal) {
  return (
    <main className="documento-legal">
      <article className="documento-legal__hoja">
        <p className="documento-legal__marca">LFMova</p>
        <h1>{titulo}</h1>
        <p className="documento-legal__fecha">Última actualización: {actualizado}</p>
        {children}
        <Link to="/iniciar-sesion" className="documento-legal__volver">
          ← Ir a LFMova
        </Link>
      </article>
    </main>
  )
}
