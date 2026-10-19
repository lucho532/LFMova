import '../estilos/componentes/GraficaBarras.css'

export interface BarraGrafica {
  etiqueta: string
  valor: number
  detalle?: string
}

interface PropiedadesGraficaBarras {
  titulo: string
  barras: BarraGrafica[]
  textoVacio: string
}

/** Gráfica de barras verticales hecha con CSS; la barra más alta ocupa todo el alto disponible. */
export function GraficaBarras({ titulo, barras, textoVacio }: PropiedadesGraficaBarras) {
  const maximo = Math.max(1, ...barras.map((barra) => barra.valor))

  return (
    <section className="grafica-barras">
      <h3>{titulo}</h3>
      {barras.length === 0 ? (
        <p className="grafica-barras__vacio">{textoVacio}</p>
      ) : (
        <div className="grafica-barras__cuerpo">
          {barras.map((barra) => (
            <div key={barra.etiqueta} className="grafica-barras__columna" title={barra.detalle ?? `${barra.valor}`}>
              <span className="grafica-barras__valor">{barra.valor}</span>
              <div className="grafica-barras__barra" style={{ height: `${Math.max(6, (barra.valor / maximo) * 100)}%` }} />
              <span className="grafica-barras__etiqueta">{barra.etiqueta}</span>
            </div>
          ))}
        </div>
      )}
    </section>
  )
}
