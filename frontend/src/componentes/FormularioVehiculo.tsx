import { useState, type FormEvent } from 'react'
import type { CrearVehiculoDatos } from '../modelos/conductor'
import { BotonPrimario } from './BotonPrimario'
import { CampoFormulario } from './CampoFormulario'
import { MensajeAlerta } from './MensajeAlerta'
import './FormularioVehiculo.css'

interface PropiedadesFormularioVehiculo {
  textoBoton: string
  textoEnviando?: string
  /** Guarda el vehículo; si lanza un error, su mensaje se muestra bajo el formulario. */
  alGuardar: (datos: CrearVehiculoDatos) => Promise<void>
  /** Datos con los que se abre el formulario (para editar un vehículo existente); vacío para un alta. */
  inicial?: { placa: string; marca: string; modelo: string; capacidad: number; vigenciaSoat: string; vigenciaTecnomecanica: string }
  /** Prefijo de los ids de los campos, para poder mostrar varios formularios en la misma pantalla. */
  idBase?: string
}

/**
 * Datos de un vehículo: placa, marca, modelo, capacidad y vigencias del SOAT
 * y de la revisión técnico-mecánica. Se usa al asignar a alguien como
 * conductor (para formar su unidad operativa) y al agregar otro vehículo.
 * Las validaciones de fondo las hace el backend.
 */
export function FormularioVehiculo({ textoBoton, textoEnviando = 'Guardando…', alGuardar, inicial, idBase = 'vehiculo' }: PropiedadesFormularioVehiculo) {
  const [placa, setPlaca] = useState(inicial?.placa ?? '')
  const [marca, setMarca] = useState(inicial?.marca ?? '')
  const [modelo, setModelo] = useState(inicial?.modelo ?? '')
  const [capacidad, setCapacidad] = useState(inicial ? String(inicial.capacidad) : '')
  const [vigenciaSoat, setVigenciaSoat] = useState(inicial?.vigenciaSoat ?? '')
  const [vigenciaTecnomecanica, setVigenciaTecnomecanica] = useState(inicial?.vigenciaTecnomecanica ?? '')
  const [enviando, setEnviando] = useState(false)
  const [mensajeError, setMensajeError] = useState<string | null>(null)

  async function alEnviar(evento: FormEvent) {
    evento.preventDefault()
    setEnviando(true)
    setMensajeError(null)
    try {
      await alGuardar({ placa: placa.trim().toUpperCase(), marca, modelo, capacidad: Number(capacidad), vigenciaSoat, vigenciaTecnomecanica })
    } catch (error) {
      setMensajeError(error instanceof Error ? error.message : 'No se pudo guardar el vehículo.')
    } finally {
      setEnviando(false)
    }
  }

  return (
    <form className="formulario-vehiculo" onSubmit={alEnviar}>
      <CampoFormulario id={`${idBase}Placa`} etiqueta="Placa (matrícula)" valor={placa} alCambiar={setPlaca} requerido />
      <CampoFormulario id={`${idBase}Marca`} etiqueta="Marca" valor={marca} alCambiar={setMarca} requerido />
      <CampoFormulario id={`${idBase}Modelo`} etiqueta="Modelo" valor={modelo} alCambiar={setModelo} requerido />
      <CampoFormulario id={`${idBase}Capacidad`} etiqueta="Capacidad de pasajeros" tipo="number" valor={capacidad} alCambiar={setCapacidad} requerido />
      <CampoFormulario id={`${idBase}Soat`} etiqueta="Vigencia del SOAT" tipo="date" valor={vigenciaSoat} alCambiar={setVigenciaSoat} requerido />
      <CampoFormulario id={`${idBase}Tecnomecanica`} etiqueta="Vigencia de la técnico-mecánica" tipo="date" valor={vigenciaTecnomecanica} alCambiar={setVigenciaTecnomecanica} requerido />
      {mensajeError && <MensajeAlerta tipo="error">{mensajeError}</MensajeAlerta>}
      <BotonPrimario disabled={enviando}>{enviando ? textoEnviando : textoBoton}</BotonPrimario>
    </form>
  )
}
