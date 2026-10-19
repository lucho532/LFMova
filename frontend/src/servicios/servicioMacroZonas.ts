import type { DatosMacroZona, MacroZona } from '../modelos/macroZona'
import { solicitarApi } from './clienteHttp'

/** Consume GET /api/empresas/{empresaId}/macro-zonas. */
export function obtenerMacroZonas(empresaId: number, token: string): Promise<MacroZona[]> {
  return solicitarApi<MacroZona[]>(`/api/empresas/${empresaId}/macro-zonas`, { token })
}

/** Consume POST /api/empresas/{empresaId}/macro-zonas. Requiere rol COORDINADOR de esa empresa. */
export function crearMacroZona(empresaId: number, datos: DatosMacroZona, token: string): Promise<MacroZona> {
  return solicitarApi<MacroZona>(`/api/empresas/${empresaId}/macro-zonas`, { metodo: 'POST', cuerpo: datos, token })
}

/** Consume POST /api/empresas/{empresaId}/macro-zonas/{macroZonaId}/activar. Requiere rol COORDINADOR de esa empresa. */
export function activarMacroZona(empresaId: number, macroZonaId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/macro-zonas/${macroZonaId}/activar`, { metodo: 'POST', token })
}

/** Consume POST /api/empresas/{empresaId}/macro-zonas/{macroZonaId}/desactivar. Requiere rol COORDINADOR de esa empresa. */
export function desactivarMacroZona(empresaId: number, macroZonaId: number, token: string): Promise<void> {
  return solicitarApi<void>(`/api/empresas/${empresaId}/macro-zonas/${macroZonaId}/desactivar`, { metodo: 'POST', token })
}
