import type { PointerEvent } from 'react'
import type { Zona } from '../modelos/zona'
import { BotonPrimario } from './BotonPrimario'
import { BotonSecundario } from './BotonSecundario'
import '../estilos/componentes/TarjetaZona.css'

/**
 * Lo que se está moviendo: un barrio suelto (`barrio`), la zona entera para
 * cambiarla de lugar en la lista (`ordenar`) o, si no hay ninguno de los dos,
 * la zona entera para unirla con otra.
 */
export interface SeleccionZona {
  zonaId: number
  barrio?: string
  ordenar?: boolean
}

interface PropiedadesTarjetaZona {
  zona: Zona
  /** Barrio o zona elegidos con un toque y que esperan destino, si hay alguno. */
  seleccion: SeleccionZona | null
  /** Lo que se está arrastrando con el ratón, si hay algo. */
  arrastrado: SeleccionZona | null
  /** Indica que lo arrastrado está encima de esta tarjeta. */
  esDestinoDelArrastre: boolean
  ocupada: boolean
  alSeleccionar: (seleccion: SeleccionZona) => void
  alPresionar: (evento: PointerEvent, seleccion: SeleccionZona) => void
  alElegirDestino: (zona: Zona) => void
  alEditar: (zona: Zona) => void
  alCambiarEstado: (zona: Zona) => void
  alEliminar: (zona: Zona) => void
}

/**
 * Tarjeta de una zona registrada: su nombre, sus datos, sus barrios como
 * etiquetas y sus acciones. Con el ratón se arrastra un barrio a otra tarjeta
 * (lo mueve), el nombre sobre otra tarjeta (las une) o la tarjeta desde
 * cualquier parte vacía (la cambia de lugar). En pantallas táctiles se toca
 * el barrio o "Unir con…" y luego el botón de la tarjeta de destino. No
 * decide qué pasa al soltar: avisa a quien la usa.
 */
export function TarjetaZona(p: PropiedadesTarjetaZona) {
  const { zona, seleccion, arrastrado } = p
  const esOrigen = seleccion?.zonaId === zona.zonaId
  const puedeRecibir = seleccion !== null && !esOrigen
  const clases = [
    'tarjeta-zona',
    p.esDestinoDelArrastre && (arrastrado?.ordenar ? 'tarjeta-zona--hueco' : 'tarjeta-zona--destino'),
    arrastrado?.zonaId === zona.zonaId && !arrastrado.barrio && 'tarjeta-zona--arrastrada',
    esOrigen && !seleccion?.barrio && 'tarjeta-zona--origen',
    !zona.activa && 'tarjeta-zona--inactiva',
  ]
    .filter(Boolean)
    .join(' ')

  function alPresionarTarjeta(evento: PointerEvent) {
    // Los botones conservan su clic normal; el resto de la tarjeta sirve para cambiarla de lugar.
    if ((evento.target as HTMLElement).closest('button')) return
    p.alPresionar(evento, { zonaId: zona.zonaId, ordenar: true })
  }

  return (
    <li className={clases} data-zona-id={zona.zonaId} onPointerDown={alPresionarTarjeta} title="Arrastra la tarjeta para cambiarla de lugar">
      <header className="tarjeta-zona__cabecera">
        <h3 onPointerDown={(evento) => p.alPresionar(evento, { zonaId: zona.zonaId })} title="Arrastra el nombre sobre otra zona para unirlas">
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
              className={esOrigen && seleccion?.barrio === barrio ? 'tarjeta-zona__barrio--elegido' : undefined}
              onClick={() => p.alSeleccionar({ zonaId: zona.zonaId, barrio })}
              onKeyDown={(evento) => evento.key === 'Enter' && p.alSeleccionar({ zonaId: zona.zonaId, barrio })}
              onPointerDown={(evento) => p.alPresionar(evento, { zonaId: zona.zonaId, barrio })}
              title="Arrástralo a otra zona, o tócalo y elige la zona de destino"
            >
              {barrio}
            </li>
          ))}
        </ul>
      )}

      <div className="tarjeta-zona__acciones">
        {puedeRecibir ? (
          <BotonPrimario type="button" onClick={() => p.alElegirDestino(zona)}>
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
