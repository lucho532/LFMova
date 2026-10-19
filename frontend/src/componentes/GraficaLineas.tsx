import { useState } from 'react'
import '../estilos/componentes/GraficaLineas.css'

export interface PuntoGraficaLineas {
  etiqueta: string
  entradas: number
  salidas: number
}

interface PropiedadesGraficaLineas {
  puntos: PuntoGraficaLineas[]
  textoVacio: string
}

const ANCHO = 640
const ALTO = 240
const MARGEN = { izquierda: 40, derecha: 16, arriba: 14, abajo: 28 }
const MARCAS_VERTICALES = 4

/** Redondea el máximo hacia arriba a un valor "limpio" para que las marcas del eje vertical sean enteras. */
function techoDelEje(maximo: number): number {
  const paso = Math.max(1, Math.ceil(maximo / MARCAS_VERTICALES))
  return paso * MARCAS_VERTICALES
}

/**
 * Gráfica de líneas con dos series (entradas y salidas) por día, dibujada en SVG. Al pasar el cursor por una
 * columna muestra el valor de ambas series. No consulta datos: solo dibuja los puntos recibidos.
 */
export function GraficaLineas({ puntos, textoVacio }: PropiedadesGraficaLineas) {
  const [activo, setActivo] = useState<number | null>(null)
  const hayDatos = puntos.some((p) => p.entradas > 0 || p.salidas > 0)

  if (!hayDatos) return <p className="grafica-lineas__vacio">{textoVacio}</p>

  const techo = techoDelEje(Math.max(...puntos.map((p) => Math.max(p.entradas, p.salidas))))
  const anchoUtil = ANCHO - MARGEN.izquierda - MARGEN.derecha
  const altoUtil = ALTO - MARGEN.arriba - MARGEN.abajo
  const x = (i: number) => MARGEN.izquierda + (puntos.length === 1 ? anchoUtil / 2 : (i / (puntos.length - 1)) * anchoUtil)
  const y = (valor: number) => MARGEN.arriba + altoUtil - (valor / techo) * altoUtil
  const linea = (clave: 'entradas' | 'salidas') => puntos.map((p, i) => `${x(i)},${y(p[clave])}`).join(' ')
  // Con muchos días solo se rotulan algunos para que las etiquetas no se encimen.
  const saltoEtiqueta = Math.ceil(puntos.length / 8)
  const seleccionado = activo === null ? null : puntos[activo]

  return (
    <div className="grafica-lineas">
      <div className="grafica-lineas__leyenda">
        <span className="grafica-lineas__serie grafica-lineas__serie--entradas">Entradas</span>
        <span className="grafica-lineas__serie grafica-lineas__serie--salidas">Salidas</span>
      </div>
      <svg viewBox={`0 0 ${ANCHO} ${ALTO}`} role="img" aria-label="Pasajeros transportados por día, entradas y salidas" onMouseLeave={() => setActivo(null)}>
        {Array.from({ length: MARCAS_VERTICALES + 1 }, (_, i) => {
          const valor = (techo / MARCAS_VERTICALES) * i
          return (
            <g key={valor}>
              <line className="grafica-lineas__rejilla" x1={MARGEN.izquierda} x2={ANCHO - MARGEN.derecha} y1={y(valor)} y2={y(valor)} />
              <text className="grafica-lineas__texto" x={MARGEN.izquierda - 6} y={y(valor) + 4} textAnchor="end">{valor}</text>
            </g>
          )
        })}
        {puntos.map((p, i) => (
          i % saltoEtiqueta === 0 && <text key={p.etiqueta} className="grafica-lineas__texto" x={x(i)} y={ALTO - 8} textAnchor="middle">{p.etiqueta}</text>
        ))}
        <polyline className="grafica-lineas__linea grafica-lineas__linea--entradas" points={linea('entradas')} />
        <polyline className="grafica-lineas__linea grafica-lineas__linea--salidas" points={linea('salidas')} />
        {activo !== null && seleccionado && (
          <g>
            <line className="grafica-lineas__guia" x1={x(activo)} x2={x(activo)} y1={MARGEN.arriba} y2={ALTO - MARGEN.abajo} />
            <circle className="grafica-lineas__punto grafica-lineas__punto--entradas" cx={x(activo)} cy={y(seleccionado.entradas)} r={4} />
            <circle className="grafica-lineas__punto grafica-lineas__punto--salidas" cx={x(activo)} cy={y(seleccionado.salidas)} r={4} />
          </g>
        )}
        {puntos.map((p, i) => (
          <rect
            key={p.etiqueta}
            className="grafica-lineas__zona"
            x={x(i) - anchoUtil / puntos.length / 2}
            y={MARGEN.arriba}
            width={anchoUtil / puntos.length}
            height={altoUtil}
            onMouseEnter={() => setActivo(i)}
          />
        ))}
      </svg>
      <p className="grafica-lineas__detalle" aria-live="polite">
        {seleccionado
          ? `${seleccionado.etiqueta}: ${seleccionado.entradas} en entradas · ${seleccionado.salidas} en salidas`
          : 'Pasa el cursor sobre la gráfica para ver el detalle de cada día.'}
      </p>
    </div>
  )
}
