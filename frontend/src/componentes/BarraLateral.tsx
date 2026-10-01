import { NavLink, useLocation } from 'react-router-dom'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { obtenerRolesDelToken } from '../servicios/tokenJwt'
import '../estilos/componentes/BarraLateral.css'

interface EnlaceMenu {
  ruta: string
  texto: string
  icono?: string
  exacto?: boolean
}

function clasesEnlace({ isActive }: { isActive: boolean }) {
  return `barra-lateral__enlace${isActive ? ' barra-lateral__enlace--activo' : ''}`
}

/**
 * Menú lateral según el rol: el administrador de plataforma ve el alta y el
 * listado de empresas; el coordinador ve las secciones de operación de su
 * propia empresa (la empresa sale de su token, nunca de un selector). Otros
 * roles no tienen menú lateral. Solo es experiencia de usuario: la
 * autorización real la valida el backend.
 */
export function BarraLateral() {
  const { token } = useAutenticacion()
  useLocation()
  const roles = token ? obtenerRolesDelToken(token) : []

  let enlaces: EnlaceMenu[] = []

  if (roles.some((claim) => claim.rol === 'ADMINISTRADOR_PLATAFORMA')) {
    enlaces = [
      { ruta: '/empresas/nueva', texto: 'Nueva empresa', icono: '➕' },
      { ruta: '/empresas', texto: 'Empresas', icono: '🏢', exacto: true },
    ]
  } else {
    const esConductor = roles.some((claim) => claim.rol === 'CONDUCTOR')
    const comoCoordinador = roles.find((claim) => claim.rol === 'COORDINADOR' && claim.empresaId !== null)
    if (comoCoordinador) {
      const base = `/empresas/${comoCoordinador.empresaId}`
      enlaces = [
        { ruta: base, texto: 'Inicio', icono: '🏠', exacto: true },
        { ruta: `${base}/conductores`, texto: 'Conductores', icono: '🚌' },
        { ruta: `${base}/empleados`, texto: 'Empleados', icono: '👥' },
        { ruta: `${base}/programacion`, texto: 'Programación', icono: '📅' },
        { ruta: `${base}/zonas`, texto: 'Zonas', icono: '🗺️' },
      ]
    } else if (esConductor) {
      enlaces = [
        { ruta: '/conductor', texto: 'Rutas asignadas', icono: '🗓', exacto: true },
        { ruta: '/conductor/finalizadas', texto: 'Rutas finalizadas', icono: '🏁' },
        { ruta: '/conductor/vehiculo', texto: 'Datos vehículo', icono: '🚐' },
      ]
    }
  }

  if (enlaces.length === 0) {
    return null
  }

  return (
    <nav className="barra-lateral" aria-label="Menú principal">
      {enlaces.map((enlace) => (
        <NavLink key={enlace.ruta} to={enlace.ruta} end={enlace.exacto} className={clasesEnlace}>
          {enlace.icono && <span className="barra-lateral__icono" aria-hidden="true">{enlace.icono}</span>}
          {enlace.texto}
        </NavLink>
      ))}
    </nav>
  )
}
