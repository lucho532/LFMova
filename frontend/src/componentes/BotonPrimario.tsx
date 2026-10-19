import type { ButtonHTMLAttributes } from 'react'
import '../estilos/componentes/BotonPrimario.css'

type PropiedadesBotonPrimario = ButtonHTMLAttributes<HTMLButtonElement>

/** Botón de acción principal (enviar formularios), con el color de acento del tema. */
export function BotonPrimario({ className, ...resto }: PropiedadesBotonPrimario) {
  const clases = className ? `boton-primario ${className}` : 'boton-primario'
  return <button type="submit" className={clases} {...resto} />
}
