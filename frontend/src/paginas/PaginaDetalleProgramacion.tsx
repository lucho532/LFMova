import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { ContenedorPagina } from '../componentes/ContenedorPagina'
import { EncabezadoPagina } from '../componentes/EncabezadoPagina'
import { FormularioProgramacion } from '../componentes/FormularioProgramacion'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { Programacion } from '../modelos/operacion'
import type { Sede } from '../modelos/sede'
import { ErrorApi } from '../servicios/clienteHttp'
import { actualizarProgramacion, obtenerProgramacion } from '../servicios/servicioOperacion'
import { obtenerSedes } from '../servicios/servicioSedes'

/** Edición de una programación existente. El empleado no se puede cambiar. */
export function PaginaDetalleProgramacion() {
  const { empresaId, programacionId } = useParams<{ empresaId: string; programacionId: string }>()
  const { token } = useAutenticacion()
  const navegar = useNavigate()
  const [programacion, setProgramacion] = useState<Programacion | null>(null)
  const [sedes, setSedes] = useState<Sede[]>([])
  const [mensajeError, setMensajeError] = useState<string | null>(null)

  useEffect(() => {
    if (!token || !empresaId || !programacionId) return
    Promise.all([obtenerProgramacion(Number(empresaId), Number(programacionId), token), obtenerSedes(Number(empresaId), token)])
      .then(([p, s]) => {
        setProgramacion(p)
        setSedes(s)
      })
      .catch((error) => setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudo cargar la programación.'))
  }, [empresaId, programacionId, token])

  return (
    <ContenedorPagina ancho="estrecho" volverA={`/empresas/${empresaId}/programaciones`} textoVolver="← Volver a programaciones">
      <EncabezadoPagina titulo="Programación" />
      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      {programacion && (
        <FormularioProgramacion
          sedes={sedes}
          inicial={programacion}
          textoBoton="Guardar cambios"
          alGuardar={async (datos) => {
            try {
              await actualizarProgramacion(Number(empresaId), programacion.programacionTransporteId, datos, token!)
              navegar(`/empresas/${empresaId}/programaciones`)
            } catch (error) {
              throw new Error(error instanceof ErrorApi ? error.message : 'No se pudieron guardar los cambios.')
            }
          }}
        />
      )}
    </ContenedorPagina>
  )
}
