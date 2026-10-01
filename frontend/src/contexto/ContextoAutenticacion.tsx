import { createContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { milisegundosHastaExpirar } from '../servicios/tokenJwt'

export interface ValorContextoAutenticacion {
  token: string | null
  estaAutenticado: boolean
  /** Nombre a mostrar en la interfaz: el editado en "Mi cuenta" si existe, o el del token. */
  nombreActualizado: string | null
  iniciarSesion: (token: string) => void
  cerrarSesion: () => void
  actualizarNombre: (nombre: string) => void
}

export const ContextoAutenticacion = createContext<ValorContextoAutenticacion | undefined>(undefined)

const CLAVE_TOKEN = 'transportapp.token'
const CLAVE_NOMBRE = 'transportapp.nombre'

/**
 * En la app instalada en el teléfono la sesión se guarda en `localStorage`,
 * para que siga abierta al cerrar y volver a abrir la app; en el navegador se
 * guarda en `sessionStorage` y se pierde al cerrar la pestaña.
 */
function almacenamiento(): Storage {
  const esAppInstalada = (window as { Capacitor?: { isNativePlatform?: () => boolean } }).Capacitor?.isNativePlatform?.() === true
  return esAppInstalada ? localStorage : sessionStorage
}

function leerAlmacenamiento(clave: string): string | null {
  try {
    return almacenamiento().getItem(clave)
  } catch {
    return null
  }
}

function escribirAlmacenamiento(clave: string, valor: string | null) {
  try {
    if (valor === null) almacenamiento().removeItem(clave)
    else almacenamiento().setItem(clave, valor)
  } catch {
    // Sin almacenamiento disponible (modo privado, etc.): la sesión sigue solo en memoria.
  }
}

function tokenGuardadoVigente(): string | null {
  const guardado = leerAlmacenamiento(CLAVE_TOKEN)
  if (guardado && milisegundosHastaExpirar(guardado) > 0) return guardado
  escribirAlmacenamiento(CLAVE_TOKEN, null)
  escribirAlmacenamiento(CLAVE_NOMBRE, null)
  return null
}

/**
 * Provee el token JWT de la sesión actual a toda la aplicación. El token se
 * guarda también en el almacenamiento del navegador (ver `almacenamiento`): en
 * la web la sesión sobrevive a recargar la página pero se pierde al cerrar la
 * pestaña; en la app instalada sigue abierta al volver a abrirla. En ambos
 * casos se cierra sola cuando el token expira. Esta protección es únicamente de experiencia de usuario: la
 * autorización real siempre se valida en el backend.
 */
export function ProveedorAutenticacion({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(tokenGuardadoVigente)
  const [nombreActualizado, setNombreActualizado] = useState<string | null>(() => leerAlmacenamiento(CLAVE_NOMBRE))

  useEffect(() => {
    if (!token) return
    const temporizador = setTimeout(() => {
      escribirAlmacenamiento(CLAVE_TOKEN, null)
      escribirAlmacenamiento(CLAVE_NOMBRE, null)
      setToken(null)
      setNombreActualizado(null)
    }, milisegundosHastaExpirar(token))
    return () => clearTimeout(temporizador)
  }, [token])

  const valor = useMemo<ValorContextoAutenticacion>(
    () => ({
      token,
      estaAutenticado: token !== null,
      nombreActualizado,
      iniciarSesion: (nuevoToken: string) => {
        escribirAlmacenamiento(CLAVE_TOKEN, nuevoToken)
        escribirAlmacenamiento(CLAVE_NOMBRE, null)
        setNombreActualizado(null)
        setToken(nuevoToken)
      },
      cerrarSesion: () => {
        escribirAlmacenamiento(CLAVE_TOKEN, null)
        escribirAlmacenamiento(CLAVE_NOMBRE, null)
        setNombreActualizado(null)
        setToken(null)
      },
      actualizarNombre: (nombre: string) => {
        escribirAlmacenamiento(CLAVE_NOMBRE, nombre)
        setNombreActualizado(nombre)
      },
    }),
    [token, nombreActualizado],
  )

  return <ContextoAutenticacion.Provider value={valor}>{children}</ContextoAutenticacion.Provider>
}
