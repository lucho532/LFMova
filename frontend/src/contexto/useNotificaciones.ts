import { useContext } from 'react'
import { ContextoNotificaciones, type ValorContextoNotificaciones } from './ContextoNotificaciones'

/** Hook para leer las notificaciones del usuario y su "versión" desde cualquier componente. */
export function useNotificaciones(): ValorContextoNotificaciones {
  const contexto = useContext(ContextoNotificaciones)
  if (!contexto) {
    throw new Error('useNotificaciones debe usarse dentro de un ProveedorNotificaciones.')
  }
  return contexto
}
