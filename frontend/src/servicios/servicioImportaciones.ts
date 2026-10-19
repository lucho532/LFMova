import type {
  DatosRutaPegada,
  DatosRutaVacia,
  PlantillaColumnasPegado,
  ResultadoImportacion,
  VistaPreviaImportacion,
} from '../modelos/importacion'
import { solicitarApi } from './clienteHttp'

/** Consume POST /api/empresas/{empresaId}/importaciones/validar: interpreta el Excel sin guardar nada. */
export function validarImportacion(empresaId: number, archivo: File, token: string): Promise<VistaPreviaImportacion> {
  const datos = new FormData()
  datos.append('archivo', archivo)
  return solicitarApi<VistaPreviaImportacion>(`/api/empresas/${empresaId}/importaciones/validar`, { metodo: 'POST', cuerpo: datos, token })
}

/**
 * Consume POST /api/empresas/{empresaId}/importaciones: crea jornada,
 * servicios y pasajeros a partir del Excel. `sedeGeneralId` se usa para los
 * grupos de pasajeros cuya sede no viene en el Excel o no se reconoce.
 */
export function importarExcel(
  empresaId: number,
  archivo: File,
  fechaOperativa: string,
  unidadOperativaId: number | null,
  repartirEntreUnidades: boolean,
  sedeGeneralId: number | null,
  token: string,
): Promise<ResultadoImportacion> {
  const datos = new FormData()
  datos.append('archivo', archivo)
  datos.append('fechaOperativa', fechaOperativa)
  if (unidadOperativaId !== null) datos.append('unidadOperativaId', String(unidadOperativaId))
  if (repartirEntreUnidades) datos.append('repartirEntreUnidades', 'true')
  if (sedeGeneralId !== null) datos.append('sedeGeneralId', String(sedeGeneralId))
  return solicitarApi<ResultadoImportacion>(`/api/empresas/${empresaId}/importaciones`, { metodo: 'POST', cuerpo: datos, token })
}

/**
 * Consume POST /api/empresas/{empresaId}/importaciones/ruta-vacia: crea una
 * ruta vacía (sin pasajeros) para una unidad operativa, o reutiliza la que ya
 * exista con esa misma fecha, hora, tipo, sede y unidad. Sirve para abrir un
 * destino nuevo antes de moverle pasajeros desde otra ruta.
 */
export function crearRutaVacia(empresaId: number, datos: DatosRutaVacia, token: string): Promise<ResultadoImportacion> {
  return solicitarApi<ResultadoImportacion>(`/api/empresas/${empresaId}/importaciones/ruta-vacia`, { metodo: 'POST', cuerpo: datos, token })
}

/**
 * Consume POST /api/empresas/{empresaId}/importaciones/ruta-pegada: crea una
 * ruta nueva a partir de pasajeros pegados desde un Excel externo, sin
 * conductor asignado.
 */
export function crearRutaPegada(empresaId: number, datos: DatosRutaPegada, token: string): Promise<ResultadoImportacion> {
  return solicitarApi<ResultadoImportacion>(`/api/empresas/${empresaId}/importaciones/ruta-pegada`, { metodo: 'POST', cuerpo: datos, token })
}

/**
 * Consume GET /api/empresas/{empresaId}/importaciones/plantilla-columnas-pegado.
 * Lanza ErrorApi con status 404 si la empresa todavía no definió ninguna.
 */
export function obtenerPlantillaColumnasPegado(empresaId: number, token: string): Promise<PlantillaColumnasPegado> {
  return solicitarApi<PlantillaColumnasPegado>(`/api/empresas/${empresaId}/importaciones/plantilla-columnas-pegado`, { token })
}

/** Consume PUT /api/empresas/{empresaId}/importaciones/plantilla-columnas-pegado: crea o reemplaza la plantilla de la empresa. */
export function guardarPlantillaColumnasPegado(
  empresaId: number,
  columnasEnOrden: string[],
  token: string,
): Promise<PlantillaColumnasPegado> {
  return solicitarApi<PlantillaColumnasPegado>(`/api/empresas/${empresaId}/importaciones/plantilla-columnas-pegado`, {
    metodo: 'PUT',
    cuerpo: { columnasEnOrden },
    token,
  })
}
