import { useState } from 'react'
import { ErrorPermiso, obtenerUbicacionObligatoria, type PosicionGps } from '../servicios/ubicacion'

/**
 * Guarda el último permiso del dispositivo (ubicación o cámara) que faltó, para que la pantalla
 * muestre `AvisoPermiso` con el botón que lleva a la configuración. No hace la acción que necesitaba
 * el permiso: solo pide la ubicación y recuerda por qué no se pudo.
 */
export function usePermisoDispositivo() {
  const [errorPermiso, setErrorPermiso] = useState<ErrorPermiso | null>(null)

  /** Pide el punto GPS; si no se puede, deja el aviso visible y devuelve `null` para que quien llama no continúe. */
  async function pedirUbicacion(opciones?: { maximumAge?: number; timeout?: number }): Promise<PosicionGps | null> {
    try {
      const posicion = await obtenerUbicacionObligatoria(opciones)
      setErrorPermiso(null)
      return posicion
    } catch (error) {
      if (error instanceof ErrorPermiso) setErrorPermiso(error)
      return null
    }
  }

  /** Muestra el aviso si el error es de permiso y devuelve verdadero; con cualquier otro error devuelve falso. */
  function registrarFallo(error: unknown): boolean {
    if (!(error instanceof ErrorPermiso)) return false
    setErrorPermiso(error)
    return true
  }

  return { errorPermiso, pedirUbicacion, registrarFallo, limpiar: () => setErrorPermiso(null) }
}
