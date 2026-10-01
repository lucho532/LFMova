const urlBaseApi = import.meta.env.VITE_API_URL as string

if (!urlBaseApi) {
  // eslint-disable-next-line no-console
  console.warn('VITE_API_URL no está configurada; ver frontend/.env.example')
}

/** Error lanzado cuando la API responde con un código de error HTTP. */
export class ErrorApi extends Error {
  readonly status: number

  constructor(status: number, mensaje: string) {
    super(mensaje)
    this.status = status
  }
}

/**
 * Construye un mensaje entendible para un error HTTP: usa el mensaje del
 * backend cuando lo trae (`mensaje`), el detalle de validación de ASP.NET
 * (`errors`, `detail`, `title`) o un texto según el código de estado.
 */
function mensajeDeError(estado: number, cuerpo: unknown): string {
  if (cuerpo && typeof cuerpo === 'object') {
    const datos = cuerpo as { mensaje?: unknown; detail?: unknown; title?: unknown; errors?: Record<string, unknown> }
    if (typeof datos.mensaje === 'string' && datos.mensaje) return datos.mensaje
    if (datos.errors && typeof datos.errors === 'object') {
      const detalles = Object.values(datos.errors).flat().filter((valor): valor is string => typeof valor === 'string')
      if (detalles.length > 0) return `Datos no válidos: ${detalles.join(' ')}`
    }
    if (typeof datos.detail === 'string' && datos.detail) return datos.detail
  }

  if (estado === 400) return 'Los datos enviados no son válidos. Revisa el formulario.'
  if (estado === 401) return 'Tu sesión no es válida o expiró. Inicia sesión de nuevo.'
  if (estado === 403) return 'No tienes permiso para realizar esta acción.'
  if (estado === 404) return 'No se encontró lo que buscas.'
  if (estado === 409) return 'La operación no se pudo completar por un conflicto con los datos actuales.'
  if (estado === 413) return 'El archivo es demasiado grande.'
  if (estado === 429) return 'Demasiados intentos seguidos. Espera un momento y vuelve a intentar.'
  if (estado >= 500) return `El servidor tuvo un problema (código ${estado}). Intenta de nuevo más tarde; si persiste, avisa al administrador.`
  return `Error al comunicarse con el servidor (código ${estado}).`
}

interface OpcionesSolicitud {
  metodo?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  cuerpo?: unknown
  token?: string | null
}

/**
 * Cliente HTTP centralizado para consumir la API REST de LFMova.Api.
 * Adjunta el token JWT como cabecera Authorization cuando se proporciona.
 * No decide reglas de negocio ni de autorización: solo transporta la
 * solicitud y traduce errores HTTP a ErrorApi.
 */
export async function solicitarApi<T>(ruta: string, opciones: OpcionesSolicitud = {}): Promise<T> {
  const { metodo = 'GET', cuerpo, token } = opciones

  // Con FormData (subida de archivos) el navegador fija el Content-Type con su boundary.
  const esFormulario = cuerpo instanceof FormData
  const encabezados: Record<string, string> = esFormulario ? {} : { 'Content-Type': 'application/json' }

  if (token) {
    encabezados.Authorization = `Bearer ${token}`
  }

  let respuesta: Response
  try {
    respuesta = await fetch(`${urlBaseApi}${ruta}`, {
      method: metodo,
      headers: encabezados,
      body: esFormulario ? cuerpo : cuerpo ? JSON.stringify(cuerpo) : undefined,
    })
  } catch {
    // fetch solo lanza excepción cuando no hubo respuesta: servidor caído, sin internet, bloqueo de CORS o dirección mal configurada.
    throw new ErrorApi(
      0,
      urlBaseApi
        ? 'No se pudo conectar con el servidor. Revisa tu conexión a internet; si estás conectado, el servicio puede estar caído. Intenta de nuevo en unos minutos.'
        : 'La aplicación no tiene configurada la dirección del servidor (VITE_API_URL).',
    )
  }

  if (!respuesta.ok) {
    const cuerpoError = await respuesta.json().catch(() => null)
    throw new ErrorApi(respuesta.status, mensajeDeError(respuesta.status, cuerpoError))
  }

  if (respuesta.status === 204) {
    return undefined as T
  }

  return (await respuesta.json()) as T
}
