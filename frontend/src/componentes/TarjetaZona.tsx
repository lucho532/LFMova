import type { DragEvent } from 'react'
import type { Zona } from '../modelos/zona'
import { BotonPrimario } from './BotonPrimario'
import { BotonSecundario } from './BotonSecundario'
import '../estilos/componentes/TarjetaZona.css'

/** Lo que se está moviendo: un barrio suelto o, si no hay `barrio`, la zona entera. */
export interface SeleccionZona {
  zonaId: number
  barrio?: string
}

interface PropiedadesTarjetaZona {
  zona: Zona
  /** Barrio o zona que se está moviendo (por arrastre o por selección con un toque), si hay alguno. */
  seleccion: SeleccionZona | null
  /** Indica que lo arrastrado está encima de esta tarjeta. */
  esDestinoDelArrastre: boolean
  ocupada: boolean
  alSeleccionar: (seleccion: SeleccionZona) => void
  alEmpezarArrastre: (evento: DragEvent, seleccion: SeleccionZona) => void
  alTerminarArrastre: () => void
  alPasarSobre: (evento: DragEvent, zona: Zona) => void
  alSalir: (evento: DragEvent<HTMLElement>) => void
  alSoltar: (zona: Zona) => void
  alEditar: (zona: Zona) => void
  alCambiarEstado: (zona: Zona) => void
  alEliminar: (zona: Zona) => void
}

/**
 * Tarjeta de una zona registrada: su nombre, sus datos, sus barrios como
 * etiquetas y sus acciones. Un barrio o la zona entera se pueden mover a otra
 * tarjeta arrastrándolos, o tocándolos y luego tocando "Mover aquí" en la
 * tarjeta de destino (para pantallas táctiles). No decide qué pasa al soltar:
 * avisa a quien la usa.
 */
export function TarjetaZona(p: PropiedadesTarjetaZona) {
  const { zona, seleccion } = p
  const esOrigen = seleccion?.zonaId === zona.zonaId
  const puedeRecibir = seleccion !== null && !esOrigen
  const clases = ['tarjeta-zona', p.esDestinoDelArrastre && 'tarjeta-zona--destino', esOrigen && !seleccion?.barrio && 'tarjeta-zona--origen', !zona.activa && 'tarjeta-zona--inactiva']
    .filter(Boolean)
    .join(' ')

  return (
    <li
      className={clases}
      onDragOver={(evento) => p.alPasarSobre(evento, zona)}
      onDragLeave={p.alSalir}
      onDrop={(evento) => {
        evento.preventDefault()
        p.alSoltar(zona)
      }}
    >
      <header className="tarjeta-zona__cabecera">
        <h3 draggable onDragStart={(evento) => p.alEmpezarArrastre(evento, { zonaId: zona.zonaId })} onDragEnd={p.alTerminarArrastre} title="Arrastra sobre otra zona para unirlas">
          <span aria-hidden="true">⠿</span> {zona.nombre}
        </h3>
        <span className={`tarjeta-zona__estado${zona.activa ? ' tarjeta-zona__estado--activa' : ''}`}>{zona.activa ? 'Activa' : 'Inactiva'}</span>
      </header>

      <p className="tarjeta-zona__datos">
        Macrozona: <strong>{zona.macroZonaNombre ?? '—'}</strong> · Corredor: <strong>{zona.corredorVialNombre ?? '—'}</strong>
      </p>

      {zona.barrios.length === 0 ? (
        <p className="tarjeta-zona__datos">Sin barrios.</p>
      ) : (
        <ul className="tarjeta-zona__barrios">
          {zona.barrios.map((barrio) => (
            <li
              key={barrio}
              role="button"
              tabIndex={0}
              draggable
              className={esOrigen && seleccion?.barrio === barrio ? 'tarjeta-zona__barrio--elegido' : undefined}
              onClick={() => p.alSeleccionar({ zonaId: zona.zonaId, barrio })}
              onKeyDown={(evento) => evento.key === 'Enter' && p.alSeleccionar({ zonaId: zona.zonaId, barrio })}
              onDragStart={(evento) => p.alEmpezarArrastre(evento, { zonaId: zona.zonaId, barrio })}
              onDragEnd={p.alTerminarArrastre}
              title="Arrástralo a otra zona, o tócalo y elige la zona de destino"
            >
              {barrio}
            </li>
          ))}
        </ul>
      )}

      <div className="tarjeta-zona__acciones">
        {puedeRecibir ? (
          <BotonPrimario type="button" onClick={() => p.alSoltar(zona)}>
            {seleccion?.barrio ? `Mover "${seleccion.barrio}" aquí` : 'Unir con esta zona'}
          </BotonPrimario>
        ) : (
          <>
            <BotonSecundario onClick={() => p.alEditar(zona)}>Editar</BotonSecundario>
            <BotonSecundario onClick={() => p.alSeleccionar({ zonaId: zona.zonaId })} disabled={p.ocupada}>Unir con…</BotonSecundario>
            <BotonSecundario onClick={() => p.alCambiarEstado(zona)} disabled={p.ocupada}>{zona.activa ? 'Desactivar' : 'Activar'}</BotonSecundario>
            <BotonSecundario onClick={() => p.alEliminar(zona)} disabled={p.ocupada}>Eliminar</BotonSecundario>
          </>
        )}
      </div>
    </li>
  )
}
