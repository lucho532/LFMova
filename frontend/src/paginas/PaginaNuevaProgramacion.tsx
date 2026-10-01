import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { ContenedorPagina } from '../componentes/ContenedorPagina'
import { EncabezadoPagina } from '../componentes/EncabezadoPagina'
import { FormularioProgramacion } from '../componentes/FormularioProgramacion'
import { MensajeAlerta } from '../componentes/MensajeAlerta'
import { useAutenticacion } from '../contexto/useAutenticacion'
import type { Empleado } from '../modelos/empleado'
import type { Sede } from '../modelos/sede'
import { ErrorApi } from '../servicios/clienteHttp'
import { obtenerEmpleados } from '../servicios/servicioEmpleados'
import { crearProgramacion } from '../servicios/servicioOperacion'
import { obtenerSedes } from '../servicios/servicioSedes'

/** Alta de una programación de transporte para un empleado de la empresa. */
export function PaginaNuevaProgramacion() {
  const { empresaId } = useParams<{ empresaId: string }>()
  const { token } = useAutenticacion()
  const navegar = useNavigate()
  const [empleados, setEmpleados] = useState<Empleado[] | null>(null)
  const [sedes, setSedes] = useState<Sede[]>([])
  const [mensajeError, setMensajeError] = useState<string | null>(null)

  useEffect(() => {
    if (!token || !empresaId) return
    Promise.all([obtenerEmpleados(Number(empresaId), token), obtenerSedes(Number(empresaId), token)])
      .then(([e, s]) => {
        setEmpleados(e)
        setSedes(s)
      })
      .catch((error) => setMensajeError(error instanceof ErrorApi ? error.message : 'No se pudieron cargar los datos.'))
  }, [empresaId, token])

  return (
    <ContenedorPagina ancho="estrecho" volverA={`/empresas/${empresaId}/programaciones`} textoVolver="← Volver a programaciones">
      <EncabezadoPagina titulo="Nueva programación" />
      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      {empleados && (
        <FormularioProgramacion
          sedes={sedes}
          empleados={empleados}
          textoBoton="Crear programación"
          alGuardar={async (datos, empleadoId) => {
            try {
              await crearProgramacion(Number(empresaId), { ...datos, empleadoId: empleadoId ?? 0 }, token!)
              navegar(`/empresas/${empresaId}/programaciones`)
            } catch (error) {
              throw new Error(error instanceof ErrorApi ? error.message : 'No se pudo crear la programación.')
            }
          }}
        />
      )}
    </ContenedorPagina>
  )
}
