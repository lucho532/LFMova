import type { ButtonHTMLAttributes } from 'react'
import './BotonSecundario.css'

type PropiedadesBotonSecundario = ButtonHTMLAttributes<HTMLButtonElement>

/** Botón de acción secundaria (activar/desactivar, cambios de estado), con borde y fondo neutro. */
export function BotonSecundario({ className, type = 'button', ...resto }: PropiedadesBotonSecundario) {
  const clases = className ? `boton-secundario ${className}` : 'boton-secundario'
  return <button type={type} className={clases} {...resto} />
}
