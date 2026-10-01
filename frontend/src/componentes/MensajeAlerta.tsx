import type { ReactNode } from 'react'
import './MensajeAlerta.css'

interface PropiedadesMensajeAlerta {
  tipo: 'error' | 'exito'
  children: ReactNode
}

/** Mensaje destacado de error o éxito, con colores que funcionan en ambos temas. */
export function MensajeAlerta({ tipo, children }: PropiedadesMensajeAlerta) {
  return (
    <p className={`mensaje-alerta mensaje-alerta--${tipo}`} role={tipo === 'error' ? 'alert' : 'status'}>
      {children}
    </p>
  )
}
