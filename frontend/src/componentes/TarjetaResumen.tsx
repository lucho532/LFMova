import { Link } from 'react-router-dom'
import './TarjetaResumen.css'

interface PropiedadesTarjetaResumen {
  titulo: string
  valor: number | string
  detalle?: string
  destacada?: boolean
  /** Si se indica, toda la tarjeta es un enlace a esa ruta con el detalle de lo que cuenta. */
  a?: string
}

/** Indicador numérico con título; se usa en las cabeceras de las pantallas de listado. Puede ser un enlace al detalle. */
export function TarjetaResumen({ titulo, valor, detalle, destacada, a }: PropiedadesTarjetaResumen) {
  const clases = `tarjeta-resumen${destacada ? ' tarjeta-resumen--destacada' : ''}${a ? ' tarjeta-resumen--enlace' : ''}`
  const contenido = (
    <>
      <span className="tarjeta-resumen__titulo">{titulo}</span>
      <strong className="tarjeta-resumen__valor">{valor}</strong>
      {detalle && <span className="tarjeta-resumen__detalle">{detalle}</span>}
      {a && <span className="tarjeta-resumen__ver">Ver detalle →</span>}
    </>
  )

  return a ? (
    <Link to={a} className={clases}>
      {contenido}
    </Link>
  ) : (
    <div className={clases}>{contenido}</div>
  )
}
