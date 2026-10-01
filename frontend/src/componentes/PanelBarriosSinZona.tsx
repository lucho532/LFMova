import { useEffect, useState } from 'react'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { Zona } from '../modelos/zona'
import { agregarBarrioAZona, crearZona, obtenerZonas } from '../servicios/servicioZonas'
import { BotonPrimario } from './BotonPrimario'
import { CampoFormulario } from './CampoFormulario'
import { MensajeAlerta } from './MensajeAlerta'
import { SelectorFormulario } from './SelectorFormulario'
import '../estilos/componentes/PanelBarriosSinZona.css'

const OPCION_ZONA_NUEVA = '__nueva__'

interface PropiedadesPanelBarriosSinZona {
  empresaId: number
  barrios: string[]
  /** Se llama con el barrio recién resuelto (agregado a una zona o con una zona nueva creada para él). */
  alResolver: (barrio: string) => void
}

/**
 * Lista los barrios del archivo que no coinciden con ninguna zona activa
 * (así que el reparto automático los deja sin agrupar) y deja resolver cada
 * uno con una acción concreta: agregarlo como alias de una zona ya existente,
 * o crear una zona nueva para él. No decide por sí solo cuál zona es la
 * correcta: siempre lo elige el coordinador.
 */
export function PanelBarriosSinZona({ empresaId, barrios, alResolver }: PropiedadesPanelBarriosSinZona) {
  const { token } = useAutenticacion()
  const [zonas, setZonas] = useState<Zona[]>([])
  const [zonaElegida, setZonaElegida] = useState<Record<string, string>>({})
  const [nombreZonaNueva, setNombreZonaNueva] = useState<Record<string, string>>({})
  const [guardando, setGuardando] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!token || barrios.length === 0) return
    obtenerZonas(empresaId, token).then(setZonas).catch(() => setZonas([]))
  }, [empresaId, token, barrios.length])

  if (barrios.length === 0) return null

  async function resolverBarrio(barrio: string) {
    if (!token) return
    const zonaId = zonaElegida[barrio]
    setError(null)
    setGuardando(barrio)
    try {
      if (zonaId === OPCION_ZONA_NUEVA || !zonaId) {
        const nombre = (nombreZonaNueva[barrio] ?? '').trim()
        if (!nombre) {
          setError(`Escribe un nombre para la zona nueva de "${barrio}".`)
          return
        }
        await crearZona(empresaId, { nombre, barrios: [barrio] }, token)
      } else {
        await agregarBarrioAZona(empresaId, Number(zonaId), barrio, token)
      }
      alResolver(barrio)
    } catch {
      setError(`No se pudo resolver el barrio "${barrio}".`)
    } finally {
      setGuardando(null)
    }
  }

  return (
    <section className="barrios-sin-zona">
      <h3>⚠ {barrios.length} barrio{barrios.length === 1 ? '' : 's'} sin zona asignada</h3>
      <p className="barrios-sin-zona__ayuda">
        Estas personas se repartirán sin agrupar por zona ni corredor. Asigna cada barrio a una zona ya existente (si es el mismo
        lugar con otro nombre) o crea una zona nueva para él.
      </p>
      {error && <MensajeAlerta tipo="error">{error}</MensajeAlerta>}
      <div className="barrios-sin-zona__lista">
        {barrios.map((barrio) => {
          const eligiendoZonaNueva = zonaElegida[barrio] === OPCION_ZONA_NUEVA
          return (
            <div key={barrio} className="barrios-sin-zona__fila">
              <span className="barrios-sin-zona__nombre">{barrio}</span>
              <SelectorFormulario
                id={`zonaPara-${barrio}`}
                etiqueta="Asignar a"
                valor={zonaElegida[barrio] ?? ''}
                alCambiar={(valor) => setZonaElegida((anterior) => ({ ...anterior, [barrio]: valor }))}
                opciones={[
                  ...zonas.map((z) => ({ valor: String(z.zonaId), texto: z.nombre })),
                  { valor: OPCION_ZONA_NUEVA, texto: '+ Crear zona nueva' },
                ]}
                textoVacio="Elige una zona"
              />
              {eligiendoZonaNueva && (
                <CampoFormulario
                  id={`nombreZonaNueva-${barrio}`}
                  etiqueta="Nombre de la zona nueva"
                  valor={nombreZonaNueva[barrio] ?? ''}
                  alCambiar={(valor) => setNombreZonaNueva((anterior) => ({ ...anterior, [barrio]: valor }))}
                />
              )}
              <BotonPrimario type="button" disabled={guardando === barrio || !zonaElegida[barrio]} onClick={() => resolverBarrio(barrio)}>
                {guardando === barrio ? 'Guardando…' : 'Aplicar'}
              </BotonPrimario>
            </div>
          )
        })}
      </div>
    </section>
  )
}
