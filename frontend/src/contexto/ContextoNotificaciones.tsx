import { createContext, useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import type { Notificacion } from '../modelos/notificacion'
import { marcarNotificacionLeida, obtenerNotificaciones } from '../servicios/servicioNotificaciones'
import { obtenerUsuarioIdDelToken } from '../servicios/tokenJwt'
import { useAutenticacion } from './useAutenticacion'

const INTERVALO_MS = 15_000

export interface ValorContextoNotificaciones {
  notificaciones: Notificacion[]
  /**
   * Cambia cada vez que llega al menos una notificación que no se había visto
   * antes. Las pantallas de conductor/empleado lo agregan a su `useEffect` de
   * carga para refrescarse solas apenas les llega algo nuevo, sin esperar a
   * que la persona recargue la página a mano.
   */
  version: number
  marcarLeida: (notificacionId: number) => Promise<void>
}

export const ContextoNotificaciones = createContext<ValorContextoNotificaciones | undefined>(undefined)

/**
 * Consulta las notificaciones del usuario autenticado cada 15 segundos y las
 * ofrece a toda la aplicación junto con una "versión" que sube cada vez que
 * aparece algo nuevo. Es la única fuente que hace polling de notificaciones:
 * antes cada pantalla que necesitaba enterarse tenía que hacer su propio
 * temporizador.
 */
export function ProveedorNotificaciones({ children }: { children: ReactNode }) {
  const { token } = useAutenticacion()
  const usuarioId = token ? obtenerUsuarioIdDelToken(token) : null
  const [notificaciones, setNotificaciones] = useState<Notificacion[]>([])
  const [version, setVersion] = useState(0)
  const vistas = useRef<Set<number> | null>(null)

  const cargar = useCallback(async () => {
    if (!token || usuarioId === null) return
    try {
      const recibidas = await obtenerNotificaciones(usuarioId, token)
      setNotificaciones(recibidas)

      // La primera carga no cuenta como "nuevo": solo lo que aparece después.
      if (vistas.current !== null && recibidas.some((n) => !vistas.current!.has(n.notificacionId))) {
        setVersion((v) => v + 1)
      }
      vistas.current = new Set(recibidas.map((n) => n.notificacionId))
    } catch {
      // Un fallo puntual no debe molestar a nadie; se reintenta en el siguiente ciclo.
    }
  }, [token, usuarioId])

  useEffect(() => {
    setNotificaciones([])
    vistas.current = null
    if (!token) return
    cargar()
    const temporizador = setInterval(cargar, INTERVALO_MS)
    return () => clearInterval(temporizador)
  }, [cargar, token])

  const marcarLeida = useCallback(
    async (notificacionId: number) => {
      if (!token || usuarioId === null) return
      await marcarNotificacionLeida(usuarioId, notificacionId, token)
      setNotificaciones((actuales) => actuales.map((n) => (n.notificacionId === notificacionId ? { ...n, leida: true } : n)))
    },
    [token, usuarioId],
  )

  const valor = useMemo<ValorContextoNotificaciones>(
    () => ({ notificaciones, version, marcarLeida }),
    [notificaciones, version, marcarLeida],
  )

  return <ContextoNotificaciones.Provider value={valor}>{children}</ContextoNotificaciones.Provider>
}
