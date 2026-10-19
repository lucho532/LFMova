import type { OpcionPeriodo } from '../hooks/usePeriodoPanorama'
import '../estilos/componentes/SelectorPeriodo.css'

interface PropiedadesSelectorPeriodo {
  opcion: OpcionPeriodo
  desde: string
  hasta: string
  alElegirOpcion: (opcion: OpcionPeriodo) => void
  alCambiarDesde: (fecha: string) => void
  alCambiarHasta: (fecha: string) => void
}

const OPCIONES: { valor: OpcionPeriodo; texto: string }[] = [
  { valor: '7', texto: '7 días' },
  { valor: '30', texto: '30 días' },
  { valor: 'personalizado', texto: 'Personalizado' }
]

/** Filtro de periodo: dos atajos (7 y 30 días) y un rango propio con dos fechas. Solo notifica la elección; no consulta datos. */
export function SelectorPeriodo({ opcion, desde, hasta, alElegirOpcion, alCambiarDesde, alCambiarHasta }: PropiedadesSelectorPeriodo) {
  return (
    <div className="selector-periodo">
      <div className="selector-periodo__opciones" role="group" aria-label="Periodo">
        {OPCIONES.map((o) => (
          <button
            key={o.valor}
            type="button"
            className={`selector-periodo__opcion${o.valor === opcion ? ' selector-periodo__opcion--activa' : ''}`}
            aria-pressed={o.valor === opcion}
            onClick={() => alElegirOpcion(o.valor)}
          >
            {o.texto}
          </button>
        ))}
      </div>
      {opcion === 'personalizado' && (
        <div className="selector-periodo__fechas">
          <label>
            Desde
            <input type="date" value={desde} max={hasta || undefined} onChange={(e) => alCambiarDesde(e.target.value)} />
          </label>
          <label>
            Hasta
            <input type="date" value={hasta} min={desde || undefined} onChange={(e) => alCambiarHasta(e.target.value)} />
          </label>
        </div>
      )}
    </div>
  )
}
