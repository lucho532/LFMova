import { useEffect, useRef, useState, type ReactNode } from 'react'
import {
  APLICACIONES_NAVEGACION,
  enlaceNavegacion,
  guardarAppPredeterminada,
  nombreAppNavegacion,
  obtenerAppPredeterminada,
  type AppNavegacion,
} from '../servicios/navegacion'
import { BotonPrimario } from './BotonPrimario'
import { BotonSecundario } from './BotonSecundario'
import '../estilos/componentes/BotonNavegar.css'

interface PropiedadesBotonNavegar {
  destino: { latitud: number; longitud: number }
  /** Clase de la pantalla que lo usa, para que el botón se vea como el resto de sus acciones. */
  className?: string
  children: ReactNode
}

/**
 * Botón que abre la navegación hasta un punto. Si el conductor ya dejó una
 * aplicación predeterminada, la abre directamente; si no, le deja elegir
 * entre las disponibles y luego le pregunta si quiere usarla siempre. No
 * obtiene ni guarda ubicaciones: solo abre la aplicación de mapas.
 */
export function BotonNavegar({ destino, className, children }: PropiedadesBotonNavegar) {
  const referencia = useRef<HTMLDialogElement>(null)
  const [abierto, setAbierto] = useState(false)
  const [elegida, setElegida] = useState<AppNavegacion | null>(null)

  useEffect(() => {
    const dialogo = referencia.current
    if (!dialogo) return
    if (abierto && !dialogo.open) dialogo.showModal()
    if (!abierto && dialogo.open) dialogo.close()
  }, [abierto])

  function abrir(app: AppNavegacion) {
    window.open(enlaceNavegacion(destino, app), '_blank', 'noopener,noreferrer')
  }

  function alTocar() {
    const predeterminada = obtenerAppPredeterminada()
    if (predeterminada) {
      abrir(predeterminada)
      return
    }
    setElegida(null)
    setAbierto(true)
  }

  function alResponder(usarSiempre: boolean) {
    if (!elegida) return
    if (usarSiempre) guardarAppPredeterminada(elegida)
    setAbierto(false)
    abrir(elegida)
  }

  return (
    <>
      <button type="button" className={className} onClick={alTocar}>
        {children}
      </button>

      <dialog ref={referencia} className="boton-navegar__dialogo" onCancel={() => setAbierto(false)} onClose={() => setAbierto(false)}>
        {elegida === null ? (
          <>
            <h2>¿Con qué quieres navegar?</h2>
            <div className="boton-navegar__opciones">
              {APLICACIONES_NAVEGACION.map((app) => (
                <BotonPrimario key={app.valor} type="button" onClick={() => setElegida(app.valor)}>
                  {app.nombre}
                </BotonPrimario>
              ))}
              <BotonSecundario onClick={() => setAbierto(false)}>Cancelar</BotonSecundario>
            </div>
          </>
        ) : (
          <>
            <h2>¿Usar siempre {nombreAppNavegacion(elegida)}?</h2>
            <p>Si eliges «Sí, siempre», no te lo volveremos a preguntar. Puedes cambiarlo después en Mi cuenta.</p>
            <div className="boton-navegar__opciones">
              <BotonPrimario type="button" onClick={() => alResponder(true)}>
                Sí, siempre
              </BotonPrimario>
              <BotonSecundario onClick={() => alResponder(false)}>Solo esta vez</BotonSecundario>
            </div>
          </>
        )}
      </dialog>
    </>
  )
}
