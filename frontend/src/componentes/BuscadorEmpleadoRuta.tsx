import { useMemo, useState } from 'react'
import { NOMBRES_TIPO_SERVICIO, formatearHora, nombreDe } from '../modelos/enumeraciones'
import type { PasajeroEmpresa } from '../modelos/estadisticas'
import './BuscadorEmpleadoRuta.css'

interface PropiedadesBuscadorEmpleadoRuta {
  pasajeros: PasajeroEmpresa[]
  alSeleccionar: (pasajero: PasajeroEmpresa) => void
}

const MAXIMO_SUGERENCIAS = 8

/**
 * Campo de búsqueda de un empleado por cédula: a medida que se escribe,
 * sugiere los empleados (y la ruta en la que participaron) cuya cédula
 * coincide, para no tener que revisar ruta por ruta.
 */
export function BuscadorEmpleadoRuta({ pasajeros, alSeleccionar }: PropiedadesBuscadorEmpleadoRuta) {
  const [texto, setTexto] = useState('')

  const sugerencias = useMemo(() => {
    const busqueda = texto.trim()
    if (busqueda.length < 2) return []
    return pasajeros
      .filter((p) => p.cedula.includes(busqueda))
      .sort((a, b) => `${b.fecha}${b.hora}`.localeCompare(`${a.fecha}${a.hora}`))
      .slice(0, MAXIMO_SUGERENCIAS)
  }, [pasajeros, texto])

  function seleccionar(pasajero: PasajeroEmpresa) {
    alSeleccionar(pasajero)
    setTexto('')
  }

  return (
    <div className="buscador-empleado-ruta">
      <label htmlFor="buscadorEmpleadoCedula">🔎 Buscar empleado por cédula</label>
      <input
        id="buscadorEmpleadoCedula"
        type="text"
        inputMode="numeric"
        placeholder="Escribe la cédula…"
        value={texto}
        onChange={(evento) => setTexto(evento.target.value)}
        autoComplete="off"
      />
      {sugerencias.length > 0 && (
        <ul className="buscador-empleado-ruta__sugerencias">
          {sugerencias.map((pasajero, indice) => (
            <li key={`${pasajero.servicioId}-${pasajero.cedula}-${indice}`}>
              <button type="button" onClick={() => seleccionar(pasajero)}>
                <strong>{pasajero.nombreCompleto}</strong>
                <span className="buscador-empleado-ruta__cedula">CC {pasajero.cedula}</span>
                <span className="buscador-empleado-ruta__ruta">
                  {pasajero.fecha} · {formatearHora(pasajero.hora)} · {nombreDe(NOMBRES_TIPO_SERVICIO, pasajero.tipo)} · {pasajero.sede}
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}
      {texto.trim().length >= 2 && sugerencias.length === 0 && <p className="buscador-empleado-ruta__sin-resultados">Ningún empleado coincide con esa cédula.</p>}
    </div>
  )
}
