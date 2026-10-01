import { createContext, useEffect, useMemo, useState, type ReactNode } from 'react'

export type Tema = 'claro' | 'oscuro'

export interface ValorContextoTema {
  tema: Tema
  alternarTema: () => void
}

export const ContextoTema = createContext<ValorContextoTema | undefined>(undefined)

const CLAVE_ALMACENAMIENTO = 'lfmova.tema'

function obtenerTemaInicial(): Tema {
  try {
    const guardado = window.localStorage.getItem(CLAVE_ALMACENAMIENTO)
    if (guardado === 'claro' || guardado === 'oscuro') {
      return guardado
    }
  } catch {
    // localStorage no disponible (navegación privada, etc.): se usa la preferencia del sistema.
  }

  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'oscuro' : 'claro'
}

/**
 * Provee el tema visual (claro/oscuro) de la aplicación y lo persiste en
 * `localStorage` (preferencia local del navegador, no una configuración de
 * usuario en el backend). Aplica el atributo `data-theme` en `<html>`, que
 * `index.css` usa para sobrescribir las variables de color.
 */
export function ProveedorTema({ children }: { children: ReactNode }) {
  const [tema, setTema] = useState<Tema>(obtenerTemaInicial)

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', tema === 'oscuro' ? 'dark' : 'light')

    try {
      window.localStorage.setItem(CLAVE_ALMACENAMIENTO, tema)
    } catch {
      // Si no se puede persistir, el tema simplemente no sobrevive a un recargo.
    }
  }, [tema])

  const valor = useMemo<ValorContextoTema>(
    () => ({
      tema,
      alternarTema: () => setTema((actual) => (actual === 'claro' ? 'oscuro' : 'claro')),
    }),
    [tema],
  )

  return <ContextoTema.Provider value={valor}>{children}</ContextoTema.Provider>
}
