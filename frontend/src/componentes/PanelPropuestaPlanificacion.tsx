import { useState } from 'react'
import type { PropuestaPlanificacion, ServicioPasajero } from '../modelos/operacion'
import { BotonSecundario } from './BotonSecundario'
import { MensajeAlerta } from './MensajeAlerta'
import '../estilos/componentes/PanelPropuestaPlanificacion.css'

interface PropiedadesPanelPropuesta {
  pasajeros: ServicioPasajero[]
  obtenerPropuesta: () => Promise<PropuestaPlanificacion>
  aplicarOrden: (propuesta: PropuestaPlanificacion) => Promise<void>
}

/**
 * Muestra la propuesta de planificación del sistema. Es una recomendación de
 * lectura: se rotula siempre como propuesta y nada cambia hasta que el
 * coordinador decide aplicar el orden sugerido.
 */
export function PanelPropuestaPlanificacion({ pasajeros, obtenerPropuesta, aplicarOrden }: PropiedadesPanelPropuesta) {
  const [propuesta, setPropuesta] = useState<PropuestaPlanificacion | null>(null)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [ocupado, setOcupado] = useState(false)

  async function ejecutar(accion: () => Promise<void>) {
    setOcupado(true)
    setMensajeError(null)
    try {
      await accion()
    } catch (error) {
      setMensajeError(error instanceof Error ? error.message : 'No se pudo completar la operación.')
    } finally {
      setOcupado(false)
    }
  }

  const nombreDe = (servicioPasajeroId: number) => pasajeros.find((p) => p.servicioPasajeroId === servicioPasajeroId)?.nombreCompletoEmpleado ?? `#${servicioPasajeroId}`

  return (
    <section className="panel-propuesta">
      <div className="panel-propuesta__cabecera">
        <h3>Propuesta del sistema</h3>
        <BotonSecundario disabled={ocupado} onClick={() => ejecutar(async () => setPropuesta(await obtenerPropuesta()))}>
          {propuesta ? 'Actualizar propuesta' : 'Generar propuesta'}
        </BotonSecundario>
      </div>
      <p className="panel-propuesta__aviso">Es solo una recomendación. La decisión final es del coordinador.</p>

      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}

      {propuesta && (
        <>
          <ol className="panel-propuesta__lista">
            {[...propuesta.ordenPropuesto]
              .sort((a, b) => a.orden - b.orden)
              .map((p) => (
                <li key={p.servicioPasajeroId}>
                  {nombreDe(p.servicioPasajeroId)}
                  {p.distanciaALaSedeKm !== null && <span> · {p.distanciaALaSedeKm.toFixed(1)} km a la sede</span>}
                </li>
              ))}
          </ol>
          {propuesta.horaSugeridaInicioRecogida && <p>Inicio de recogida sugerido: {propuesta.horaSugeridaInicioRecogida.slice(0, 5)}</p>}
          {propuesta.horaLimiteLlegadaSede && <p>Hora límite de llegada a la sede: {propuesta.horaLimiteLlegadaSede.slice(0, 5)}</p>}
          {propuesta.advertencias.map((advertencia) => (
            <MensajeAlerta key={advertencia} tipo="error">
              {advertencia}
            </MensajeAlerta>
          ))}
          <BotonSecundario disabled={ocupado} onClick={() => ejecutar(() => aplicarOrden(propuesta))}>
            Aplicar el orden sugerido
          </BotonSecundario>
        </>
      )}
    </section>
  )
}
