import { useEffect, useRef, useState } from 'react'
import { useAutenticacion } from '../contexto/useAutenticacion'
import { registrarDuracionLlamada, registrarLlamada, type ReferenciaPasajero } from '../servicios/servicioLlamadas'
import { HistorialLlamadas } from './HistorialLlamadas'
import '../estilos/componentes/BotonLlamar.css'

interface PropiedadesBotonLlamar {
  pasajero: ReferenciaPasajero
  nombre: string
  telefono: string
  /** Clase del botón que abre el menú, para que herede el estilo de la tarjeta donde va. */
  className?: string
}

/** Espera máxima antes de abrir el marcador: registrar la llamada nunca debe retrasar la llamada. */
const ESPERA_MAXIMA_MS = 3000

/** Resuelve con el resultado de la promesa o con `null` si falla o tarda más de lo indicado. */
function conLimite<T>(promesa: Promise<T>, milisegundos: number): Promise<T | null> {
  return Promise.race([promesa.catch(() => null), new Promise<null>((resolver) => setTimeout(() => resolver(null), milisegundos))])
}

/** Última ubicación conocida del conductor (sin esperar un punto GPS nuevo), o `null` si no hay. */
function ubicacionRapida(): Promise<GeolocationPosition | null> {
  return new Promise((resolver) => {
    if (!navigator.geolocation) {
      resolver(null)
      return
    }
    navigator.geolocation.getCurrentPosition(resolver, () => resolver(null), { maximumAge: 120000, timeout: 1500 })
  })
}

/**
 * Botón "Llamar" del conductor. Abre un menú con dos opciones: llamar al
 * pasajero o ver el historial de llamadas. Al llamar deja constancia en el
 * servidor (hora y ubicación) antes de abrir el marcador del teléfono y, al
 * volver a la aplicación, guarda cuánto tiempo estuvo fuera como duración
 * aproximada. No puede saber si el pasajero contestó.
 */
export function BotonLlamar({ pasajero, nombre, telefono, className }: PropiedadesBotonLlamar) {
  const { token } = useAutenticacion()
  const dialogo = useRef<HTMLDialogElement>(null)
  const [vista, setVista] = useState<'cerrado' | 'menu' | 'historial'>('cerrado')
  const [llamando, setLlamando] = useState(false)
  // Llamada en marcha: su identificador y el instante en que la app pasó a segundo plano (al abrirse el marcador).
  const enMarcha = useRef<{ registroLlamadaId: number; salida: number | null } | null>(null)
  const referencia = useRef(pasajero)
  referencia.current = pasajero

  useEffect(() => {
    const elemento = dialogo.current
    if (!elemento) return
    if (vista !== 'cerrado' && !elemento.open) elemento.showModal()
    if (vista === 'cerrado' && elemento.open) elemento.close()
  }, [vista])

  useEffect(() => {
    function alCambiarVisibilidad() {
      const llamada = enMarcha.current
      if (!llamada || !token) return
      if (document.visibilityState === 'hidden') {
        llamada.salida ??= Date.now()
        return
      }
      if (llamada.salida === null) return
      const segundos = Math.round((Date.now() - llamada.salida) / 1000)
      enMarcha.current = null
      // Si falla, la llamada queda registrada igual, solo que sin duración.
      registrarDuracionLlamada(referencia.current, llamada.registroLlamadaId, segundos, token).catch(() => {})
    }
    document.addEventListener('visibilitychange', alCambiarVisibilidad)
    return () => document.removeEventListener('visibilitychange', alCambiarVisibilidad)
  }, [token])

  async function alLlamar() {
    if (!token || llamando) return
    setLlamando(true)
    const posicion = await ubicacionRapida()
    const registro = await conLimite(
      registrarLlamada(pasajero, posicion?.coords.latitude ?? null, posicion?.coords.longitude ?? null, token),
      ESPERA_MAXIMA_MS,
    )
    enMarcha.current = registro ? { registroLlamadaId: registro.registroLlamadaId, salida: null } : null
    setLlamando(false)
    setVista('cerrado')
    window.location.href = `tel:${telefono}`
  }

  return (
    <>
      <button type="button" className={className} onClick={() => setVista('menu')}>
        Llamar
      </button>
      <dialog ref={dialogo} className="boton-llamar" onCancel={() => setVista('cerrado')} onClose={() => setVista('cerrado')}>
        <h2>{nombre}</h2>
        {vista === 'historial' ? (
          <HistorialLlamadas pasajero={pasajero} />
        ) : (
          <div className="boton-llamar__opciones">
            <button type="button" className="boton-llamar__opcion boton-llamar__opcion--principal" disabled={llamando || !telefono} onClick={alLlamar}>
              📞 {llamando ? 'Abriendo el teléfono…' : telefono ? `Llamar al ${telefono}` : 'Sin teléfono registrado'}
            </button>
            <button type="button" className="boton-llamar__opcion" onClick={() => setVista('historial')}>
              🕘 Ver historial de llamadas
            </button>
          </div>
        )}
        <button type="button" className="boton-llamar__cerrar" onClick={() => setVista(vista === 'historial' ? 'menu' : 'cerrado')}>
          {vista === 'historial' ? '← Volver' : 'Cancelar'}
        </button>
      </dialog>
    </>
  )
}
