import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAutenticacion } from '../contexto/useAutenticacion'

/**
 * Redirige a la página de inicio de sesión cuando no existe un token en el
 * contexto de autenticación. Es una protección de experiencia de usuario
 * únicamente: la autorización real se valida siempre en el backend.
 */
export function RutaProtegida({ children }: { children: ReactNode }) {
  const { estaAutenticado } = useAutenticacion()

  if (!estaAutenticado) {
    return <Navigate to="/iniciar-sesion" replace />
  }

  return <>{children}</>
}
