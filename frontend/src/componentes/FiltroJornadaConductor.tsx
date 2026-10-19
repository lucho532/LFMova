import { BotonSecundario } from './BotonSecundario'
import { SelectorFormulario } from './SelectorFormulario'

interface PropiedadesFiltroJornadaConductor {
  /** Nombres de los conductores presentes en las rutas cargadas. */
  conductores: string[]
  /** Conductor elegido; cadena vacía significa todos. */
  conductor: string
  alCambiarConductor: (conductor: string) => void
  /** Fija el rango de fechas a la jornada de hoy (toda la jornada, todas las rutas). */
  alElegirJornadaHoy: () => void
}

/**
 * Atajos del resumen de rutas del coordinador: ver toda la jornada de hoy y limitar la lista a un
 * solo conductor. Solo dibuja los controles; el filtrado lo hace la página que lo usa.
 */
export function FiltroJornadaConductor({ conductores, conductor, alCambiarConductor, alElegirJornadaHoy }: PropiedadesFiltroJornadaConductor) {
  return (
    <>
      <BotonSecundario type="button" onClick={alElegirJornadaHoy}>
        Toda la jornada de hoy
      </BotonSecundario>
      <SelectorFormulario
        id="rutasConductor"
        etiqueta="Conductor"
        valor={conductor}
        opciones={conductores.map((nombre) => ({ valor: nombre, texto: nombre }))}
        alCambiar={alCambiarConductor}
        textoVacio="Todos los conductores"
      />
    </>
  )
}
