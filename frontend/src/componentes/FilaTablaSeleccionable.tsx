import type { KeyboardEvent, ReactNode } from 'react'
import '../estilos/componentes/FilaTablaSeleccionable.css'

interface PropiedadesFilaTablaSeleccionable {
  /** Se invoca al hacer clic en la fila o al pulsar Enter / Espacio estando enfocada. */
  alSeleccionar: () => void
  children: ReactNode
}

/**
 * Fila de tabla que se puede abrir: al pasar el cursor (o enfocarla con el teclado) se adelanta, crece un poco
 * y se resalta en azul con una barra a la izquierda, para que se vea cuál se va a abrir. Es la misma para la
 * lista de rutas y la de inasistencias. Un botón dentro de la fila (por ejemplo, eliminar) debe detener la
 * propagación del clic para no abrirla. Solo presenta y avisa la selección: no decide qué se abre.
 */
export function FilaTablaSeleccionable({ alSeleccionar, children }: PropiedadesFilaTablaSeleccionable) {
  function alPulsarTecla(evento: KeyboardEvent<HTMLTableRowElement>) {
    // Solo si la tecla se pulsó sobre la propia fila: Enter sobre un botón interno no debe abrirla.
    if (evento.target !== evento.currentTarget) return
    if (evento.key === 'Enter' || evento.key === ' ') {
      evento.preventDefault()
      alSeleccionar()
    }
  }

  return (
    <tr className="fila-tabla-seleccionable" tabIndex={0} onClick={alSeleccionar} onKeyDown={alPulsarTecla}>
      {children}
    </tr>
  )
}
