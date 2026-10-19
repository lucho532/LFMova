import { useState } from 'react'
import type { Sede } from '../modelos/sede'
import { ErrorApi } from '../servicios/clienteHttp'
import '../estilos/componentes/SedeEditable.css'

interface PropiedadesSedeEditable {
  /** Nombre de la sede que se muestra. */
  nombre: string
  /** Sede actual de la ruta, para no ofrecerla como opción. */
  sedeIdActual: number
  /** Sedes activas de la empresa entre las que se puede elegir. */
  sedes: Sede[]
  /** Cambia la sede de la ruta; si falla, su mensaje se muestra junto a la lista. */
  alCambiar: (sedeId: number) => Promise<void>
}

/**
 * Nombre de la sede de una ruta que, al pulsarlo, se convierte en una lista para elegir otra sede de la
 * empresa. Solo presenta la elección y delega el cambio; las reglas (ruta sin salir, unidad sin cruces, etc.)
 * las valida el backend y su mensaje se muestra aquí.
 */
export function SedeEditable({ nombre, sedeIdActual, sedes, alCambiar }: PropiedadesSedeEditable) {
  const [editando, setEditando] = useState(false)
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function elegir(valor: string) {
    if (!valor) return
    setGuardando(true)
    setError(null)
    try {
      await alCambiar(Number(valor))
      setEditando(false)
    } catch (excepcion) {
      setError(excepcion instanceof ErrorApi ? excepcion.message : 'No se pudo cambiar la sede.')
    } finally {
      setGuardando(false)
    }
  }

  if (!editando) {
    return (
      <button type="button" className="sede-editable__nombre" title="Cambiar la sede de esta ruta" onClick={() => setEditando(true)}>
        {nombre} <span aria-hidden="true">✎</span>
      </button>
    )
  }

  return (
    <span className="sede-editable">
      <select aria-label="Elegir otra sede" value="" disabled={guardando} autoFocus onChange={(evento) => elegir(evento.target.value)}>
        <option value="">{guardando ? 'Cambiando…' : 'Elige la sede…'}</option>
        {sedes
          .filter((sede) => sede.activa && sede.sedeId !== sedeIdActual)
          .map((sede) => (
            <option key={sede.sedeId} value={sede.sedeId}>
              {sede.nombre}
            </option>
          ))}
      </select>
      <button type="button" className="sede-editable__cancelar" onClick={() => setEditando(false)} disabled={guardando}>
        Cancelar
      </button>
      {error && <span className="sede-editable__error">{error}</span>}
    </span>
  )
}
