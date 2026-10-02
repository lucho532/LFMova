import { useState } from 'react'
import { APLICACIONES_NAVEGACION, guardarAppPredeterminada, obtenerAppPredeterminada, type AppNavegacion } from '../servicios/navegacion'
import { SelectorFormulario } from './SelectorFormulario'

/**
 * Permite al conductor cambiar la aplicación con la que navega en este
 * dispositivo, o volver a que se le pregunte cada vez. Es una preferencia
 * local del dispositivo: no se guarda en la cuenta.
 */
export function PreferenciaNavegacion() {
  const [app, setApp] = useState<AppNavegacion | null>(obtenerAppPredeterminada)

  function alCambiar(valor: string) {
    const nueva = APLICACIONES_NAVEGACION.find((a) => a.valor === valor)?.valor ?? null
    guardarAppPredeterminada(nueva)
    setApp(nueva)
  }

  return (
    <>
      <h2>Navegación</h2>
      <SelectorFormulario
        id="appNavegacion"
        etiqueta="Aplicación para navegar en este dispositivo"
        valor={app ?? ''}
        opciones={APLICACIONES_NAVEGACION.map((a) => ({ valor: a.valor, texto: a.nombre }))}
        alCambiar={alCambiar}
        textoVacio="Preguntarme cada vez"
      />
    </>
  )
}
