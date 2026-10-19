import type { DatosActualizarEmpleado, Empleado } from '../modelos/empleado'
import { solicitarApi } from './clienteHttp'

/** Consume GET /api/empresas/{empresaId}/empleados. */
export function obtenerEmpleados(empresaId: number, token: string): Promise<Empleado[]> {
  return solicitarApi<Empleado[]>(`/api/empresas/${empresaId}/empleados`, { token })
}

/** Consume GET /api/empresas/{empresaId}/empleados/{empleadoId}. */
export function obtenerEmpleado(empresaId: number, empleadoId: number, token: string): Promise<Empleado> {
  return solicitarApi<Empleado>(`/api/empresas/${empresaId}/empleados/${empleadoId}`, { token })
}

/** Consume PUT /api/empresas/{empresaId}/empleados/{empleadoId}. */
export function actualizarEmpleado(empresaId: number, empleadoId: number, datos: DatosActualizarEmpleado, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/empleados/${empleadoId}`, { metodo: 'PUT', cuerpo: datos, token })
}

/**
 * Consume DELETE /api/empresas/{empresaId}/empleados/{empleadoId}: elimina a la persona. La respuesta
 * indica si se borró la cuenta completa o solo se la quitó de esta empresa (cuando también es de otra).
 */
export function eliminarEmpleado(empresaId: number, empleadoId: number, token: string): Promise<{ cuentaEliminada: boolean }> {
  return solicitarApi<{ cuentaEliminada: boolean }>(`/api/empresas/${empresaId}/empleados/${empleadoId}`, { metodo: 'DELETE', token })
}
