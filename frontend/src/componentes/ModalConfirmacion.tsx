import { useEffect, useRef } from 'react'
import { BotonPrimario } from './BotonPrimario'
import { BotonSecundario } from './BotonSecundario'
import './ModalConfirmacion.css'

interface PropiedadesModalConfirmacion {
  abierto: boolean
  titulo: string
  mensaje: string
  textoConfirmar?: string
  alConfirmar: () => void
  alCancelar: () => void
}

/**
 * Diálogo de confirmación para operaciones relevantes (por ejemplo,
 * publicar una jornada). Usa el elemento nativo `<dialog>`, que ya gestiona
 * el foco y la tecla Escape.
 */
export function ModalConfirmacion({ abierto, titulo, mensaje, textoConfirmar = 'Confirmar', alConfirmar, alCancelar }: PropiedadesModalConfirmacion) {
  const referencia = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialogo = referencia.current
    if (!dialogo) return
    if (abierto && !dialogo.open) dialogo.showModal()
    if (!abierto && dialogo.open) dialogo.close()
  }, [abierto])

  return (
    <dialog ref={referencia} className="modal-confirmacion" onCancel={alCancelar}>
      <h2>{titulo}</h2>
      <p>{mensaje}</p>
      <div className="modal-confirmacion__acciones">
        <BotonSecundario onClick={alCancelar}>Cancelar</BotonSecundario>
        <BotonPrimario type="button" onClick={alConfirmar}>
          {textoConfirmar}
        </BotonPrimario>
      </div>
    </dialog>
  )
}
