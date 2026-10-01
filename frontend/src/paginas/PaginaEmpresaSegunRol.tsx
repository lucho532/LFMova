import { useAutenticacion } from '../contexto/useAutenticacion'
import { obtenerRolesDelToken } from '../servicios/tokenJwt'
import { PaginaDetalleEmpresa } from './PaginaDetalleEmpresa'
import { PaginaInicioCoordinador } from './PaginaInicioCoordinador'

/** Ficha de la empresa para el administrador de plataforma; resumen de operación para el coordinador. */
export function PaginaEmpresaSegunRol() {
  const { token } = useAutenticacion()
  const esAdministrador = token ? obtenerRolesDelToken(token).some((claim) => claim.rol === 'ADMINISTRADOR_PLATAFORMA') : false
  return esAdministrador ? <PaginaDetalleEmpresa /> : <PaginaInicioCoordinador />
}
