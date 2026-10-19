import { useEffect, useState } from 'react'
import type { ModoSeparacionRutas, ResultadoImportacion, VistaPreviaRutasArmadas } from '../modelos/importacion'
import { ErrorApi } from '../servicios/clienteHttp'
import { importarRutasArmadas, validarRutasArmadas } from '../servicios/servicioImportaciones'

const ESPERA_ESCRITURA_MS = 600

function textoGuardado(empresaId: number): string {
  try {
    return localStorage.getItem(`lfmova.textoInicioRuta.${empresaId}`) ?? 'RUTA'
  } catch {
    return 'RUTA'
  }
}

/**
 * Estado y acciones de la carga de un Excel con rutas armadas: lee el archivo apenas se elige (o cuando
 * cambia el texto de inicio de ruta) y muestra qué se crearía; importar solo ocurre al pedirlo. El texto
 * de inicio se recuerda por empresa en este navegador. No dibuja nada.
 */
export function useImportacionRutasArmadas(
  empresaId: number,
  token: string | null,
  modo: ModoSeparacionRutas,
  fecha: string,
  cambiarFecha: (fecha: string) => void,
  alTerminar: () => void,
) {
  const [archivo, setArchivo] = useState<File | null>(null)
  const [textoInicio, setTextoInicio] = useState(() => textoGuardado(empresaId))
  const [sedeGeneral, setSedeGeneral] = useState('')
  const [vista, setVista] = useState<VistaPreviaRutasArmadas | null>(null)
  const [resultado, setResultado] = useState<ResultadoImportacion | null>(null)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [ocupado, setOcupado] = useState(false)

  const necesitaTexto = modo === 'LINEA_INICIO'
  const puedeLeer = Boolean(archivo) && (!necesitaTexto || textoInicio.trim() !== '')

  useEffect(() => {
    if (!token || !archivo || !puedeLeer) return
    let cancelado = false
    const temporizador = setTimeout(async () => {
      setOcupado(true)
      setMensajeError(null)
      setResultado(null)
      try {
        const previa = await validarRutasArmadas(empresaId, archivo, modo, textoInicio, token)
        if (cancelado) return
        setVista(previa)
        if (previa.fechaOperativaSugerida) cambiarFecha(previa.fechaOperativaSugerida)
      } catch (error) {
        if (cancelado) return
        setVista(null)
        setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo leer el archivo.')
      } finally {
        if (!cancelado) setOcupado(false)
      }
    }, ESPERA_ESCRITURA_MS)
    return () => {
      cancelado = true
      clearTimeout(temporizador)
    }
  }, [empresaId, token, archivo, modo, textoInicio, puedeLeer, cambiarFecha])

  function cambiarTexto(nuevo: string) {
    setTextoInicio(nuevo)
    try {
      localStorage.setItem(`lfmova.textoInicioRuta.${empresaId}`, nuevo)
    } catch {
      // Sin almacenamiento disponible solo se pierde el recuerdo del texto.
    }
  }

  function elegirArchivo(nuevo: File) {
    setArchivo(nuevo)
    setVista(null)
    setResultado(null)
    setMensajeError(null)
  }

  async function importar() {
    if (!token || !archivo || !vista) return
    setOcupado(true)
    setMensajeError(null)
    try {
      setResultado(await importarRutasArmadas(empresaId, archivo, modo, textoInicio, fecha, sedeGeneral ? Number(sedeGeneral) : null, token))
      setVista(null)
      setArchivo(null)
      alTerminar()
    } catch (error) {
      setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudieron crear las rutas.')
    } finally {
      setOcupado(false)
    }
  }

  const listoParaImportar = Boolean(vista?.puedeImportar) && !ocupado && fecha !== '' && (!vista?.faltaSede || sedeGeneral !== '')
  return { archivo, textoInicio, sedeGeneral, vista, resultado, mensajeError, ocupado, necesitaTexto, listoParaImportar, cambiarTexto, setSedeGeneral, elegirArchivo, importar }
}
