import { useEffect, useState } from 'react'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { obtenerZonas } from '../servicios/servicioZonas'

/**
 * Conjunto de barrios (normalizados) que pertenecen a una zona clasificada bajo la macrozona
 * "Villamaría". Manizales y Villamaría comparten nombres de barrio parecidos o iguales, así que
 * las pantallas que listan pasajeros aclaran la ciudad junto al barrio cuando corresponde.
 */
export function useBarriosVillamaria(empresaId: number): Set<string> {
  const { token } = useAutenticacion()
  const [barrios, setBarrios] = useState<Set<string>>(new Set())

  useEffect(() => {
    if (!token) return
    let cancelado = false
    obtenerZonas(empresaId, token)
      .then((zonas) => {
        if (cancelado) return
        const deVillamaria = zonas
          .filter((z) => z.macroZonaNombre?.toLowerCase() === 'villamaría')
          .flatMap((z) => z.barrios)
        setBarrios(new Set(deVillamaria.map(normalizar)))
      })
      .catch(() => {
        if (!cancelado) setBarrios(new Set())
      })
    return () => {
      cancelado = true
    }
  }, [empresaId, token])

  return barrios
}

/**
 * Nombre del barrio con "(Villamaría)" al lado si ese barrio pertenece a esa ciudad. Si se conoce la
 * dirección y pide un cruce de calle y carrera que no existe en Villamaría (mismo criterio que
 * ReglasNomenclaturaVillamaria en el backend), no se marca: es un barrio con el mismo nombre en Manizales.
 */
export function formatearBarrioConCiudad(barrio: string | null | undefined, barriosVillamaria: Set<string>, direccion?: string): string {
  if (!barrio) return barrio ?? ''
  if (!barriosVillamaria.has(normalizar(barrio))) return barrio
  return direccionPuedeSerDeVillamaria(direccion) === false ? barrio : `${barrio} (Villamaría)`
}

/** Sectores de Villamaría: calles y carreras que existen en cada uno (ver ReglasNomenclaturaVillamaria en el backend). */
const SECTORES_VILLAMARIA = [
  { calles: [1, 22], carreras: [1, 20] }, // casco urbano
  { calles: [40, 75], carreras: [30, 75] }, // sector oriental, con la numeración de Manizales
]

/** `true`/`false` si el cruce de calle y carrera de la dirección existe o no en Villamaría; `undefined` si no se puede saber. */
function direccionPuedeSerDeVillamaria(direccion?: string): boolean | undefined {
  if (!direccion) return undefined
  const sinTildes = direccion.toUpperCase().normalize('NFD').replace(/[̀-ͯ]/g, '')
  const m = /^\s*(CALLE|CLL|CL|CARRERA|CRA|KRA|KR|CR)\.?\s*(\d+)[^#]*?(?:#|N\s*[O°º]\.?|NO\.?)\s*(\d+)/.exec(sinTildes)
  if (!m) return undefined
  const esCalle = m[1] === 'CALLE' || m[1] === 'CLL' || m[1] === 'CL'
  const [calle, carrera] = esCalle ? [Number(m[2]), Number(m[3])] : [Number(m[3]), Number(m[2])]
  return SECTORES_VILLAMARIA.some(
    (s) => calle >= s.calles[0] && calle <= s.calles[1] && carrera >= s.carreras[0] && carrera <= s.carreras[1],
  )
}

function normalizar(texto: string): string {
  return texto.trim().toUpperCase()
}
