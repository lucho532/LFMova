import type { FormEvent, ReactNode } from 'react'
import './TarjetaFormulario.css'

interface PropiedadesTarjetaFormulario {
  alEnviar: (evento: FormEvent) => void
  children: ReactNode
  columnas?: 1 | 2
}

/** Formulario dentro de una tarjeta, con sus campos en una o dos columnas. */
export function TarjetaFormulario({ alEnviar, children, columnas = 1 }: PropiedadesTarjetaFormulario) {
  return (
    <form className={`tarjeta-formulario tarjeta-formulario--${columnas}`} onSubmit={alEnviar}>
      {children}
    </form>
  )
}
