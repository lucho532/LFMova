import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import './TarjetaAutenticacion.css'

interface PropiedadesTarjetaAutenticacion {
  titulo: string
  subtitulo?: string
  children: ReactNode
}

/**
 * Contenedor centrado con el estilo común de las pantallas públicas de
 * cuenta (crear cuenta, confirmar correo, recuperar contraseña). Incluye el
 * enlace de regreso al inicio de sesión.
 */
export function TarjetaAutenticacion({ titulo, subtitulo, children }: PropiedadesTarjetaAutenticacion) {
  return (
    <main className="tarjeta-autenticacion">
      <section className="tarjeta-autenticacion__tarjeta">
        <p className="tarjeta-autenticacion__marca">TransportApp</p>
        <h1 className="tarjeta-autenticacion__titulo">{titulo}</h1>
        {subtitulo && <p className="tarjeta-autenticacion__subtitulo">{subtitulo}</p>}
        {children}
        <Link to="/iniciar-sesion" className="tarjeta-autenticacion__volver">
          ← Volver a iniciar sesión
        </Link>
      </section>
    </main>
  )
}
