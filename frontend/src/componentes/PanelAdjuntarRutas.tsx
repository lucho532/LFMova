import { useState, type ReactNode } from 'react'
import type { ModoSeparacionRutas } from '../modelos/importacion'
import type { Sede } from '../modelos/sede'
import { CampoFormulario } from './CampoFormulario'
import { PanelRutasArmadas } from './PanelRutasArmadas'
import { SelectorFormulario } from './SelectorFormulario'
import '../estilos/componentes/PanelAdjuntarRutas.css'

interface PropiedadesPanelAdjuntarRutas {
  empresaId: number
  sedes: Sede[]
  /** Fecha de las rutas que se van a crear (vista general): la usan todas las opciones que no la traen en el archivo. */
  fecha: string
  alCambiarFecha: (fecha: string) => void
  /** Avisa al crear rutas armadas, para que la pantalla recargue las tarjetas. */
  alTerminar: () => void
  /** Contenido de la opción «Excel de pasajeros»: el que la app reparte entre las unidades. */
  repartir: ReactNode
  /** Contenido de la opción «Pegar filas»: crear una ruta pegando filas copiadas de un Excel. */
  pegado: ReactNode
}

type OpcionAdjuntar = '' | 'REPARTIR' | 'PEGADO' | ModoSeparacionRutas

const OPCIONES = [
  { valor: 'REPARTIR', texto: 'Excel de pasajeros: la app arma y reparte las rutas' },
  { valor: 'COLUMNA_NUMERO_RUTA', texto: 'Rutas ya armadas: cada fila trae su número de ruta' },
  { valor: 'LINEA_INICIO', texto: 'Rutas ya armadas: una línea de texto abre cada ruta' },
  { valor: 'PEGADO', texto: 'Crear una ruta pegando filas de Excel' },
]

/**
 * Desprendible para adjuntar rutas. Arriba, la vista general (la fecha de las rutas) y un desplegable con las
 * cuatro formas de traerlas; solo se muestra la elegida y, mientras no se elija ninguna, no se ve ninguna.
 * Elige y muestra: no lee ni guarda archivos.
 */
export function PanelAdjuntarRutas({ empresaId, sedes, fecha, alCambiarFecha, alTerminar, repartir, pegado }: PropiedadesPanelAdjuntarRutas) {
  const [opcion, setOpcion] = useState<OpcionAdjuntar>('')

  return (
    <details className="panel-adjuntar-rutas" open>
      <summary>Adjuntar rutas</summary>
      <div className="panel-adjuntar-rutas__general">
        <CampoFormulario id="fecha-rutas" etiqueta="Fecha de las rutas" tipo="date" valor={fecha} alCambiar={alCambiarFecha} requerido />
        <SelectorFormulario
          id="opcion-adjuntar-rutas"
          etiqueta="¿Cómo traes las rutas?"
          valor={opcion}
          opciones={OPCIONES}
          alCambiar={(valor) => setOpcion(valor as OpcionAdjuntar)}
          textoVacio="Elige una opción…"
        />
      </div>
      {opcion === 'REPARTIR' && repartir}
      {opcion === 'PEGADO' && pegado}
      {(opcion === 'COLUMNA_NUMERO_RUTA' || opcion === 'LINEA_INICIO') && (
        <PanelRutasArmadas key={opcion} empresaId={empresaId} modo={opcion} sedes={sedes} fecha={fecha} alCambiarFecha={alCambiarFecha} alTerminar={alTerminar} />
      )}
    </details>
  )
}
