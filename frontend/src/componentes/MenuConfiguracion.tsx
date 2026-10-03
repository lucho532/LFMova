import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { useTema } from '../contexto/useTema'
import '../estilos/componentes/MenuConfiguracion.css'

/**
 * Rueda dentada de la barra superior: al pulsarla despliega las opciones de
 * configuración (cambiar entre tema claro y oscuro y, con sesión iniciada,
 * ir a "Mi cuenta" y cerrar sesión). Se cierra al elegir una opción, al
 * pulsar fuera o con Escape. Al cerrar sesión, `RutaProtegida` redirige a
 * `/iniciar-sesion` porque deja de haber token.
 */
export function MenuConfiguracion() {
  const { estaAutenticado, cerrarSesion } = useAutenticacion()
  const { tema, alternarTema } = useTema()
  const [abierto, setAbierto] = useState(false)
  const contenedor = useRef<HTMLDivElement>(null)
  const esOscuro = tema === 'oscuro'

  useEffect(() => {
    if (!abierto) return
    const alPulsarFuera = (evento: PointerEvent) => {
      if (!contenedor.current?.contains(evento.target as Node)) setAbierto(false)
    }
    const alTeclear = (evento: KeyboardEvent) => {
      if (evento.key === 'Escape') setAbierto(false)
    }
    document.addEventListener('pointerdown', alPulsarFuera)
    document.addEventListener('keydown', alTeclear)
    return () => {
      document.removeEventListener('pointerdown', alPulsarFuera)
      document.removeEventListener('keydown', alTeclear)
    }
  }, [abierto])

  return (
    <div className="menu-configuracion" ref={contenedor}>
      <button
        type="button"
        className="menu-configuracion__boton"
        onClick={() => setAbierto((valor) => !valor)}
        aria-label="Configuración"
        aria-haspopup="menu"
        aria-expanded={abierto}
        title="Configuración"
      >
        <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <circle cx="12" cy="12" r="3" />
          <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z" />
        </svg>
      </button>

      {abierto && (
        <div className="menu-configuracion__panel" role="menu">
          <button type="button" role="menuitem" className="menu-configuracion__opcion" onClick={alternarTema}>
            <span aria-hidden="true">{esOscuro ? '☀️' : '🌙'}</span>
            {esOscuro ? 'Cambiar a tema claro' : 'Cambiar a tema oscuro'}
          </button>
          {estaAutenticado && (
            <>
              <Link to="/mi-cuenta" role="menuitem" className="menu-configuracion__opcion" onClick={() => setAbierto(false)}>
                <span aria-hidden="true">👤</span>
                Mi cuenta
              </Link>
              <button
                type="button"
                role="menuitem"
                className="menu-configuracion__opcion menu-configuracion__opcion--salir"
                onClick={() => {
                  setAbierto(false)
                  cerrarSesion()
                }}
              >
                <span aria-hidden="true">⎋</span>
                Cerrar sesión
              </button>
            </>
          )}
        </div>
      )}
    </div>
  )
}
