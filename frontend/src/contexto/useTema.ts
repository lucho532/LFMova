import { useContext } from 'react'
import { ContextoTema, type ValorContextoTema } from './ContextoTema'

/** Hook para acceder al tema visual actual y alternarlo desde cualquier componente. */
export function useTema(): ValorContextoTema {
  const contexto = useContext(ContextoTema)
  if (!contexto) {
    throw new Error('useTema debe usarse dentro de un ProveedorTema.')
  }
  return contexto
}
