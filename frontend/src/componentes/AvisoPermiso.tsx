import { useState } from 'react'
import { abrirConfiguracion, puedeAbrirConfiguracion, type ErrorPermiso } from '../servicios/ubicacion'
import '../estilos/componentes/AvisoPermiso.css'

interface PropiedadesAvisoPermiso {
  /** Permiso que faltó; si es `null` no se muestra nada. */
  error: ErrorPermiso | null
  /** Vuelve a intentar la acción que se bloqueó (por ejemplo, tras conceder el permiso en la configuración). */
  alReintentar?: () => void
}

/**
 * Aviso de que falta un permiso o servicio del dispositivo (ubicación, GPS apagado, cámara), con el botón
 * que abre directamente la configuración del teléfono para cambiarlo. En el navegador, donde no se puede
 * abrir la configuración, explica dónde cambiar el permiso. No pide permisos ni ejecuta la acción bloqueada.
 */
export function AvisoPermiso({ error, alReintentar }: PropiedadesAvisoPermiso) {
  const [fallo, setFallo] = useState(false)
  if (!error) return null

  async function alAbrirConfiguracion() {
    setFallo(false)
    try {
      await abrirConfiguracion(error!.motivo)
    } catch {
      setFallo(true)
    }
  }

  return (
    <div className="aviso-permiso" role="alert">
      <p className="aviso-permiso__mensaje">{error.message}</p>
      {!puedeAbrirConfiguracion() && (
        <p className="aviso-permiso__ayuda">En el navegador, toca el candado junto a la dirección de la página y permite el acceso.</p>
      )}
      {fallo && <p className="aviso-permiso__ayuda">No se pudo abrir la configuración. Ábrela manualmente desde los ajustes del teléfono.</p>}
      <div className="aviso-permiso__acciones">
        {puedeAbrirConfiguracion() && (
          <button type="button" className="aviso-permiso__boton aviso-permiso__boton--principal" onClick={alAbrirConfiguracion}>
            ⚙ Abrir configuración
          </button>
        )}
        {alReintentar && (
          <button type="button" className="aviso-permiso__boton" onClick={alReintentar}>
            Reintentar
          </button>
        )}
      </div>
    </div>
  )
}
