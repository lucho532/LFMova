import './EtiquetaEstado.css'

interface PropiedadesEtiquetaEstado {
  activo: boolean
  textoActivo?: string
  textoInactivo?: string
}

/** Etiqueta tipo "pill" para mostrar si algo (empresa, coordinador) está activo o inactivo. */
export function EtiquetaEstado({ activo, textoActivo = 'Activa', textoInactivo = 'Inactiva' }: PropiedadesEtiquetaEstado) {
  return (
    <span className={`etiqueta-estado ${activo ? 'etiqueta-estado--activa' : 'etiqueta-estado--inactiva'}`}>
      {activo ? textoActivo : textoInactivo}
    </span>
  )
}
