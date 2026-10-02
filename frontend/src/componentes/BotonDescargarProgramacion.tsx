import { useState } from 'react'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { descargarSoporteJornada } from '../servicios/servicioOperacion'
import { BotonSecundario } from './BotonSecundario'
import { MensajeAlerta } from './MensajeAlerta'
import '../estilos/componentes/BotonDescargarProgramacion.css'

interface PropiedadesBotonDescargarProgramacion {
  empresaId: number
  /** Jornadas que hay en pantalla: se descarga un Excel por cada una (lo normal es una sola). */
  jornadaIds: number[]
}

/** Entrega un archivo ya descargado al navegador para que lo guarde con el nombre indicado. */
function guardarArchivo(nombre: string, archivo: Blob) {
  const url = URL.createObjectURL(archivo)
  const enlace = document.createElement('a')
  enlace.href = url
  enlace.download = nombre
  document.body.appendChild(enlace)
  enlace.click()
  enlace.remove()
  URL.revokeObjectURL(url)
}

/**
 * Botón del coordinador para descargar en Excel toda la programación que
 * tiene en pantalla: las rutas de todos los conductores con sus pasajeros.
 * Solo descarga: no publica ni modifica nada.
 */
export function BotonDescargarProgramacion({ empresaId, jornadaIds }: PropiedadesBotonDescargarProgramacion) {
  const { token } = useAutenticacion()
  const [descargando, setDescargando] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function alDescargar() {
    if (!token) return
    setDescargando(true)
    setError(null)
    try {
      for (const jornadaId of jornadaIds) {
        const { nombre, archivo } = await descargarSoporteJornada(empresaId, jornadaId, token)
        guardarArchivo(nombre, archivo)
      }
    } catch (causa) {
      setError(causa instanceof Error ? causa.message : 'No se pudo descargar la programación.')
    } finally {
      setDescargando(false)
    }
  }

  return (
    <div className="boton-descargar-programacion">
      <BotonSecundario
        disabled={descargando || jornadaIds.length === 0}
        title={jornadaIds.length === 0 ? 'Todavía no hay rutas para descargar.' : undefined}
        onClick={alDescargar}
      >
        {descargando ? 'Descargando…' : '⬇ Descargar Excel'}
      </BotonSecundario>
      {error && <MensajeAlerta tipo="error">{error}</MensajeAlerta>}
    </div>
  )
}
