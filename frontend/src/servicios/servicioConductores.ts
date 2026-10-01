import type {
  Conductor,
  CrearConductorDatos,
  CrearVehiculoDatos,
  UnidadOperativa,
  Vehiculo,
  VincularConductorDatos,
} from '../modelos/conductor'
import { solicitarApi } from './clienteHttp'

/** Consume GET /api/empresas/{empresaId}/conductores. */
export function obtenerConductores(empresaId: number, token: string): Promise<Conductor[]> {
  return solicitarApi<Conductor[]>(`/api/empresas/${empresaId}/conductores`, { token })
}

/** Consume GET /api/empresas/{empresaId}/conductores/{conductorId}. */
export function obtenerConductor(empresaId: number, conductorId: number, token: string): Promise<Conductor> {
  return solicitarApi<Conductor>(`/api/empresas/${empresaId}/conductores/${conductorId}`, { token })
}

/** Consume POST /api/empresas/{empresaId}/conductores. Registra un conductor nuevo y lo vincula a la empresa. */
export function crearConductor(empresaId: number, datos: CrearConductorDatos, token: string): Promise<Conductor> {
  return solicitarApi<Conductor>(`/api/empresas/${empresaId}/conductores`, { metodo: 'POST', cuerpo: datos, token })
}

/** Consume POST /api/empresas/{empresaId}/conductores/vincular. Vincula un conductor ya existente en la plataforma. */
export function vincularConductor(empresaId: number, datos: VincularConductorDatos, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/conductores/vincular`, { metodo: 'POST', cuerpo: datos, token })
}

/** Consume POST /api/empresas/{empresaId}/conductores/{conductorId}/activar-vinculacion. */
export function activarVinculacion(empresaId: number, conductorId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/conductores/${conductorId}/activar-vinculacion`, { metodo: 'POST', token })
}

/** Consume POST /api/empresas/{empresaId}/conductores/{conductorId}/desactivar-vinculacion. */
export function desactivarVinculacion(empresaId: number, conductorId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/conductores/${conductorId}/desactivar-vinculacion`, { metodo: 'POST', token })
}

/** Consume GET /api/conductores/{conductorId}/vehiculos. */
export function obtenerVehiculos(conductorId: number, token: string): Promise<Vehiculo[]> {
  return solicitarApi<Vehiculo[]>(`/api/conductores/${conductorId}/vehiculos`, { token })
}

/** Consume POST /api/conductores/{conductorId}/vehiculos. */
export function crearVehiculo(conductorId: number, datos: CrearVehiculoDatos, token: string): Promise<Vehiculo> {
  return solicitarApi<Vehiculo>(`/api/conductores/${conductorId}/vehiculos`, { metodo: 'POST', cuerpo: datos, token })
}

/** Consume PUT /api/conductores/{conductorId}/vehiculos/{vehiculoId}: actualiza los datos del vehículo. */
export function actualizarVehiculo(conductorId: number, vehiculoId: number, datos: CrearVehiculoDatos, token: string): Promise<Vehiculo> {
  return solicitarApi<Vehiculo>(`/api/conductores/${conductorId}/vehiculos/${vehiculoId}`, { metodo: 'PUT', cuerpo: datos, token })
}

/** Consume POST /api/conductores/{conductorId}/vehiculos/{vehiculoId}/activar. */
export function activarVehiculo(conductorId: number, vehiculoId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/conductores/${conductorId}/vehiculos/${vehiculoId}/activar`, { metodo: 'POST', token })
}

/** Consume POST /api/conductores/{conductorId}/vehiculos/{vehiculoId}/desactivar. */
export function desactivarVehiculo(conductorId: number, vehiculoId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/conductores/${conductorId}/vehiculos/${vehiculoId}/desactivar`, { metodo: 'POST', token })
}

/** Consume GET /api/conductores/{conductorId}/unidades-operativas. */
export function obtenerUnidadesOperativas(conductorId: number, token: string): Promise<UnidadOperativa[]> {
  return solicitarApi<UnidadOperativa[]>(`/api/conductores/${conductorId}/unidades-operativas`, { token })
}

/** Consume POST /api/conductores/{conductorId}/unidades-operativas. */
export function crearUnidadOperativa(conductorId: number, vehiculoId: number, token: string): Promise<UnidadOperativa> {
  return solicitarApi<UnidadOperativa>(`/api/conductores/${conductorId}/unidades-operativas`, {
    metodo: 'POST',
    cuerpo: { vehiculoId },
    token,
  })
}

/** Consume POST /api/conductores/{conductorId}/unidades-operativas/{unidadOperativaId}/activar. */
export function activarUnidadOperativa(conductorId: number, unidadOperativaId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/conductores/${conductorId}/unidades-operativas/${unidadOperativaId}/activar`, {
    metodo: 'POST',
    token,
  })
}

/** Consume POST /api/conductores/{conductorId}/unidades-operativas/{unidadOperativaId}/desactivar. */
export function desactivarUnidadOperativa(conductorId: number, unidadOperativaId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/conductores/${conductorId}/unidades-operativas/${unidadOperativaId}/desactivar`, {
    metodo: 'POST',
    token,
  })
}

/** Una unidad operativa activa, ya identificada por el conductor y la placa, para elegirla como "ruta". */
export interface OpcionUnidadOperativa {
  unidadOperativaId: number
  texto: string
}

/**
 * Arma la lista de unidades operativas activas de todos los conductores de
 * la empresa, con el nombre del conductor y la placa de su vehículo. Se usa
 * para elegir a qué unidad pertenece una ruta.
 */
export async function obtenerOpcionesDeUnidades(empresaId: number, token: string): Promise<OpcionUnidadOperativa[]> {
  const conductores = await obtenerConductores(empresaId, token)
  const opciones: OpcionUnidadOperativa[] = []
  for (const conductor of conductores) {
    const [unidades, vehiculos] = await Promise.all([obtenerUnidadesOperativas(conductor.conductorId, token), obtenerVehiculos(conductor.conductorId, token)])
    for (const unidad of unidades.filter((u) => u.activa)) {
      const placa = vehiculos.find((v) => v.vehiculoId === unidad.vehiculoId)?.placa
      opciones.push({ unidadOperativaId: unidad.unidadOperativaId, texto: placa ? `${conductor.nombreCompleto} · ${placa}` : conductor.nombreCompleto })
    }
  }
  return opciones
}
