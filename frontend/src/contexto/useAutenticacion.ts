import { useContext } from 'react'
import { ContextoAutenticacion, type ValorContextoAutenticacion } from './ContextoAutenticacion'

/** Hook para acceder al estado de autenticación desde cualquier componente. */
export function useAutenticacion(): ValorContextoAutenticacion {
  const contexto = useContext(ContextoAutenticacion)
  if (!contexto) {
    throw new Error('useAutenticacion debe usarse dentro de un ProveedorAutenticacion.')
  }
  return contexto
}
