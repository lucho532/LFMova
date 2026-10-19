import { useState, type FormEvent } from 'react'
import type { NumeroInternoUnidad } from '../modelos/conductor'
import { ErrorApi } from '../servicios/clienteHttp'
import { asignarNumeroInterno } from '../servicios/servicioConductores'
import '../estilos/componentes/CampoNumeroInternoUnidad.css'

interface PropiedadesCampoNumeroInternoUnidad {
  empresaId: number
  unidadOperativaId: number
  /** Números internos de la empresa; de ahí se toma el que ya tiene la unidad (vacío si no tiene). */
  numeros: NumeroInternoUnidad[]
  token: string
  /** Avisa tras guardar o quitar el número, para que la pantalla recargue. */
  alGuardar: () => void
}

/**
 * Campo para escribir el número interno que la empresa le da a una unidad (su vehículo), por ejemplo «103».
 * Se guarda con su propio botón; dejarlo vacío lo quita. Solo edita este dato de la empresa: no toca la placa
 * ni los demás datos del vehículo.
 */
export function CampoNumeroInternoUnidad({ empresaId, unidadOperativaId, numeros, token, alGuardar }: PropiedadesCampoNumeroInternoUnidad) {
  const numeroInicial = numeros.find((n) => n.unidadOperativaId === unidadOperativaId)?.numero ?? ''
  const [numero, setNumero] = useState(numeroInicial)
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const sinCambios = numero.trim() === numeroInicial

  async function guardar(evento: FormEvent) {
    evento.preventDefault()
    setGuardando(true)
    setError(null)
    try {
      await asignarNumeroInterno(empresaId, unidadOperativaId, numero, token)
      alGuardar()
    } catch (excepcion) {
      setError(excepcion instanceof ErrorApi ? excepcion.message : 'No se pudo guardar el número interno.')
    } finally {
      setGuardando(false)
    }
  }

  return (
    <form className="campo-numero-interno" onSubmit={guardar}>
      <input
        type="text"
        value={numero}
        maxLength={30}
        placeholder="Sin número"
        aria-label="Número interno de la empresa"
        onChange={(evento) => setNumero(evento.target.value)}
      />
      <button type="submit" disabled={guardando || sinCambios}>
        {guardando ? 'Guardando…' : 'Guardar'}
      </button>
      {error && <span className="campo-numero-interno__error">{error}</span>}
    </form>
  )
}
