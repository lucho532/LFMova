import { useState, type FormEvent } from 'react'
import type { Empleado } from '../modelos/empleado'
import { NOMBRES_TIPO_SERVICIO } from '../modelos/enumeraciones'
import type { DatosProgramacion, Programacion } from '../modelos/operacion'
import type { Sede } from '../modelos/sede'
import { BotonPrimario } from './BotonPrimario'
import { CampoFormulario } from './CampoFormulario'
import { MensajeAlerta } from './MensajeAlerta'
import { SelectorFormulario } from './SelectorFormulario'
import { TarjetaFormulario } from './TarjetaFormulario'

interface PropiedadesFormularioProgramacion {
  sedes: Sede[]
  /** Solo al crear: lista de empleados entre los que elegir. Al editar no se puede cambiar el empleado. */
  empleados?: Empleado[]
  inicial?: Programacion
  textoBoton: string
  alGuardar: (datos: DatosProgramacion, empleadoId: number | null) => Promise<void>
}

/**
 * Formulario de una programación de transporte, compartido por las pantallas
 * de alta y de edición. Las validaciones de fondo las hace el backend.
 */
export function FormularioProgramacion({ sedes, empleados, inicial, textoBoton, alGuardar }: PropiedadesFormularioProgramacion) {
  const [empleadoId, setEmpleadoId] = useState('')
  const [sedeId, setSedeId] = useState(inicial ? String(inicial.sedeId) : '')
  const [fecha, setFecha] = useState(inicial?.fecha ?? '')
  const [hora, setHora] = useState(inicial ? inicial.hora.slice(0, 5) : '')
  const [tipo, setTipo] = useState(inicial ? String(inicial.tipo) : '')
  const [direccion, setDireccion] = useState(inicial?.direccionRecogida ?? '')
  const [barrio, setBarrio] = useState(inicial?.barrioRecogida ?? '')
  const [enviando, setEnviando] = useState(false)
  const [mensajeError, setMensajeError] = useState<string | null>(null)

  async function alEnviar(evento: FormEvent) {
    evento.preventDefault()
    setEnviando(true)
    setMensajeError(null)
    try {
      await alGuardar(
        { sedeId: Number(sedeId), fecha, hora, tipo: Number(tipo), direccionRecogida: direccion, barrioRecogida: barrio },
        empleados ? Number(empleadoId) : null,
      )
    } catch (error) {
      setMensajeError(error instanceof Error ? error.message : 'No se pudo guardar la programación.')
    } finally {
      setEnviando(false)
    }
  }

  return (
    <TarjetaFormulario alEnviar={alEnviar} columnas={2}>
      {empleados && (
        <SelectorFormulario
          id="empleadoProgramacion"
          etiqueta="Empleado"
          valor={empleadoId}
          alCambiar={setEmpleadoId}
          opciones={empleados.filter((e) => e.activo).map((e) => ({ valor: String(e.empleadoId), texto: `${e.nombreCompleto} (${e.cedula})` }))}
          requerido
        />
      )}
      <SelectorFormulario
        id="sedeProgramacion"
        etiqueta="Sede"
        valor={sedeId}
        alCambiar={setSedeId}
        opciones={sedes.filter((s) => s.activa).map((s) => ({ valor: String(s.sedeId), texto: s.nombre }))}
        requerido
      />
      <CampoFormulario id="fechaProgramacion" etiqueta="Fecha" tipo="date" valor={fecha} alCambiar={setFecha} requerido />
      <CampoFormulario id="horaProgramacion" etiqueta="Hora" tipo="time" valor={hora} alCambiar={setHora} requerido />
      <SelectorFormulario
        id="tipoProgramacion"
        etiqueta="Tipo"
        valor={tipo}
        alCambiar={setTipo}
        opciones={Object.entries(NOMBRES_TIPO_SERVICIO).map(([valor, texto]) => ({ valor, texto }))}
        requerido
      />
      <CampoFormulario id="direccionProgramacion" etiqueta="Dirección de recogida" valor={direccion} alCambiar={setDireccion} requerido />
      <CampoFormulario id="barrioProgramacion" etiqueta="Barrio de recogida" valor={barrio} alCambiar={setBarrio} requerido />
      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      <BotonPrimario disabled={enviando}>{enviando ? 'Guardando…' : textoBoton}</BotonPrimario>
    </TarjetaFormulario>
  )
}
