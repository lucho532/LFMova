import type { CambiarContrasenaDatos, Cuenta } from '../modelos/cuenta'
import { solicitarApi } from './clienteHttp'

/** Consume GET /api/cuenta. */
export function obtenerCuenta(token: string): Promise<Cuenta> {
  return solicitarApi<Cuenta>('/api/cuenta', { token })
}

/** Consume PUT /api/cuenta (nombre y teléfono). */
export function actualizarCuenta(datos: { nombreCompleto: string; telefono: string }, token: string): Promise<Cuenta> {
  return solicitarApi<Cuenta>('/api/cuenta', { metodo: 'PUT', cuerpo: datos, token })
}

/** Consume POST /api/cuenta/cambiar-contrasena. */
export function cambiarContrasena(datos: CambiarContrasenaDatos, token: string): Promise<void> {
  return solicitarApi<void>('/api/cuenta/cambiar-contrasena', { metodo: 'POST', cuerpo: datos, token })
}
