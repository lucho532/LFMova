/** Un claim de rol del token, ya separado en su rol y su empresa (si aplica). */
export interface ClaimRol {
  rol: string
  empresaId: number | null
}

interface PayloadJwt {
  rol?: string | string[]
  nombre?: string
  exp?: number
  usuarioId?: string
}

function decodificarPayload(token: string): PayloadJwt | null {
  try {
    const payloadBase64Url = token.split('.')[1]
    const payloadBase64 = payloadBase64Url.replace(/-/g, '+').replace(/_/g, '/')
    const payloadJson = decodeURIComponent(
      atob(payloadBase64)
        .split('')
        .map((caracter) => `%${caracter.charCodeAt(0).toString(16).padStart(2, '0')}`)
        .join(''),
    )
    return JSON.parse(payloadJson) as PayloadJwt
  } catch {
    return null
  }
}

/** Nombre completo de la persona autenticada, tal como viene en el token (claim "nombre"). */
export function obtenerNombreDelToken(token: string): string {
  return decodificarPayload(token)?.nombre ?? ''
}

/** Identificador del usuario autenticado (claim "usuarioId"), o null si el token no lo trae. */
export function obtenerUsuarioIdDelToken(token: string): number | null {
  const valor = decodificarPayload(token)?.usuarioId
  return valor ? Number(valor) : null
}

/** Milisegundos que faltan para que el token expire (0 si ya expiró o no se puede leer). */
export function milisegundosHastaExpirar(token: string): number {
  const exp = decodificarPayload(token)?.exp
  return exp ? Math.max(0, exp * 1000 - Date.now()) : 0
}

const NOMBRES_DE_ROL: Record<string, string> = {
  ADMINISTRADOR_PLATAFORMA: 'Administrador',
  COORDINADOR: 'Coordinador',
  CONDUCTOR: 'Conductor',
  EMPLEADO: 'Empleado',
}

/** Etiqueta legible del rol principal de la sesión (el de mayor alcance si hay varios). */
export function etiquetaDelRolPrincipal(roles: ClaimRol[]): string {
  const orden = ['ADMINISTRADOR_PLATAFORMA', 'COORDINADOR', 'CONDUCTOR', 'EMPLEADO']
  const principal = orden.find((rol) => roles.some((claim) => claim.rol === rol))
  return principal ? NOMBRES_DE_ROL[principal] : ''
}

/**
 * Decodifica el payload de un JWT (sin verificar la firma; eso ya lo hizo el
 * backend al emitirlo) para leer sus claims de rol. Se usa únicamente para
 * decidir a qué pantalla navegar tras iniciar sesión; nunca para decisiones
 * de autorización, que siempre las valida el backend.
 */
export function obtenerRolesDelToken(token: string): ClaimRol[] {
  const payload = decodificarPayload(token)
  if (!payload) return []

  const valores = Array.isArray(payload.rol) ? payload.rol : payload.rol ? [payload.rol] : []

  return valores.map((valor) => {
    const [rol, empresaId] = valor.split(':')
    return { rol, empresaId: empresaId ? Number(empresaId) : null }
  })
}

/**
 * Decide a qué pantalla debe ir un usuario recién autenticado, según sus
 * roles. El administrador de plataforma ve el listado de empresas; un
 * coordinador ve directamente el detalle de su empresa; cualquier otra
 * persona (empleado, conductor) llega a "Mi transporte".
 */
export function rutaInicialSegunRoles(roles: ClaimRol[]): string {
  if (roles.some((claim) => claim.rol === 'ADMINISTRADOR_PLATAFORMA')) {
    return '/empresas'
  }

  const comoCoordinador = roles.find((claim) => claim.rol === 'COORDINADOR' && claim.empresaId !== null)
  if (comoCoordinador) {
    return `/empresas/${comoCoordinador.empresaId}`
  }

  if (roles.some((claim) => claim.rol === 'CONDUCTOR')) {
    return '/conductor'
  }

  return '/mi-transporte'
}
